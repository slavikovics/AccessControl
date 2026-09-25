namespace MongoLab;

static class Config
{
    public const string DataDir = "/var/lib/mongo_lab_data";
    public const string LogFile = "/var/log/mongo_lab.log";
    public const int Port = 27017;

    public const string MongoExpressLogFile = "/var/log/mongo_lab_express.log";
    public const int MongoExpressPort = 8081;

    public const string AdminUser = "admin";
    public const string AdminPassword = "AdminP@ss1";

    public const string AppDb = "labdb";
    public const string PublicCollection = "public_notes";
    public const string PrivateCollection = "private_notes";

    public const string AppUser = "labuser";
    public const string AppUserPassword = "UserP@ss1";

    public const string GuestUser = "labguest";
    public const string GuestPassword = "GuestP@ss1";
    public const string GuestRole = "guestReader";
}
