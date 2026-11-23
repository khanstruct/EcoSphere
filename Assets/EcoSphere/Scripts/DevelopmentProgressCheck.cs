using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class DevelopmentProgressCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            // Deliberately not a "Smoke" flag: exercise normal runtime persistence policy.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-developmentProgressCheck") >= 0)
                new GameObject("Development progress checks").AddComponent<DevelopmentProgressCheck>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return null; yield return null;
            FindAnyObjectByType<SystemView>().DismissIntroductionForSmoke(); yield return null;
            if (WorldCatalog.Current.persistProgressBetweenSessions || GameServices.Progress.PersistenceEnabled)
                throw new Exception("Development session enabled cross-session persistence");
            byte[] diskBefore = File.Exists(GameServices.Progress.SavePath) ? File.ReadAllBytes(GameServices.Progress.SavePath) : null;
            if (GameServices.Progress.Find(1,0) != null || GameServices.Progress.IsSphereAvailable(SphereIds.Sunlight))
                throw new Exception("Development session restored previous progress");
            var firstSystem = GameServices.Progress.GetSystem(1);
            var gains = new double[Enum.GetValues(typeof(ElementId)).Length];
            gains[(int)ElementId.Fire] = gains[(int)ElementId.Void] = 620;
            var completed = GameServices.Progress.Complete(1,0,.43f,123,null,gains).world;
            if (!GameServices.Progress.IsSphereAvailable(SphereIds.Sunlight)) throw new Exception("Session unlock did not survive completion");
            GameServices.Progress.ClearPending();
            var system = FindAnyObjectByType<SystemView>();
            system.SetSystemNumber(1); system.OpenPlanet(0);
            yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f);
            var results = FindAnyObjectByType<ResultsView>();
            if (SceneManager.GetActiveScene().name != "Results" || !results || !results.Settled || !results.ContinueButton.interactable ||
                results.OkButton || results.DiscoveryPlayCount != 0 || FindAnyObjectByType<LevelPrepView>() || FindAnyObjectByType<EcoSpherePrototype>())
                throw new Exception("Completed world did not open settled results directly");
            if (GameServices.Progress.Pending.world != completed || GameServices.Progress.Pending.discoveries.Count != 0)
                throw new Exception("Viewing results lost its outcome or replayed discoveries");
            var duplicate = GameServices.Progress.Complete(1,0,.9f,900,new int[]{SphereIds.Stone},gains);
            if (duplicate.world != completed || duplicate.world.seaLevel != .43f || duplicate.world.rotation != 123 ||
                duplicate.discoveries.Count != 0 || GameServices.Progress.IsSphereAvailable(SphereIds.Stone))
                throw new Exception("Duplicate completion changed a finished world or granted rewards");
            results.ContinueButton.onClick.Invoke(); yield return null; yield return null;
            system = FindAnyObjectByType<SystemView>();
            if (!system || system.systemNumber != 1) throw new Exception("Completed-world results did not return to its system");
            system.OpenPlanet(1); yield return null; yield return null;
            var prep = FindAnyObjectByType<LevelPrepView>();
            if (SceneManager.GetActiveScene().name != "LevelPrep" || !prep || prep.Title.text != "Genesis Beta")
                throw new Exception("Uncompleted world did not retain its preparation route");
            GameServices.Progress.ResetSession(); // Same lifecycle reset used on the next Play/game session.
            if (GameServices.Progress.Find(1,0) != null || GameServices.Progress.Pending != null || GameServices.Progress.CurrentSystem != 1 ||
                GameServices.Progress.IsSphereAvailable(SphereIds.Sunlight) || GameServices.Progress.SystemIntroductionSeen || GameServices.Progress.TryOpenCompletedResults(1,0) ||
                ReferenceEquals(firstSystem,GameServices.Progress.GetSystem(1)))
                throw new Exception("Session restart retained completed worlds, unlocks or generated systems");
            byte[] diskAfter = File.Exists(GameServices.Progress.SavePath) ? File.ReadAllBytes(GameServices.Progress.SavePath) : null;
            if ((diskBefore == null) != (diskAfter == null) || (diskBefore != null && !Equal(diskBefore,diskAfter)))
                throw new Exception("Development session modified a disk save");
            Debug.Log("ECOSPHERE_DEVELOPMENT_PROGRESS_CHECK_PASSED: normal runtime persistence disabled, completed-world results without replay/rewards, unfinished-world setup, fresh session reset, disk save untouched");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static bool Equal(byte[] first, byte[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }
    }
}
