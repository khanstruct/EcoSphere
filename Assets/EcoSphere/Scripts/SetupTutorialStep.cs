using System;
namespace EcoSphere
{
    public enum SetupTutorialTarget { RequiredSlots, OwnedSpheres, Begin }
    [Serializable]
    public sealed class SetupTutorialStep
    {
        public string message;
        public SetupTutorialTarget target;
        public bool waitForFullLoadout;
    }
    [Serializable]
    public sealed class SetupTutorialProgress
    {
        public string worldId;
        public int step;
    }
}
