using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [System.Serializable] public sealed class SunTypeDefinition
    {
        public string id,displayName;
        [Tooltip("Static fallback when no animated sun material is assigned on the catalog.")]
        public Sprite sprite;
        [Tooltip("Optional material for this type (e.g. a black hole). Empty uses the catalog's animated sun material.")]
        public Material material;
        [Tooltip("Display size relative to the System sun slot. Use it to match core sizes across types, e.g. a black hole's horizon to a sun's disc.")]
        [Min(.1f)] public float displayScale=1;
        public SunAppearance appearance=new SunAppearance();
    }

    // Per-sun look for the shared animated sun shader. Every sun type uses the same textures and shader.
    [System.Serializable] public sealed class SunAppearance
    {
        public Color coreColor=new Color(1,.97f,.8f),surfaceColor=new Color(1,.72f,.2f),spotColor=new Color(.6f,.18f,.02f),coronaColor=new Color(1,.78f,.35f);
        [Min(0)] public float surfaceSpeed=.025f,coronaSpeed=.032f;
        [Min(0)] public float surfaceContrast=1.6f,coronaStrength=1.2f,glowStrength=.8f;

        public void Apply(Material material)
        {
            material.SetColor("_CoreColor",coreColor);material.SetColor("_SurfaceColor",surfaceColor);
            material.SetColor("_SpotColor",spotColor);material.SetColor("_CoronaColor",coronaColor);
            material.SetFloat("_SurfaceSpeed",surfaceSpeed);material.SetFloat("_CoronaSpeed",coronaSpeed);
            material.SetFloat("_SurfaceContrast",surfaceContrast);material.SetFloat("_CoronaStrength",coronaStrength);
            material.SetFloat("_GlowStrength",glowStrength);
        }
    }
    [CreateAssetMenu(menuName = "EcoSphere/World Catalog")]
    public sealed class WorldCatalog : ScriptableObject
    {
        [Header("Development")]
        [Tooltip("Leave disabled during development: each game session starts fresh and disk saves are ignored.")]
        public bool persistProgressBetweenSessions;
        public List<PredefinedWorld> predefinedWorlds = new List<PredefinedWorld>();
        public List<WorldModifierDefinition> proceduralModifiers = new List<WorldModifierDefinition>();
        public Sprite ratingStar;
        public string defaultSunTypeId="yellow-star",defaultTerrainStyleId="barren-v1";
        public List<SunTypeDefinition> sunTypes=new List<SunTypeDefinition>();
        [Tooltip("Shared animated sun material (EcoSphere/UI/Animated Sun) holding the plasma and corona masks. Each sun type tints its own instance.")]
        public Material animatedSunMaterial;
        [Header("Sun in gameplay levels (a low arch behind the visualizer and cannon)")]
        [Tooltip("How far below the cannon base the top of the sun's disc sits, in world units.")]
        [Min(.1f)] public float levelSunTopBelowCannon=3.2f;
        [Tooltip("Disc radius in world units. Larger makes a flatter arch (the playfield is 9.3 wide).")]
        [Min(1)] public float levelSunRadius=7.5f;
        [Tooltip("How far the full corona visibly extends beyond the disc, as a fraction of the disc radius. Used to shorten the corona so it just reaches the cannon base.")]
        [Range(.1f,2)] public float levelSunCoronaReach=.85f;
        // One rule for every scene: a type's own material, else the shared animated sun material.
        public Material SunMaterial(SunTypeDefinition sun)=>sun!=null && sun.material ? sun.material : animatedSunMaterial;
        public SunTypeDefinition Sun(string id)=>sunTypes.Find(sun=>sun.id==id);
        [Min(4)] public int minimumProceduralSpheres = 4, maximumProceduralSpheres = 4;
        public WorldRules proceduralRules = new WorldRules();
        [Range(0,1)] public float minimumClustering=.1f, maximumClustering=.9f;
        public string proceduralGameplayScene=WorldLevels.ProceduralSceneName;
        public int planetsPerSystem = 5;
        public string[] nameOpenings = { "Av", "El", "Ner", "Or", "Tal", "Vael", "Caer", "Ith", "Ser", "Vel", "Ar", "Mer", "Sol", "Thal", "Esh", "Lir" };
        public string[] nameMiddles = { "a", "e", "i", "o", "ara", "ela", "eri", "ora", "ali", "une", "ira", "eth" };
        public string[] nameEndings = { "ra", "ris", "reth", "on", "ara", "en", "is", "ith", "ora", "une", "avel", "ian", "os", "elle", "an", "iel" };
        static WorldCatalog current;
        public static WorldCatalog Current
        {
            get
            {
                if (!current) current = Resources.Load<WorldCatalog>("WorldCatalog");
                if (!current) throw new InvalidOperationException("Missing WorldCatalog. Run EcoSphere/Configure Worlds.");
                return current;
            }
        }
        public PredefinedWorld Find(string id) => predefinedWorlds.Find(world => world && world.id == id);
        public WorldModifierDefinition Modifier(string id)
        {
            foreach (var modifier in proceduralModifiers) if (modifier && modifier.id == id) return modifier;
            foreach (var world in predefinedWorlds)
                foreach (var modifier in world.modifiers) if (modifier && modifier.id == id) return modifier;
            throw new InvalidOperationException("Unknown world modifier: " + id);
        }

        public void Validate()
        {
            if (planetsPerSystem < 2 || planetsPerSystem > 5 || minimumProceduralSpheres < 4 || maximumProceduralSpheres < minimumProceduralSpheres)
                throw new InvalidOperationException("World catalog requires at least two slots and procedural sphere requirements of at least four");
            ValidateRules(proceduralRules);
            if (nameOpenings == null || nameOpenings.Length == 0 || nameMiddles == null || nameMiddles.Length == 0 || nameEndings == null || nameEndings.Length == 0)
                throw new InvalidOperationException("Planet name fragments are required");
            foreach (var pool in new[] { nameOpenings, nameMiddles, nameEndings })
                foreach (string fragment in pool)
                    if (string.IsNullOrWhiteSpace(fragment)) throw new InvalidOperationException("Empty planet name fragment");
            if(float.IsNaN(minimumClustering) || float.IsNaN(maximumClustering) || minimumClustering<0 || maximumClustering>1 || maximumClustering<minimumClustering)
                throw new InvalidOperationException("Invalid procedural clustering range");
            var ids = new HashSet<string>(); var slots = new HashSet<string>();
            foreach (var world in predefinedWorlds)
            {
                if (!world || string.IsNullOrWhiteSpace(world.id) || !ids.Add(world.id) || string.IsNullOrWhiteSpace(world.displayName) || string.IsNullOrWhiteSpace(world.gameplayScene))
                    throw new InvalidOperationException("Predefined worlds need unique IDs, names and gameplay scenes");
                ValidateRules(world.rules);
                var naturalCells=new HashSet<Vector2Int>();
                foreach(var natural in world.naturalSpheres)
                    if(natural==null || !SphereCatalog.Current.Get(natural.sphereId).AmmoEligible || natural.cell==Vector2Int.zero || !naturalCells.Add(natural.cell))
                        throw new InvalidOperationException("Invalid natural sphere placement on "+world.name);
                    else if(Mathf.Max(Mathf.Abs(natural.cell.x),Mathf.Abs(natural.cell.y),Mathf.Abs(natural.cell.x+natural.cell.y))>world.rules.clusterRings)
                        throw new InvalidOperationException("Natural sphere lies outside the cluster on "+world.name);
                if(!string.IsNullOrWhiteSpace(world.nextWorldTutorialMessage) && Find(world.nextWorldTutorialTarget)==null)
                    throw new InvalidOperationException("Unknown next-world tutorial target: "+world.name);
                if(world.setupTutorial!=null) foreach(var step in world.setupTutorial)
                    if(step==null || string.IsNullOrWhiteSpace(step.message) || !Enum.IsDefined(typeof(SetupTutorialTarget),step.target))
                        throw new InvalidOperationException("Invalid setup tutorial step: "+world.name);
                if (world.guaranteedSystem < 0 || world.guaranteedSlot < 0 || world.guaranteedSlot >= planetsPerSystem ||
                    world.minimumSystem < 1 || (world.maximumSystem > 0 && world.maximumSystem < world.minimumSystem) ||
                    float.IsNaN(world.appearanceChance) || world.appearanceChance < 0 || world.appearanceChance > 1)
                    throw new InvalidOperationException("Invalid placement settings: " + world.name);
                if (world.guaranteedSystem > 0 && !slots.Add(world.guaranteedSystem + ":" + world.guaranteedSlot))
                    throw new InvalidOperationException("Two predefined worlds claim the same guaranteed slot");
                var modifiers = new HashSet<string>();
                foreach (var modifier in world.modifiers)
                    if (!modifier || string.IsNullOrWhiteSpace(modifier.id) || !modifiers.Add(modifier.id)) throw new InvalidOperationException("Invalid modifiers on " + world.name);
                    else modifier.Validate();
                foreach (int sphere in world.requiredUnlockedSpheres) SphereCatalog.Current.Get(sphere);
                foreach (string required in world.requiredCompletedWorlds)
                    if (!predefinedWorlds.Exists(other => other && other.id == required)) throw new InvalidOperationException("Unknown prerequisite world: " + required);
            }
            ids.Clear();
            foreach (var modifier in proceduralModifiers)
            {
                if (!modifier || string.IsNullOrWhiteSpace(modifier.id) || string.IsNullOrWhiteSpace(modifier.displayName) ||
                    float.IsNaN(modifier.proceduralChance) || modifier.proceduralChance < 0 || modifier.proceduralChance > 1 || !ids.Add(modifier.id))
                    throw new InvalidOperationException("Invalid procedural modifier pool");
                modifier.Validate();
                if(modifier.guaranteedSlot>=planetsPerSystem)throw new InvalidOperationException("Modifier guaranteed slot outside system");
            }
        }

        static void ValidateRules(WorldRules rules)
        {
            if(rules!=null && (rules.clusterRings<2 || rules.clusterRings>GameplayGeometry.MaximumSafeRings))throw new InvalidOperationException("Cluster rings exceed the playable geometry");
            if (rules == null || rules.requiredSpheres < 1 || rules.inclusionGoal < 1 || rules.abundanceGoal <= rules.inclusionGoal || rules.rotationDuration <= 0 || float.IsNaN(rules.clustering) || rules.clustering<0 || rules.clustering>1)
                throw new InvalidOperationException("Invalid world rules");
            if(rules.ammoChambers<0 || rules.ammoChambers>AmmoRing.MaximumChambers)
                throw new InvalidOperationException("Ammo chamber count must be between 0 and "+AmmoRing.MaximumChambers);
            if(rules.oneStarScore<0 || rules.twoStarScore<=rules.oneStarScore || rules.threeStarScore<=rules.twoStarScore)
                throw new InvalidOperationException("World star thresholds must increase from one to three stars");
        }
    }
}
