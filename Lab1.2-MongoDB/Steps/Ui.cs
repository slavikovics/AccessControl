using System.Net.Http;

namespace MongoLab.Steps;

static class Ui
{
    public static void Run()
    {
        Report.Step(9, "Start Mongo Express so the setup can be checked visually");
        Report.Execute("Install Mongo Express", "npm", "install", "-g", "mongo-express@1.0.0");

        var encodedAdminPassword = Uri.EscapeDataString(Config.AdminPassword);
        var env = new Dictionary<string, string>
        {
            ["ME_CONFIG_MONGODB_SERVER"] = "127.0.0.1",
            ["ME_CONFIG_MONGODB_PORT"] = Config.Port.ToString(),
            ["ME_CONFIG_MONGODB_ADMINUSERNAME"] = Config.AdminUser,
            ["ME_CONFIG_MONGODB_ADMINPASSWORD"] = encodedAdminPassword,
            ["ME_CONFIG_MONGODB_AUTH_DATABASE"] = "admin",
            ["ME_CONFIG_BASICAUTH_USERNAME"] = "",
            ["PORT"] = Config.MongoExpressPort.ToString()
        };
        Report.ExecuteBackground("Start Mongo Express", env, "mongo-express");

        WaitReady();
        Report.Note($"Open http://localhost:{Config.MongoExpressPort} to browse '{Config.AppDb}' " +
                     $"({Config.PublicCollection}/{Config.PrivateCollection}) visually, connected as '{Config.AdminUser}'");
    }

    static void WaitReady()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var url = $"http://localhost:{Config.MongoExpressPort}/";

        for (var i = 0; i < 30; i++)
        {
            try
            {
                using var response = client.GetAsync(url).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode) return;
            }
            catch (Exception)
            {
            }
            Thread.Sleep(1000);
        }

        Report.Note("Mongo Express did not become ready in time", ok: false);
    }
}
