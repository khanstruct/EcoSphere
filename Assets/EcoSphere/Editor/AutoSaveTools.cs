using System;
using UnityEngine;

namespace EcoSphere.Editor
{
    // Run these checks in separate Unity processes to verify session-only progress.
    public static class AutoSaveTools
    {
        public static void WriteCheck()
        {
            GameServices.Progress.VisitSystem(4);
            if (GameServices.Progress.Find(4, 0) != null) throw new Exception("Visiting must not complete a world");
            WorldLevels.Get(4,2).naturalSpheres.Add(new NaturalSpherePlacement{sphereId=SphereIds.Stone,cell=Vector2Int.up});
            var result = GameServices.Progress.Complete(4, 2, .37f, 123,new int[]{SphereIds.Stone});
            var world = GameServices.Progress.Find(4, 2);
            if (result.discoveries.Count != 1 || world == null || GameServices.Progress.CurrentSystem != 4 ||
                Mathf.Abs(world.seaLevel - .37f) > .001f || Mathf.Abs(world.rotation - 123) > .001f)
                throw new Exception("Session progress or world appearance was lost");
            if (GameServices.Progress.Complete(4, 2, .37f, 123,new int[]{SphereIds.Stone}).discoveries.Count != 0)
                throw new Exception("Stone should only unlock once within a session");
            var serialized = JsonUtility.ToJson(world);
            var restored = JsonUtility.FromJson<CompletedWorld>(serialized);
            if (restored.discoveries.Count != 1 || restored.discoveries[0] != SphereIds.Stone)
                throw new Exception("Serialized world lost its discovery history");
            Debug.Log("ECOSPHERE_SESSION_PROGRESS_PASSED");
        }

        public static void ReadCheck()
        {
            if (GameServices.Progress.CurrentSystem != 1 || GameServices.Progress.Find(4, 2) != null || GameServices.Progress.Pending != null)
                throw new Exception("A new session restored old progress");
            WorldLevels.Get(4,2).naturalSpheres.Add(new NaturalSpherePlacement{sphereId=SphereIds.Stone,cell=Vector2Int.up});
            if (GameServices.Progress.Complete(4, 2, .37f, 123,new int[]{SphereIds.Stone}).discoveries.Count != 1)
                throw new Exception("Stone should be discoverable again in a fresh session");
            Debug.Log("ECOSPHERE_SESSION_RESET_PASSED: fresh system, empty worlds, fresh discoveries, no disk save");
        }
    }
}