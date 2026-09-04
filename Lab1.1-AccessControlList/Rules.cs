namespace AclLab;

static class Rules
{
    public static void ApplyRule(string path, int subject, int permIndex)
    {
        var octal = Config.PermOctals[permIndex];
        var sym = Config.PermSyms[permIndex];

        Report.Execute($"Reset ACL on '{path}'", "setfacl", "-b", path);

        switch (subject)
        {
            case 1: // owner
                Report.Execute($"Apply access rule (owner, {sym}) on '{path}'", "chmod", $"{octal}00", path);
                break;

            case 2: // named group group_iit1
                Report.Execute($"Clear classic bits on '{path}'", "chmod", "000", path);
                Report.Execute($"Apply access rule (group 'group_iit1', {sym}) on '{path}'",
                    "setfacl", "-m", $"g:group_iit1:{sym}", path);
                break;

            case 3: // other
                Report.Execute($"Apply access rule (other, {sym}) on '{path}'", "chmod", $"00{octal}", path);
                break;

            case 4: // all
                Report.Execute($"Apply access rule (all, {sym}) on '{path}'", "chmod", $"{octal}{octal}{octal}", path);
                break;

            case 5: // admin (root) only
                Report.Execute($"Set owner of '{path}' to root", "chown", "root:root", path);
                Report.Execute($"Apply access rule (administrator only, {sym}) on '{path}'", "chmod", $"{octal}00", path);
                break;
        }
    }

    public static void WriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            Console.WriteLine($"[OK] Create file '{path}'");
            Report.RecordOp(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Create file '{path}'  {ex.Message}");
            Report.RecordOp(false);
        }
    }
}
