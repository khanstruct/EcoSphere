using System;
using UnityEngine;
namespace EcoSphere
{
    [CreateAssetMenu(menuName="EcoSphere/Development Settings")]
    public sealed class DevelopmentSettings : ScriptableObject
    {
        [Tooltip("Skip tutorial messages and expose every selectable sphere without permanently granting the collection.")]
        public bool devModeEnabled=true;
    }
    public static class DevelopmentMode
    {
        static DevelopmentSettings settings;
        public static bool Enabled
        {
            get
            {
                // Explicit override keeps normal progression/tutorial regression checks available.
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-noDevMode")>=0)return false;
                if(!settings)settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");
                return settings && settings.devModeEnabled;
            }
        }
    }
}
