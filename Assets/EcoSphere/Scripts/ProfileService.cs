using System;
using System.Collections.Generic;
using UnityEngine;
namespace EcoSphere
{
    // Owns the active profile and its object graph; scene objects never own progression data.
    public sealed class ProfileService
    {
        PlayerProfile active;
        readonly Dictionary<int,SystemRecord> systems=new Dictionary<int,SystemRecord>();
        public event Action Changed;
        bool SmokeRun=>Array.Exists(Environment.GetCommandLineArgs(),s=>s.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0);
        public bool PersistenceEnabled=>Application.isPlaying && WorldCatalog.Current.persistProgressBetweenSessions && !SmokeRun;
        public string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,"worlds-v1.json");
        public PlayerProfile Active
        {
            get
            {
                if(active==null)
                {
                    active=PersistenceEnabled ? WorldSaveStore.Load(SavePath) : new PlayerProfile{universeSeed=Application.isPlaying && !SmokeRun ? Guid.NewGuid().GetHashCode()&int.MaxValue :3187};
                    foreach(var system in active.systems)systems.Add(system.number,system);
                }
                return active;
            }
        }
        public SystemRecord GetSystem(int number)
        {
            _=Active;number=Mathf.Max(1,number);
            if(systems.TryGetValue(number,out var found))return found;
            var system=WorldGeneration.GetOrCreate(Active,WorldCatalog.Current,number);systems.Add(number,system);Save();return system;
        }
        public bool TryGetSystem(int number,out SystemRecord system){_=Active;return systems.TryGetValue(number,out system);}
        public CompletedWorld FindCompletion(int system,int planet)
        {
            if(!TryGetSystem(system,out var record) || planet<0 || planet>=record.worlds.Count)return null;
            return record.worlds[planet].completion;
        }
        public WorldRecord GetPlanet(int system,int index)
        {
            var record=GetSystem(system);if(index<0 || index>=record.worlds.Count)throw new ArgumentOutOfRangeException(nameof(index));return record.worlds[index];
        }
        public bool OwnsSphere(int id)
        {
            var sphere=SphereCatalog.Current.Get(id);
            return sphere.availableAtStart || Active.unlockedSpheres.Contains(id) || (DevelopmentMode.Enabled && sphere.AmmoEligible);
        }
        public bool UnlockSphere(int id)
        {
            if(!GrantSphere(id))return false;Save();return true;
        }
        internal bool GrantSphere(int id)
        {
            var sphere=SphereCatalog.Current.Get(id);if(!sphere.selectable || OwnsSphere(id))return false;
            Active.unlockedSpheres.Add(id);return true;
        }
        public void VisitSystem(int number){Active.currentSystem=Mathf.Max(1,number);GetSystem(Active.currentSystem);Save();}
        public void Save(){if(PersistenceEnabled)WorldSaveStore.Save(SavePath,Active);Changed?.Invoke();}
    }
}
