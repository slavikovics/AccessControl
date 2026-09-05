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

    // Administrator: the built-in Openfire "admin" account — full access to the Admin
    // Console and every subsystem. Its password is set during autosetup.
    public const string AdminUser = "admin";
    public const string AdminPassword = "AdminP@ss1";

    // User: a registered XMPP account, granted "member" affiliation (voice) in the room
    // the administrator created — full participation in the subsystem it was given.
    public const string AppUser = "iuser";
    public const string AppUserPassword = "UserP@ss1";

    // Guest: no account at all — anonymous login, joins the same room as a "visitor" in a
    // moderated room, i.e. can read the conversation but has no voice to post in it.
    public const string GuestNickname = "guest";

    public const string RoomName = "labroom";
    public static string RoomJid => $"{RoomName}@conference.{Domain}";
}
