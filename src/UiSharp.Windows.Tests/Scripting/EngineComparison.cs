namespace UiSharp.Windows.Tests.Scripting;

internal static class EngineComparison
{
    public static readonly IReadOnlyDictionary<string, string> NoVars =
        new Dictionary<string, string>();

    /// <summary>
    /// Runs a delegate on a dedicated STA thread. UiSharp.exe's entry point is
    /// [STAThread], and the IActiveScript host is happiest there, whereas the
    /// xunit thread pool is MTA — so tests must reproduce the real apartment
    /// rather than rely on whatever the runner provides.
    /// </summary>
    public static T OnStaThread<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { result = work(); }
            catch (Exception ex) { failure = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null) throw failure;
        return result;
    }
}
