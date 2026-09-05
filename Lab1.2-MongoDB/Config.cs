namespace MongoLab;

static class Config
{
    public const string ContainerName = "mongo_lab";
    public const string Image = "mongo:7";
    public const string Volume = "mongo_lab_data";
    public const int Port = 27017;

    // Administrator: full privileges over the whole deployment (root role, admin db).
    public const string AdminUser = "admin";
    public const string AdminPassword = "AdminP@ss1";

    public const string AppDb = "labdb";
    public const string PublicCollection = "public_notes";
    public const string PrivateCollection = "private_notes";

    // User: full rights (readWrite) within the single application database the
    // administrator granted access to — no rights anywhere else.
    public const string AppUser = "labuser";
    public const string AppUserPassword = "UserP@ss1";

    // Guest: read-only rights restricted to one collection fragment, via a
    // custom role — not a built-in one — so that the restriction is exact.
    public const string GuestUser = "labguest";
    public const string GuestPassword = "GuestP@ss1";
    public const string GuestRole = "guestReader";
}
