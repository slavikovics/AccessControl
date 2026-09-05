using System.Text.RegularExpressions;

namespace OpenfireLab.Steps;

static class Verify
{
    public static void Run()
    {
        Report.Step(8, "Verify who can log into the Admin Console");
        AdminConsoleCheck(Config.AdminUser, Config.AdminUser, Config.AdminPassword);
        AdminConsoleCheck(Config.AppUser, Config.AppUser, Config.AppUserPassword);
        AdminConsoleCheck(Config.AdminUser + " (wrong password)", Config.AdminUser, "not-the-password");

        Report.Step(9, $"Verify who can speak in '{Config.RoomName}' (moderated: only members and the owner get voice)");
        var probeScriptPath = Path.Combine(Path.GetTempPath(), "openfire_lab_muc_probe.py");
        File.WriteAllText(probeScriptPath, Templates.Render("muc_probe.py.template", Templates.DefaultValues()));
        Docker.Cp("Copy the room probe script into the container", probeScriptPath, "/tmp/muc_probe.py");

        MucCheck(Config.AdminUser, Config.AdminUser, Config.AdminPassword, Config.AdminUser);
        MucCheck(Config.AppUser, Config.AppUser, Config.AppUserPassword, Config.AppUser);
        MucCheckAnonymous(Config.GuestNickname);
        MucCheck("attacker (unknown account)", "attacker", "wrong-password", "attacker");
    }

    static void AdminConsoleCheck(string identity, string username, string password)
    {
        var cookieJar = Path.Combine(Path.GetTempPath(), $"openfire_lab_cookies_{Guid.NewGuid():N}.txt");
        var loginUrl = $"http://localhost:{Config.AdminConsolePort}/login.jsp";
        try
        {
            var (_, loginPage, _) = Report.RunCapture("curl", "-s", "-c", cookieJar, "-b", cookieJar, loginUrl);
            var csrfMatch = Regex.Match(loginPage, "name=\"csrf\" value=\"([^\"]*)\"");
            var csrf = csrfMatch.Success ? csrfMatch.Groups[1].Value : "";

            var (_, headerText, _) = Report.RunCapture("curl", "-s", "-D", "-", "-o", "/dev/null",
                "-b", cookieJar, "-c", cookieJar,
                "--data-urlencode", $"csrf={csrf}",
                "--data-urlencode", $"username={username}",
                "--data-urlencode", $"password={password}",
                "--data-urlencode", "login=true",
                loginUrl);

            var allowed = headerText.Contains("Location: /index.jsp", StringComparison.OrdinalIgnoreCase);
            Report.Record(identity, "AdminConsoleLogin", "/index.jsp", allowed);
        }
        finally
        {
            File.Delete(cookieJar);
        }
    }

    static void MucCheck(string identity, string jidUser, string password, string nick)
    {
        var (ok, stdOut) = Docker.ExecCapture("/opt/xmppenv/bin/python3", "/tmp/muc_probe.py",
            $"{jidUser}@{Config.Domain}", password, nick);
        var allowed = ok && stdOut.Contains("RESULT: ALLOWED");
        Report.Record(identity, "SpeakInRoom", Config.RoomJid, allowed);
    }

    static void MucCheckAnonymous(string nick)
    {
        var (ok, stdOut) = Docker.ExecCapture("/opt/xmppenv/bin/python3", "/tmp/muc_probe.py", "--anonymous", nick);
        var allowed = ok && stdOut.Contains("RESULT: ALLOWED");
        Report.Record("guest (anonymous)", "SpeakInRoom", Config.RoomJid, allowed);
    }
}
