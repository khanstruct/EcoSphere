using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [Serializable]
    public sealed class CompletedWorld
    {
        public int system = 1, planet;
        public float seaLevel = -.02f, rotation;
    }

    public sealed class WorldResult
    {
        public CompletedWorld world;
        public readonly List<int> discoveries = new List<int>();
    }

    public static class WorldProgress
    {
        sealed class SessionData
        {
            public List<CompletedWorld> worlds = new List<CompletedWorld>();
            public bool stoneUnlocked;
            public int currentSystem = 1;
        }
        static SessionData data;
        public static WorldResult Pending { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            data = new SessionData(); Pending = null;
        }

        static SessionData Data
        {
            get
            {
                if (data == null)
                {
                    data = new SessionData();
                }
                return data;
            }
        }

        public static CompletedWorld Find(int system, int planet) => Data.worlds.Find(w => w.system == system && w.planet == planet);
        public static int CurrentSystem => Mathf.Max(1, Data.currentSystem);

        public static void VisitSystem(int system)
        {
            system = Mathf.Max(1, system);
            if (Data.currentSystem == system) return;
            Data.currentSystem = system;
        }

        public static WorldResult Complete(int system, int planet, float seaLevel, float rotation, bool releasedStone)
        {
            var world = new CompletedWorld { system = system, planet = planet, seaLevel = seaLevel, rotation = rotation };
            Data.worlds.RemoveAll(w => w.system == system && w.planet == planet);
            Data.worlds.Add(world);
            Data.currentSystem = system;
            Pending = new WorldResult { world = world };
            if (releasedStone && !Data.stoneUnlocked)
            {
                Data.stoneUnlocked = true;
                Pending.discoveries.Add(3);
            }
            return Pending;
        }

        public static void ClearPending() { Pending = null; }
    }
}
