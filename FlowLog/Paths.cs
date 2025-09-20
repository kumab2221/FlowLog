using System;
using System.IO;

namespace FlowLog
{
    public static class Paths
    {
        public static string LocalRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowLog");
        public static string LocalRepo => Path.Combine(LocalRoot, "repo");

        public static string RoamingRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowLog");
        public static string ConfigJson => Path.Combine(RoamingRoot, "config.json");

        public static void EnsureDirs()
        {
            Directory.CreateDirectory(LocalRoot);
            Directory.CreateDirectory(RoamingRoot);
        }
    }
}