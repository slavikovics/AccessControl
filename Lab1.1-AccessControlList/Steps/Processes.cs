using System.Diagnostics;

namespace AclLab.Steps;

static class Processes
{
    public static void Run()
    {
        Report.Step(15, "Run every filex5 script as iit11, then try to stop it as every identity");
        foreach (var fpath in Config.CreatedFiles)
        {
            if (fpath.EndsWith('5'))
            {
                TryRunAndStop(fpath);
            }
        }
    }

    static void TryRunAndStop(string fpath)
    {
        var psi = new ProcessStartInfo("su")
        {
            RedirectStandardInput = true, // left open (never written to/closed) -- keeps the script's `read` blocked
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("-");
        psi.ArgumentList.Add("iit11");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(fpath);

        using var process = Process.Start(psi)!;
        Thread.Sleep(400);

        if (process.HasExited)
        {
            var stdErr = process.StandardError.ReadToEnd();
            var firstLine = stdErr.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            Console.WriteLine($"[FAIL] Start '{fpath}' as 'iit11'  {firstLine}");
            Report.RecordOp(false);
            return;
        }

        Console.WriteLine($"[OK] Start '{fpath}' as 'iit11'");
        Report.RecordOp(true);
        var pid = FindPid("iit11", fpath) ?? process.Id;

        foreach (var user in Config.AllIdentities)
        {
            if (user == "root")
                Report.RunSilent("kill", pid.ToString());
            else
                Report.RunSilent("su", "-", user, "-c", $"kill {pid}");

            Thread.Sleep(200);
            var stillAlive = Report.RunSilent("kill", "-0", pid.ToString());
            var tag = stillAlive ? "DENIED " : "ALLOWED";
            Console.WriteLine($"[{tag}] {user,-6} {"StopProcess",-14} {fpath}");
            if (!stillAlive) break;
        }

        Report.RunSilent("kill", pid.ToString());
        TryKill(process);
    }

    static int? FindPid(string user, string fpath)
    {
        var psi = new ProcessStartInfo("pgrep") { RedirectStandardOutput = true };
        psi.ArgumentList.Add("-u");
        psi.ArgumentList.Add(user);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add(fpath);

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var first = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is not null && int.TryParse(first, out var pid) ? pid : null;
    }

    static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }
}
