using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EcoSphere
{
    public sealed class MenuMusicSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-menuMusicSmoke")>=0)
                new GameObject("Menu music checks").AddComponent<MenuMusicSmoke>();
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return new WaitForSecondsRealtime(.5f);
            FindAnyObjectByType<SystemView>().DismissIntroductionForSmoke(); yield return null;
            var music=FindAnyObjectByType<MenuMusic>();
            if(!music || !music.Source.isPlaying || !music.Source.loop)throw new System.Exception("Menu music not looping");
            var originalSource=music.Source;
            music.Source.timeSamples=Mathf.Min(music.Source.clip.samples/2,music.Source.clip.frequency*3);
            int before=music.Source.timeSamples;
            FindAnyObjectByType<SystemView>().OpenPlanet(0);
            yield return new WaitForSecondsRealtime(.4f);
            FindAnyObjectByType<LevelPrepView>()?.DismissSetupTutorialForSmoke();
            if(!music || FindObjectsByType<MenuMusic>(FindObjectsSortMode.None).Length!=1 || music.Source!=originalSource || music.Source.timeSamples<before)
                throw new System.Exception("Music restarted or duplicated entering prep");
            before=music.Source.timeSamples;
            GameObject.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSeconds(.4f);
            if(!music || FindObjectsByType<MenuMusic>(FindObjectsSortMode.None).Length!=1 || music.Source!=originalSource || music.Source.timeSamples<before)
                throw new System.Exception("Music restarted or duplicated returning to System");
            SceneManager.LoadScene("FirstLight");
            yield return null; yield return null;
            if(FindAnyObjectByType<MenuMusic>())throw new System.Exception("Menu music leaked into gameplay");
            SceneManager.LoadScene("LevelPrep");
            yield return new WaitForSeconds(.4f);
            music=FindAnyObjectByType<MenuMusic>();
            if(!music || !music.Source.isPlaying)throw new System.Exception("Direct prep entry did not start music");
            Debug.Log("ECOSPHERE_MENU_MUSIC_PASSED: same source and playback position across System/Prep/Back, no duplicates, gameplay cleanup, direct prep entry.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
            #endif
        }
    }
}
