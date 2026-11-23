using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed class PlanetProgressService
    {
        PlayerProfile Data=>GameServices.Profile.Active;
        public WorldResult Pending {get=>GameServices.Session.PendingResults;private set=>GameServices.Session.PendingResults=value;}
        public bool PersistenceEnabled=>GameServices.Profile.PersistenceEnabled;
        public string SavePath=>GameServices.Profile.SavePath;
        void Save()=>GameServices.Profile.Save();
        public void ResetSession()=>GameServices.Reset();
        public CompletedWorld Find(int system,int planet)=>GameServices.Profile.FindCompletion(system,planet);
        public bool TryOpenCompletedResults(int system, int planet)
        {
            var completed = Find(system, planet);
            if (completed == null) return false;
            Data.currentSystem = system;
            // Viewing results never repeats discovery rewards or replaces the completed world's state.
            Pending = new WorldResult { world = completed, revisiting = true };
            Save();
            return true;
        }
        public int CurrentSystem => Mathf.Max(1, Data.currentSystem);

        public SystemRecord GetSystem(int number)=>GameServices.Profile.GetSystem(number);
        public void VisitSystem(int system)=>GameServices.Profile.VisitSystem(system);
        public bool IsSphereAvailable(int type)=>GameServices.Profile.OwnsSphere(type);
        public int AvailableSphereCount
        {
            get
            {
                int count = 0;
                foreach (var sphere in SphereCatalog.Current.spheres)
                    if (sphere.AmmoEligible && IsSphereAvailable(sphere.id)) count++;
                return count;
            }
        }
        public bool SystemIntroductionSeen => Data.systemIntroductionSeen;
        public void FinishSystemIntroduction() { Data.systemIntroductionSeen = true; Save(); }

        public int SetupTutorialStep(string worldId) => Data.setupTutorials?.Find(entry=>entry.worldId==worldId)?.step ?? 0;
        public void AdvanceSetupTutorial(string worldId)
        {
            if(Data.setupTutorials==null) Data.setupTutorials=new List<SetupTutorialProgress>();
            var progress=Data.setupTutorials.Find(entry=>entry.worldId==worldId);
            if(progress==null) { progress=new SetupTutorialProgress { worldId=worldId }; Data.setupTutorials.Add(progress); }
            progress.step++; Save();
        }

        public PredefinedWorld TutorialDefinition(CompletedWorld world) => WorldCatalog.Current.Find(WorldLevels.Get(world.system,world.planet).predefinedId);
        public void FinishDiscoveryTutorial(CompletedWorld world) { world.discoveryTutorialSeen=true; Save(); }
        public void FinishNextWorldTutorial(CompletedWorld world) { world.nextWorldTutorialSeen=true; Save(); }
        public bool TryGetNextWorldTutorial(int system,out CompletedWorld source,out PredefinedWorld definition,out int target)
        {
            source=null; definition=null; target=-1;
            if(DevelopmentMode.Enabled)return false;
            var generated=GetSystem(system);
            foreach(var completed in Data.CompletedPlanets)
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

        void Discover(DiscoveryRecord discovery)
        {
            if(!GameServices.Profile.GrantSphere(discovery.sphereId))return;
            Pending.discoveries.Add(discovery.sphereId);Pending.discoveryDetails.Add(discovery);
        }
        public WorldResult Complete(int system, int planet, float seaLevel, float rotation, IReadOnlyCollection<int> releasedNaturalSpheres, IReadOnlyList<double> elements = null, IReadOnlyList<ElementId> leaderOrder = null)
        {
            if (TryOpenCompletedResults(system, planet)) return Pending;
            var record = WorldLevels.Get(system,planet);
            record.highestScore = Math.Max(record.highestScore,new ElementComposition(elements).Total);
            var world = new CompletedWorld { system = system, planet = planet, seaLevel = seaLevel, rotation = rotation, score=new ElementComposition(elements).Total, terrainSeed=record.seed, terrainStyleId=record.terrainStyleId };
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
            record.completion=world;

            Data.currentSystem = system;
            Pending = new WorldResult { world = world };
            if(releasedNaturalSpheres!=null)
                foreach(int type in releasedNaturalSpheres)
                    if(record.naturalSpheres.Exists(natural=>natural.sphereId==type))
                        Discover(new DiscoveryRecord{sphereId=type,kind=DiscoveryKind.Natural});
            // Use the same standings as the visualizer, including its incumbent-on-tie order.
            if (elements != null)
            {
                var standings = new ElementStandings(); standings.Update(elements,leaderOrder);
                foreach (var recipe in SphereCatalog.Current.recipeBook.recipes)
                    if (ElementRecipeBook.Qualifies(recipe,standings,record.rules.inclusionGoal)) Discover(new DiscoveryRecord{sphereId=recipe.output.id,kind=DiscoveryKind.Recipe,first=recipe.first,second=recipe.second});
            }
            world.discoveries.AddRange(Pending.discoveries);
            world.discoveryDetails.AddRange(Pending.discoveryDetails);
            Save();
            return Pending;
        }

        public void ClearPending() { Pending = null; }
    }
}
