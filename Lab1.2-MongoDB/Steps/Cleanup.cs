namespace MongoLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(13, "Remove MongoDB, its data volume and its image");
        Report.Execute("Remove the container", "docker", "rm", "-f", Config.ContainerName);
        Report.Execute("Remove the data volume", "docker", "volume", "rm", Config.Volume);
        Report.Execute("Remove the MongoDB image", "docker", "rmi", Config.Image);
    }
}
