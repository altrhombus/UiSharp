using System.Reflection;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Diagnostics;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;
using UiSharp.Windows.Ldap;
using UiSharp.Windows.Scripting;
using UiSharp.Windows.Variables;

namespace UiSharp;

internal static class Program
{
    private const int ExitSuccess    = 0;
    private const int ExitCancel     = 1;
    private const int ExitBadConfig  = 2;
    private const int ExitFatal      = 3;
    private const int ExitSelfTestFailed = 4;

    // Set once the log is open so the crash handlers can use it. They are
    // installed before the log exists, because opening it is itself something
    // that can fail.
    private static ICMLog _log = NullLog.Instance;

    [STAThread]
    private static int Main(string[] args)
    {
        InstallCrashHandlers();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var opts = ParseArgs(args);

        // Env + log. LogDirectory is a DIRECTORY (_SMSTSLogPath in a task
        // sequence); TryOpen appends the file name and falls back to the temp
        // directory, never throwing — a deployment must not die because its log
        // could not be opened.
        var env = new ConfigMgrTSEnv();
        _log = CMTraceLog.TryOpen(env.LogDirectory, out var logFailure);
        var log = _log;

        using var disposableLog = log as IDisposable;

        log.Write($"{LogFile.DefaultComponent} starting. Config: {opts.ConfigPath}");

        if (logFailure is not null)
            log.Write($"Log fallback in effect: {logFailure}", LogSeverity.Warning);

        // /selftest runs the diagnostics instead of a configuration. It exists
        // because the task-sequence path cannot be reached from a unit test:
        // every serious fault found so far -- the log directory, enumerating
        // variables, starting up at all -- only appears once this is running
        // where it is meant to run.
        if (opts.SelfTest)
            return RunSelfTest(opts, env, log);

        // Load config
        LoadedConfig config;
        try
        {
            config = LoadConfig(opts, log, env);
        }
        catch (Exception ex)
        {
            log.Write($"Failed to load config: {ex.Message}", LogSeverity.Error);
            return ExitBadConfig;
        }

        // Apply global defaults from config into env
        var provider = new UiSharp.Windows.Variables.WindowsDefaultValueProvider();
        var defaultAction = new UiSharp.Core.Actions.ActionData
        {
            ActionNode           = config.Document.Root!,
            Conditions           = new NativeConditionEvaluator(),
            TsEnv                = env,
            Log                  = log,
            GlobalDialogTraits   = config.GlobalTraits,
            InTS                 = env.InTS,
            Software             = config.Software,
            Messages             = config.Messages,
            DefaultValueProvider = provider,
            Ldap                 = new WindowsLdap(),
        };

        // Respect /disabletsvareditor
        if (opts.DisableTsVarEditor)
            config.GlobalTraits.Flags &= ~DialogTraitFlags.AllowVarEditor;

        // Register all action types from all assemblies
        var factory = BuildActionFactory();

        var actionsEl = config.Document.Root?.Element(XmlConstants.Elements.Actions);
        if (actionsEl is null)
        {
            log.Write("No <Actions> element found in config.", LogSeverity.Warning);
            return ExitSuccess;
        }

        // CLI /conditionengine: overrides the XML ConditionEngine attribute.
        // ConditionEngine is a whole-document setting; see ActionProcessor.
        var engineName = opts.ConditionEngine ?? config.ConditionEngine;
        var evaluator  = SelectConditionEngine(engineName, log);

        var processor = new ActionProcessor(factory, evaluator);
        var result    = processor.Run(actionsEl, defaultAction);

        log.Write($"{LogFile.DefaultComponent} finished. Result: {result}");

        return result switch
        {
            ActionResult.Next   => ExitSuccess,
            ActionResult.Cancel => ExitCancel,
            _                   => ExitCancel,
        };
    }

    private static ActionFactory BuildActionFactory()
    {
        var factory = new ActionFactory();
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UiSharp.Core.Actions.ActionBase))!);
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UiSharp.Windows.Actions.ActionRegRead))!);
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UiSharp.UI.Actions.ActionInfo))!);
        return factory;
    }

    // -------------------------------------------------------------------------
    // Self-test
    // -------------------------------------------------------------------------

    private static int RunSelfTest(CliOptions opts, ITSEnv env, ICMLog log)
    {
        log.Write("Running the self-test. No configuration will be processed.");

        var report = SelfTestRunner.Standard().Run(env, log, BuildActionFactory());

        // Every line goes to the log as well as the report file: in a task
        // sequence the log is collected automatically, and a report nobody
        // collects is a report nobody reads.
        foreach (var result in report.Results)
        {
            var severity = result.Outcome == CheckOutcome.Fail
                ? LogSeverity.Error
                : LogSeverity.Info;

            var detail = string.IsNullOrWhiteSpace(result.Detail) ? "" : $" -- {result.Detail}";

            log.Write($"Self-test [{result.Area}] {result.Outcome}: {result.Name}{detail}", severity);
        }

        log.Write($"Self-test finished: {report.Summary}",
            report.AllPassed ? LogSeverity.Info : LogSeverity.Error);

        var path = SelfTestReportPath(opts, log);

        try
        {
            File.WriteAllText(path, report.ToText());
            log.Write($"Self-test report written to {path}");
        }
        catch (Exception ex)
        {
            log.Write($"Could not write the self-test report to {path}: {ex.Message}",
                LogSeverity.Warning);
        }

        return report.AllPassed ? ExitSuccess : ExitSelfTestFailed;
    }

    /// <summary>
    /// Where the report goes. Beside the log by default, so whatever collects
    /// SMSTS logs picks it up without anyone configuring a second location.
    /// </summary>
    private static string SelfTestReportPath(CliOptions opts, ICMLog log)
    {
        const string fileName = "UiSharp_selftest.txt";

        if (opts.SelfTestReport is { Length: > 0 } requested)
        {
            // A directory is as reasonable a thing to pass as a file, and
            // treating one as the other is the exact bug that used to stop the
            // runtime dead at startup.
            return Directory.Exists(requested)
                ? Path.Combine(requested, fileName)
                : requested;
        }

        if (log.FilePath is { Length: > 0 } logFile &&
            Path.GetDirectoryName(logFile) is { Length: > 0 } logDir)
        {
            return Path.Combine(logDir, fileName);
        }

        return Path.Combine(Path.GetTempPath(), fileName);
    }

    // -------------------------------------------------------------------------
    // Crash reporting
    //
    // Without this, an unhandled exception during a deployment leaves nothing
    // behind at all: no dialog, no log line, just a non-zero exit. Anything
    // that reaches here is written to the log and to a crash file, and the
    // process exits with a distinct code rather than a runtime fault.
    //
    // No message box: a modal dialog in an unattended task sequence would hang
    // the deployment until someone walked over to the machine.
    // -------------------------------------------------------------------------

    private static void InstallCrashHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportFatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");

        Application.ThreadException += (_, e) =>
            ReportFatal(e.Exception, "Application.ThreadException");

        // Route WinForms UI-thread exceptions to ThreadException rather than
        // letting the default dialog appear.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    }

    private static void ReportFatal(Exception? ex, string source)
    {
        var detail = ex?.ToString() ?? "(no exception object)";
        var report =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {LogFile.DefaultComponent} fatal error " +
            $"via {source}{Environment.NewLine}{detail}{Environment.NewLine}";

        try { _log.Write($"Fatal error via {source}: {detail}", LogSeverity.Error); }
        catch { /* the log may be the thing that failed */ }

        // A separate file as well, because the log may never have opened and
        // because a crash file is easier to spot than one line among thousands.
        foreach (var dir in CrashFileDirectories())
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(dir, $"{LogFile.DefaultComponent}_crash.txt"), report);
                break;
            }
            catch { /* try the next location */ }
        }

        Environment.Exit(ExitFatal);
    }

    private static IEnumerable<string> CrashFileDirectories()
    {
        // Alongside the log first, so the crash file travels with it when
        // SaveItems or the deployment collects logs.
        if (_log.FilePath is { Length: > 0 } logFile)
        {
            var dir = Path.GetDirectoryName(logFile);
            if (!string.IsNullOrEmpty(dir)) yield return dir;
        }

        yield return Path.GetTempPath();
        yield return AppContext.BaseDirectory;
    }

    // -------------------------------------------------------------------------
    // Condition engine selection
    //
    // The native engine is the default and needs no WinPE component. VBScript
    // remains available for the handful of constructs the native engine cannot
    // evaluate -- GetObject, Eval, Execute, Split -- but it is a legacy path:
    // Microsoft is deprecating VBScript, so every use is announced in the log
    // with the native alternative, and asking for an engine that is not
    // installed is an error rather than a silent downgrade.
    // -------------------------------------------------------------------------

    private static IConditionEvaluator SelectConditionEngine(string engineName, ICMLog log)
    {
        var wantsVbScript = engineName.Equals(
            XmlConstants.Values.ConditionEngineVbscript, StringComparison.OrdinalIgnoreCase);

        if (!wantsVbScript)
            return new NativeConditionEvaluator();

        if (!VBScriptConditionEvaluator.IsAvailable)
        {
            // Falling back quietly would evaluate every COM condition as false,
            // which reads as a passing deployment making wrong choices.
            log.Write(
                "ConditionEngine=\"vbscript\" was requested but the VBScript engine is " +
                "not registered on this system. Add the WinPE-Scripting component to the " +
                "boot image, or use the native engine. Conditions needing a script host " +
                "will now evaluate as false and be reported individually.",
                LogSeverity.Error);

            return new NativeConditionEvaluator();
        }

        log.Write(
            "Using the VBScript condition engine. This is a legacy path: it requires the " +
            "WinPE-Scripting component, and Microsoft is deprecating VBScript. The native " +
            "engine handles everything except GetObject, Eval, Execute and Split - prefer " +
            "<Action Type=\"WMIRead\"> and <Action Type=\"RegRead\"> over CreateObject for " +
            "WMI and the registry.",
            LogSeverity.Warning);

        return new VBScriptConditionEvaluator();
    }

    private static LoadedConfig LoadConfig(CliOptions opts, ICMLog log, ITSEnv env)
    {
        if (IsHttpUrl(opts.ConfigPath))
        {
            // Run async HTTP fetch on thread pool — safe to block here since no message loop
            // is running yet and there is no SynchronizationContext to deadlock against.
            return Task.Run(() => ConfigLoader.LoadAsync(
                opts.ConfigPath,
                opts.ConfigFallback,
                opts.ConfigRetry,
                CancellationToken.None,
                env)).GetAwaiter().GetResult();
        }

        return ConfigLoader.Load(opts.ConfigPath, env);
    }

    private static bool IsHttpUrl(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // -------------------------------------------------------------------------
    // CLI args

    private sealed record CliOptions(
        string ConfigPath,
        string? ConfigFallback,
        int ConfigRetry,
        bool DisableTsVarEditor,
        string? ConditionEngine,
        bool SelfTest,
        string? SelfTestReport);

    private static CliOptions ParseArgs(string[] args)
    {
        string  configPath         = XmlConstants.DefaultConfigFilename;
        string? configFallback     = null;
        int     configRetry        = 3;
        bool    disableTsVarEditor = false;
        string? conditionEngine    = null;
        bool    selfTest           = false;
        string? selfTestReport     = null;

        foreach (var arg in args)
        {
            if (TrySwitch(arg, "/config:",           out var v)) { configPath      = v; continue; }
            if (TrySwitch(arg, "/configfallback:",    out v))    { configFallback  = v; continue; }
            if (TrySwitch(arg, "/conditionengine:",   out v))    { conditionEngine = v; continue; }
            if (TrySwitch(arg, "/configretry:",       out v))
            {
                if (int.TryParse(v, out var n)) configRetry = n;
                continue;
            }
            if (TrySwitch(arg, "/selftestreport:",     out v))
            {
                selfTestReport = v;
                selfTest       = true;
                continue;
            }
            if (arg.Equals("/selftest", StringComparison.OrdinalIgnoreCase))
            {
                selfTest = true;
                continue;
            }
            if (arg.Equals("/disabletsvareditor", StringComparison.OrdinalIgnoreCase))
            {
                disableTsVarEditor = true;
                continue;
            }
            // Positional arg — treat as config path.
            if (!arg.StartsWith('/') && !arg.StartsWith('-'))
                configPath = arg;
        }

        return new CliOptions(configPath, configFallback, configRetry, disableTsVarEditor,
            conditionEngine, selfTest, selfTestReport);
    }

    private static bool TrySwitch(string arg, string prefix, out string value)
    {
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = arg[prefix.Length..];
            return true;
        }
        value = string.Empty;
        return false;
    }
}
