using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed class SystemView : MonoBehaviour
    {
        public Button[] planets;
        public Text selection;
        public Color orbitColor = new Color(1, .76f, .24f);
        public int SelectedPlanet { get; private set; } = -1;

        void Awake()
        {
            for (int i = 0; i < planets.Length; i++)
            {
                int index = i;
                planets[i].onClick.AddListener(() => SelectPlanet(index));
            }
        }

        public void SelectPlanet(int index)
        {
            if (index < 0 || index >= planets.Length) return;
            SelectedPlanet = index;
            for (int i = 0; i < planets.Length; i++)
            {
                var ring = planets[i].GetComponent<SystemMapGraphic>();
                ring.color = i == index ? Color.white : orbitColor;
                ring.thickness = i == index ? 5 : 2.5f;
                ring.SetVerticesDirty();
            }
            selection.text = "Planet " + (index + 1) + " selected";
        }
    }
}
