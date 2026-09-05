namespace OpenfireLab.Steps;

static class Install
{
    public static void Run()
    {
        Report.Step(1, "Start a bare Linux container to install Openfire into");
        Report.Execute("Run base container", "docker", "run", "-d", "--name", Config.ContainerName,
            "-p", $"{Config.AdminConsolePort}:9090",
            "-p", $"{Config.SecureAdminConsolePort}:9091",
            "-p", $"{Config.ClientPort}:5222",
            "-p", $"{Config.ServerPort}:5269",
            Config.Image, "sleep", "infinity");

        Report.Step(2, "Install the Java runtime Openfire needs");
        Docker.Exec("Refresh package index", "apt-get", "update", "-qq");
        Docker.Exec("Install default-jre-headless, wget, python3-venv", "apt-get", "install", "-y", "-qq",
            "default-jre-headless", "wget", "python3-venv");

        Report.Step(3, "Download and install the official Openfire package");
        Docker.Exec("Download the Openfire .deb package", "wget", "-q", Config.DebUrl, "-O", "/tmp/openfire.deb");
        Docker.Exec("Install the Openfire package", "dpkg", "-i", "/tmp/openfire.deb");

        Report.Step(4, "Install a small Python XMPP client for the later protocol-level checks");
        Docker.Exec("Create a virtualenv for test tooling", "python3", "-m", "venv", "/opt/xmppenv");
        Docker.Exec("Install slixmpp into the virtualenv", "/opt/xmppenv/bin/pip", "install", "-q", "slixmpp");
    }
}
