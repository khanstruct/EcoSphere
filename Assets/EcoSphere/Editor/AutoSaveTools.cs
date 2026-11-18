using System;
using UnityEngine;

namespace EcoSphere.Editor
{
    // Run these checks in separate Unity processes to verify session-only progress.
    public static class AutoSaveTools
    {
        public static void WriteCheck()
        {
            WorldProgress.VisitSystem(4);
            if (WorldProgress.Find(4, 0) != null) throw new Exception("Visiting must not complete a world");
            var result = WorldProgress.Complete(4, 2, .37f, 123, true);
            var world = WorldProgress.Find(4, 2);
            if (result.discoveries.Count != 1 || world == null || WorldProgress.CurrentSystem != 4 ||
                Mathf.Abs(world.seaLevel - .37f) > .001f || Mathf.Abs(world.rotation - 123) > .001f)
                throw new Exception("Session progress or world appearance was lost");
            if (WorldProgress.Complete(4, 2, .37f, 123, true).discoveries.Count != 0)
                throw new Exception("Stone should only unlock once within a session");
            Debug.Log("ECOSPHERE_SESSION_PROGRESS_PASSED");
        }

        public static void ReadCheck()
        {
            if (WorldProgress.CurrentSystem != 1 || WorldProgress.Find(4, 2) != null || WorldProgress.Pending != null)
                throw new Exception("A new session restored old progress");
            if (WorldProgress.Complete(4, 2, .37f, 123, true).discoveries.Count != 1)
                throw new Exception("Stone should be discoverable again in a fresh session");
            Debug.Log("ECOSPHERE_SESSION_RESET_PASSED: fresh system, empty worlds, fresh discoveries, no disk save");
        }
    }
}