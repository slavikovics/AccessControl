using OpenfireLab;
using OpenfireLab.Steps;

if (!Report.RunSilent("docker", "info"))
{
    Console.Error.WriteLine("This program needs access to the Docker daemon (add the current user to the 'docker' group, or run as root).");
    return 1;
}

Install.Run();
Configure.Run();
Verify.Run();
Cleanup.Run();

Report.WaitForContinue();
Report.PrintSummary();

return 0;
