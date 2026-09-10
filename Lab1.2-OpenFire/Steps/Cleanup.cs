namespace OpenfireLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(10, "Remove Openfire and every file it created");
        Report.Execute("Remove the container", "docker", "rm", "-f", Config.ContainerName);
    }
}
