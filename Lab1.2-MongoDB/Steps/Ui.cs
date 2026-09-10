using System.Net.Http;

namespace MongoLab.Steps;

static class Ui
{
    public static void Run()
    {
        Report.Step(9, "Start Mongo Express so the setup can be checked visually");
        Report.Execute("Pull Mongo Express image", "docker", "pull", Config.MongoExpressImage);

        var encodedAdminPassword = Uri.EscapeDataString(Config.AdminPassword);
        Report.Execute("Run Mongo Express container", "docker", "run", "-d", "--name", Config.MongoExpressContainerName,
            "-p", $"{Config.MongoExpressPort}:8081",
            "--link", $"{Config.ContainerName}:{Config.ContainerName}",
            "-e", $"ME_CONFIG_MONGODB_SERVER={Config.ContainerName}",
            "-e", "ME_CONFIG_MONGODB_PORT=27017",
            "-e", $"ME_CONFIG_MONGODB_ADMINUSERNAME={Config.AdminUser}",
            "-e", $"ME_CONFIG_MONGODB_ADMINPASSWORD={encodedAdminPassword}",
            "-e", "ME_CONFIG_MONGODB_AUTH_DATABASE=admin",
            "-e", "ME_CONFIG_BASICAUTH=false",
            Config.MongoExpressImage);

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
