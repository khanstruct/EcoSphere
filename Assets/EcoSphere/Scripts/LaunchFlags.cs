using System;
using System.Collections.Generic;
namespace EcoSphere
{
    public static class LaunchFlags
    {
        static readonly string[] arguments=Environment.GetCommandLineArgs();
        static readonly HashSet<string> flags=new HashSet<string>(arguments,StringComparer.Ordinal);
        public static bool Has(string flag)=>flags.Contains(flag);
        public static bool IsSmokeRun {get;}=Array.Exists(arguments,arg=>arg.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0);
    }
}
