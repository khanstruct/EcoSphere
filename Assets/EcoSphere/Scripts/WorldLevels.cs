namespace EcoSphere
{
    public static class WorldLevels
    {
        public static string Name(int system, int planet) =>
            system == 1 && planet == 0 ? "Tutorial 1" : "Planet " + (planet + 1);

        public static string GameplayScene(int system, int planet) =>
            system == 1 && planet == 0 ? "Tutorial1" : null;
    }
}
