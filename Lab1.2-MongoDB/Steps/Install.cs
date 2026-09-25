namespace MongoLab.Steps;

static class Install
{
    public static void Run()
    {
        Report.Step(1, "Add the MongoDB apt repository");
        AddRepo();

        Report.Step(2, "Install the MongoDB server package");
        Report.Execute("Install mongodb-org", "sudo", "apt-get", "install", "-y", "mongodb-org");

        Report.Step(3, "Create a persistent data directory");
        Report.Execute("Create data directory", "sudo", "mkdir", "-p", Config.DataDir);

        Report.Step(4, "Start MongoDB (bootstrap mode, no authentication yet)");
        Report.Execute("Start mongod", "sudo", "mongod", "--fork", "--dbpath", Config.DataDir,
            "--port", Config.Port.ToString(), "--logpath", Config.LogFile, "--bind_ip_all");

        WaitReady();
    }

    static void AddRepo()
    {
        Report.Execute("Import MongoDB GPG key", "bash", "-c",
            "curl -fsSL https://www.mongodb.org/static/pgp/server-8.0.asc | " +
            "sudo gpg --yes --dearmor -o /usr/share/keyrings/mongodb-server-8.0.gpg");

        Report.Execute("Add MongoDB apt source", "bash", "-c",
            "echo 'deb [ signed-by=/usr/share/keyrings/mongodb-server-8.0.gpg ] " +
            "https://repo.mongodb.org/apt/ubuntu noble/mongodb-org/8.0 multiverse' | " +
            "sudo tee /etc/apt/sources.list.d/mongodb-org-8.0.list");

        Report.Execute("Refresh apt package index", "sudo", "apt-get", "update");
    }

    static void WaitReady()
    {
        for (var i = 0; i < 30; i++)
        {
            if (Report.RunSilent("mongosh", "--port", Config.Port.ToString(), "--quiet", "--eval", "db.runCommand({ping:1})"))
            {
                Report.Note("MongoDB accepted a connection and is ready");
                return;
            }
            Thread.Sleep(1000);
        }

        Report.Note("MongoDB did not become ready in time", ok: false);
    }
}
