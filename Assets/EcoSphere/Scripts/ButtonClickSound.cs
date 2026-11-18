using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSound : MonoBehaviour
    {
        Button button;

        public static void Attach(Button target)
        {
            if (!target.GetComponent<ButtonClickSound>()) target.gameObject.AddComponent<ButtonClickSound>();
        }

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Play);
        }

        // Keep the original listener order when pooled or modal buttons are shown again.
        // Their action may hide the button or change scenes before later listeners run.
        void OnDestroy() { if (button) button.onClick.RemoveListener(Play); }

        void Play()
        {
            if (button.IsActive() && button.IsInteractable()) UIClickAudio.Play();
        }
    }
}
