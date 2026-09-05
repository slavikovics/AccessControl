namespace MongoLab.Steps;

static class Install
{
    public static void Run()
    {
        Report.Step(1, "Pull the MongoDB server image");
        Report.Execute("Pull MongoDB image", "docker", "pull", Config.Image);

        Report.Step(2, "Create a persistent data volume");
        Report.Execute("Create data volume", "docker", "volume", "create", Config.Volume);

        Report.Step(3, "Start MongoDB (bootstrap mode, no authentication yet)");
        Report.Execute("Run MongoDB container", "docker", "run", "-d", "--name", Config.ContainerName,
            "-p", $"{Config.Port}:27017", "-v", $"{Config.Volume}:/data/db", Config.Image);

        WaitReady();
    }

    // The container's port is open before mongod inside it has finished initialising, so
    // poll with a plain (pre-auth) ping instead of assuming the first connection succeeds.
    static void WaitReady()
    {
        for (var i = 0; i < 30; i++)
        {
            if (Report.RunSilent("docker", "exec", Config.ContainerName, "mongosh", "--quiet", "--eval", "db.runCommand({ping:1})"))
            {
                Report.Note("MongoDB accepted a connection and is ready");
                return;
            }
            Thread.Sleep(1000);
        }

        Report.Note("MongoDB did not become ready in time", ok: false);
    }
}
