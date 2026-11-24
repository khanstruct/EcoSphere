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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static DevelopmentSettings settings;
#endif
        // DEV mode exists only in the Editor and Development Builds. Release builds always use normal
        // progression and tutorials, regardless of the serialized DevelopmentSettings asset.
        public static bool Enabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Explicit override keeps normal progression/tutorial regression checks available.
                if(LaunchFlags.Has("-noDevMode"))return false;
                if(!settings)settings=Resources.Load<DevelopmentSettings>("DevelopmentSettings");
                return settings && settings.devModeEnabled;
#else
                return false;
#endif
            }
        }
    }
}
