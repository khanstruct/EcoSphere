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
        public float stoneRuggedness;
        public float iceCoverage;
        public float fireHeat, atmosphereStrength, atmosphereWidth;
        public Color atmosphereColor;
        public PlanetStats planetStats;
        public List<int> discoveries = new List<int>();
        public bool discoveryTutorialSeen, nextWorldTutorialSeen;
        public List<DiscoveryRecord> discoveryDetails = new List<DiscoveryRecord>();
        public PlanetStats AppearanceStats => planetStats ?? PlanetStats.Legacy(stoneRuggedness,iceCoverage,fireHeat,atmosphereColor,atmosphereStrength,atmosphereWidth);
        public void SetStats(PlanetStats stats)
        {
            planetStats=stats.Copy();
            stoneRuggedness=planetStats.Ruggedness;iceCoverage=planetStats.IceCoverage;fireHeat=planetStats.Heat;
            atmosphereColor=planetStats.atmosphereColor;atmosphereStrength=planetStats.AtmosphereDensity;atmosphereWidth=planetStats.atmosphereWidth;
        }
    }

    public sealed class WorldResult
    {
        public CompletedWorld world;
        public bool revisiting;
        public IReadOnlyList<int> DisplayedDiscoveries => revisiting ? (IReadOnlyList<int>)world.discoveries : discoveries;
        public readonly List<int> discoveries = new List<int>();
        public readonly List<DiscoveryRecord> discoveryDetails = new List<DiscoveryRecord>();
    }

    public static class WorldProgress
    {
        static WorldSaveData data;
        static readonly Dictionary<int,SystemRecord> systemsByNumber=new Dictionary<int,SystemRecord>();
        static readonly Dictionary<(int system,int planet),CompletedWorld> completedByLocation=new Dictionary<(int,int),CompletedWorld>();
        public static WorldResult Pending { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetSession()
        {
            ClearSessionState();
            Application.quitting -= ClearSessionState;
            Application.quitting += ClearSessionState;
        }

        static void ClearSessionState()
        {
            data = null; Pending = null;systemsByNumber.Clear();completedByLocation.Clear();
        }

        static bool IsSmokeRun => Array.Exists(Environment.GetCommandLineArgs(),
            argument => argument.IndexOf("Smoke", StringComparison.OrdinalIgnoreCase) >= 0);
        public static bool PersistenceEnabled => Application.isPlaying && WorldCatalog.Current.persistProgressBetweenSessions && !IsSmokeRun;
        public static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "worlds-v1.json");
        static void Save() { if (PersistenceEnabled) WorldSaveStore.Save(SavePath, Data); }

        static WorldSaveData Data
        {
            get
            {
                if (data == null)
                {
                    data = PersistenceEnabled ? WorldSaveStore.Load(SavePath) : new WorldSaveData {
                        universeSeed = Application.isPlaying && !IsSmokeRun ? Guid.NewGuid().GetHashCode() & int.MaxValue : 3187 };
                    systemsByNumber.Clear();completedByLocation.Clear();
                    foreach(var system in data.systems)systemsByNumber.TryAdd(system.number,system);
                    foreach(var world in data.worlds)completedByLocation.TryAdd((world.system,world.planet),world);
                }
                return data;
            }
        }

        public static CompletedWorld Find(int system, int planet)
        {
            _=Data;return completedByLocation.TryGetValue((system,planet),out var world) ? world : null;
        }
        public static bool TryOpenCompletedResults(int system, int planet)
        {
            var completed = Find(system, planet);
            if (completed == null) return false;
            Data.currentSystem = system;
            // Viewing results never repeats discovery rewards or replaces the completed world's state.
            Pending = new WorldResult { world = completed, revisiting = true };
            Save();
            return true;
        }
        public static int CurrentSystem => Mathf.Max(1, Data.currentSystem);

        public static SystemRecord GetSystem(int number)
        {
            var save=Data;number=Mathf.Max(1,number);
            if(systemsByNumber.TryGetValue(number,out var system))return system;
            int previousCount = save.systems.Count;
            system = WorldGeneration.GetOrCreate(save, WorldCatalog.Current, number);
            systemsByNumber[number]=system;
            if (save.systems.Count != previousCount) Save();
            return system;
        }

        public static void VisitSystem(int system)
        {
            system = Mathf.Max(1, system);
            if (Data.currentSystem == system) return;
            Data.currentSystem = system;
            GetSystem(system);
            Save();
        }

        public static bool IsSphereAvailable(int type) => (DevelopmentMode.Enabled && SphereCatalog.Current.Get(type).AmmoEligible) || IsSphereEarned(type);
        static bool IsSphereEarned(int type) => SphereCatalog.Current.Get(type).availableAtStart ||
            Data.unlockedSpheres.Contains(type) || (type == SphereIds.Stone && Data.stoneUnlocked);

        public static int AvailableSphereCount
        {
            get
            {
                int count = 0;
                foreach (var sphere in SphereCatalog.Current.spheres)
                    if (sphere.AmmoEligible && IsSphereAvailable(sphere.id)) count++;
                return count;
            }
        }
        public static bool SystemIntroductionSeen => Data.systemIntroductionSeen;
        public static void FinishSystemIntroduction() { Data.systemIntroductionSeen = true; Save(); }

        public static int SetupTutorialStep(string worldId) => Data.setupTutorials?.Find(entry=>entry.worldId==worldId)?.step ?? 0;
        public static void AdvanceSetupTutorial(string worldId)
        {
            if(Data.setupTutorials==null) Data.setupTutorials=new List<SetupTutorialProgress>();
            var progress=Data.setupTutorials.Find(entry=>entry.worldId==worldId);
            if(progress==null) { progress=new SetupTutorialProgress { worldId=worldId }; Data.setupTutorials.Add(progress); }
            progress.step++; Save();
        }

        public static PredefinedWorld TutorialDefinition(CompletedWorld world) => WorldCatalog.Current.Find(WorldLevels.Get(world.system,world.planet).predefinedId);
        public static void FinishDiscoveryTutorial(CompletedWorld world) { world.discoveryTutorialSeen=true; Save(); }
        public static void FinishNextWorldTutorial(CompletedWorld world) { world.nextWorldTutorialSeen=true; Save(); }
        public static bool TryGetNextWorldTutorial(int system,out CompletedWorld source,out PredefinedWorld definition,out int target)
        {
            source=null; definition=null; target=-1;
            if(DevelopmentMode.Enabled)return false;
            var generated=GetSystem(system);
            foreach(var completed in Data.worlds)
            {
                if(completed.system!=system || !completed.discoveryTutorialSeen || completed.nextWorldTutorialSeen)continue;
                var authored=TutorialDefinition(completed);
                if(!authored || string.IsNullOrWhiteSpace(authored.nextWorldTutorialMessage))continue;
                int index=generated.worlds.FindIndex(world=>world.predefinedId==authored.nextWorldTutorialTarget);
                if(index<0 || Find(system,index)!=null || AvailableSphereCount<generated.worlds[index].RequiredSpheres)continue;
                source=completed; definition=authored; target=index; return true;
            }
            return false;
        }

        static void Discover(ElementRecipe recipe)
        {
            int type=recipe.output.id;
            if (IsSphereEarned(type) || !SphereCatalog.Current.Get(type).selectable) return;
            Data.unlockedSpheres.Add(type); Pending.discoveries.Add(type);
            Pending.discoveryDetails.Add(new DiscoveryRecord { sphereId=type, kind=DiscoveryKind.Recipe, first=recipe.first, second=recipe.second });
        }

        public static WorldResult Complete(int system, int planet, float seaLevel, float rotation, bool releasedStone, IReadOnlyList<double> elements = null, int inclusionGoal = ElementScoring.Inclusion, IReadOnlyList<ElementId> leaderOrder = null)
        {
            if (TryOpenCompletedResults(system, planet)) return Pending;
            GetSystem(system);
            var world = new CompletedWorld { system = system, planet = planet, seaLevel = seaLevel, rotation = rotation };
            if(elements!=null)
            {
                world.SetStats(PlanetStatCalculator.Evaluate(elements));
                if(DevelopmentMode.Enabled)
                {
                    double total=0;for(int i=0;i<elements.Count;i++)total+=Math.Max(0,elements[i]);
                    double ice=(int)ElementId.Ice<elements.Count ? elements[(int)ElementId.Ice] : 0;
                    double fire=(int)ElementId.Fire<elements.Count ? elements[(int)ElementId.Fire] : 0;
                    double sunlight=(int)ElementId.Sunlight<elements.Count ? elements[(int)ElementId.Sunlight] : 0;
                    Debug.Log($"ECOSPHERE_PLANET_CLIMATE total={total}, Ice={ice}, Fire={fire}, Sunlight={sunlight}, temperature={world.planetStats.Temperature}, frozen={world.planetStats.IceCoverage}, fullFreezeAt={world.planetStats.fullyFrozenAt}");
                }
            }
            Data.worlds.Add(world);
            completedByLocation.Add((system,planet),world);
            Data.currentSystem = system;
            Pending = new WorldResult { world = world };
            if (releasedStone && !Data.stoneUnlocked)
            {
                Data.stoneUnlocked = true;
                Pending.discoveries.Add(SphereIds.Stone);
                Pending.discoveryDetails.Add(new DiscoveryRecord { sphereId=SphereIds.Stone,kind=DiscoveryKind.Natural });
            }
            // Use the same standings as the visualizer, including its incumbent-on-tie order.
            if (elements != null)
            {
                var standings = new ElementStandings(); standings.Update(elements,leaderOrder);
                foreach (var recipe in SphereCatalog.Current.recipeBook.recipes)
                    if (ElementRecipeBook.Qualifies(recipe,standings,inclusionGoal)) Discover(recipe);
            }
            world.discoveries.AddRange(Pending.discoveries);
            world.discoveryDetails.AddRange(Pending.discoveryDetails);
            Save();
            return Pending;
        }

        public static void ClearPending() { Pending = null; }
    }
}
