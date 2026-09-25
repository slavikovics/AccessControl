using MongoLab;
using MongoLab.Steps;

if (!Report.RunSilent("which", "apt-get"))
{
    Console.Error.WriteLine("This program needs 'apt-get' available (run on a Debian/Ubuntu host with sudo access).");
    return 1;
}

Install.Run();
Configure.Run();
Ui.Run();
Verify.Run();
Cleanup.Run();

Report.WaitForContinue();
Report.PrintSummary();

return 0;
