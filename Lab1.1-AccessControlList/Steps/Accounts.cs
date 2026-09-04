namespace AclLab.Steps;

static class Accounts
{
    public static void Run()
    {
        Report.Step(1, "Create groups group_iit1, group_iit2");
        foreach (var g in Config.LabGroups)
            Report.Execute($"Create group '{g}'", "groupadd", g);

        Report.Step(2, "Create users iit11, iit12 in group_iit1");
        foreach (var u in new[] { "iit11", "iit12" })
            Report.Execute($"Create user '{u}'", "useradd", "-m", "-G", "group_iit1", u);

        Report.Step(3, "Create users iit21, iit22 in group_iit2");
        foreach (var u in new[] { "iit21", "iit22" })
            Report.Execute($"Create user '{u}'", "useradd", "-m", "-G", "group_iit2", u);

        Report.Step(4, "Grant iit21 administrative privileges");
        Report.Execute("Grant admin privileges to 'iit21' (group 'sudo')", "usermod", "-aG", "sudo", "iit21");

        Report.Step(5, "Create user iit3");
        Report.Execute("Create user 'iit3'", "useradd", "-m", "iit3");
    }
}
