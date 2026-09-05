using System.Diagnostics;

namespace MongoLab;

static class Report
{
    static int _opTotal;
    static int _opFailed;
    static bool _stepStarted;
    static readonly Dictionary<string, int> CheckTotal = new();
    static readonly Dictionary<string, int> CheckAllowed = new();

    public static void Step(int number, string description)
    {
        if (_stepStarted) WaitForContinue();
        _stepStarted = true;

        Console.WriteLine();
        Console.WriteLine($"== Step {number}: {description} ==");
    }

    // Lets the operator inspect real system state between steps; skipped when stdin isn't a
    // TTY (piped/CI runs) so the program doesn't hang waiting for a key that will never come.
    public static void WaitForContinue()
    {
        if (Console.IsInputRedirected) return;
        Console.WriteLine();
        Console.Write("-- press any key to continue to the next step --");
        Console.ReadKey(true);
        Console.WriteLine();
    }

    public static bool Execute(string description, string file, params string[] args)
    {
        _opTotal++;
        var (ok, _, stdErr) = Run(file, args);
        var commandText = $"{file} {string.Join(' ', args)}";

        if (ok)
        {
            Console.WriteLine($"[OK] {description}  $ {commandText}");
        }
        else
        {
            _opFailed++;
            Console.WriteLine($"[FAIL] {description}  $ {commandText}");
            var firstLine = FirstLine(stdErr);
            if (firstLine is not null)
            {
                Console.WriteLine($"       {firstLine}");
            }
        }

        return ok;
    }

    // A synthetic checkpoint (e.g. "service became ready") that isn't itself one external
    // command but should still count towards the operation summary.
    public static void Note(string description, bool ok = true)
    {
        _opTotal++;
        if (!ok) _opFailed++;
        Console.WriteLine($"[{(ok ? "OK" : "FAIL")}] {description}");
    }

    // Runs `file args...` as the given identity's attempt at `label` on `resource`; a zero
    // exit code means the attempt was allowed, anything else means the server denied it.
    public static bool Check(string identity, string label, string resource, string file, params string[] args)
    {
        CheckTotal[label] = CheckTotal.GetValueOrDefault(label) + 1;
        var allowed = RunSilent(file, args);
        if (allowed) CheckAllowed[label] = CheckAllowed.GetValueOrDefault(label) + 1;

        var tag = allowed ? "ALLOWED" : "DENIED ";
        Console.WriteLine($"[{tag}] {identity,-10} {label,-14} {resource}");
        return allowed;
    }

    public static bool RunSilent(string file, params string[] args) => Run(file, args).Ok;

    public static (bool Ok, string StdOut, string StdErr) RunCapture(string file, params string[] args) => Run(file, args);

    public static void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Operations: {_opTotal} total, {_opFailed} failed");

        foreach (var label in CheckTotal.Keys.OrderBy(l => l))
        {
            var total = CheckTotal[label];
            var allowed = CheckAllowed.GetValueOrDefault(label);
            Console.WriteLine($"  {label,-14} allowed {allowed,5} / {total,-5} denied {total - allowed}");
        }
    }

    static (bool Ok, string StdOut, string StdErr) Run(string file, string[] args)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)!;
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode == 0, stdOut, stdErr);
    }

    static string? FirstLine(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
}
