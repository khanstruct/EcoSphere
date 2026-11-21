using System;
using System.Collections;
using UnityEngine;

namespace EcoSphere
{
    public sealed class SphereVoiceSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-sphereVoiceSmoke") >= 0)
                new GameObject("Sphere voice checks").AddComponent<SphereVoiceSmoke>();
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.3f);
            int[] types = { SphereIds.Fire, SphereIds.Void, SphereIds.Ice, SphereIds.Sunlight, SphereIds.Stone };
            string[] clips = { "pyrosphere", "shadowsphere", "cryosphere", "heliosphere", "lithosphere" };
            for (int i = 0; i < types.Length; i++)
            {
                var definition = SphereCatalog.Current.Get(types[i]);
                if (!definition.resourceVoice || definition.resourceVoice.name != clips[i])
                    throw new Exception("Sphere voice assignment: " + definition.displayName);
                var audio = new GameObject("Voice check " + definition.displayName).AddComponent<LevelAudio>();
                audio.voiceVolume = 0; audio.musicVolume = 0; audio.Initialize();
                if (audio.AnnounceMatch(types[i], 24)) throw new Exception("Subthreshold match announced");
                if (!audio.AnnounceMatch(types[i], 25)) throw new Exception("Qualifying match was silent");
                var source = audio.transform.Find("Level result announcer").GetComponent<AudioSource>();
                if (source.clip != definition.resourceVoice) throw new Exception("Wrong sphere match voice");
                Destroy(audio.gameObject);
            }
            Debug.Log("ECOSPHERE_SPHERE_VOICE_SMOKE_PASSED: five authored assignments, 500-point threshold, actual AudioSource clip selection");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
