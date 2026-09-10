namespace OpenfireLab;

static class Config
{
    public const string ContainerName = "openfire_lab";
    public const string Image = "ubuntu:24.04";
    public const string DebUrl = "https://download.igniterealtime.org/openfire/openfire_5.1.2_all.deb";

    public const string Domain = "localhost";
    public const int AdminConsolePort = 9090;
    public const int SecureAdminConsolePort = 9091;
    public const int ClientPort = 5222;
    public const int ServerPort = 5269;

    public const string AdminUser = "admin";
    public const string AdminPassword = "AdminP@ss1";

    public const string AppUser = "iuser";
    public const string AppUserPassword = "UserP@ss1";

    public const string GuestNickname = "guest";

    public const string RoomName = "labroom";
    public static string RoomJid => $"{RoomName}@conference.{Domain}";
}
