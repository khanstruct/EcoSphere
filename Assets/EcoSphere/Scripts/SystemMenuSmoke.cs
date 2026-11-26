using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EcoSphere
{
    // Real pointer input on the System screen's corner buttons: placement, raycasts, click sound and reported id.
    public sealed class SystemMenuSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if (LaunchFlags.Has("-systemMenuSmoke")) new GameObject("System menu checks").AddComponent<SystemMenuSmoke>();
        }
        static void Expect(bool condition, string message) { if (!condition) throw new Exception("System menu: " + message); }

        IEnumerator Start()
        {
            yield return null; yield return null;
            var view = FindAnyObjectByType<SystemView>(); view.DismissIntroductionForSmoke(); yield return null;
            var settings = SystemMenuSettings.Current; var menu = view.CornerButtons;
            Expect(menu && menu.Buttons.Count == settings.entries.Count && settings.entries.Count >= 2, "buttons not built from settings");
            string selected = null; menu.Selected += id => selected = id;
            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Canvas.ForceUpdateCanvases();
            var safe = Screen.safeArea;
            foreach (var entry in settings.entries)
            {
                var button = menu.Buttons[entry.id];
                var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]), max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                Expect(safe.Contains(min) && safe.Contains(max), entry.id + " leaves the safe area");
                bool right = entry.corner == ScreenCorner.TopRight || entry.corner == ScreenCorner.BottomRight;
                bool top = entry.corner == ScreenCorner.TopLeft || entry.corner == ScreenCorner.TopRight;
                Expect((right ? min.x > safe.center.x : max.x < safe.center.x) && (top ? min.y > safe.center.y : max.y < safe.center.y), entry.id + " is not in its corner");
                Vector2 point = (min + max) * .5f;
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                Expect(hits.Count > 0 && hits[0].gameObject == button.gameObject, entry.id + " is blocked from pointer raycasts");
                int clicks = UIClickAudio.PlayCount; selected = null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                Expect(UIClickAudio.PlayCount == clicks + 1, entry.id + " did not play the click sound exactly once");
                Expect(selected == entry.id, entry.id + " did not report its id");
                Expect(view.SelectedPlanet < 0 && FindAnyObjectByType<SystemView>() == view, entry.id + " triggered another action");
            }
            Capture(view, 900, 1600, "SystemMenu-portrait"); Capture(view, 1600, 900, "SystemMenu-wide");
            Debug.Log("ECOSPHERE_SYSTEM_MENU_PASSED: corner buttons from settings, safe-area corners, real pointer clicks, click sound and ids");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static void Capture(SystemView view, int width, int height, string name)
        {
            var camera = Camera.main; var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
            var canvas = view.GetComponent<Canvas>(); var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            view.GetComponent<SystemResponsiveLayout>().Apply(camera); Canvas.ForceUpdateCanvases();
            foreach (var preview in FindObjectsByType<SpherePreview>()) if (preview.PreviewCamera) preview.PreviewCamera.Render();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/" + name + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; canvas.renderMode = mode;
            target.Release(); Destroy(target); Destroy(pixels);
        }
    }
}
