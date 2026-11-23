using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EcoSphere
{
    public static class UIInput
    {
        public static EventSystem EnsureEventSystem()
        {
            var existing=Object.FindAnyObjectByType<EventSystem>();
            return existing ? existing : new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
        }
    }
}
