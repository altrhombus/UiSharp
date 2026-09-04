using System.Reflection;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;
using UIpp.Windows.Ldap;
using UIpp.Windows.Scripting;
using UIpp.Windows.Variables;

namespace UIpp;

internal static class Program
{
    private const int ExitSuccess    = 0;
    private const int ExitCancel     = 1;
    private const int ExitBadConfig  = 2;
    private const int ExitFatal      = 3;

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
        var provider = new UIpp.Windows.Variables.WindowsDefaultValueProvider();
        var defaultAction = new UIpp.Core.Actions.ActionData
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
        var factory = new ActionFactory();
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UIpp.Core.Actions.ActionBase))!);
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UIpp.Windows.Actions.ActionRegRead))!);
        factory.RegisterFromAssembly(Assembly.GetAssembly(typeof(UIpp.UI.Actions.ActionInfo))!);

        var actionsEl = config.Document.Root?.Element(XmlConstants.Elements.Actions);
        if (actionsEl is null)
        {
            log.Write("No <Actions> element found in config.", LogSeverity.Warning);
            return ExitSuccess;
        }

        // CLI /conditionengine: overrides the XML ConditionEngine attribute.
        var engineName = opts.ConditionEngine ?? config.ConditionEngine;
        IConditionEvaluator evaluator = engineName.Equals(
            XmlConstants.Values.ConditionEngineVbscript, StringComparison.OrdinalIgnoreCase)
            ? new VBScriptConditionEvaluator()
            : new NativeConditionEvaluator();

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
        string? ConditionEngine);

    private static CliOptions ParseArgs(string[] args)
    {
        string  configPath         = XmlConstants.DefaultConfigFilename;
        string? configFallback     = null;
        int     configRetry        = 3;
        bool    disableTsVarEditor = false;
        string? conditionEngine    = null;

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
            if (arg.Equals("/disabletsvareditor", StringComparison.OrdinalIgnoreCase))
            {
                disableTsVarEditor = true;
                continue;
            }
            // Positional arg — treat as config path.
            if (!arg.StartsWith('/') && !arg.StartsWith('-'))
                configPath = arg;
        }

        return new CliOptions(configPath, configFallback, configRetry, disableTsVarEditor, conditionEngine);
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
