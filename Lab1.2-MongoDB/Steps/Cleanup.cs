namespace MongoLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(14, "Remove MongoDB, Mongo Express, the data volume and their images");
        Report.Execute("Remove the container", "docker", "rm", "-f", Config.ContainerName);
        Report.Execute("Remove the Mongo Express container", "docker", "rm", "-f", Config.MongoExpressContainerName);
        Report.Execute("Remove the data volume", "docker", "volume", "rm", Config.Volume);
        //Report.Execute("Remove the MongoDB image", "docker", "rmi", Config.Image);
        //Report.Execute("Remove the Mongo Express image", "docker", "rmi", Config.MongoExpressImage);
    }
}
