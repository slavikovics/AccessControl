namespace MongoLab.Steps;

static class Cleanup
{
    public static void Run()
    {
        Report.Step(14, "Stop MongoDB and Mongo Express, wipe the data directory (packages stay installed)");
        Report.Execute("Stop mongod", "sudo", "mongod", "--shutdown", "--dbpath", Config.DataDir);
        Report.Execute("Stop Mongo Express", "sudo", "pkill", "-f", "mongo-express");
        Report.Execute("Remove the data directory", "sudo", "rm", "-rf", Config.DataDir);
        //Report.Execute("Remove the MongoDB package", "sudo", "apt-get", "remove", "-y", "mongodb-org");
        //Report.Execute("Remove Mongo Express package", "sudo", "npm", "uninstall", "-g", "mongo-express");
    }
}
