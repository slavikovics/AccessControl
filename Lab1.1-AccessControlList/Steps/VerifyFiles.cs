namespace AclLab.Steps;

static class VerifyFiles
{
    public static void Run()
    {
        Report.Step(14, "Check read/write/execute on every file for every identity");
        foreach (var fpath in Config.CreatedFiles)
        foreach (var user in Config.AllIdentities)
        {
            Report.Check(user, "Read", fpath, $"test -r '{fpath}'");
            Report.Check(user, "Write", fpath, $"test -w '{fpath}'");
            Report.Check(user, "Execute", fpath, $"test -x '{fpath}'");
        }
    }
}
