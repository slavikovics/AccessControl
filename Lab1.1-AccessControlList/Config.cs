namespace AclLab;

static class Config
{
    public const string BasePath = "/acl_lab";
    public const string Pzs = BasePath + "/pzs";

    public static readonly string[] LabGroups = ["group_iit1", "group_iit2"];
    public static readonly string[] TestUsers = ["iit11", "iit12", "iit21", "iit22", "iit3"];
    public static readonly string[] AllIdentities = ["iit11", "iit12", "iit21", "iit22", "iit3", "root"];

    public static readonly string[] PermSyms = ["", "r--", "rw-", "-w-", "rwx", "--x"];
    public static readonly string[] PermOctals = ["", "4", "6", "2", "7", "1"];

    public static readonly List<string> CreatedFiles = [];
}
