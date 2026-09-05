namespace MongoLab.Steps;

static class Configure
{
    public static void Run()
    {
        Report.Step(4, "Create the administrator account through the localhost exception");
        MongoEvalNoAuth("Create admin user (role 'root' on 'admin')",
            $"db.getSiblingDB('admin').createUser({{user:'{Config.AdminUser}',pwd:'{Config.AdminPassword}',roles:[{{role:'root',db:'admin'}}]}})");

        Report.Step(5, "Restart MongoDB with authorization enforced");
        Report.Execute("Stop the bootstrap container", "docker", "rm", "-f", Config.ContainerName);
        Report.Execute("Run MongoDB container with --auth", "docker", "run", "-d", "--name", Config.ContainerName,
            "-p", $"{Config.Port}:27017", "-v", $"{Config.Volume}:/data/db", Config.Image, "--auth");
        WaitReadyAuthenticated();

        Report.Step(6, "Seed the application database with public and private data");
        MongoEvalAuth("Insert a document into public_notes",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PublicCollection}.insertOne({{title:'public seed', visibility:'public'}})");
        MongoEvalAuth("Insert a document into private_notes",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PrivateCollection}.insertOne({{title:'private seed', visibility:'private'}})");

        Report.Step(7, $"Create '{Config.AppUser}' — readWrite on '{Config.AppDb}' only");
        MongoEvalAuth($"Create user '{Config.AppUser}' (role readWrite on {Config.AppDb})",
            $"db.getSiblingDB('{Config.AppDb}').createUser({{user:'{Config.AppUser}',pwd:'{Config.AppUserPassword}',roles:[{{role:'readWrite',db:'{Config.AppDb}'}}]}})");

        Report.Step(8, $"Create '{Config.GuestUser}' — read-only on '{Config.AppDb}.{Config.PublicCollection}' only");
        MongoEvalAuth($"Create role '{Config.GuestRole}' (find on {Config.AppDb}.{Config.PublicCollection} only)",
            $"db.getSiblingDB('{Config.AppDb}').createRole({{role:'{Config.GuestRole}'," +
            $"privileges:[{{resource:{{db:'{Config.AppDb}',collection:'{Config.PublicCollection}'}},actions:['find']}}],roles:[]}})");
        MongoEvalAuth($"Create user '{Config.GuestUser}' (role {Config.GuestRole})",
            $"db.getSiblingDB('{Config.AppDb}').createUser({{user:'{Config.GuestUser}',pwd:'{Config.GuestPassword}',roles:[{{role:'{Config.GuestRole}',db:'{Config.AppDb}'}}]}})");
    }

    static void WaitReadyAuthenticated()
    {
        for (var i = 0; i < 30; i++)
        {
            if (Report.RunSilent("docker", "exec", Config.ContainerName, "mongosh",
                    "-u", Config.AdminUser, "-p", Config.AdminPassword, "--authenticationDatabase", "admin",
                    "--quiet", "--eval", "db.runCommand({ping:1})"))
            {
                Report.Note("MongoDB is back up with authorization enforced");
                return;
            }
            Thread.Sleep(1000);
        }

        Report.Note("MongoDB did not come back up with authorization enabled", ok: false);
    }

    static void MongoEvalNoAuth(string description, string js) =>
        Report.Execute(description, "docker", "exec", Config.ContainerName, "mongosh", "--quiet", "--eval", js);

    static void MongoEvalAuth(string description, string js) =>
        Report.Execute(description, "docker", "exec", Config.ContainerName, "mongosh",
            "-u", Config.AdminUser, "-p", Config.AdminPassword, "--authenticationDatabase", "admin",
            "--quiet", "--eval", js);
}
