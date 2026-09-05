namespace OpenfireLab.Steps;

static class Configure
{
    public static void Run()
    {
        Report.Step(5, "Provision Openfire unattended (autosetup) with the admin and user accounts");
        var configPath = WriteTempFile("openfire.xml.template", "openfire.xml");
        Docker.Cp("Copy the autosetup configuration into the container", configPath, "/etc/openfire/openfire.xml");
        Docker.Exec("Fix ownership of the configuration file", "chown", "openfire:openfire", "/etc/openfire/openfire.xml");
        Docker.Exec("Fix permissions of the configuration file", "chmod", "640", "/etc/openfire/openfire.xml");

        Report.Step(6, "Start Openfire and wait for the autosetup run to complete");
        Docker.Exec("Start the Openfire service", "/etc/init.d/openfire", "start");
        WaitAdminConsoleReady();

        Report.Step(7, $"Create the '{Config.RoomName}' room and grant '{Config.AppUser}' member rights in it");
        var setupScriptPath = WriteTempFile("muc_setup.py.template", "muc_setup.py");
        Docker.Cp("Copy the room setup script into the container", setupScriptPath, "/tmp/muc_setup.py");
        Docker.Exec("Configure the chat room's access control", "/opt/xmppenv/bin/python3", "/tmp/muc_setup.py");
    }

    static void WaitAdminConsoleReady()
    {
        var url = $"http://localhost:{Config.AdminConsolePort}/";
        for (var i = 0; i < 30; i++)
        {
            var (ok, stdOut, _) = Report.RunCapture("curl", "-s", "-o", "/dev/null", "-w", "%{http_code}", url);
            if (ok && stdOut.Trim() == "200")
            {
                Report.Note("Admin Console is up and autosetup has completed");
                return;
            }
            Thread.Sleep(2000);
        }

        Report.Note("Admin Console did not come up in time", ok: false);
    }

    static string WriteTempFile(string templateName, string outputName)
    {
        var content = Templates.Render(templateName, Templates.DefaultValues());
        var path = Path.Combine(Path.GetTempPath(), $"openfire_lab_{outputName}");
        File.WriteAllText(path, content);
        return path;
    }
}
