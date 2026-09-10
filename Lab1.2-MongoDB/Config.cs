namespace MongoLab;

static class Config
{
    public const string ContainerName = "mongo_lab";
    public const string Image = "mongo:8.2-noble";
    public const string Volume = "mongo_lab_data";
    public const int Port = 27017;

    public const string MongoExpressContainerName = "mongo_lab_express";
    public const string MongoExpressImage = "mongo-express:1";
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
