namespace OpenfireLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(10, "Remove Openfire and every file it created");
        // Openfire's install, config, embedded database and plugins all live inside this
        // one container's writable layer — removing it removes all of them in one step.
        Report.Execute("Remove the container", "docker", "rm", "-f", Config.ContainerName);
    }
}
