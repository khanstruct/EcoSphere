using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed class ButtonAudioSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (LaunchFlags.Has("-buttonAudioSmoke"))
                new GameObject("Button audio checks").AddComponent<ButtonAudioSmoke>();
        }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return new WaitForSecondsRealtime(.4f);
            var system = FindAnyObjectByType<SystemView>();
            system.DismissIntroductionForSmoke(); yield return null;
            int initialClicks = UIClickAudio.PlayCount;
            system.planets[0].onClick.Invoke();
            var source = UIClickAudio.Source;
            if (!source || !source.clip || source.clip.name != "button-click" || UIClickAudio.PlayCount != initialClicks + 1)
                throw new Exception("Planet click did not play the button sound");
            yield return new WaitForSecondsRealtime(.4f);
            if (UIClickAudio.Source != source) throw new Exception("Click source was lost during scene navigation");
            var prep = FindAnyObjectByType<LevelPrepView>();
            prep.DismissSetupTutorialForSmoke();
            int count = UIClickAudio.PlayCount;
            prep.Begin.onClick.Invoke();
            if (UIClickAudio.PlayCount != count) throw new Exception("Disabled button played a sound");
            foreach (var button in FindObjectsByType<Button>())
                if (!button.GetComponent<ButtonClickSound>()) throw new Exception("Prep button has no click audio: " + button.name);
            GameObject.Find("Back").GetComponent<Button>().onClick.Invoke();
            if (UIClickAudio.PlayCount != count + 1) throw new Exception("Back click did not play exactly once");
            yield return new WaitForSecondsRealtime(.3f);
            if (UIClickAudio.Source != source) throw new Exception("Back navigation replaced click source");
            SceneManager.LoadScene("Tutorial1");
            yield return new WaitForSecondsRealtime(.4f);
            if (Time.timeScale != 0) throw new Exception("Tutorial is not paused for the click test");
            count = UIClickAudio.PlayCount;
            GameObject.Find("Continue").GetComponent<Button>().onClick.Invoke();
            if (UIClickAudio.PlayCount != count + 1 || !source.isPlaying || !source.ignoreListenerPause)
                throw new Exception("Tutorial Continue did not play once while paused");
            if (FindObjectsByType<UIClickAudio>().Length != 1) throw new Exception("Duplicate click audio sources");
            Time.timeScale = 1;
            Debug.Log("ECOSPHERE_BUTTON_AUDIO_SMOKE_PASSED: planet, navigation, prep coverage, disabled silence, paused tutorial, persistent source");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
