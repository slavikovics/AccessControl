namespace MongoLab.Steps;

static class Verify
{
    public static void Run()
    {
        Report.Step(9, "Verify the administrator's privileges");
        MongoCheck(Config.AdminUser, Config.AdminPassword, "admin",
            "ListDatabases", "admin (cluster-wide)", "db.adminCommand({listDatabases:1})");

        Report.Step(10, $"Verify '{Config.AppUser}' has full rights inside '{Config.AppDb}' only");
        MongoCheck(Config.AppUser, Config.AppUserPassword, Config.AppDb,
            "Read", $"{Config.AppDb}.{Config.PublicCollection}",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PublicCollection}.find().toArray()");
        MongoCheck(Config.AppUser, Config.AppUserPassword, Config.AppDb,
            "Write", $"{Config.AppDb}.{Config.PrivateCollection}",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PrivateCollection}.insertOne({{from:'{Config.AppUser}'}})");
        MongoCheck(Config.AppUser, Config.AppUserPassword, Config.AppDb,
            "CreateUser", $"{Config.AppDb} (admin-only op)",
            $"db.getSiblingDB('{Config.AppDb}').createUser({{user:'sneaky',pwd:'x',roles:['readWrite']}})");
        MongoCheck(Config.AppUser, Config.AppUserPassword, Config.AppDb,
            "Read", "otherdb.secrets (no role granted)",
            "db.getSiblingDB('otherdb').secrets.find().toArray()");

        Report.Step(11, $"Verify '{Config.GuestUser}' can only read '{Config.PublicCollection}'");
        MongoCheck(Config.GuestUser, Config.GuestPassword, Config.AppDb,
            "Read", $"{Config.AppDb}.{Config.PublicCollection}",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PublicCollection}.find().toArray()");
        MongoCheck(Config.GuestUser, Config.GuestPassword, Config.AppDb,
            "Read", $"{Config.AppDb}.{Config.PrivateCollection}",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PrivateCollection}.find().toArray()");
        MongoCheck(Config.GuestUser, Config.GuestPassword, Config.AppDb,
            "Write", $"{Config.AppDb}.{Config.PublicCollection}",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PublicCollection}.insertOne({{from:'{Config.GuestUser}'}})");

        Report.Step(12, "Verify that an unauthenticated client is rejected outright");
        MongoCheckNoAuth("Write", $"{Config.AppDb}.{Config.PublicCollection} (no credentials)",
            $"db.getSiblingDB('{Config.AppDb}').{Config.PublicCollection}.insertOne({{from:'anonymous'}})");
    }

    static bool MongoCheck(string identity, string password, string authDb, string label, string resource, string js) =>
        Report.Check(identity, label, resource, "docker", "exec", Config.ContainerName, "mongosh",
            "-u", identity, "-p", password, "--authenticationDatabase", authDb, "--quiet", "--eval", js);

    static bool MongoCheckNoAuth(string label, string resource, string js) =>
        Report.Check("(none)", label, resource, "docker", "exec", Config.ContainerName, "mongosh",
            "--quiet", "--eval", js);
}
