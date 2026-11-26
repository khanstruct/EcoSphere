using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public enum ScreenCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    // One icon button pinned to a corner of the System screen. The id names the destination it will open.
    [Serializable] public sealed class SystemMenuEntry
    {
        public string id, label;
        public Sprite icon;
        public ScreenCorner corner;
    }

    [CreateAssetMenu(menuName = "EcoSphere/System Menu Settings")]
    public sealed class SystemMenuSettings : ScriptableObject
    {
        [Tooltip("Button size in layout units (the reference canvas is 900 x 1600 portrait, 1600 x 900 wide).")]
        [Min(40)] public float buttonSize = 110;
        [Tooltip("Distance from the safe-area edges to the button's outer edge, in layout units.")]
        public Vector2 margin = new Vector2(30, 30);
        [Tooltip("Spacing between buttons that share a corner; they stack inward along the top or bottom edge.")]
        [Min(0)] public float spacing = 20;
        public Color normalColor = Color.white, highlightedColor = new Color(1, .9f, .65f), pressedColor = new Color(.8f, .7f, .5f);
        public List<SystemMenuEntry> entries = new List<SystemMenuEntry>();

        static SystemMenuSettings current;
        public static SystemMenuSettings Current
        {
            get
            {
                if (!current) current = Resources.Load<SystemMenuSettings>("SystemMenuSettings");
                if (!current) throw new InvalidOperationException("Missing Resources/SystemMenuSettings asset");
                return current;
            }
        }

        public void Validate()
        {
            var ids = new HashSet<string>();
            foreach (var entry in entries)
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id) || string.IsNullOrWhiteSpace(entry.label) || !entry.icon ||
                    !Enum.IsDefined(typeof(ScreenCorner), entry.corner))
                    throw new InvalidOperationException("System menu entries need unique ids, labels, icons and a valid corner");
                else if (Mathf.Max(entry.icon.texture.width, entry.icon.texture.height) > 256)
                    throw new InvalidOperationException("System menu icon exceeds 256px: " + entry.id);
        }
    }
}
