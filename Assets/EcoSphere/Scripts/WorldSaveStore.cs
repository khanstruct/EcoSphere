using System;
using System.IO;
using UnityEngine;

namespace EcoSphere
{
    public static class WorldSaveStore
    {
        public static WorldSaveData Load(string path)
        {
            if (!File.Exists(path)) return new WorldSaveData { universeSeed = Guid.NewGuid().GetHashCode() & int.MaxValue };
            var save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(path));
            if (save == null || save.version != 1 || save.systems == null || save.placedPredefinedWorlds == null ||
                save.worlds == null || save.unlockedSpheres == null)
                throw new InvalidDataException("Invalid EcoSphere world save: " + path);
            foreach (var world in save.worlds)
            {
                if (world.discoveries == null) world.discoveries = new System.Collections.Generic.List<int>();
                if (world.discoveryDetails == null) world.discoveryDetails = new System.Collections.Generic.List<DiscoveryRecord>();
            }
            return save;
        }

        public static void Save(string path, WorldSaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
