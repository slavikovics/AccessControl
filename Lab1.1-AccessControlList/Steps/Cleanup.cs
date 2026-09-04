namespace AclLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(17, "Clean up files, folders, users and groups");

        foreach (var fpath in Config.CreatedFiles)
            Report.Execute($"Delete file '{fpath}'", "rm", "-f", fpath);

        Report.Execute($"Delete directory '{Config.Pzs}'", "rm", "-rf", Config.Pzs);

        foreach (var u in Config.TestUsers)
            Report.Execute($"Delete user '{u}'", "userdel", "-r", u);

        foreach (var g in Config.LabGroups)
            Report.Execute($"Delete group '{g}'", "groupdel", g);
    }
}
