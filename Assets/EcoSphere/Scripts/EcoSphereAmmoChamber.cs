using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoSphere
{
    // Ammo chambers beside the cannon plus the shooter's pointer/keyboard input.
    public sealed partial class EcoSpherePrototype
    {
        AmmoRing ammoRing;
        AmmoChamberStyle chamberStyle;
        Transform chamberRoot;
        readonly List<GameObject> chamberSpheres = new List<GameObject>();
        readonly List<SpriteRenderer> chamberFrames = new List<SpriteRenderer>();
        Coroutine ringTurn;
        Texture2D placeholderChamberTexture;
        // Touch: drag to aim, release to fire, release over the meters (or below the cannon) to cancel.
        bool touchAiming, touchOnChamber, touchCancelled;
        int touchChamber = -1;
        float lastTouchTime = -10;

        public int ChamberCount => ammoRing?.ChamberCount ?? 0;
        public IReadOnlyList<int> ChamberTypes => ammoRing?.Chambers;
        public int LoadedType => loadedType;
        Vector3 LoadedLocal => new Vector3(0, chamberStyle.LoadedLift, -.1f);
        // Style offsets are measured from the cannon's base, which no longer moves with aiming.
        Vector3 CannonBaseWorld => new Vector3(GameplayGeometry.CannonBase.x, GameplayGeometry.CannonBase.y, gun.position.z);

        void BuildChambers()
        {
            chamberStyle = AmmoChamberStyle.Current;
            ammoRing = new AmmoRing(preparedWorld?.rules.ammoChambers ?? 0);
            // Stationary mount at the cannon pivot: holds the cradle and the chambers; it never rotates with aiming.
            chamberRoot = new GameObject("Cannon mount").transform;
            chamberRoot.position = CannonBaseWorld;
            if (chamberStyle.cradle)
            {
                var cradle = SpritePart("Cannon cradle", chamberStyle.cradle, chamberRoot,
                    (Vector3)chamberStyle.CradleCenter + new Vector3(0, 0, chamberStyle.cradleDepth), chamberStyle.cradleWidth, chamberStyle.cradleSortingOrder);
                cannonContacts.Add(new CannonContactPart { transform = cradle.transform, vertices = cradle.sprite.vertices, triangles = cradle.sprite.triangles });
            }
            if (ammoRing.ChamberCount == 0) return;
            Sprite frame = chamberStyle.frame ? chamberStyle.frame : PlaceholderChamber();
            for (int i = 0; i < ammoRing.ChamberCount; i++)
            {
                // Each chamber sits behind the piece before it, so its left connector tucks under that piece's connector.
                var part = SpritePart("Ammo chamber " + (i + 1), frame, chamberRoot,
                    (Vector3)chamberStyle.ChamberOffset(i) + new Vector3(0, 0, -.7f + .02f * i), chamberStyle.frameWidth,
                    chamberStyle.cradleSortingOrder - 1 - i);
                chamberFrames.Add(part);
            }
        }

        Sprite PlaceholderChamber()
        {
            const int size = 128;
            placeholderChamberTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Placeholder chamber" };
            var pixels = new Color32[size * size];
            Color32 ring = chamberStyle.placeholderColor;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size * .5f + .5f, y - size * .5f + .5f).magnitude / (size * .5f);
                pixels[y * size + x] = d > .62f && d < .98f ? ring : new Color32(0, 0, 0, 0);
            }
            placeholderChamberTexture.SetPixels32(pixels); placeholderChamberTexture.Apply();
            var sprite = Sprite.Create(placeholderChamberTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            generatedSprites.Add(sprite);
            return sprite;
        }

        bool TryDrawAmmo(out int type)
        {
            if (StoneTutorial && !LaserTesting && !stoneReleased) { type = TutorialOuterType; return true; }
            return shotSelector.TryNext(ElementTypes(), out type);
        }

        // Replaces held types that left the board, then moves the next type into the cannon.
        bool TryLoadFromRing(out int type)
        {
            if (ammoRing.ChamberCount > 0)
            {
                var remaining = new HashSet<int>(ElementTypes());
                foreach (int i in ammoRing.ReplaceWhere(t => Definition(t).AmmoEligible && !remaining.Contains(t), TryDrawAmmo))
                {
                    if (i < chamberSpheres.Count && chamberSpheres[i]) Destroy(chamberSpheres[i]);
                    if (i < chamberSpheres.Count) chamberSpheres[i] = null;
                }
            }
            if (!ammoRing.TryAdvance(TryDrawAmmo, out type)) return false;
            return true;
        }

        // Called after `loaded` holds the new cannon sphere: shifts chamber visuals and spawns missing ones.
        void AfterLoad(bool advanced)
        {
            if (ammoRing.ChamberCount == 0) return;
            ammoRing.Fill(TryDrawAmmo);
            while (chamberSpheres.Count < ammoRing.ChamberCount) chamberSpheres.Add(null);
            var order = new List<GameObject> { loaded };
            if (advanced)
            {
                // The first chamber's sphere becomes the cannon sphere; reuse it so it visibly slides in.
                if (chamberSpheres[0]) { Destroy(loaded); order[0] = chamberSpheres[0]; order[0].name = "Loaded " + Definition(loadedType).displayName; }
                for (int i = 1; i < ammoRing.ChamberCount; i++) order.Add(chamberSpheres[i]);
                order.Add(null);
            }
            else order.AddRange(chamberSpheres);
            ArrangeRing(order);
        }

        bool CanSwap => ready && loaded && !armBusy && !tutorialPaused && !complete && !failed && ammoRing != null && ammoRing.ChamberCount > 0;

        public bool TrySwap()
        {
            if (!CanSwap) return false;
            loadedType = ammoRing.Rotate(loadedType);
            var order = new List<GameObject>();
            for (int i = 0; i < ammoRing.ChamberCount; i++) order.Add(chamberSpheres[i]);
            order.Add(loaded);
            ArrangeRing(order);
            if (levelAudio) levelAudio.AmmoSwap();
            return true;
        }

        // order[0] is the cannon, order[1..] the chambers. Null entries are spawned from the ring's data.
        void ArrangeRing(List<GameObject> order)
        {
            FinishRingTurn();
            for (int slot = 0; slot < order.Count; slot++)
            {
                int type = slot == 0 ? loadedType : ammoRing.Chambers[slot - 1];
                if (!order[slot])
                {
                    order[slot] = MakeSphere("Chamber " + Definition(type).displayName, type, slot == 0 ? gun : chamberRoot,
                        slot == 0 ? LoadedLocal : SlotLocal(slot));
                    order[slot].transform.localScale = Vector3.zero;
                }
                order[slot].transform.SetParent(slot == 0 ? gun : chamberRoot, true);
            }
            loaded = order[0];
            chamberSpheres.Clear();
            for (int i = 1; i < order.Count; i++) chamberSpheres.Add(order[i]);
            ringTurn = StartCoroutine(TurnRing(order));
        }

        Vector3 SlotLocal(int slot) => slot == 0 ? LoadedLocal : (Vector3)chamberStyle.ChamberOffset(slot - 1) + new Vector3(0, 0, -.1f);
        float SlotScale(int slot) => slot == 0 ? 1 : chamberStyle.sphereScale;

        IEnumerator TurnRing(List<GameObject> order)
        {
            var from = new Vector3[order.Count]; var scale = new float[order.Count];
            for (int i = 0; i < order.Count; i++) { from[i] = order[i].transform.localPosition; scale[i] = order[i].transform.localScale.x; }
            for (float t = 0; t < chamberStyle.turnDuration; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / chamberStyle.turnDuration);
                for (int i = 0; i < order.Count; i++) if (order[i])
                {
                    order[i].transform.localPosition = Vector3.Lerp(from[i], SlotLocal(i), p);
                    order[i].transform.localScale = Vector3.one * Mathf.Lerp(scale[i], SlotScale(i), p);
                }
                yield return null;
            }
            ringTurn = null;
            SnapRing();
        }

        void SnapRing()
        {
            if (loaded) { loaded.transform.localPosition = LoadedLocal; loaded.transform.localScale = Vector3.one; }
            for (int i = 0; i < chamberSpheres.Count; i++) if (chamberSpheres[i])
            {
                chamberSpheres[i].transform.localPosition = SlotLocal(i + 1);
                chamberSpheres[i].transform.localScale = Vector3.one * chamberStyle.sphereScale;
            }
        }

        // Firing or a new turn always starts from settled positions.
        void FinishRingTurn()
        {
            if (ringTurn == null) return;
            StopCoroutine(ringTurn); ringTurn = null; SnapRing();
        }

        int ChamberAt(Vector2 world)
        {
            if (chamberRoot == null) return -1;
            for (int i = 0; i < chamberFrames.Count; i++)
                if (((Vector2)chamberFrames[i].transform.position - world).magnitude <= chamberStyle.frameWidth * .5f) return i;
            return -1;
        }

        Vector2 PointerWorld(Vector2 screen) => view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 25));

        void AimAt(Vector2 target)
        {
            Vector2 delta = target - (Vector2)gun.position;
            float angle = Mathf.Clamp(Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, -chamberStyle.maxAimAngle, chamberStyle.maxAimAngle);
            gun.rotation = Quaternion.Euler(0, 0, -angle);
            direction = gun.up;
        }

        bool AboveCannon(Vector2 target) => target.y > GameplayGeometry.CannonBase.y + .15f;
        bool InCancelZone(Vector2 screen) => !AboveCannon(PointerWorld(screen)) ||
            (resourcePanel && RectTransformUtility.RectangleContainsScreenPoint(resourcePanel, screen, view));

        void HandleShooterInput()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TrySwap();
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
            {
                lastTouchTime = Time.unscaledTime;
                HandleTouch(touch.position.ReadValue(), touch.press.wasPressedThisFrame, touch.press.wasReleasedThisFrame);
                return;
            }
            // Touch screens may also synthesize mouse events; ignore them briefly after a touch.
            if (Mouse.current == null || Time.unscaledTime - lastTouchTime < .5f) return;
            Vector2 pointer = Mouse.current.position.ReadValue();
            Vector2 target = PointerWorld(pointer);
            bool pressed = Mouse.current.leftButton.wasPressedThisFrame;
            int chamber = ChamberAt(target);
            if (chamber >= 0) { if (pressed) TrySwap(); return; }
            AimAt(target);
            if (pressed && playableScreen.Contains(pointer) && AboveCannon(target) &&
                Mathf.Abs(target.x) <= 4.65f && target.y <= 7.8f) Fire();
        }

        void HandleTouch(Vector2 screen, bool began, bool released)
        {
            Vector2 target = PointerWorld(screen);
            if (began)
            {
                touchChamber = ChamberAt(target);
                touchOnChamber = touchChamber >= 0;
                touchAiming = !touchOnChamber && playableScreen.Contains(screen);
            }
            if (touchOnChamber)
            {
                if (released) { if (ChamberAt(target) == touchChamber) TrySwap(); touchOnChamber = false; }
                return;
            }
            if (!touchAiming) return;
            touchCancelled = InCancelZone(screen);
            if (!touchCancelled) AimAt(target);
            if (released)
            {
                touchAiming = false;
                if (!touchCancelled) Fire();
                touchCancelled = false;
            }
        }

        string ShooterInstructions()
        {
            bool chambers = ChamberCount > 0;
            return Application.isMobilePlatform
                ? "Drag to aim · Release to fire · Drag onto the meters to cancel" + (chambers ? " · Tap the chamber to swap" : "")
                : "Mouse to aim · Click to fire" + (chambers ? " · Space or click the chamber to swap" : "") + " · R to restart";
        }
    }
}
