using System;
using System.Collections;
using UnityEngine;

namespace EcoSphere
{
    public sealed class SharedAudioSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-sharedAudioSmoke") >= 0)
                new GameObject("Shared gameplay audio checks").AddComponent<SharedAudioSmoke>();
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.3f);
            var root = new GameObject("Unconfigured future level");
            var gameplay = root.AddComponent<EcoSpherePrototype>(); gameplay.enabled = false;
            var audio = root.GetComponent<LevelAudio>();
            if (!audio || root.GetComponents<LevelAudio>().Length != 1)
                throw new Exception("Gameplay did not automatically receive a single shared audio component");
            audio.musicVolume = 0; audio.Initialize();
            if (audio.Settings != GameAudioSettings.Current || audio.miss != GameAudioSettings.Current.miss || audio.miss.name != "miss")
                throw new Exception("Unconfigured level did not receive the global miss clip");
            var effects = root.transform.Find("Shot and ricochet effects").GetComponent<AudioSource>();
            int sources = root.GetComponentsInChildren<AudioSource>().Length;
            audio.Initialize();
            if (root.GetComponentsInChildren<AudioSource>().Length != sources || effects.isPlaying)
                throw new Exception("Shared audio initialization duplicated sources or played an unsolicited effect");
            audio.Miss();
            if (!effects.isPlaying || audio.IsAnnouncing)
                throw new Exception("Global miss effect did not play independently of the announcer");
            Destroy(root);
            Debug.Log("ECOSPHERE_SHARED_AUDIO_SMOKE_PASSED: bare gameplay object receives shared defaults, no per-level clip assignment, idempotent initialization and actual miss-effect playback");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
