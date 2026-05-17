using System.Reflection;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Windows.Ldap;
using UIpp.Windows.Variables;

namespace UIpp;

internal static class Program
{
    private const int ExitSuccess    = 0;
    private const int ExitCancel     = 1;
    private const int ExitBadConfig  = 2;

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var opts = ParseArgs(args);

        // Env + log
        var env = new ConfigMgrTSEnv();
        var logPath = env.LogPath ?? Path.Combine(Path.GetTempPath(), "UIpp.log");
        using var log = new CMTraceLog(logPath);

        log.Write($"UIpp starting. Config: {opts.ConfigPath}");

        // Load config
        LoadedConfig config;
        try
        {
            config = LoadConfig(opts, log);
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

        // CLI /conditionengine: overrides the XML ConditionEngine attribute; VBScript not implemented.
        var engineName = opts.ConditionEngine ?? config.ConditionEngine;
        if (engineName.Equals(XmlConstants.Values.ConditionEngineVbscript, StringComparison.OrdinalIgnoreCase))
            log.Write("VBScript condition engine is not yet implemented; using native evaluator.", LogSeverity.Warning);

        var processor = new ActionProcessor(factory, new NativeConditionEvaluator());
        var result    = processor.Run(actionsEl, defaultAction);

        log.Write($"UIpp finished. Result: {result}");

        return result switch
        {
            ActionResult.Next   => ExitSuccess,
            ActionResult.Cancel => ExitCancel,
            _                   => ExitCancel,
        };
    }

    private static LoadedConfig LoadConfig(CliOptions opts, ICMLog log)
    {
        if (IsHttpUrl(opts.ConfigPath))
        {
            // Run async HTTP fetch on thread pool — safe to block here since no message loop
            // is running yet and there is no SynchronizationContext to deadlock against.
            return Task.Run(() => ConfigLoader.LoadAsync(
                opts.ConfigPath,
                opts.ConfigFallback,
                opts.ConfigRetry,
                CancellationToken.None)).GetAwaiter().GetResult();
        }

        return ConfigLoader.Load(opts.ConfigPath);
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
