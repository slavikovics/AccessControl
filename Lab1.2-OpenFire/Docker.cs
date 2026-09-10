namespace OpenfireLab;

static class Docker
{
    public static bool Exec(string description, string file, params string[] args)
    {
        var dockerArgs = new List<string> { "exec", Config.ContainerName, file };
        dockerArgs.AddRange(args);
        return Report.Execute(description, "docker", dockerArgs.ToArray());
    }

    public static bool ExecSilent(string file, params string[] args)
    {
        var dockerArgs = new List<string> { "exec", Config.ContainerName, file };
        dockerArgs.AddRange(args);
        return Report.RunSilent("docker", dockerArgs.ToArray());
    }

    public static (bool Ok, string StdOut) ExecCapture(string file, params string[] args)
    {
        var dockerArgs = new List<string> { "exec", Config.ContainerName, file };
        dockerArgs.AddRange(args);
        var (ok, stdOut, _) = Report.RunCapture("docker", dockerArgs.ToArray());
        return (ok, stdOut);
    }

    public static bool Cp(string description, string localPath, string containerPath) =>
        Report.Execute(description, "docker", "cp", localPath, $"{Config.ContainerName}:{containerPath}");
}
