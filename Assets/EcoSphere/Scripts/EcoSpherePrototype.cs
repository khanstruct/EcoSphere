using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    public sealed class EcoSpherePrototype : MonoBehaviour
    {
        [Header("Approved artwork")]
        public Texture2D iceSurface, sunlightSurface, coreSurface;
        public Sprite iceIcon, sunlightIcon, coreHalo, leftArm, rightArm, foreground;
        [Header("Space backdrop")]
        public Sprite starfield, twinkleStar;
        [Header("Element RGB colors")]
        public Color iceColor = new Color32(55, 198, 255, 255);
        public Color sunlightColor = new Color32(255, 194, 40, 255);
        [Header("Turn settings")]
        public float shotSpeed = 13;
        public float rotationDegrees = 24;
        public float rotationDuration = .65f;
        [Header("Level resource target")]
        [Min(1)] public int resourceGoal = 15;
        public const float VisualScale = .6f;
        public const float Radius = .44f * VisualScale;
        const int InitialRings = 4;
        const float Left = -4.35f, Right = 4.35f, Top = 6.5f, Bottom = -5.9f;
        readonly Vector2 center = new Vector2(0, 1.6f);
        readonly BoardRules board = new BoardRules();
        readonly ShotSelector shotSelector = new ShotSelector();
        readonly Dictionary<Vector2Int, GameObject> visuals = new Dictionary<Vector2Int, GameObject>();
        readonly List<Vector2> aimObstacles = new List<Vector2>();
        readonly int[] totals = new int[2];
        readonly float[] shown = new float[2];
        readonly List<Material> materials = new List<Material>();
        readonly List<Sprite> generatedSprites = new List<Sprite>();
        Transform cluster, gun, gunArtwork, leftHinge, rightHinge, halo, planet;
        Camera view;
        SpaceBackground spaceBackground;
        LevelAudio levelAudio;
        GameObject loaded, projectile;
        Material iceMaterial, sunlightMaterial, coreMaterial, lineMaterial;
        LineRenderer aim;
        LineRenderer cannonBacking;
        Vector2 direction = Vector2.up, projectileDirection;
        int loadedType, matchedCount, shots, lost, lastMatch, lastRelease, ricochets;
        bool ready, complete, failed, armBusy;
        int shotRicochets;
        bool shotHasElementCallout;
        sealed class CannonContactPart
        {
            public Transform transform;
            public Vector2[] vertices;
            public ushort[] triangles;
        }
        readonly List<CannonContactPart> cannonContacts = new List<CannonContactPart>();
        float spinAngle;
        Text status, detail, iceLabel, sunLabel;
        RectTransform goalLine;
        readonly List<RectTransform>[] bars = { new List<RectTransform>(), new List<RectTransform>() };
        readonly List<Image>[] barImages = { new List<Image>(), new List<Image>() };
        Font font;
        bool smoke;
        public int CellCount => board.Cells.Count;

        void Start()
        {
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-ecosphereSmoke") >= 0;
            BuildWorld();
            levelAudio = GetComponent<LevelAudio>();
            if (levelAudio) levelAudio.Initialize();
            BuildInterface();
            BuildBoard();
            LoadNext();
            if (smoke) StartCoroutine(SmokeRun());
        }

        Material Surface(Texture2D texture, bool unlit = false)
        {
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = texture ? texture.name + " material" : "Planet material";
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            if (!unlit) { material.SetFloat("_Smoothness", .35f); material.SetFloat("_Metallic", .05f); }
            materials.Add(material); return material;
        }

        LineRenderer Line(string name, Transform parent, Color color, float width, int order = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial; line.useWorldSpace = false;
            line.startColor = line.endColor = color; line.startWidth = line.endWidth = width;
            line.numCapVertices = 6; line.sortingOrder = order;
            return line;
        }

        void ApplySurface(Renderer renderer, Material material, Texture2D texture, Color color)
        {
            renderer.sharedMaterial = material;
            // Explicit per-renderer data also keeps dynamic texture selection independent of SRP batching.
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_BaseMap", texture ? texture : Texture2D.whiteTexture);
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        SpriteRenderer SpritePart(string name, Sprite sprite, Transform parent, Vector3 position,
            float width, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (sprite) go.transform.localScale = Vector3.one * width / sprite.bounds.size.x;
            return renderer;
        }

        void BuildWorld()
        {
            view = new GameObject("Portrait Camera").AddComponent<Camera>();
            view.tag = "MainCamera"; view.orthographic = true;
            view.gameObject.AddComponent<AudioListener>();
            view.transform.position = new Vector3(0, 0, -25);
            view.backgroundColor = new Color(.018f, .028f, .065f); view.clearFlags = CameraClearFlags.SolidColor;
            view.nearClipPlane = .1f; view.farClipPlane = 60;
            FitCamera();
            spaceBackground = new GameObject("Space backdrop").AddComponent<SpaceBackground>();
            spaceBackground.Initialize(view, starfield, twinkleStar);
            var light = new GameObject("Upper-left soft light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .5f, .6f);
            lineMaterial = new Material(Shader.Find("Sprites/Default")); materials.Add(lineMaterial);
            iceMaterial = Surface(iceSurface); sunlightMaterial = Surface(sunlightSurface);
            coreMaterial = Surface(coreSurface, true);
            cluster = new GameObject("Rotating hex cluster").transform; cluster.position = center;
            MakeSphere("Black hole core", -1, cluster, Vector3.zero);
            halo = SpritePart("Pulsing core halo", coreHalo, cluster, new Vector3(0, 0, .6f), 1.8f * VisualScale, -1).transform;
            // A modest placeholder planet: the later ground-view reveal is outside this test level.
            var globe = GameObject.CreatePrimitive(PrimitiveType.Sphere); globe.name = "Receiving planet";
            globe.transform.position = new Vector3(0, 11.5f, 3); globe.transform.localScale = Vector3.one * 10;
            Destroy(globe.GetComponent<Collider>()); planet = globe.transform;
            var pm = Surface(null); pm.SetColor("_BaseColor", new Color(.09f, .2f, .34f));
            ApplySurface(globe.GetComponent<Renderer>(), pm, null, new Color(.09f, .2f, .34f));
            foreach (float x in new[] { Left, Right })
            {
                var boundary = Line("Ricochet boundary", transform, new Color(.2f, .38f, .52f, .65f), .025f);
                boundary.positionCount = 2;
                boundary.SetPosition(0, new Vector3(x, Bottom, 1)); boundary.SetPosition(1, new Vector3(x, Top, 1));
            }
            var ceiling = Line("Top ricochet boundary", transform, new Color(.2f, .38f, .52f, .65f), .025f);
            ceiling.positionCount = 2; ceiling.SetPosition(0, new Vector3(Left, Top, 1)); ceiling.SetPosition(1, new Vector3(Right, Top, 1));
            BuildGun();
        }

        void FitCamera()
        {
            // Keep a 9 x 16 playfield completely visible, including in a landscape editor Game view.
            view.orthographicSize = Mathf.Max(8, 4.65f / Mathf.Max(.1f, view.aspect));
        }

        void BuildGun()
        {
            gun = new GameObject("Aiming cannon").transform; gun.position = new Vector3(0, -5.45f, -1);
            gunArtwork = new GameObject("Scaled cannon artwork").transform;
            gunArtwork.SetParent(gun, false); gunArtwork.localScale = Vector3.one * VisualScale;
            var backing = Line("Rigid interior backing", gunArtwork, new Color(.035f, .045f, .06f), .29f * VisualScale, 2);
            cannonBacking = backing;
            backing.positionCount = 33;
            for (int i = 0; i < 33; i++)
            {
                float a = (190 + i * 160f / 32) * Mathf.Deg2Rad;
                backing.SetPosition(i, new Vector3(Mathf.Cos(a) * .87f, .55f + Mathf.Sin(a) * .78f, .3f));
            }
            leftHinge = MakeArm("Left arm hinge", leftArm, new Vector3(-.82f, .05f, -.2f), new Vector2(.31f, .074f));
            rightHinge = MakeArm("Right arm hinge", rightArm, new Vector3(.82f, .05f, -.2f), new Vector2(.69f, .074f));
            SpritePart("Foreground covers hinges and sphere", foreground, gunArtwork, new Vector3(0, -.27f, -.7f), 2.38f, 7);
            foreach (var part in gunArtwork.GetComponentsInChildren<SpriteRenderer>())
                if (part.sprite) cannonContacts.Add(new CannonContactPart {
                    transform = part.transform, vertices = part.sprite.vertices, triangles = part.sprite.triangles });
            aim = Line("Loaded element aiming line", transform, iceColor, .03f, 9);
            aim.positionCount = 2;
        }

        Transform MakeArm(string name, Sprite source, Vector3 position, Vector2 pivot)
        {
            var hinge = new GameObject(name).transform; hinge.SetParent(gunArtwork, false); hinge.localPosition = position;
            if (source)
            {
                var adjusted = Sprite.Create(source.texture, source.rect, pivot, source.pixelsPerUnit);
                generatedSprites.Add(adjusted);
                var arm = SpritePart(name + " artwork", adjusted, hinge, Vector3.zero, .84f, 5);
                // Arms pivot about the circular joint; sprite remains behind front shell.
                arm.transform.localScale = Vector3.one * 1.72f / adjusted.bounds.size.y;
            }
            return hinge;
        }

        GameObject MakeSphere(string name, int type, Transform parent, Vector3 local)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = local;
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere); mesh.name = "Rotating surface";
            mesh.transform.SetParent(root.transform, false); mesh.transform.localScale = Vector3.one * Radius * 2;
            Destroy(mesh.GetComponent<Collider>());
            ApplySurface(mesh.GetComponent<Renderer>(), type < 0 ? coreMaterial : type == 0 ? iceMaterial : sunlightMaterial,
                type < 0 ? coreSurface : type == 0 ? iceSurface : sunlightSurface, Color.white);
            var appearance = root.AddComponent<SphereAppearance>(); appearance.surface = mesh.transform;
            appearance.spin = type < 0 ? 8 : 12;
            if (type >= 0)
                appearance.icon = SpritePart("Camera-facing type icon", type == 0 ? iceIcon : sunlightIcon,
                    root.transform, new Vector3(0, 0, -.47f * VisualScale), .5f * VisualScale, 4).transform;
            return root;
        }

        void BuildBoard()
        {
            // Same-type groups intentionally exist at startup. Only the fired sphere seeds matching.
            for (int q = -InitialRings; q <= InitialRings; q++) for (int r = -InitialRings; r <= InitialRings; r++)
            {
                if (Mathf.Abs(q + r) > InitialRings || (q == 0 && r == 0)) continue;
                var cell = new Vector2Int(q, r);
                Vector2 p = BoardRules.Position(cell);
                float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                int sector = Mathf.FloorToInt((angle + 180) / 60);
                int type = sector % 2;
                board.Cells.Add(cell, type);
                visuals.Add(cell, MakeSphere((type == 0 ? "Ice " : "Sunlight ") + cell, type, cluster, p));
            }
        }

        void LoadNext()
        {
            if (complete || failed) return;
            if (!shotSelector.TryNext(board.Cells.Values, out loadedType)) return;
            loaded = MakeSphere("Loaded " + (loadedType == 0 ? "Ice" : "Sunlight"), loadedType, gun,
                new Vector3(0, .57f * VisualScale, -.1f));
            ready = true; status.text = "AIM · CLICK TO FIRE";
        }

        void Update()
        {
            FitCamera();
            if (halo) halo.localScale = Vector3.one * (1.8f * VisualScale / coreHalo.bounds.size.x) * (1 + .035f * Mathf.Sin(Time.time * 2));
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !smoke)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (ready && !smoke && Mouse.current != null)
            {
                Vector2 target = view.ScreenToWorldPoint(new Vector3(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 25));
                Vector2 delta = target - (Vector2)gun.position;
                float angle = Mathf.Clamp(Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, -75, 75);
                gun.rotation = Quaternion.Euler(0, 0, -angle);
                direction = gun.up;
                if (Mouse.current.leftButton.wasPressedThisFrame && target.y > gun.position.y + .15f &&
                    Mathf.Abs(target.x) <= 4.65f && target.y <= 7.8f) Fire();
            }
            aim.enabled = ready && loaded;
            if (aim.enabled)
            {
                Vector2 muzzle = (Vector2)loaded.transform.position + direction * (.55f * VisualScale);
                aimObstacles.Clear();
                aimObstacles.Add(center);
                foreach (var cell in board.Cells.Keys)
                    aimObstacles.Add(cluster.TransformPoint(BoardRules.Position(cell)));
                var path = BoardRules.AimPath(muzzle, direction, (Right - Left) * .75f,
                    Left + Radius, Right - Radius, Top - Radius, Bottom, aimObstacles, Radius * 2);
                aim.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++) aim.SetPosition(i, new Vector3(path[i].x, path[i].y, -2));
                aim.startColor = aim.endColor = ColorFor(loadedType);
            }
            if (projectile) AdvanceProjectile(Mathf.Min(Time.deltaTime, .1f) * shotSpeed);
            UpdateVisualizer();
        }

        Color ColorFor(int type) => type == 0 ? iceColor : sunlightColor;

        void Fire()
        {
            if (!ready || !loaded || armBusy) return;
            ready = false; shots++;
            shotRicochets = 0; shotHasElementCallout = false;
            if (levelAudio) levelAudio.Shoot();
            projectile = loaded; loaded = null;
            projectile.transform.SetParent(transform, true);
            projectileDirection = direction.normalized;
            status.text = "SHOT IN FLIGHT";
            StartCoroutine(ReleaseArms());
        }

        IEnumerator ReleaseArms()
        {
            armBusy = true;
            for (float t = 0; t < .22f; t += Time.deltaTime)
            {
                float open = Mathf.Sin(Mathf.Clamp01(t / .22f) * Mathf.PI) * 11;
                leftHinge.localRotation = Quaternion.Euler(0, 0, open);
                rightHinge.localRotation = Quaternion.Euler(0, 0, -open);
                yield return null;
            }
            leftHinge.localRotation = rightHinge.localRotation = Quaternion.identity; armBusy = false;
        }

        void AdvanceProjectile(float remaining)
        {
            // Exact swept collision, including several wall contacts within one frame. No rigidbody friction/spin.
            for (int iteration = 0; iteration < 12 && remaining > .00001f && projectile; iteration++)
            {
                Vector2 origin = projectile.transform.position;
                float nearest = remaining; bool hit = false, sphereHit = false, bottomHit = false;
                Vector2 normal = Vector2.zero; Vector2Int hitCell = Vector2Int.zero;
                float wall = float.PositiveInfinity;
                if (projectileDirection.x > .00001f) { wall = (Right - Radius - origin.x) / projectileDirection.x; normal = Vector2.left; }
                else if (projectileDirection.x < -.00001f) { wall = (Left + Radius - origin.x) / projectileDirection.x; normal = Vector2.right; }
                if (wall >= 0 && wall <= nearest) { nearest = wall; hit = true; }
                if (projectileDirection.y > .00001f)
                {
                    float top = (Top - Radius - origin.y) / projectileDirection.y;
                    if (top >= 0 && top <= nearest) { nearest = top; normal = Vector2.down; hit = true; }
                }
                else if (projectileDirection.y < -.00001f)
                {
                    float bottom = (Bottom - origin.y) / projectileDirection.y;
                    if (bottom >= 0 && bottom <= nearest) { nearest = bottom; hit = bottomHit = true; }
                }
                if (BoardRules.Sweep(origin, projectileDirection, center, Radius * 2, nearest, out float d))
                { nearest = d; hit = sphereHit = true; bottomHit = false; }
                foreach (var entry in board.Cells)
                {
                    Vector2 position = cluster.TransformPoint(BoardRules.Position(entry.Key));
                    if (BoardRules.Sweep(origin, projectileDirection, position, Radius * 2, nearest, out d))
                    { nearest = d; hitCell = entry.Key; hit = sphereHit = true; bottomHit = false; }
                }
                projectile.transform.position += (Vector3)(projectileDirection * nearest);
                remaining -= nearest;
                if (!hit) break;
                if (sphereHit) { Attach(hitCell, projectileDirection); return; }
                if (bottomHit)
                {
                    Destroy(projectile); projectile = null; lost++; lastMatch = lastRelease = 0;
                    StartCoroutine(FinishTurn()); return;
                }
                projectileDirection = BoardRules.Reflect(projectileDirection, normal).normalized;
                ricochets++;
                shotRicochets++;
                if (levelAudio) levelAudio.Ricochet();
                projectile.transform.position += (Vector3)(projectileDirection * .0001f);
                remaining = Mathf.Max(0, remaining - .0001f);
            }
        }

        void Attach(Vector2Int hit, Vector2 incoming)
        {
            Vector2 impact = projectile.transform.position;
            bool found = false; float best = float.PositiveInfinity; Vector2Int chosen = default;
            foreach (var offset in BoardRules.Directions)
            {
                var cell = hit + offset;
                if (cell == Vector2Int.zero || board.Cells.ContainsKey(cell)) continue;
                Vector2 point = cluster.TransformPoint(BoardRules.Position(cell));
                // Do not snap through an occupied sphere into a hidden pocket.
                Vector2 segment = point - impact; float length = segment.magnitude;
                bool blocked = length > .0001f && BoardRules.Sweep(impact + segment.normalized * .002f,
                    segment.normalized, center, Radius * 2 - .025f, length, out _);
                foreach (var other in board.Cells)
                {
                    if (blocked) break;
                    Vector2 otherPos = cluster.TransformPoint(BoardRules.Position(other.Key));
                    if (length > .0001f && BoardRules.Sweep(impact + segment.normalized * .002f,
                        segment.normalized, otherPos, Radius * 2 - .025f, length, out _)) blocked = true;
                }
                if (blocked) continue;
                float distance = (point - impact).sqrMagnitude;
                if (distance < best) { best = distance; chosen = cell; found = true; }
            }
            if (!found)
            {
                // A completely obstructed contact reflects instead of overlapping or teleporting.
                Vector2 contact = hit == Vector2Int.zero ? center : (Vector2)cluster.TransformPoint(BoardRules.Position(hit));
                Vector2 n = (impact - contact).normalized;
                projectileDirection = BoardRules.Reflect(incoming, n).normalized;
                projectile.transform.position += (Vector3)(n * .003f);
                return;
            }
            projectile.transform.SetParent(cluster, false);
            projectile.transform.localPosition = BoardRules.Position(chosen);
            board.Cells.Add(chosen, loadedType); visuals.Add(chosen, projectile); projectile = null;
            // Even a would-be match loses immediately if its attached position touches an obstacle.
            if (CheckGameOver()) return;
            int iceBefore = totals[0], sunlightBefore = totals[1];
            var matches = board.MatchFrom(chosen);
            lastMatch = matches.Count; matchedCount += matches.Count;
            RemoveCells(matches, 0);
            var released = board.Disconnected(); lastRelease = released.Count;
            RemoveCells(released, 3);
            if (levelAudio) levelAudio.Resolve(lastMatch, lastRelease);
            if (levelAudio) shotHasElementCallout = levelAudio.AnnounceResources(totals[0] - iceBefore, totals[1] - sunlightBefore);
            complete = board.Cells.Count == 0;
            // Resolve the callout in the impact frame, not after the rotation animation.
            if (levelAudio && !complete && !shotHasElementCallout && lastMatch >= 3 && shotRicochets > 0 &&
                !CheckGameOver(true, spinAngle + rotationDegrees))
                levelAudio.TryImpressive(lastMatch, shotRicochets, false);
            StartCoroutine(FinishTurn());
        }

        void RemoveCells(HashSet<Vector2Int> cells, int contributionMultiplier)
        {
            foreach (var cell in cells)
            {
                int type = board.Cells[cell]; totals[type] += contributionMultiplier;
                var visual = visuals[cell]; visuals.Remove(cell); board.Cells.Remove(cell);
                if (contributionMultiplier > 0) StartCoroutine(Ascend(visual, type));
                else StartCoroutine(DissolveMatch(visual));
            }
        }

        IEnumerator DissolveMatch(GameObject visual)
        {
            visual.transform.SetParent(transform, true);
            Vector3 initialScale = visual.transform.localScale;
            var icons = visual.GetComponentsInChildren<SpriteRenderer>();
            const float duration = .24f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                visual.transform.localScale = initialScale * (1 - Mathf.SmoothStep(0, 1, p));
                foreach (var icon in icons)
                {
                    Color color = icon.color; color.a = 1 - p; icon.color = color;
                }
                yield return null;
            }
            Destroy(visual);
        }

        IEnumerator Ascend(GameObject visual, int type)
        {
            visual.transform.SetParent(transform, true);
            Vector3 start = visual.transform.position;
            Vector3 destination = new Vector3(start.x * .3f, 7.6f, 0);
            var streak = visual.AddComponent<TrailRenderer>(); streak.sharedMaterial = lineMaterial;
            streak.time = .32f; streak.startWidth = .16f; streak.endWidth = 0;
            streak.startColor = ColorFor(type); streak.endColor = new Color(ColorFor(type).r, ColorFor(type).g, ColorFor(type).b, 0);
            streak.sortingOrder = 8;
            float duration = .65f + Mathf.Abs(start.x) * .04f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = t / duration, eased = p * p;
                visual.transform.position = Vector3.Lerp(start, destination, eased);
                visual.transform.localScale = Vector3.one * Mathf.Lerp(1, .07f, Mathf.Min(1, p * 2));
                yield return null;
            }
            Destroy(visual);
        }

        IEnumerator FinishTurn()
        {
            if (failed) yield break;
            status.text = complete ? "PLANET FORMED" : "RESOLVING";
            yield return new WaitForSeconds(.85f);
            if (complete)
            {
                status.text = "WORLD COMPLETE · R TO RESTART";
                if (levelAudio) levelAudio.AnnounceResult(true);
                yield break;
            }
            status.text = "CLUSTER ROTATING";
            float previous = spinAngle; spinAngle += rotationDegrees;
            for (float t = 0; t < rotationDuration; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / rotationDuration);
                cluster.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(previous, spinAngle, p));
                yield return null;
            }
            cluster.rotation = Quaternion.Euler(0, 0, spinAngle);
            while (armBusy) yield return null;
            if (CheckGameOver()) yield break;
            LoadNext();
        }

        bool CheckGameOver(bool previewOnly = false, float? previewAngle = null)
        {
            foreach (var cell in board.Cells.Keys)
            {
                Vector2 point = previewAngle.HasValue
                    ? (Vector2)cluster.position + (Vector2)(Quaternion.Euler(0, 0, previewAngle.Value) * (Vector3)BoardRules.Position(cell))
                    : (Vector2)cluster.TransformPoint(BoardRules.Position(cell));
                bool touches = BoardRules.TouchesEdge(point, Radius, Left, Right, Top, Bottom);
                for (int i = 1; !touches && i < cannonBacking.positionCount; i++)
                {
                    Vector2 a = cannonBacking.transform.TransformPoint(cannonBacking.GetPosition(i - 1));
                    Vector2 b = cannonBacking.transform.TransformPoint(cannonBacking.GetPosition(i));
                    touches = BoardRules.CircleTouchesTriangle(point, Radius + cannonBacking.startWidth * .5f, a, b, b);
                }
                foreach (var part in cannonContacts)
                {
                    if (touches) break;
                    for (int i = 0; i < part.triangles.Length; i += 3)
                        if (BoardRules.CircleTouchesTriangle(point, Radius,
                            part.transform.TransformPoint(part.vertices[part.triangles[i]]),
                            part.transform.TransformPoint(part.vertices[part.triangles[i + 1]]),
                            part.transform.TransformPoint(part.vertices[part.triangles[i + 2]])))
                        { touches = true; break; }
                }
                if (!touches) continue;
                if (previewOnly) return true;
                failed = true; ready = false; complete = false;
                totals[0] = totals[1] = matchedCount = lastMatch = lastRelease = 0;
                shown[0] = shown[1] = 0;
                if (levelAudio) levelAudio.CancelResolution();
                if (levelAudio) levelAudio.AnnounceResult(false);
                aim.enabled = false;
                status.text = "CREATION FAILED · NO REWARDS · R TO RESTART";
                return true;
            }
            return false;
        }

        Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, int fontsize)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = go.AddComponent<Text>(); label.font = font; label.fontSize = fontsize;
            label.text = text; label.color = new Color(.85f, .91f, 1);
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }

        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }

        void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("Prototype HUD", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = view; canvas.planeDistance = 10;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600); scaler.matchWidthOrHeight = 1;
            // Match the letterboxed world area instead of stretching the HUD across a landscape monitor.
            var hud = Panel("Portrait HUD", canvas.transform, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(900, 1600), Color.clear);
            Label("Title", hud, new Vector2(.5f, 1), new Vector2(0, -45), new Vector2(850, 60), "ECOSPHERE / FIRST LIGHT", 29);
            status = Label("Turn", hud, new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(850, 45), "", 21);
            detail = Label("Counters", hud, new Vector2(.5f, 0), new Vector2(0, 525), new Vector2(850, 42), "", 18);
            var strip = Panel("Resource visualizer", hud, new Vector2(.5f, 0), new Vector2(0, 95), new Vector2(860, 180), new Color(.01f, .018f, .035f, .96f));
            for (int type = 0; type < 2; type++) for (int i = 0; i < 10; i++)
            {
                float x = -398 + type * 420 + i * 39;
                var bar = Panel((type == 0 ? "Ice " : "Sunlight ") + i, strip, new Vector2(.5f, 0),
                    new Vector2(x, 36), new Vector2(24, 2), ColorFor(type));
                bar.pivot = new Vector2(.5f, 0); bars[type].Add(bar); barImages[type].Add(bar.GetComponent<Image>());
            }
            goalLine = Panel("Fixed resource goal", strip, new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(825, 2), Color.white);
            iceLabel = Label("Ice total", strip, new Vector2(.25f, 0), new Vector2(0, 16), new Vector2(390, 30), "ICE 0", 18);
            sunLabel = Label("Sunlight total", strip, new Vector2(.75f, 0), new Vector2(0, 16), new Vector2(390, 30), "SUNLIGHT 0", 18);
            Label("Instructions", hud, new Vector2(.5f, 0), new Vector2(0, 485), new Vector2(850, 35),
                "Mouse to aim · Click to fire · R to restart", 18);
        }

        void UpdateVisualizer()
        {
            int goal = Mathf.Max(1, resourceGoal);
            // Fixed display range keeps the target line stationary as resources arrive.
            float scale = goal * 1.6f;
            for (int type = 0; type < 2; type++)
            {
                shown[type] = Mathf.Lerp(shown[type], totals[type], 1 - Mathf.Exp(-Time.deltaTime * 7));
                for (int i = 0; i < 10; i++)
                {
                    bars[type][i].sizeDelta = new Vector2(24, Mathf.Clamp(shown[type] / scale * 125, 2, 125));
                    barImages[type][i].color = Color.Lerp(ColorFor(type) * .65f, ColorFor(type), .4f + .6f * i / 9);
                }
            }
            goalLine.anchoredPosition = new Vector2(0, 36 + goal / scale * 125);
            iceLabel.text = $"ICE {totals[0]}/{goal}" + (totals[0] >= goal ? " READY" : "");
            sunLabel.text = $"SUNLIGHT {totals[1]}/{goal}" + (totals[1] >= goal ? " READY" : "");
            detail.text = $"Matched {lastMatch} · Released {lastRelease} (x3) · Lost {lost} · Goal {goal}";
        }

        IEnumerator SmokeRun()
        {
            yield return new WaitForSeconds(.6f);
            if (CellCount != 3 * InitialRings * (InitialRings + 1))
                throw new Exception("Initial pre-existing groups were incorrectly resolved");
            direction = SmokeMatchingDirection(); gun.up = direction; Fire();
            float deadline = Time.time + 15;
            while (!ready && !complete && Time.time < deadline) yield return null;
            if (Time.time >= deadline) throw new Exception("Smoke shot did not resolve in time");
            if (lastMatch < 3) throw new Exception("Known matching shot failed to clear its initial group");
            if (!complete && Mathf.Abs(Mathf.DeltaAngle(cluster.eulerAngles.z, rotationDegrees)) > .1f)
                throw new Exception("Next sphere loaded before cluster rotation completed");
            Debug.Log($"ECOSPHERE_SMOKE resolved: board={CellCount}, matched={lastMatch}, released={lastRelease}, lost={lost}");
            if (!complete)
            {
                int before = ricochets;
                direction = new Vector2(.97f, .26f).normalized; gun.up = direction; Fire();
                deadline = Time.time + 20;
                while (!ready && !complete && Time.time < deadline) yield return null;
                if (Time.time >= deadline || ricochets <= before) throw new Exception("Bank shot failed to ricochet and resolve");
                Debug.Log($"ECOSPHERE_SMOKE bank shot: ricochets={ricochets - before}, board={CellCount}");
            }
            if (!complete)
            {
                int previousLost = lost;
                // Exercise the bottom-return path directly, without depending on a particular board gap.
                direction = Vector2.down; Fire();
                deadline = Time.time + 5;
                while (!ready && Time.time < deadline) yield return null;
                if (lost != previousLost + 1 || !ready) throw new Exception("Bottom loss did not advance and reload the turn");
                Debug.Log("ECOSPHERE_SMOKE bottom loss passed");
                gun.rotation = Quaternion.identity; direction = Vector2.up;
            }
            yield return new WaitForSeconds(.3f);
            string output = System.IO.Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png");
            ScreenCapture.CaptureScreenshot(output);
            yield return new WaitForSeconds(1);
            Debug.Log("ECOSPHERE_SMOKE_COMPLETE");
            CapturePreview();
            SmokeScoringChecks();
            SmokeLossChecks();
            yield return SmokeVoiceChecks();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }

        void CapturePreview()
        {
            var target = new RenderTexture(900, 1600, 24);
            var previousTarget = view.targetTexture;
            var previousActive = RenderTexture.active;
            view.targetTexture = target; view.aspect = 900f / 1600; FitCamera();
            if (spaceBackground) spaceBackground.RefreshLayout();
            Canvas.ForceUpdateCanvases();
            view.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(900, 1600, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 900, 1600), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../Logs/EcoSphere-test-level.png"), pixels.EncodeToPNG());
            view.targetTexture = previousTarget; RenderTexture.active = previousActive;
            target.Release(); Destroy(target); Destroy(pixels);
        }

        void SmokeLossChecks()
        {
            // A matching attachment beyond the boundary must lose before it can clear or score.
            board.Cells.Clear(); cluster.rotation = Quaternion.identity;
            var hitCell = new Vector2Int(6, 2);
            board.Cells[hitCell] = board.Cells[new Vector2Int(6, 1)] = board.Cells[new Vector2Int(5, 2)] = 0;
            projectile = new GameObject("Boundary attachment test");
            projectile.transform.position = cluster.TransformPoint(BoardRules.Position(new Vector2Int(7, 2)));
            loadedType = 0; totals[0] = 8;
            Attach(hitCell, Vector2.left);
            if (!failed || board.Cells.Count != 4 || lastMatch != 0 || totals[0] != 0)
                throw new Exception("Boundary attachment was allowed to match before losing");
            failed = false;
            board.Cells.Clear();
            var cell = new Vector2Int(6, 3);
            board.Cells[cell] = 0;
            cluster.rotation = Quaternion.identity;
            if (CheckGameOver()) throw new Exception("Safe pre-rotation cluster incorrectly lost");
            Vector2 offset = BoardRules.Position(cell);
            float losingAngle = -Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (!CheckGameOver(true, losingAngle) || failed)
                throw new Exception("Rotation preview must detect loss without ending the level early");
            cluster.rotation = Quaternion.Euler(0, 0, losingAngle);
            totals[0] = 8; matchedCount = 6;
            if (!CheckGameOver() || ready || totals[0] != 0 || matchedCount != 0)
                throw new Exception("Rotation-edge contact must end level and discard all rewards");
            Debug.Log("ECOSPHERE_LOSS_CHECKS_PASSED");
        }

        void SmokeScoringChecks()
        {
            board.Cells.Clear(); visuals.Clear(); totals[0] = totals[1] = 0;
            var ice = new Vector2Int(1, 0); var sun = new Vector2Int(0, 1);
            board.Cells[ice] = 0; board.Cells[sun] = 1;
            visuals[ice] = new GameObject("Matched scoring test");
            visuals[sun] = new GameObject("Released scoring test");
            RemoveCells(new HashSet<Vector2Int> { ice }, 0);
            RemoveCells(new HashSet<Vector2Int> { sun }, 3);
            if (totals[0] != 0 || totals[1] != 3) throw new Exception("Matches must score zero; breakaways must score three");
            var additional = new HashSet<Vector2Int>();
            for (int i = 1; i <= 4; i++)
            {
                var cell = new Vector2Int(i, 2); additional.Add(cell);
                board.Cells[cell] = 1; visuals[cell] = new GameObject("Threshold scoring test");
            }
            RemoveCells(additional, 3);
            if (totals[1] != 15 || LevelAudio.ResourceCallout(new[] { totals[0], totals[1] }) != 1)
                throw new Exception("Five breakaway Sunlight spheres must qualify for Heliosphere");
            UpdateVisualizer(); float initialLine = goalLine.anchoredPosition.y;
            totals[0] = totals[1] = resourceGoal * 4;
            UpdateVisualizer();
            if (Mathf.Abs(initialLine - goalLine.anchoredPosition.y) > .001f)
                throw new Exception("Fixed target line moved when resource totals increased");
            Debug.Log("ECOSPHERE_SCORING_CHECKS_PASSED");
        }

        Vector2 SmokeMatchingDirection()
        {
            for (int step = -240; step <= 240; step++)
            {
                float degrees = step * .25f;
                Vector2 candidate = new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));
                Vector2 origin = (Vector2)gun.position + candidate * (.57f * VisualScale);
                float nearest = 20; int hitType = -1; Vector2 hitCenter = default;
                foreach (var entry in board.Cells)
                    if (BoardRules.Sweep(origin, candidate, cluster.TransformPoint(BoardRules.Position(entry.Key)),
                        Radius * 2, nearest, out float distance))
                    { nearest = distance; hitType = entry.Value; hitCenter = cluster.TransformPoint(BoardRules.Position(entry.Key)); }
                Vector2 offset = hitCenter - origin;
                float perpendicular = Mathf.Abs(offset.x * candidate.y - offset.y * candidate.x);
                if (hitType == loadedType && perpendicular < .12f) return candidate;
            }
            throw new Exception("No direct matching smoke-test trajectory found");
        }

        IEnumerator SmokeVoiceChecks()
        {
            if (!levelAudio || !levelAudio.worldComplete || !levelAudio.creationFailed ||
                !levelAudio.cryosphere || !levelAudio.heliosphere || !levelAudio.ecosphere)
                throw new Exception("Level result voice clips are missing");
            var mainAudio = levelAudio;
            var lossSource = mainAudio.transform.Find("Level result announcer").GetComponent<AudioSource>();
            if (lossSource.clip != mainAudio.creationFailed)
                throw new Exception("Actual game-over checkpoint did not announce Creation Failed");
            var test = new GameObject("Win announcement test").AddComponent<LevelAudio>();
            test.worldComplete = mainAudio.worldComplete; test.creationFailed = mainAudio.creationFailed;
            test.cryosphere = mainAudio.cryosphere; test.heliosphere = mainAudio.heliosphere;
            test.ecosphere = mainAudio.ecosphere;
            test.voiceVolume = 0; test.Initialize();
            test.AnnounceResources(15, 18);
            var source = test.transform.Find("Level result announcer").GetComponent<AudioSource>();
            if (source.clip != mainAudio.ecosphere)
                throw new Exception("Two qualifying resource gains did not announce EcoSphere");
            levelAudio = test; failed = false; complete = true;
            yield return FinishTurn();
            float deadline = Time.time + 15;
            while (source.clip != mainAudio.worldComplete && Time.time < deadline) yield return null;
            if (source.clip != mainAudio.worldComplete || source.clip.length <= 0)
                throw new Exception("Completion did not select World Complete");
            test.AnnounceResult(false);
            if (source.clip != mainAudio.worldComplete)
                throw new Exception("Level result was announced more than once");
            levelAudio = mainAudio; Destroy(test.gameObject);
            Debug.Log("ECOSPHERE_RESULT_VOICE_CHECKS_PASSED");
        }

        void OnDestroy()
        {
            foreach (var material in materials) if (material) Destroy(material);
            foreach (var sprite in generatedSprites) if (sprite) Destroy(sprite);
        }
    }
}
