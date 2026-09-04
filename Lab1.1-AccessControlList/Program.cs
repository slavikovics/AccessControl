using AclLab;
using AclLab.Steps;

if (!Environment.IsPrivilegedProcess)
{
    Console.Error.WriteLine("This program must run as root — creating users/groups and setting permissions/ACLs is impossible without it.");
    return 1;
}

Accounts.Run();
Folders.Run();
Files.Run();
VerifyFiles.Run();
Processes.Run();
VerifyFolders.Run();
Cleanup.Run();

Report.WaitForContinue();
Report.PrintSummary();

return 0;
