namespace AclLab.Steps;

static class VerifyFolders
{
    public static void Run()
    {
        Report.Step(16, "Check list/create/delete on every folder for every identity");
        var folders = new[]
        {
            Config.Pzs,
            $"{Config.Pzs}/pzs11", $"{Config.Pzs}/pzs12", $"{Config.Pzs}/pzs13",
            $"{Config.Pzs}/pzs14", $"{Config.Pzs}/pzs15"
        };

        foreach (var dir in folders)
        foreach (var user in Config.AllIdentities)
        {
            Report.Check(user, "ListDirectory", dir, $"ls '{dir}' >/dev/null");

            var createProbe = $"{dir}/.acl_probe_c_{Guid.NewGuid():N}";
            Report.Check(user, "CreateEntry", dir, $"touch '{createProbe}' && rm -f '{createProbe}'");

            // Deletion depends on the directory, not the file's owner -- so we create a disposable
            // probe file ourselves (as root) and let the target user try to delete THAT.
            var deleteProbe = $"{dir}/.acl_probe_d_{Guid.NewGuid():N}";
            File.WriteAllText(deleteProbe, "");
            Report.Check(user, "DeleteEntry", dir, $"rm -f '{deleteProbe}'");
            if (File.Exists(deleteProbe)) File.Delete(deleteProbe);
        }
    }
}
