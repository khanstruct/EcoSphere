namespace EcoSphere
{
    public static class WorldLevels
    {
        public const string ProceduralSceneName="Procedural";
        public static WorldRecord Get(int system, int planet)
        {
            var generated = GameServices.Progress.GetSystem(system);
            if (planet < 0 || planet >= generated.worlds.Count) throw new System.ArgumentOutOfRangeException(nameof(planet));
            return generated.worlds[planet];
        }

        public static string Name(int system, int planet) => Get(system, planet).displayName;

        public static string GameplayScene(int system, int planet) => Get(system, planet).GameplayScene;

        public static string SceneName(string scene)
        {
            foreach (var world in WorldCatalog.Current.predefinedWorlds)
                if (world.gameplayScene == scene) return world.displayName;
            return scene;
        }
    }
}
