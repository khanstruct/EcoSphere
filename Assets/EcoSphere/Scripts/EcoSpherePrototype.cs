using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(LevelAudio))]
    public sealed partial class EcoSpherePrototype : MonoBehaviour
    {
        public enum LevelLayout { StoneTutorial, FirstTutorial, Procedural }
        [Header("Level layout")]
        public LevelLayout levelLayout = LevelLayout.StoneTutorial;
        bool ProceduralLayout => levelLayout==LevelLayout.Procedural;
        bool StoneTutorial => levelLayout == LevelLayout.StoneTutorial;
        [Header("Approved artwork")]
        public Material receivingPlanetMaterial;
        const int StoneType = 3;
        public TMP_FontAsset uiFont;
        public Sprite leftArm, rightArm, foreground;
        [Header("Space backdrop")]
        public Sprite starfield, twinkleStar;
        [Header("Turn settings")]
        public float shotSpeed = 13;
        public float rotationDegrees = 24;
        public float rotationDuration = .65f;
        [Header("Level resource target")]
        [Min(1)] public int abundanceGoal = ElementScoring.Abundance;
        // Whether this world can award recipe discoveries (its flares and threshold guides follow the same rule).
        public bool RecipesEnabled => preparedWorld == null || preparedWorld.rules.recipeDiscoveries;
        int score;
        readonly HashSet<int> releasedNaturalSpheres=new HashSet<int>();
        bool stoneReleased=>releasedNaturalSpheres.Contains(StoneType);
        public const float VisualScale = GameplayGeometry.SphereScale;
        public const float Radius = GameplayGeometry.SphereRadius;
        int InitialRings => preparedWorld?.rules.clusterRings ?? GameplayGeometry.DefaultClusterRings;
        const float Left = GameplayGeometry.Left, Right = GameplayGeometry.Right, Top = GameplayGeometry.Top, Bottom = GameplayGeometry.Bottom;
        readonly Vector2 center = GameplayGeometry.ClusterCenter;
        readonly BoardRules board = new BoardRules();
        readonly ShotSelector shotSelector = new ShotSelector();
        readonly Dictionary<Vector2Int, GameObject> visuals = new Dictionary<Vector2Int, GameObject>();
        readonly BoardCollisionSnapshot collisionSnapshot = new BoardCollisionSnapshot();
        readonly List<Vector2> aimPoints = new List<Vector2>(13);
        readonly double[] totals = new double[Enum.GetValues(typeof(ElementId)).Length];
        ElementLedger elementLedger;
        // Every gameplay element gain goes through this ledger so contribution limits apply uniformly.
        internal ElementLedger Elements { get { if (elementLedger == null) { elementLedger = new ElementLedger(totals); elementLedger.ShortageOccurred += ShowShortage; } return elementLedger; } }
        readonly float[] shown = new float[Enum.GetValues(typeof(ElementId)).Length];
        readonly List<Material> materials = new List<Material>();
        readonly List<Sprite> generatedSprites = new List<Sprite>();
        Transform cluster, gun, gunArtwork, leftHinge, rightHinge, halo, planet;
        Camera view;
        SpaceBackground spaceBackground;
        LevelAudio levelAudio;
        GameObject loaded, projectile;
        Material lineMaterial;
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
        TMP_Text status;
        TMP_FontAsset font;
        bool smoke;
        public int CellCount => board.Cells.Count;

        void Start()
        {
            bool planetTesting = LaunchFlags.Has("-planetSmoke");
            smoke = LaunchFlags.Has("-ecosphereSmoke") || LaserTesting || TutorialTesting || planetTesting || FirstTutorialTesting || SphereDataTesting || CompositionTesting;
            InitializeSphereData();
            BuildWorld();
            levelAudio = GetComponent<LevelAudio>();
            if (!levelAudio) levelAudio = gameObject.AddComponent<LevelAudio>();
            levelAudio.Initialize();
            BuildInterface();
            StartCoroutine(PrewarmShortageNotice());
            BuildBoard();
            CaptureCompositionBaseline();
            ApplyInfestation();
            LoadNext();
            if (levelLayout==LevelLayout.FirstTutorial && (!smoke || FirstTutorialTesting)) ShowTutorial(0);
            else if (StoneTutorial && (!smoke || TutorialTesting)) ShowTutorial(2);
            if (CompositionTesting) StartCoroutine(CompositionSmokeRun());
            else if (SphereDataTesting) StartCoroutine(SphereDataSmokeRun());
            else if (FirstTutorialTesting) StartCoroutine(FirstTutorialSmokeRun());
            else if (planetTesting) StartCoroutine(PlanetSmokeRun());
            else if (TutorialTesting) StartCoroutine(TutorialSmokeRun());
            else if (LaserTesting) StartCoroutine(LaserSmokeRun());
            else if (smoke) StartCoroutine(SmokeRun());
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
            spaceBackground.gameObject.layer = 29;
            spaceBackground.Initialize(view, starfield, twinkleStar);
            BuildLevelSun();
            var light = new GameObject("Upper-left soft light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .5f, .6f);
            lineMaterial = new Material(Shader.Find("Sprites/Default")); materials.Add(lineMaterial);
            foreach (var definition in Catalog.spheres)
            {
                var material = Surface(definition.surface, definition.unlit);
                SphereShapes.ApplyFinish(material, definition.shape);
                sphereMaterials.Add(definition.id, material);
            }
            cluster = new GameObject("Rotating hex cluster").transform; cluster.position = center;
            MakeSphere("Black hole core", -1, cluster, Vector3.zero);
            halo = SpritePart("Pulsing core halo", coreHalo, cluster, new Vector3(0, 0, .6f), 1.8f * VisualScale, -1).transform;
            // Keep the receiving world's existing framing; its surface rotates independently.
            var globe = GameObject.CreatePrimitive(PrimitiveType.Sphere); globe.name = "Receiving planet";
            globe.transform.position = new Vector3(0, 11.5f, 3); globe.transform.localScale = Vector3.one * 10;
            Destroy(globe.GetComponent<Collider>()); planet = globe.transform;
            var planetStyle=PlanetVisualStyle.Current;
            if(planetStyle)receivingPlanetMaterial=planetStyle.Terrain(preparedWorld.terrainStyleId,receivingPlanetMaterial);
            var pm = receivingPlanetMaterial ? new Material(receivingPlanetMaterial) : Surface(null);
            if (receivingPlanetMaterial) materials.Add(pm);
            globe.GetComponent<Renderer>().sharedMaterial = pm;
            globe.AddComponent<PlanetSurface>();
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
            FitPresentation();
        }

        void BuildGun()
        {
            // The aiming transform sits at the authored pivot; the artwork origin stays at the cannon base when straight.
            var cannonStyle = AmmoChamberStyle.Current;
            gun = new GameObject("Aiming cannon").transform; gun.position = (Vector3)cannonStyle.Pivot + new Vector3(0, 0, -1);
            gunArtwork = new GameObject("Scaled cannon artwork").transform;
            gunArtwork.SetParent(gun, false); gunArtwork.localScale = Vector3.one * VisualScale * cannonStyle.cannonScale;
            gunArtwork.localPosition = new Vector3(0, -cannonStyle.pivotHeight, 0);
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
            aim = Line("Loaded element aiming line", transform, ColorFor(activeSphereTypes[0]), .03f, 9);
            aim.positionCount = 2;
            BuildChambers();
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
            var definition = Definition(type);
            var mesh = SphereShapes.CreateBody(definition.shape, "Rotating surface");
            mesh.transform.SetParent(root.transform, false); mesh.transform.localScale = Vector3.one * Radius * 2;
            ApplySurface(mesh.GetComponent<Renderer>(), sphereMaterials[type], definition.surface, definition.surfaceTint);
            var appearance = root.AddComponent<SphereAppearance>(); appearance.surface = mesh.transform; appearance.shape = definition.shape;
            appearance.glow=SphereGlow.Create(root.transform,definition,Radius*2,root.layer);
            appearance.spin = type < 0 ? 8 : 12;
            if (type == LaserType) MakeLaserDisplay(root);
            else if (type >= 0)
                appearance.icon = SpritePart("Camera-facing type icon", definition.icon,
                    root.transform, new Vector3(0, 0, -.47f * VisualScale), .5f * VisualScale, 4).transform;
            return root;
        }

        void BuildBoard()
        {
            releasedNaturalSpheres.Clear();
            if(ProceduralLayout)ProceduralCluster.Populate(board,activeSphereTypes,InitialRings,preparedWorld.seed,preparedWorld.rules.clustering,WorldCatalog.Current.clusterGroups);
            else if(StoneTutorial)StoneTutorialLayout.Populate(board,activeSphereTypes,InitialRings);
            else
            {
                // Authored opening topology; element identities come from the selected loadout.
                for(int q=-InitialRings;q<=InitialRings;q++)for(int r=-InitialRings;r<=InitialRings;r++)
                {
                    if(Mathf.Abs(q+r)>InitialRings || (q==0 && r==0))continue;
                    var cell=new Vector2Int(q,r);Vector2 point=BoardRules.Position(cell);
                    board.Cells.Add(cell,point.x<-.0001f || (Mathf.Abs(point.x)<=.0001f && point.y<0) ? activeSphereTypes[0] : activeSphereTypes[1]);
                }
            }
            ApplyNaturalPlacements();
            foreach(var cell in board.Cells)
                visuals.Add(cell.Key,MakeSphere(Definition(cell.Value).displayName+" "+cell.Key,cell.Value,cluster,BoardRules.Position(cell.Key)));
        }
        void ApplyNaturalPlacements()
        {
            foreach(var natural in preparedWorld.naturalSpheres)
            {
                if(!board.Cells.ContainsKey(natural.cell) || !NaturalOccurrence.IsAllowedCell(natural.cell,InitialRings))throw new System.InvalidOperationException("Natural sphere placement lies outside the cluster or touches the core");
                board.Cells[natural.cell]=natural.sphereId;
            }
        }
        // Whether this cell still holds the natural sphere the world placed there (authored or rolled; see NaturalOccurrence).
        bool IsNaturalPlacement(Vector2Int cell,int type)=>preparedWorld!=null && preparedWorld.naturalSpheres.Exists(natural=>natural.cell==cell && natural.sphereId==type);
        void LoadNext()
        {
            if (complete || failed) return;
            bool advanced = !(shots == 0 && LaserTesting);
            if (!advanced) loadedType = LaserType;
            else if (!TryLoadFromRing(out loadedType)) return;
            loaded = MakeSphere("Loaded " + Definition(loadedType).displayName, loadedType, gun, LoadedLocal);
            AfterLoad(advanced);
            ready = true; status.text = "AIM · CLICK TO FIRE";
            if (StoneTutorial && (!smoke || TutorialTesting) && pendingTutorial >= 0) ShowTutorial(pendingTutorial);
        }

        void Update()
        {
            FitCamera();
            if (tutorialPaused || Time.frameCount <= tutorialResumeFrame) return;
            if (halo) halo.localScale = Vector3.one * (1.8f * VisualScale / coreHalo.bounds.size.x) * (1 + .035f * Mathf.Sin(Time.time * 2));
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !smoke)
                GameServices.UI.Retry();
            if (ready && !smoke) HandleShooterInput();
            // Only show the guide once the cannon sphere has settled, not while the ring is turning.
            aim.enabled = ready && loaded && !touchCancelled && ringTurn == null;
            if (aim.enabled)
            {
                Vector2 muzzle = (Vector2)loaded.transform.position + direction * (.55f * VisualScale);
                collisionSnapshot.Refresh(board.Cells.Keys,cluster.localToWorldMatrix,center);
                var path=ProjectileSimulation.Trace(muzzle,direction,(Right-Left)*.75f,
                    new ShotBounds(Left+Radius,Right-Radius,Top-Radius,Bottom),collisionSnapshot.Centers,Radius*2,aimPoints);
                aim.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++) aim.SetPosition(i, new Vector3(path[i].x, path[i].y, -2));
                aim.startColor = aim.endColor = ColorFor(loadedType);
            }
            if (projectile) AdvanceProjectile(Mathf.Min(Time.deltaTime, .1f) * shotSpeed);
            UpdateVisualizer();
        }


        void Fire()
        {
            if (tutorialPaused || !ready || !loaded || armBusy) return;
            FinishRingTurn();
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
            collisionSnapshot.Refresh(board.Cells.Keys,cluster.localToWorldMatrix,center);
            var bounds=new ShotBounds(Left+Radius,Right-Radius,Top-Radius,Bottom);
            for(int iteration=0;iteration<ProjectileSimulation.MaximumContacts && remaining>.00001f && projectile;iteration++)
            {
                Vector2 origin=projectile.transform.position;
                var contact=ProjectileSimulation.Sweep(origin,projectileDirection,remaining,bounds,collisionSnapshot.Centers,Radius*2);
                float nearest=contact.Distance;
                bool hit=contact.Kind!=ShotContactKind.None,sphereHit=contact.Kind==ShotContactKind.Sphere,bottomHit=contact.Kind==ShotContactKind.Bottom;
                Vector2 normal=contact.Normal;
                Vector2Int hitCell=sphereHit ? collisionSnapshot.CellAt(contact.SphereIndex) : Vector2Int.zero;
                projectile.transform.position += (Vector3)(projectileDirection * nearest);
                remaining -= nearest;
                if (!hit) break;
                AudioClip bounceSound=null;
                if (sphereHit)
                {
                    var target=hitCell==Vector2Int.zero ? null : Definition(board.Cells[hitCell]);
                    var impact=ShotDeflection.Resolve(target,loadedType,SphereDamage.HitsOn(visuals.TryGetValue(hitCell,out var struck) ? struck : null));
                    if (impact==ShotImpact.Attach) { Attach(hitCell, projectileDirection); return; }
                    if (impact==ShotImpact.Break) { BreakDeflector(hitCell); Place(hitCell); return; }
                    // A deflecting sphere bounces the shot like a boundary, and it counts as a ricochet.
                    SphereDamage.Strike(struck);ShowDamage(struck,target);
                    normal=(origin+projectileDirection*nearest-collisionSnapshot.Centers[contact.SphereIndex]).normalized;
                    bounceSound=target.deflectSound;
                }
                if (bottomHit)
                {
                    if (levelAudio) levelAudio.Miss();
                    Destroy(projectile); projectile = null; lost++; lastMatch = lastRelease = 0;
                    ErodeAfterLostShot(loadedType);
                    StartCoroutine(FinishTurn()); return;
                }
                projectileDirection = BoardRules.Reflect(projectileDirection, normal).normalized;
                ricochets++;
                shotRicochets++;
                if (levelAudio) { if (bounceSound) levelAudio.PlayEffect(bounceSound); else levelAudio.Ricochet(); }
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
            Place(chosen);
        }

        // A broken deflector is simply lost: no score, no element contributions, no streak.
        void BreakDeflector(Vector2Int cell)
        {
            LoseSphere(cell,Definition(board.Cells[cell]).breakSound);
        }

        // Seats the projectile in a board cell and resolves matches, breakaways and audio for the shot.
        void Place(Vector2Int chosen)
        {
            projectile.transform.SetParent(cluster, false);
            projectile.transform.localPosition = BoardRules.Position(chosen);
            board.Cells.Add(chosen, loadedType); visuals.Add(chosen, projectile); projectile = null;
            if (loadedType == LaserType)
            {
                visuals[chosen].transform.localRotation = Quaternion.identity;
                visuals[chosen].GetComponent<LaserSphere>().placedTurn = shots;
            }
            foreach (var offset in BoardRules.Directions) RotateLaserContact(chosen + offset);
            var cleansedInfestation=ClearAdjacentInfestations(chosen);
            // Even a would-be match loses immediately if its attached position touches an obstacle.
            if (CheckGameOver()) return;
            resolutionSpherePoints.Clear();
            var matches = board.MatchFrom(chosen,cleansedInfestation);
            lastMatch = matches.Count; matchedCount += matches.Count;
            HashSet<Vector2Int> released;
            // One shot is one resolution: everything it gains raises contribution caps before they are checked.
            using (Elements.Batch())
            {
                RemoveCells(matches, ElementScoring.MatchRate(matches.Count),matchOrigin:chosen,voicePoints:resolutionSpherePoints);
                // No match: an eroding sphere disappears first, so what it held joins this shot's breakaway.
                if (matches.Count == 0) ErodeAfterMiss(board.Cells[chosen]);
                released = FallingAfterBreak(); lastRelease = released.Count;
                RecordResolution(lastMatch, lastRelease, shotRicochets);
                AdvanceTutorial(released);
                RemoveCells(released, ElementScoring.Breakaway, true,voicePoints:resolutionSpherePoints);
            }
            if (levelAudio) levelAudio.Resolve(lastMatch, lastRelease);
            if (levelAudio)shotHasElementCallout=levelAudio.AnnounceResolution(resolutionSpherePoints);
            RefreshFreedGlows();
            complete = board.Cells.Count == 0;
            // Resolve the callout in the impact frame, not after the rotation animation.
            if (levelAudio && !complete && !shotHasElementCallout && lastMatch >= 3 && shotRicochets > 0 &&
                !CheckGameOver(true, spinAngle + rotationDegrees))
                levelAudio.TryImpressive(lastMatch, shotRicochets, false);
            StartCoroutine(FinishTurn());
        }

        readonly Dictionary<int,int> resolutionSpherePoints=new Dictionary<int,int>();
        void RemoveCells(HashSet<Vector2Int> cells, int pointsPerSphere, bool released = false,Vector2Int? matchOrigin=null,Dictionary<int,int> voicePoints=null)
        {
            var delays=!released && matchOrigin.HasValue ? MatchWave.Delays(cells,matchOrigin.Value) : null;
            Vector2Int representative=delays!=null ? MatchWave.LastCell(delays) : default;
            int freedAliens=0;
            foreach (var cell in cells)
            {
                if(!released && board.Infested.Contains(cell))continue;
                if(board.Infested.Contains(cell)){KnockOffAlien(cell,false);freedAliens++;}
                int type = board.Cells[cell];
                if(released && IsNaturalPlacement(cell,type))releasedNaturalSpheres.Add(type);
                var definition = Definition(type);
                if(voicePoints!=null && definition.Matchable)
                {
                    voicePoints.TryGetValue(type,out int previousPoints);voicePoints[type]=previousPoints+pointsPerSphere;
                }
                bool contributes = definition.Contributes;
                if (contributes)
                {
                    Elements.Add(definition, pointsPerSphere);
                }
                if (type != LaserType) score += pointsPerSphere;
                var visual = visuals[cell]; visuals.Remove(cell); board.Cells.Remove(cell);
                if (released && contributes) StartCoroutine(Ascend(visual, type));
                else StartCoroutine(DissolveMatch(visual,delays!=null && delays.TryGetValue(cell,out var delay) ? delay : 0,
                    delays!=null && cell==representative ? type : (int?)null));
            }
            // Several aliens breaking away together get their own callout.
            if(released && infestation && levelAudio && freedAliens>=infestation.breakawayVoiceMinimum)levelAudio.Announce(infestation.breakawayVoice);
        }

        Material matchFlareMaterial;
        IEnumerator DissolveMatch(GameObject visual,float delay=0,int? streakType=null)
        {
            float begins=Time.time+delay;
            while(Time.time<begins)yield return null;
            if(!visual)yield break;
            visual.transform.SetParent(transform, true);
            Vector3 initialScale = visual.transform.localScale;
            var icons = visual.GetComponentsInChildren<SpriteRenderer>();
            var originalAlpha=new float[icons.Length];
            for(int i=0;i<icons.Length;i++)originalAlpha[i]=icons[i].color.a;
            // Shared additive mesh overlay also flashes black/unlit spheres such as Void.
            Renderer flare=null;MaterialPropertyBlock flareProperties=null;
            var mesh=visual.GetComponentInChildren<MeshFilter>();
            if(!matchFlareMaterial)matchFlareMaterial=Resources.Load<Material>("MatchFlare");
            if(mesh && matchFlareMaterial)
            {
                var go=new GameObject("Match flash");go.layer=mesh.gameObject.layer;go.transform.SetParent(mesh.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                flare=go.AddComponent<MeshRenderer>();flare.sharedMaterial=matchFlareMaterial;
                flare.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;flare.receiveShadows=false;
                flareProperties=new MaterialPropertyBlock();
            }
            const float duration = MatchWave.DissolveDuration;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                // A very short bright pulse precedes the collapse, with no added turn wait.
                float pulse=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.075f,t));
                if(flare){flareProperties.SetFloat("_Strength",pulse*2);flare.SetPropertyBlock(flareProperties);}
                float collapse=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,duration,t));
                visual.transform.localScale = initialScale * (1-collapse)*(1+.07f*pulse);
                for(int i=0;i<icons.Length;i++)
                {
                    Color color=icons[i].color; color.a=originalAlpha[i]*(1-p); icons[i].color=color;
                }
                yield return null;
            }
            if(streakType.HasValue)StartMatchedStreak(visual.transform.position,streakType.Value);
            Destroy(visual);
        }

        IEnumerator FinishTurn()
        {
            if (failed) yield break;
            status.text = complete ? "PLANET FORMED" : "RESOLVING";
            yield return new WaitForSeconds(.85f);
            if (complete)
            {
                yield return CompleteWorld();
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
            yield return ResolveLasers();
            if (board.Cells.Count == 0)
            {
                complete = true;
                yield return CompleteWorld();
                yield break;
            }
            LoadNext();
        }

        IEnumerator CompleteWorld()
        {
            GameServices.Session.Phase=GamePhase.Completing;
            ready = false;
            status.text = "WORLD COMPLETE";
            if (levelAudio) levelAudio.AnnounceResult(true);
            if (smoke) yield break;
            if (levelAudio) yield return levelAudio.WaitForAnnouncements();
            else yield return new WaitForSecondsRealtime(1);
            yield return new WaitForSecondsRealtime(.15f);
            while(activeStreaks>0 || activeAlienStreaks>0)yield return null;
            var appearance = planet.GetComponent<PlanetSurface>();
            UpdateComposition();
            ReportComposition("completed",true);
            GameServices.Progress.Complete(GameServices.Session.SystemNumber, GameServices.Session.PlanetIndex,
                appearance ? appearance.TargetSeaLevel : -.02f, planet.eulerAngles.y, releasedNaturalSpheres, selectedElementTotals, composition.Ranked,
                displayedElements.Count, levelScore.Bonus);
            GameServices.Session.Phase=GamePhase.Rating;
            var rating=new GameObject("Level star rating").AddComponent<StarRatingPresentation>();
            yield return rating.Present(preparedWorld.EarnedStars,WorldCatalog.Current.ratingStar,tutorialButtonPrefab,uiFont);
            Destroy(rating.gameObject);
            GameServices.UI.ShowResults();
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
                GameServices.Session.Phase=GamePhase.Failed;
                ReportComposition("failed",false);
                Elements.Clear(); Array.Clear(shown, 0, shown.Length);
                score = matchedCount = lastMatch = lastRelease = 0;
                ResetScoreTracking();
                if (levelAudio) levelAudio.CancelResolution();
                if (levelAudio) levelAudio.AnnounceResult(false);
                aim.enabled = false;
                status.text = "CREATION FAILED · NO REWARDS · R TO RESTART";
                return true;
            }
            return false;
        }

        TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, int fontsize)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 31; go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = fontsize;
            label.text = text; label.color = new Color(.85f, .91f, 1);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }

        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 31; go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }

        void BuildInterface()
        {
            UIInput.EnsureEventSystem();
            font = uiFont;
            var canvas = new GameObject("Prototype HUD", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            gameplayCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = view; canvas.planeDistance = 10;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600); scaler.matchWidthOrHeight = 1;
            // The HUD adapts independently around the uniformly scaled playable rectangle.
            var hud = Panel("Portrait HUD", canvas.transform, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(900, 1600), Color.clear);
            gameplayRoot = hud;
            string worldName = preparedWorld != null ? preparedWorld.displayName : WorldLevels.SceneName(SceneManager.GetActiveScene().name);
            gameplayTitle = Label("Title", hud, new Vector2(.5f, 1), new Vector2(0, -45), new Vector2(850, 60), "ECOSPHERE / " + worldName.ToUpperInvariant(), 29);
            status = Label("Turn", hud, new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(850, 45), "", 21);
            TemperatureLabel=Label("Average temperature",hud,new Vector2(.5f,1),new Vector2(0,-185),new Vector2(850,35),PlanetTemperatureDisplay.Average(59),22);
            var strip = Panel("Resource visualizer", hud, new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(860, 210), new Color(.01f, .018f, .035f, .96f));
            resourcePanel = strip;
            BuildResourceVisualizer(strip);
            instructions = Label("Instructions", hud, new Vector2(.5f, 0), new Vector2(0, 485), new Vector2(850, 35),
                ShooterInstructions(), 18);
            instructions.enableAutoSizing = true; instructions.fontSizeMin = 12; instructions.fontSizeMax = 18;
        }

        void OnDestroy()
        {
            GameServices.Pause.Release(this);
            if (tutorialBackingTexture) Destroy(tutorialBackingTexture);
            if (tutorialSolidTexture) Destroy(tutorialSolidTexture);
            if (placeholderChamberTexture) Destroy(placeholderChamberTexture);
            foreach (var material in materials) if (material) Destroy(material);
            foreach (var sprite in generatedSprites) if (sprite) Destroy(sprite);
        }
    }
}
