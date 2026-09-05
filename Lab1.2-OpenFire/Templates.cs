namespace OpenfireLab;

static class Templates
{
    static string AssetsDir => Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string Render(string templateFileName, Dictionary<string, string> values)
    {
        var text = File.ReadAllText(Path.Combine(AssetsDir, templateFileName));
        foreach (var (key, value) in values)
        {
            text = text.Replace("{{" + key + "}}", value);
        }
        return text;
    }

    public static Dictionary<string, string> DefaultValues() => new()
    {
        ["DOMAIN"] = Config.Domain,
        ["ADMIN_PORT"] = Config.AdminConsolePort.ToString(),
        ["SECURE_ADMIN_PORT"] = Config.SecureAdminConsolePort.ToString(),
        ["CLIENT_PORT"] = Config.ClientPort.ToString(),
        ["ADMIN_USER"] = Config.AdminUser,
        ["ADMIN_PASSWORD"] = Config.AdminPassword,
        ["APP_USER"] = Config.AppUser,
        ["APP_USER_PASSWORD"] = Config.AppUserPassword,
        ["ROOM_JID"] = Config.RoomJid,
    };
}
