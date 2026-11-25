using UnityEngine;

namespace EcoSphere
{
    // The level's system sun, half-risen from the bottom edge of the screen as a reminder of the sun type.
    // It sits in front of the starfield but behind the cannon; the HUD visualizer draws over it.
    public sealed partial class EcoSpherePrototype
    {
        const float LevelSunDepth = 15;
        const int LevelSunSortingOrder = -50;
        SpriteRenderer levelSun;
        float levelSunScale = 1;

        void BuildLevelSun()
        {
            var catalog = WorldCatalog.Current;
            var system = GameServices.Progress.GetSystem(GameServices.Session.SystemNumber);
            var sun = catalog.Sun(system != null ? system.sunTypeId : null) ?? catalog.Sun(catalog.defaultSunTypeId);
            if (sun == null || !sun.sprite) return;
            levelSun = new GameObject("Level sun (" + sun.id + ")").AddComponent<SpriteRenderer>();
            levelSun.sprite = sun.sprite;
            levelSun.sortingOrder = LevelSunSortingOrder;
            var template = catalog.SunMaterial(sun);
            if (template)
            {
                var material = new Material(template) { name = template.name + " (level)" };
                sun.appearance.Apply(material);
                // Shorten the corona so it reaches the cannon base rather than scaling with this very large sun.
                material.SetFloat("_CoronaExtent", Mathf.Clamp(catalog.levelSunTopBelowCannon /
                    (catalog.levelSunRadius * catalog.levelSunCoronaReach), .1f, 1));
                materials.Add(material);
                levelSun.sharedMaterial = material;
            }
            levelSunScale = sun.displayScale;
            PositionLevelSun();
        }

        void PositionLevelSun()
        {
            if (!levelSun || !view) return;
            var catalog = WorldCatalog.Current;
            // Anchored to the cannon rather than the screen edge, so every screen shape shows the same low arch:
            // the disc top sits below the cannon and the corona (shortened in BuildLevelSun) reaches up to it.
            float top = GameplayGeometry.CannonBase.y - catalog.levelSunTopBelowCannon;
            float radius = catalog.levelSunRadius;
            // _DiscRadius is the disc radius as a fraction of the image's half-width, so the image is
            // radius / _DiscRadius wide on each side. Other types match it through their display scale.
            float discFraction = catalog.animatedSunMaterial ? catalog.animatedSunMaterial.GetFloat("_DiscRadius") : 1;
            float width = radius * 2 / discFraction * levelSunScale;
            levelSun.transform.localScale = Vector3.one * width / levelSun.sprite.bounds.size.x;
            levelSun.transform.position = new Vector3(GameplayGeometry.ClusterCenter.x, top - radius, LevelSunDepth);
        }
    }
}
