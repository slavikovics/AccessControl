namespace AclLab.Steps;

static class Folders
{
    public static void Run()
    {
        Report.Step(6, "Create folder pzs");
        Report.Execute($"Create directory '{Config.Pzs}'", "mkdir", "-p", Config.Pzs);

        Report.Step(7, "Create folders pzs11..pzs15 (owner/group/other/all/admin, rwx)");
        for (var subject = 1; subject <= 5; subject++)
        {
            var dir = $"{Config.Pzs}/pzs1{subject}";
            Report.Execute($"Create directory '{dir}'", "mkdir", "-p", dir);
            Rules.ApplyRule(dir, subject, 4);
        }
    }
}
