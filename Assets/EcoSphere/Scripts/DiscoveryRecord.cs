using System;
namespace EcoSphere
{
    public enum DiscoveryKind { Natural, Recipe }
    [Serializable]
    public sealed class DiscoveryRecord
    {
        public int sphereId;
        public DiscoveryKind kind;
        public ElementId first, second;
    }
}
