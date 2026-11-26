using System;
using System.Collections.Generic;
namespace EcoSphere
{
    public static class LaunchFlags
    {
        static readonly string[] arguments=Environment.GetCommandLineArgs();
        static readonly HashSet<string> flags=new HashSet<string>(arguments,StringComparer.Ordinal);
        public static bool Has(string flag)=>flags.Contains(flag);
        // The argument after a flag, e.g. "-sphereSmoke 12" gives "12"; null when absent.
        public static string Value(string flag)
        {
            int index=Array.IndexOf(arguments,flag);
            return index>=0 && index+1<arguments.Length ? arguments[index+1] : null;
        }
        public static bool IsSmokeRun {get;}=Array.Exists(arguments,arg=>arg.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0);
    }
}
