namespace AclLab.Steps;

static class Files
{
    public static void Run()
    {
        Report.Step(12, "Switch current user to iit11");
        Console.WriteLine("[OK] Acting user is now 'iit11'");

        Report.Step(13, "Create the file permission matrix in pzs11..pzs14");
        for (var dirNum = 1; dirNum <= 4; dirNum++)
        {
            var dir = $"{Config.Pzs}/pzs1{dirNum}";
            for (var subject = 1; subject <= 5; subject++)
            {
                for (var perm = 1; perm <= 5; perm++)
                {
                    var fpath = $"{dir}/file{subject}{perm}";
                    var content = perm == 5
                        ? "#!/bin/bash\nread testVariable\n"
                        : "#!/bin/bash\necho \"Hello World\"\n";

                    Rules.WriteFile(fpath, content);
                    Report.Execute($"Set owner of '{fpath}' to 'iit11'", "chown", "iit11", fpath);
                    Rules.ApplyRule(fpath, subject, perm);
                    Config.CreatedFiles.Add(fpath);
                }
            }
        }
    }
}
