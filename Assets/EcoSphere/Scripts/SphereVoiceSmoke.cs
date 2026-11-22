using System;
using System.Collections;
using System.Collections.Generic;
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
                int minimum=audio.Settings.minimumMatchedSpheresForVoice;
                if (audio.AnnounceMatch(types[i], minimum-1)) throw new Exception("Subthreshold match announced");
                if (!audio.AnnounceMatch(types[i], minimum)) throw new Exception("Qualifying match was silent");
                var source = audio.transform.Find("Level result announcer").GetComponent<AudioSource>();
                if (source.clip != definition.resourceVoice) throw new Exception("Wrong sphere match voice");
                Destroy(audio.gameObject);
            }
            int threshold=LevelAudio.SphereVoiceMinimumPoints;
            int releasePoints=Mathf.CeilToInt(threshold/(float)ElementScoring.Breakaway)*ElementScoring.Breakaway;
            var settings=GameAudioSettings.Current;
            var cases=new[]{
                new Dictionary<int,int>{{SphereIds.Fire,threshold-1},{SphereIds.Void,threshold-1}},
                new Dictionary<int,int>{{SphereIds.Fire,threshold},{SphereIds.Void,threshold-1}},
                new Dictionary<int,int>{{SphereIds.Fire,threshold},{SphereIds.Void,threshold}},
                new Dictionary<int,int>{{SphereIds.Fire,releasePoints},{SphereIds.Void,releasePoints}},
                new Dictionary<int,int>{{SphereIds.Mud,threshold*2}}
            };
            AudioClip[] expected={null,SphereCatalog.Current.Get(SphereIds.Fire).resourceVoice,settings.ecosphere,settings.ecosphere,SphereCatalog.Current.Get(SphereIds.Mud).resourceVoice};
            for(int i=0;i<cases.Length;i++)
            {
                var audio=new GameObject("Whole resolution voice check").AddComponent<LevelAudio>();audio.voiceVolume=0;audio.musicVolume=0;audio.Initialize();
                if(audio.AnnounceResolution(cases[i])!=(expected[i]!=null))throw new Exception("Resolution qualification mismatch");
                var source=audio.transform.Find("Level result announcer").GetComponent<AudioSource>();
                if(source.clip!=expected[i])throw new Exception("Multiple sphere voices must select EcoSphere first, without individual playback");
                yield return audio.WaitForAnnouncements();
                if(source.clip!=expected[i])throw new Exception("Individual voice followed EcoSphere");
                Destroy(audio.gameObject);
            }
            Debug.Log("ECOSPHERE_SPHERE_VOICE_SMOKE_PASSED: individual match boundary, whole-shot qualification, EcoSphere replaces two qualifying match/release types, mixed Mud remains one type, actual AudioSource and no queued individual voices");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
