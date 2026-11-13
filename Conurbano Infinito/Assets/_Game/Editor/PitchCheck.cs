using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PitchCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Names = { "Kiosco 24hs", "Parrilla del Barrio", "Canchita del Club", "Mural del barrio", "Ronda de mate", "Bicicletería" };
    private static int errors;
    private static int editorSearchErrors;
    private static string output;
    private static bool expectPlacementFailure;
    private static int expectedPlacementErrors;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_PITCH_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_PITCH_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            CheckPlacementAndAssets();
            PoiIndicatorsCheck.Run();
            QuilomboSliceCheck.Run();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(250);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            CheckHealing();
            await SetSize(1280, 720);
            foreach (CharacterData character in CharacterData.All) await CheckPhysicalVisit(character);
            await CheckFullHealthAndDeath();
            await CheckVisitOrders();
            await CheckBuffEdges();
            await CheckTemporaryBenefits();
            await CheckOtherRunEndings();
            await CheckIndicators();
            await CheckWorldLabels();
            Require(errors == 0, "Gameplay Console errors: " + errors);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("PITCH_CHECK_PASS characters=2 real-hold=6s exit-reset=True pause=True healing=capped,no-revive,no-invulnerability-reset one-use=True original-routes=6 original-quilombo=35 total-POIs=6 restart=True placement-seeds=100 indicators=1536-original-layouts,6-same-edge,4-resolutions knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckPlacementAndAssets()
    {
        RiskZone[] zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        var placement = Object.FindFirstObjectByType<QuilomboManager>();
        Require(zones.Length == 6 && Names.All(n => zones.Any(z => z.name == n)), "Six POIs missing.");
        Require((int)RiskZoneReward.Damage == 0 && (int)RiskZoneReward.CollectionRadius == 1 &&
            (int)RiskZoneReward.Healing == 2, "Original serialized reward values changed.");
        for (int i = 3; i < Names.Length; i++)
        {
            RiskZoneData definition = Data(zones.Single(z => z.name == Names[i]));
            var fields = new SerializedObject(definition);
            Require(definition.RequiredDuration == (i == 3 ? 5 : 4) && definition.QuilomboReward == 5 &&
                definition.RewardDuration == (i == 5 ? 15 : 20) && fields.FindProperty("reward").enumValueIndex == i &&
                Mathf.Approximately(fields.FindProperty("temporaryBonus").floatValue,i == 3 ? 0.2f : 0.15f), "New POI values.");
        }
        var settings = new SerializedObject(placement);
        var elements = settings.FindProperty("elements");
        Require(elements.arraySize == 6 && Enumerable.Range(0, 6).All(i =>
            zones.Contains(elements.GetArrayElementAtIndex(i).objectReferenceValue as RiskZone)), "Placement registry.");
        CheckPlacementSeeds(Path.Combine(output, "placement-seeds.csv"));
        CheckPlacementBoundaries();
        RiskZone pitch = zones.Single(z => z.name == Names[2]);
        Vector3 originalScale = pitch.transform.localScale;
        pitch.transform.localScale *= 4f;
        Require(!placement.FitsLawn(pitch, pitch.transform.position), "Oversized footprint accepted.");
        pitch.transform.localScale = originalScale;
        RiskZoneData data = Data(pitch);
        Require(data.RequiredDuration == 6 && data.QuilomboReward == 10 && data.HealingAmount == 2 &&
            data.RewardDuration == 0 && data.HasReward, "Pitch reward configuration.");
        var collider = pitch.GetComponent<CircleCollider2D>();
        Sprite sprite = pitch.GetComponent<SpriteRenderer>().sprite;
        Require(collider.isTrigger && Mathf.Approximately(collider.radius, 1.1f) &&
            sprite != null && sprite.rect.size == Vector2.one * 192 && sprite.pixelsPerUnit == 48 &&
            sprite.pivot == Vector2.one * 96, "Pitch shape/import.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
        Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
            importer.textureCompression == TextureImporterCompression.Uncompressed, "Pitch texture settings.");
        Require(new SerializedObject(pitch).FindProperty("quilombo").objectReferenceValue == Object.FindFirstObjectByType<QuilomboSystem>() &&
            new SerializedObject(pitch).FindProperty("gameCamera").objectReferenceValue == Camera.main, "Pitch wiring.");
        EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
        Debug.Log("PITCH_ASSETS_PASS POIs=6 placement-seeds=100 import,shape,wiring=True");
    }

    public static void CheckPlacementSeeds(string csvPath = null)
    {
        var placement = Object.FindFirstObjectByType<QuilomboManager>();
        var settings = new SerializedObject(placement);
        var zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.InstanceID);
        var candidates = settings.FindProperty("spawnPoints");
        var player = (Transform)settings.FindProperty("playerSpawn").objectReferenceValue;
        float playerDistance = settings.FindProperty("minimumPlayerDistance").floatValue;
        float zoneDistance = settings.FindProperty("minimumElementDistance").floatValue;
        Require(zones.Length == 6 && candidates.arraySize == 8, "Expected six POIs and eight candidates.");
        for (int i = 0; i < candidates.arraySize; i++)
        {
            var point = (Transform)candidates.GetArrayElementAtIndex(i).objectReferenceValue;
            foreach (RiskZone zone in zones)
            {
                Vector3 previous = zone.transform.position;
                zone.transform.position = new Vector3(point.position.x, point.position.y, previous.z);
                try
                {
                    CheckGroundFootprint(zone, -1);
                    Require(placement.FitsLawn(zone, point.position), "Configured candidate cannot fit every POI.");
                }
                finally { zone.transform.position = previous; }
            }
        }
        var rows = new System.Collections.Generic.List<string> { "seed,zone,candidate,x,y,minX,minY,maxX,maxY" };
        var random = UnityEngine.Random.state;
        try
        {
            for (int seed = 0; seed < 100; seed++)
            {
                UnityEngine.Random.InitState(seed);
                Invoke(placement, "Start");
                foreach (RiskZone zone in zones)
                {
                    Require(zone.gameObject.activeSelf && Vector2.Distance(zone.transform.position, player.position) >= playerDistance,
                        "POI too close to player or disabled: seed=" + seed);
                    CheckGroundFootprint(zone, seed);
                    Require(placement.FitsLawn(zone, zone.transform.position), "POI outside lawn: seed=" + seed);
                    Bounds bounds = GroundBounds(zone);
                    foreach (RiskZone other in zones)
                        if (zone != other)
                        {
                            Require(Vector2.Distance(zone.transform.position, other.transform.position) >= zoneDistance,
                                "POIs too close: seed=" + seed);
                            Bounds b = GroundBounds(other);
                            Require(!(bounds.min.x < b.max.x && bounds.max.x > b.min.x && bounds.min.y < b.max.y && bounds.max.y > b.min.y),
                                "POI footprints overlap: seed=" + seed);
                        }
                    int slot = Enumerable.Range(0, candidates.arraySize).Where(i =>
                        Vector2.Distance(zone.transform.position, ((Transform)candidates.GetArrayElementAtIndex(i).objectReferenceValue).position) < 0.01f).DefaultIfEmpty(-1).First();
                    Require(slot >= 0, "POI is not on a configured candidate.");
                    rows.Add(FormattableString.Invariant($"{seed},{zone.name},{slot},{zone.transform.position.x:R},{zone.transform.position.y:R},{bounds.min.x:R},{bounds.min.y:R},{bounds.max.x:R},{bounds.max.y:R}"));
                }
            }
        }
        finally { UnityEngine.Random.state = random; }
        csvPath ??= Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/quilombo-placement-seeds.csv"));
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllLines(csvPath, rows);
        Debug.Log("PLACEMENT_SEEDS_PASS seeds=0..99 active=6 candidates=8 footprint,margin,streets,reservations=True csv=" + csvPath);
    }

    private static Bounds GroundBounds(RiskZone zone)
    {
        var sprites = zone.GetComponentsInChildren<SpriteRenderer>();
        Bounds bounds = sprites[0].bounds;
        foreach (SpriteRenderer sprite in sprites) bounds.Encapsulate(sprite.bounds);
        bounds.Expand(0.5f);
        return bounds;
    }

    private static void CheckGroundFootprint(RiskZone zone, int seed)
    {
        var floor = GameObject.Find("PlazaPiso");
        Mesh mesh = floor.GetComponent<MeshFilter>().sharedMesh;
        for (int i = 0; i < mesh.vertexCount; i++)
            Require(Vector2.Distance(mesh.uv[i], (Vector2)mesh.vertices[i] + Vector2.one * 0.5f) < 0.001f,
                "Floor UV mapping changed; update lawn regions.");
        Material material = floor.GetComponent<Renderer>().sharedMaterial;
        Require(material.GetTexture("_BaseMap").width == 512 && material.GetTexture("_BaseMap").height == 512 &&
            material.GetTexture("_BaseMap").wrapMode == TextureWrapMode.Repeat, "Floor texture contract changed.");
        Vector2 scale = material.GetTextureScale("_BaseMap"), offset = material.GetTextureOffset("_BaseMap");
        Bounds bounds = GroundBounds(zone);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (Vector2 corner in new[] { new Vector2(bounds.min.x,bounds.min.y), new Vector2(bounds.min.x,bounds.max.y),
            new Vector2(bounds.max.x,bounds.min.y), new Vector2(bounds.max.x,bounds.max.y) })
        {
            Vector3 local = floor.transform.InverseTransformPoint(new Vector3(corner.x,corner.y,floor.transform.position.z));
            Require(Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f, "Footprint outside the floor mesh.");
            Vector2 uv = Vector2.Scale((Vector2)local + Vector2.one * 0.5f, scale) + offset;
            min = Vector2.Min(min, uv); max = Vector2.Max(max, uv);
        }
        // Independently locate the continuous lawn between two painted crosses, including their borders.
        Vector2 cell = new(Mathf.Floor(min.x - 0.5f), Mathf.Floor(min.y - 0.5f));
        Require(min.x >= cell.x + 0.5f + 50f / 512f && max.x <= cell.x + 1.5f - 50f / 512f &&
            min.y >= cell.y + 0.5f + 46f / 512f && max.y <= cell.y + 1.5f - 46f / 512f,
            $"POI footprint invades a street: seed={seed} zone={zone.name} bounds={bounds} uv={min}..{max}");
    }

    private static void CheckPlacementBoundaries()
    {
        var manager = Object.FindFirstObjectByType<QuilomboManager>();
        RiskZone pitch = Zone(Names[2]);
        var fields = new SerializedObject(manager);
        var points = fields.FindProperty("spawnPoints");
        var candidates = Enumerable.Range(0, points.arraySize).Select(i =>
            (Transform)points.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Vector3[] original = candidates.Select(p => p.position).ToArray();
        var floor = GameObject.Find("PlazaPiso").transform;
        Vector3 floorPosition = floor.position, floorScale = floor.localScale;
        Quaternion floorRotation = floor.rotation;
        Vector3 pitchPosition = pitch.transform.position, pitchScale = pitch.transform.localScale;
        Quaternion pitchRotation = pitch.transform.rotation;
        var random = UnityEngine.Random.state;
        float originalDistance = fields.FindProperty("minimumElementDistance").floatValue;
        try
        {
            candidates[0].position = new Vector3(0,20,0);
            Require(!manager.FitsLawn(pitch, candidates[0].position), "A candidate on a road is accepted.");
            candidates[0].position = original[0];
            Require(manager.FitsLawn(pitch, (Vector2)original[0] + Vector2.right * 2f), "Valid placement near the lawn boundary rejected.");
            Require(!manager.FitsLawn(pitch, (Vector2)original[0] + Vector2.right * 2.1f), "Street-side overhang accepted.");
            Require(!manager.FitsLawn(pitch, new Vector2(10000,10000)), "Floor edge overhang accepted.");
            pitch.transform.localScale *= 4;
            Require(!manager.FitsLawn(pitch, original[0]), "Oversized pitch accepted.");
            pitch.transform.localScale = pitchScale;
            pitch.transform.rotation = Quaternion.Euler(0,0,35);
            Require(manager.FitsLawn(pitch, original[0]), "Rotated pitch cannot fit a central lawn.");
            pitch.transform.rotation = pitchRotation;
            var renderer = pitch.GetComponent<SpriteRenderer>();
            Sprite originalSprite = renderer.sprite;
            Sprite shiftedPivot = Sprite.Create(originalSprite.texture, originalSprite.rect,
                new Vector2(0.2f,0.8f), originalSprite.pixelsPerUnit);
            try
            {
                renderer.sprite = shiftedPivot;
                Require(!manager.FitsLawn(pitch,(Vector2)original[0] + Vector2.right * 1.7f), "Sprite pivot ignored by footprint.");
            }
            finally { renderer.sprite = originalSprite; Object.DestroyImmediate(shiftedPivot); }
            // A child outside the root sprite must also invalidate the placement.
            var child = new GameObject("Footprint child check");
            child.transform.SetParent(pitch.transform, false);
            child.transform.localPosition = Vector3.right * 4;
            child.AddComponent<SpriteRenderer>().sprite = pitch.GetComponent<SpriteRenderer>().sprite;
            Require(!manager.FitsLawn(pitch, original[0]), "Child decoration ignored by footprint.");
            Object.DestroyImmediate(child);
            foreach (Vector3 scale in new[] { new Vector3(24000,28000,1),new Vector3(-24000,28000,1) })
            {
                Vector3 local = floor.InverseTransformPoint(original[0]);
                floor.position = new Vector3(73,-41,5);
                floor.rotation = Quaternion.Euler(0,0,27);
                floor.localScale = scale;
                Vector2 center = floor.TransformPoint(local);
                Require(manager.FitsLawn(pitch,center), "Local/world floor mapping lost a valid region.");
                pitch.transform.position = new Vector3(center.x,center.y,pitchPosition.z);
                CheckGroundFootprint(pitch,-2);
                Vector3 streetLocal = local; streetLocal.x = 0;
                Require(!manager.FitsLawn(pitch,floor.TransformPoint(streetLocal)), "Transformed floor road accepted.");
                floor.SetPositionAndRotation(floorPosition,floorRotation); floor.localScale = floorScale;
            }
            pitch.transform.position = pitchPosition;
            var floorRenderer = floor.GetComponent<Renderer>();
            Material originalMaterial = floorRenderer.sharedMaterial;
            var changedMaterial = new Material(originalMaterial);
            try
            {
                floorRenderer.sharedMaterial = changedMaterial;
                changedMaterial.SetTextureOffset("_BaseMap",Vector2.one * 0.5f);
                Require(!manager.FitsLawn(pitch,original[0]), "Material offset ignored by lawn regions.");
                changedMaterial.SetTextureOffset("_BaseMap",Vector2.zero);
                changedMaterial.SetTextureScale("_BaseMap",originalMaterial.GetTextureScale("_BaseMap") * 2);
                Require(!manager.FitsLawn(pitch,original[0]), "Material tiling ignored by footprint capacity.");
            }
            finally { floorRenderer.sharedMaterial = originalMaterial; Object.DestroyImmediate(changedMaterial); }
            // Only slot 0 fits the pitch. The earlier small POIs must leave it available.
            for (int i = 1; i < candidates.Length; i++) candidates[i].position = original[i] + Vector3.right * 2.1f;
            Require(manager.FitsLawn(pitch,candidates[0].position) && candidates.Skip(1).All(p => !manager.FitsLawn(pitch,p.position)),
                "Scarce-candidate fixture did not constrain the pitch.");
            var rows = new System.Collections.Generic.List<string> { "seed,pitchCandidate,activePOIs" };
            for (int seed = 0; seed < 100; seed++)
            {
                UnityEngine.Random.InitState(seed); Invoke(manager,"Start");
                RiskZone[] zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
                Require(zones.Length == 6 && Vector2.Distance(pitch.transform.position,candidates[0].position) < 0.01f,
                    "Sequential selection starved the pitch: seed=" + seed);
                foreach (RiskZone zone in zones) CheckGroundFootprint(zone,seed);
                rows.Add($"{seed},0,{zones.Length}");
            }
            File.WriteAllLines(Path.Combine(output,"scarce-candidate-seeds.csv"),rows);
            // Taking the middle of three nearby candidates blocks two slots needed by six POIs.
            fields.FindProperty("minimumElementDistance").floatValue = 4.5f;
            fields.ApplyModifiedPropertiesWithoutUndo();
            candidates[0].position = original[0] + Vector3.left * 2.5f;
            candidates[1].position = original[0];
            candidates[2].position = original[0] + Vector3.right * 2.5f;
            for (int i = 3; i < 7; i++) candidates[i].position = original[i-2];
            candidates[7].position = candidates[6].position;
            var capacityRows = new System.Collections.Generic.List<string> { "seed,activePOIs" };
            for (int seed = 0; seed < 100; seed++)
            {
                UnityEngine.Random.InitState(seed); Invoke(manager,"Start");
                var active = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
                Require(active.Length == 6, "Nearby candidates starved a later POI: seed=" + seed);
                foreach (RiskZone zone in active) CheckGroundFootprint(zone,seed);
                capacityRows.Add($"{seed},{active.Length}");
            }
            File.WriteAllLines(Path.Combine(output,"capacity-seeds.csv"),capacityRows);
            for (int i = 0; i < candidates.Length; i++) candidates[i].position = original[i];
            fields.FindProperty("minimumElementDistance").floatValue = originalDistance;
            fields.ApplyModifiedPropertiesWithoutUndo();
            var allZones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
            Vector3[] previousPositions = allZones.Select(z => z.transform.position).ToArray();
            pitch.transform.localScale *= 4;
            expectedPlacementErrors = 0;
            expectPlacementFailure = true;
            try { Invoke(manager,"Start"); }
            finally { expectPlacementFailure = false; }
            Require(expectedPlacementErrors == 1 && allZones.All(z => !z.gameObject.activeSelf) &&
                allZones.Select((z,i) => z.transform.position == previousPositions[i]).All(same => same),
                "Impossible placement must report one explicit error and apply no partial assignment.");
            pitch.transform.localScale = pitchScale;
            foreach (RiskZone zone in allZones) zone.gameObject.SetActive(true);
            Invoke(manager,"Start");
            Require(allZones.All(z => z.gameObject.activeSelf), "Placement did not recover after invalid fixture cleanup.");
        }
        finally
        {
            for (int i = 0; i < candidates.Length; i++) candidates[i].position = original[i];
            fields.FindProperty("minimumElementDistance").floatValue = originalDistance;
            fields.ApplyModifiedPropertiesWithoutUndo();
            floor.SetPositionAndRotation(floorPosition,floorRotation); floor.localScale = floorScale;
            pitch.transform.SetPositionAndRotation(pitchPosition,pitchRotation); pitch.transform.localScale = pitchScale;
            UnityEngine.Random.state = random;
        }
        Debug.Log("PLACEMENT_BOUNDARIES_PASS moved-candidate,street-overhang,floor-edge,oversize,rotation,pivot,child,local-world,negative-scale,tiling,offset,controlled-failure,recovery=True scarcity-seeds=0..99 capacity-seeds=0..99 active=6");
    }

    private static void CheckHealing()
    {
        var target = new GameObject("Healing check");
        try
        {
            var health = target.AddComponent<Health>();
            health.TakeDamage(3);
            health.Heal(1);
            Require(health.CurrentHealth == 3 && health.MaximumHealth == 5, "Basic healing.");
            health.Heal(0);
            health.Heal(-1);
            Require(health.CurrentHealth == 3, "Invalid healing changed health.");
            health.Heal(int.MaxValue);
            Require(health.CurrentHealth == 5, "Healing exceeds maximum.");
            health.IncreaseMaximumHealth(2);
            health.TakeDamage(1.2f);
            health.Heal(2);
            Require(health.CurrentHealth == 7 && health.MaximumHealth == 7, "Healing ignores bonus/fractional health.");
            health.TakeDamage(7);
            health.Heal(2);
            Require(health.IsDead && health.CurrentHealth == 0, "Healing revives a dead target.");
        }
        finally { Object.DestroyImmediate(target); }
        var playerHealth = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        Require(playerHealth.TakeDamage(1), "Invulnerability fixture.");
        playerHealth.Heal(1);
        Require(!playerHealth.TakeDamage(1), "Healing reset invulnerability.");
        Debug.Log("HEALING_CHECK_PASS positive-only,cap,maximum-bonus,fractional,no-revive,preserve-invulnerability=True");
    }

    private static async Task CheckPhysicalVisit(CharacterData character)
    {
        await FreshMain(character);
        RiskZone pitch = Zone(Names[2]);
        var player = Object.FindFirstObjectByType<PlayerController>();
        Health health = player.GetComponent<Health>();
        var game = Object.FindFirstObjectByType<GameManager>();
        var quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
        Require(health.TakeDamage(3), "Damage before pitch.");
        await MoveTo(pitch.transform.position);
        await Until(() => pitch.State == RiskZone.ZoneState.Active);
        await Task.Delay(300);
        float progress = pitch.Progress;
        Require(progress > 0 && progress < 6 && pitch.HasPendingReward && quilombo.Current == 0, "Pitch early progress.");
        game.PauseGame();
        await Task.Delay(250);
        Require(pitch.Progress == progress && quilombo.Current == 0, "Pause advanced pitch.");
        game.ContinueGame();
        if (character.name == "LaPibaDelClub") await Capture("canchita-resistiendo-1280x720");
        await MoveTo(pitch.transform.position + Vector3.right * 3);
        Require(pitch.State == RiskZone.ZoneState.Inactive && pitch.Progress == 0 && pitch.HasPendingReward &&
            quilombo.Current == 0 && health.CurrentHealth == health.MaximumHealth - 3, "Early exit did not reset.");
        await MoveTo(pitch.transform.position);
        float start = Time.time;
        float alreadyHeld = pitch.Progress;
        await Until(() => pitch.State == RiskZone.ZoneState.Completed);
        Require(Time.time - start + alreadyHeld >= 5.98f && !pitch.HasPendingReward && quilombo.Current == 10 &&
            health.CurrentHealth == health.MaximumHealth - 1, "Actual six-second hold/reward.");
        if (character.name == "LaPibaDelClub")
        {
            await Task.Delay(200);
            await Capture("canchita-recuperacion-1280x720");
        }
        Require(health.TakeDamage(1), "Damage after reward.");
        int life = health.CurrentHealth;
        await MoveTo(pitch.transform.position + Vector3.right * 3);
        await MoveTo(pitch.transform.position);
        Require(quilombo.Current == 10 && health.CurrentHealth == life && !pitch.HasPendingReward, "Pitch can be farmed.");
        Debug.Log("PITCH_VISIT_PASS character=" + character.name + " physics=enter,exit real-hold=6s healing=2 pause,one-use=True");
    }

    private static async Task CheckFullHealthAndDeath()
    {
        await FreshMain(CharacterData.All[0]);
        RiskZone pitch = Zone(Names[2]);
        var health = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        RiskZone kiosk = Zone(Names[0]);
        var experience = health.GetComponent<ExperienceSystem>();
        float radius = experience.CollectionRadius;
        await Complete(kiosk);
        await MoveTo(pitch.transform.position);
        Require(Notice(pitch) && !Notice(kiosk) && experience.CollectionRadius > radius,
            "Kiosk notice masks current challenge or bonus was cancelled.");
        await Capture("canchita-con-kiosco-activo-1280x720");
        SetProgress(pitch);
        Invoke(pitch, "Update");
        Require(health.CurrentHealth == health.MaximumHealth && !pitch.HasPendingReward &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 20 && Notice(pitch) && !Notice(kiosk),
            "Full-health visit semantics or reward notice priority.");
        await FreshMain(CharacterData.All[1]);
        pitch = Zone(Names[2]);
        health = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        await MoveTo(pitch.transform.position);
        Require(pitch.State == RiskZone.ZoneState.Active, "Death fixture did not enter pitch.");
        health.TakeDamage(health.MaximumHealth);
        SetProgress(pitch);
        Invoke(pitch, "Update");
        Data(pitch).ApplyReward(Object.FindFirstObjectByType<PlayerController>());
        Require(health.IsDead && health.CurrentHealth == 0 && !pitch.HasPendingReward &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 0 &&
            Object.FindFirstObjectByType<GameManager>().State == GameManager.GameState.Defeat,
            "Dead player receives completion or revives.");
        Debug.Log("PITCH_BOUNDARIES_PASS full-health=consumed-once death=no-reward,no-revive");
    }

    private static async Task CheckVisitOrders()
    {
        int[][] orders = { new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0} };
        foreach (int[] order in orders)
        {
            await FreshMain(CharacterData.All[0]);
            var quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
            var waves = Object.FindFirstObjectByType<WaveDirector>();
            float expected = 0;
            foreach (int index in order)
            {
                RiskZone zone = Zone(Names[index]);
                await Complete(zone);
                expected += Data(zone).QuilomboReward;
                Require(quilombo.Current == expected && (float)Field(waves, "quilomboThresholdBonus") ==
                    (expected >= 25 ? 0.35f : 0.15f) && !(bool)Field(waves, "elitesEnabled"), "Visit order thresholds.");
            }
            Require(quilombo.Current == 35 && Mathf.Approximately(Object.FindFirstObjectByType<WeaponController>().DamageMultiplier, 1.2f),
                "Three rewards/Parrilla changed.");
            waves.enabled = true;
            waves.Tick(210);
            Require(Mathf.Abs((float)Field(waves, "nextSpawnSeconds") - (210 + 0.57f / 1.7f)) < 0.0001f,
                "Q=35 interval multiplier.");
            waves.enabled = false;
            Object.FindFirstObjectByType<GameManager>().RestartGame();
            await Task.Delay(250);
            Require(Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None).All(z => z.HasPendingReward && z.Progress == 0) &&
                Object.FindFirstObjectByType<QuilomboSystem>().Current == 0 &&
                Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().CurrentHealth == CharacterData.Selected.StartingHealth,
                "Restart retains visits/health.");
        }
        Debug.Log("PITCH_ROUTES_PASS orders=6 real-contacts=True timer-fixtures=True quilombo=35 thresholds=10,25 multiplier=1.7 restart=True");
    }

    private static async Task CheckIndicators()
    {
        await FreshMain(CharacterData.All[1]);
        var player = Object.FindFirstObjectByType<PlayerController>();
        player.GetComponent<WeaponController>().ApplyTemporaryDamage(0.2f, 20f);
        player.GetComponent<WeaponController>().ApplyTemporaryAttack(0.15f, 20f);
        player.ApplyTemporaryMovement(0.15f, 15f);
        player.GetComponent<ExperienceSystem>().ApplyCollectionRadiusMultiplier(1.25f, 10f);
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        Health target = null;
        for (int i = 0; i < 24; i++)
        {
            string prefab = i % 2 == 0 ? "MosquitoGigante" : "PalomaDePlaza";
            Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(0.1f + (i % 6) * 0.16f, 0.15f + (i / 6) * 0.22f, 10));
            position.z = 0;
            GameObject actor = pool.Spawn(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + prefab + ".prefab"), position, Quaternion.identity);
            foreach (MonoBehaviour behavior in actor.GetComponents<MonoBehaviour>()) behavior.enabled = false;
            actor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            target = actor.GetComponent<Health>();
        }
        var weapons = player.GetComponent<WeaponController>();
        foreach (string name in new[] { "Aerosol", "Petardo", "Fernets Orbitales" })
            Require(weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + name + ".asset")), "Visual effects fixture.");
        foreach (object item in (System.Collections.IList)Field(weapons, "equipped"))
        {
            WeaponData data = (WeaponData)item.GetType().GetField("Data").GetValue(item);
            typeof(WeaponController).GetMethod(data.Pattern == WeaponPattern.Orbiting ? "EnsureOrbitals" : "Fire", Flags)
                .Invoke(weapons, data.Pattern == WeaponPattern.Orbiting ? new[] { item } : new object[] { item, target });
        }
        Time.timeScale = 0;
        GameUi.CheckTextLayout = (rect, value, style) =>
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(value)) return;
            var content = new GUIContent(value);
            Require((style.wordWrap ? style.CalcHeight(content, rect.width) : style.CalcSize(content).y) <= rect.height + 2 &&
                (style.wordWrap || style.CalcSize(content).x <= rect.width + 2), "POI/buff layout: " + value);
        };
        RiskZone[] zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        foreach (var size in new[] { new Vector2Int(1024,768),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080) })
        {
            await SetSize(size.x, size.y);
            foreach (RiskZone zone in zones)
            {
                float depth = Camera.main.WorldToViewportPoint(zone.transform.position).z;
                zone.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(2,0.05f,depth));
            }
            await Task.Delay(100);
            await Capture("indicadores-6-" + size.x + "x" + size.y);
            var panels = (Rect[])Field(Object.FindFirstObjectByType<GameHud>(), "poiPanels");
            Require(panels.Length == 6 && panels.All(p => p.width > 0 && p.height > 0), "HUD did not place all six indicators.");
            for (int i = 0; i < 6; i++)
                for (int j = i+1; j < 6; j++) Require(!panels[i].Overlaps(panels[j]), "Actual HUD indicators overlap.");
            RiskZone noticeZone = zones.Single(z => z.name == Names[3]);
            typeof(RiskZone).GetField("<State>k__BackingField",Flags).SetValue(noticeZone,RiskZone.ZoneState.Active);
            Time.timeScale = 1;
            await Capture("indicadores-6-con-aviso-" + size.x + "x" + size.y);
            Require(noticeZone.TryGetNoticePanel(out Rect notice) && panels.All(p => !p.Overlaps(notice)),
                "POI notice covers an indicator.");
            for (int i = 0; i < 6; i++)
                for (int j = i+1; j < 6; j++) Require(!panels[i].Overlaps(panels[j]), "Indicators overlap above the notice.");
            Time.timeScale = 0;
            typeof(RiskZone).GetField("<State>k__BackingField",Flags).SetValue(noticeZone,RiskZone.ZoneState.Inactive);
            Time.timeScale = 1;
            foreach (RiskZone zone in zones)
            {
                typeof(RiskZone).GetField("<State>k__BackingField",Flags).SetValue(zone,RiskZone.ZoneState.Active);
                SetProgress(zone);
                await Capture("poi-previa-" + zone.name + "-" + size.x + "x" + size.y);
                typeof(RiskZone).GetField("rewardStarted",Flags).SetValue(zone,true);
                if (Data(zone).RewardDuration == 0)
                    typeof(RiskZone).GetField("<State>k__BackingField",Flags).SetValue(zone,RiskZone.ZoneState.Completed);
                typeof(RiskZone).GetField("feedbackUntil",Flags).SetValue(zone,Time.unscaledTime + 30);
                typeof(RiskZone).GetField("rewardEndTime",Flags).SetValue(zone,Time.time + Data(zone).RewardDuration);
                await Capture("poi-premio-" + zone.name + "-" + size.x + "x" + size.y);
                typeof(RiskZone).GetField("rewardStarted",Flags).SetValue(zone,false);
                typeof(RiskZone).GetField("feedbackUntil",Flags).SetValue(zone,0f);
                typeof(RiskZone).GetField("<Progress>k__BackingField",Flags).SetValue(zone,0f);
                typeof(RiskZone).GetField("<State>k__BackingField",Flags).SetValue(zone,RiskZone.ZoneState.Inactive);
            }
            Time.timeScale = 0;
        }
        GameUi.CheckTextLayout = null;
        Debug.Log("PITCH_INDICATORS_PASS actual-HUD=6-on-same-edge with-4-buffs,with-without-notice POI-copy=6-preview+reward enemies=24 effects=4-weapons resolutions=4");
    }

    private static async Task CheckWorldLabels()
    {
        await FreshMain(CharacterData.All[0]);
        await SetSize(1280,720);
        Time.timeScale = 0;
        var zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        RiskZone pitch = Zone(Names[2]), mate = Zone(Names[4]);
        Vector3 validPitchPosition = pitch.transform.position;
        Camera.main.GetComponent<CameraFollow>().enabled = false;
        float pitchDepth = Camera.main.WorldToViewportPoint(pitch.transform.position).z;
        Vector3 projectedPitch = Camera.main.ScreenToWorldPoint(new Vector3(570,530,pitchDepth)) - Vector3.up * 1.4f;
        Camera.main.transform.position += validPitchPosition - projectedPitch;
        pitch.transform.position = validPitchPosition;
        CheckGroundFootprint(pitch,-3);
        foreach (RiskZone zone in zones)
            if (zone != pitch) zone.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(-2,0.1f,pitchDepth));
        mate.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(0.8f,2,pitchDepth));
        var label = new Rect(454,190,232,34);
        bool drawn = false;
        GameUi.CheckTextLayout = (rect,value,style) =>
        {
            if (value == Names[2] && Mathf.Approximately(rect.width,192)) drawn = true;
        };
        try
        {
            await Task.Delay(100);
            var hud = Object.FindFirstObjectByType<GameHud>();
            var panels = (Rect[])Field(hud,"poiPanels");
            Require(panels.Any(p => p.Overlaps(label)), "UI-02 fixture did not overlap the world label.");
            await Capture("ui-02-label-ceding-1280x720");
            Require(!drawn, "World name covers an offscreen POI indicator.");
            mate.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(0.8f,-2,pitchDepth));
            drawn = false;
            await Task.Delay(100);
            await Capture("ui-02-label-clear-1280x720");
            Require(drawn, "World name stayed hidden after the indicator moved away.");
            typeof(RiskZone).GetField("rewardStarted",Flags).SetValue(pitch,true);
            bool used = false;
            GameUi.CheckTextLayout = (rect,value,style) => { if (value == "Ya pasaste por acá") used = true; };
            await Capture("poi-usado-1280x720");
            Require(used, "Used POI status is missing.");
        }
        finally { GameUi.CheckTextLayout = null; }
        Debug.Log("WORLD_LABEL_PASS UI-02 collision=yield clear=visible same-GUI-pass=True");
    }

    private static async Task CheckBuffEdges()
    {
        await FreshMain(CharacterData.All[0]);
        var player = Object.FindFirstObjectByType<PlayerController>();
        var weapons = player.GetComponent<WeaponController>();
        var experience = player.GetComponent<ExperienceSystem>();
        var fernet = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Fernets Orbitales.asset");
        weapons.EquipWeapon(fernet);
        Invoke(weapons, "Update");
        var item = ((System.Collections.IList)Field(weapons,"equipped"))[0];
        var pending = item.GetType().GetField("NextAttackTime");
        var orbital = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First();
        var contacts = (System.Collections.Generic.Dictionary<Health,float>)Field(orbital,"nextOrbitHit");
        Health target = player.GetComponent<Health>();
        contacts[target] = Time.time + 10;
        pending.SetValue(item, Time.time + 10);
        float orbitInterval = (float)Field(orbital,"orbitInterval");
        weapons.ApplyTemporaryAttack(0.15f, 0.2f);
        Require(Mathf.Abs(contacts[target] - Time.time - 8.5f) < 0.001f &&
            Mathf.Approximately((float)Field(orbital,"orbitInterval"),orbitInterval * 0.85f),
            "Existing orbital contact cooldown did not follow cadence.");
        await Until(() => weapons.AttackBoostRemaining == 0);
        float pendingBefore = (float)pending.GetValue(item) - Time.time, orbitBefore = contacts[target] - Time.time;
        Invoke(weapons,"Update");
        Require(Mathf.Abs((float)pending.GetValue(item) - Time.time - pendingBefore / 0.85f) < 0.001f &&
            Mathf.Abs(contacts[target] - Time.time - orbitBefore / 0.85f) < 0.001f,
            "Cadence expiry did not rescale pending shot/orbital contact.");
        weapons.ReduceFireInterval(0.0001f);
        pending.SetValue(item,Time.time + 10);
        weapons.ApplyTemporaryAttack(0.15f,0.2f);
        Require(Mathf.Abs((float)pending.GetValue(item) - Time.time - 10) < 0.001f,
            "Clamped temporary cadence accelerated the pending shot.");
        weapons.ReduceFireInterval(0.9f);
        weapons.ReduceWeaponFireInterval(weapons.GetEquippedWeapon(0),0.9f);
        Require(Mathf.Abs((float)pending.GetValue(item) - Time.time - 10) < 0.001f,
            "Permanent cadence at the clamp accelerated the pending shot.");

        float radius = experience.CollectionRadius;
        experience.ApplyCollectionRadiusMultiplier(1.4f,0.1f);
        experience.ApplyCollectionRadiusMultiplier(1.1f,0.3f);
        Require(Mathf.Approximately(experience.CollectionRadius,radius * 1.4f) && experience.CollectionBonusRemaining > 0.29f,
            "Weaker collection renewal reduced active intensity or failed to renew duration.");
        player.ApplyTemporaryMovement(0.3f,0.1f);
        player.ApplyTemporaryMovement(0.1f,0.3f);
        weapons.ApplyTemporaryDamage(0.3f,0.1f);
        weapons.ApplyTemporaryDamage(0.1f,0.3f);
        weapons.ApplyTemporaryAttack(0.3f,0.1f);
        weapons.ApplyTemporaryAttack(0.1f,0.3f);
        Require(Mathf.Approximately(player.MovementBoostPercent,30) && Mathf.Approximately(weapons.DamageBoostPercent,30) &&
            Mathf.Approximately(weapons.AttackBoostPercent,30), "Renewal must retain strongest active intensity.");
        float[] remaining = { experience.CollectionBonusRemaining, player.MovementBoostRemaining, weapons.DamageBoostRemaining, weapons.AttackBoostRemaining };
        var game = Object.FindFirstObjectByType<GameManager>();
        game.PauseGame();
        await Task.Delay(200);
        Require(remaining.SequenceEqual(new[] { experience.CollectionBonusRemaining, player.MovementBoostRemaining,
            weapons.DamageBoostRemaining, weapons.AttackBoostRemaining }), "Pause consumed one of the four benefits.");
        game.ContinueGame();
        await Until(() => weapons.DamageBoostRemaining == 0 && experience.CollectionBonusRemaining == 0 &&
            player.MovementBoostRemaining == 0 && weapons.AttackBoostRemaining == 0);
        experience.ApplyCollectionRadiusMultiplier(1.1f,1);
        player.ApplyTemporaryMovement(0.1f,1);
        weapons.ApplyTemporaryDamage(0.1f,1);
        weapons.ApplyTemporaryAttack(0.1f,1);
        Require(Mathf.Approximately(experience.CollectionRadius,radius * 1.1f) && Mathf.Approximately(player.MovementBoostPercent,10) &&
            Mathf.Approximately(weapons.DamageBoostPercent,10) && Mathf.Approximately(weapons.AttackBoostPercent,10),
            "Expired stronger intensity leaked into a new activation.");
        Debug.Log("BUFF_EDGES_PASS pending-shot,orbital-contact,expiry,clamps,renewal-all-4,strongest,new-after-expiry,pause=True");
    }

    private static async Task CheckTemporaryBenefits()
    {
        await FreshMain(CharacterData.All[0]);
        var player = Object.FindFirstObjectByType<PlayerController>();
        var weapons = player.GetComponent<WeaponController>();
        var experience = player.GetComponent<ExperienceSystem>();
        var game = Object.FindFirstObjectByType<GameManager>();
        float damage = ActualDamage(weapons), interval = weapons.FireInterval, speed = player.MovementSpeed;
        WeaponData startingWeapon = weapons.GetEquippedWeapon(0);
        float rawDamage = startingWeapon.Damage;
        float affinity = startingWeapon.DirectProjectile ? player.Character.DirectProjectileDamageBonus : 0f;
        Require(Mathf.Approximately(damage, rawDamage * (1f + affinity)), "Starting affinity fixture.");
        object first = ((System.Collections.IList)Field(weapons, "equipped"))[0];
        first.GetType().GetField("NextAttackTime").SetValue(first, Time.time + 10f);
        weapons.ApplyTemporaryDamage(0.2f, 1f);
        weapons.ApplyTemporaryAttack(0.15f, 1f);
        player.ApplyTemporaryMovement(0.15f, 1f);
        experience.ApplyCollectionRadiusMultiplier(1.25f, 1f);
        Require(Mathf.Approximately(ActualDamage(weapons), rawDamage * (1.2f + affinity)) &&
            Mathf.Approximately(weapons.FireInterval, interval * 0.85f) &&
            Mathf.Approximately(player.MovementSpeed, speed * 1.15f), "Temporary statistics.");
        Require(Mathf.Abs((float)first.GetType().GetField("NextAttackTime").GetValue(first) - Time.time - 8.5f) < 0.02f,
            "Attack bonus did not rescale pending shot.");
        weapons.ApplyTemporaryDamage(0.3f, 1f);
        weapons.ApplyTemporaryDamage(0.1f, 1f);
        Require(Mathf.Approximately(ActualDamage(weapons), rawDamage * (1.3f + affinity)), "Renewal stacked or weakened active damage.");
        float remaining = weapons.DamageBoostRemaining;
        float[] otherRemaining = { weapons.AttackBoostRemaining, player.MovementBoostRemaining, experience.CollectionBonusRemaining };
        game.PauseGame();
        await Task.Delay(200);
        Require(weapons.DamageBoostRemaining == remaining && otherRemaining.SequenceEqual(new[] {
            weapons.AttackBoostRemaining, player.MovementBoostRemaining, experience.CollectionBonusRemaining }), "Pause consumed buff duration.");
        await Capture("pausa-con-beneficios-1280x720");
        game.ContinueGame();
        experience.AddExperience(experience.RequiredExperience);
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        Require(selection.IsShowing && Time.timeScale == 0f, "Level-up did not pause.");
        await Task.Delay(200);
        Require(weapons.DamageBoostRemaining == remaining && otherRemaining.SequenceEqual(new[] {
            weapons.AttackBoostRemaining, player.MovementBoostRemaining, experience.CollectionBonusRemaining }), "Upgrade choice consumed buff duration.");
        bool benefitDrawn = false;
        GameUi.CheckTextLayout = (rect,value,style) =>
        {
            if (value.StartsWith("Radio de XP") || value.StartsWith("Mural: daño") || value.StartsWith("Ataques: espera") ||
                value.StartsWith("Bici: movimiento")) benefitDrawn = true;
        };
        try { await Capture("seleccion-con-beneficios-1280x720"); Require(!benefitDrawn, "Benefits cover upgrade selection."); }
        finally { GameUi.CheckTextLayout = null; }
        int offeredWeapon = selection.Options.ToList().IndexOf(player.Character.FirstWeaponUpgrade);
        Require(offeredWeapon >= 0 && selection.Options[offeredWeapon].Effect == UpgradeEffect.EquipWeapon,
            "The guaranteed first weapon must be available for the pause fixture.");
        selection.Select(offeredWeapon);
        await Until(() => !selection.IsShowing);
        weapons.ApplyDamageMultiplier(1.2f);
        weapons.IncreaseDamage(1);
        weapons.ReduceFireInterval(0.9f);
        player.IncreaseMovementSpeed(1.1f);
        Require(Mathf.Abs(ActualDamage(weapons) - (rawDamage + 1f) * (1.5f + affinity)) < 0.001f,
            "Parrilla/permanent upgrade lost while damage buff active.");
        await Until(() => weapons.DamageBoostRemaining == 0f && weapons.AttackBoostRemaining == 0f && player.MovementBoostRemaining == 0f);
        Invoke(weapons, "Update");
        Require(Mathf.Abs(ActualDamage(weapons) - (rawDamage + 1f) * (1.2f + affinity)) < 0.001f &&
            Mathf.Abs(weapons.FireInterval - interval * 0.9f) < 0.001f &&
            Mathf.Abs(player.MovementSpeed - speed * 1.1f) < 0.001f,
            "Expiration removed permanent progress or retained a buff.");

        weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Fernets Orbitales.asset"));
        Invoke(weapons, "Update");
        var orbital = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
            .First(p => p.gameObject.activeInHierarchy && (float)Field(p, "damage") > 0);
        float orbitalDamage = (float)Field(orbital, "damage");
        weapons.ApplyTemporaryDamage(0.2f, 0.2f);
        Require((float)Field(orbital, "damage") > orbitalDamage, "Existing orbital ignored damage buff.");
        await Until(() => weapons.DamageBoostRemaining == 0f);
        Invoke(weapons, "Update");
        Require(Mathf.Abs((float)Field(orbital, "damage") - orbitalDamage) < 0.001f, "Orbital retained expired damage.");

        await FreshMain(CharacterData.All[1]);
        player = Object.FindFirstObjectByType<PlayerController>();
        weapons = player.GetComponent<WeaponController>();
        game = Object.FindFirstObjectByType<GameManager>();
        var zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        var snapshots = zones.ToDictionary(z => Data(z), z => EditorJsonUtility.ToJson(Data(z)));
        foreach (string name in Names.Skip(3))
        {
            RiskZone zone = Zone(name);
            int rewards = 0; zone.RewardEarned += _ => rewards++;
            await Complete(zone);
            await MoveTo(zone.transform.position + Vector3.right * 3);
            zone.gameObject.SetActive(false);
            zone.gameObject.SetActive(true);
            await MoveTo(zone.transform.position);
            Invoke(zone, "StartReward");
            Require(rewards == 1 && !zone.HasPendingReward, "New POI reactivated on reentry or disable/enable.");
        }
        Require(game.Score.PoiCount == 3 && game.Score.PoiCentipoints == 75000 &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 15 && weapons.DamageBoostRemaining > 0 &&
            weapons.AttackBoostRemaining > 0 && player.MovementBoostRemaining > 0, "New POIs must coexist and award once.");
        await Complete(Zone(Names[0]));
        GameUi.CheckTextLayout = (rect, value, style) =>
        {
            if (!style.wordWrap) Require(style.CalcSize(new GUIContent(value)).x <= rect.width + 2,
                "POI/buff text overflow: " + value);
        };
        game.PauseGame();
        // Keep GameManager playing with scaled time stopped, so all four HUD benefits remain visible.
        typeof(GameManager).GetField("<State>k__BackingField", Flags).SetValue(game, GameManager.GameState.Playing);
        foreach (var size in new[] { new Vector2Int(1024,768), new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1080) })
        {
            await SetSize(size.x, size.y);
            await Capture("beneficios-4-" + size.x + "x" + size.y);
        }
        GameUi.CheckTextLayout = null;
        foreach (var entry in snapshots) Require(EditorJsonUtility.ToJson(entry.Key) == entry.Value, "Runtime mutated POI definition.");
        game.ContinueGame();
        typeof(GameManager).GetField("<State>k__BackingField", Flags).SetValue(game, GameManager.GameState.Playing);
        Time.timeScale = 1;
        await Complete(Zone(Names[1]));
        await Complete(Zone(Names[2]));
        var waves = Object.FindFirstObjectByType<WaveDirector>();
        Require(game.Score.PoiCount == 6 && game.Score.PoiCentipoints == 150000 &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 50 &&
            Mathf.Approximately((float)Field(waves,"quilomboThresholdBonus"),0.35f) && !(bool)Field(waves,"elitesEnabled"),
            "Six rewards/pressure changed the original thresholds or duplicated score.");
        waves.enabled = true;
        waves.Tick(210);
        Require(Mathf.Abs(waves.CurrentSpawnInterval - 0.57f / 1.85f) < 0.0001f, "Q=50 pressure formula.");
        waves.enabled = false;
        player.GetComponent<Health>().TakeDamage(player.GetComponent<Health>().MaximumHealth);
        Require(weapons.DamageBoostRemaining == 0 && weapons.AttackBoostRemaining == 0 && player.MovementBoostRemaining == 0 &&
            player.GetComponent<ExperienceSystem>().CollectionBonusRemaining == 0, "Run end retained benefits.");
        Require(zones.All(z => !z.HasPendingReward && z.Progress == 0 && !Notice(z)), "Run end retained POI activation/UI.");
        long closedScore = game.Score.PoiCentipoints;
        foreach (RiskZone zone in zones) Invoke(zone,"StartReward");
        Require(game.Score.PoiCentipoints == closedScore, "Closed run accepted another POI reward.");
        game.RestartGame();
        await Until(() => Object.FindFirstObjectByType<GameManager>() != game);
        await Task.Delay(100);
        Require(Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None).All(z => z.HasPendingReward), "Restart retained used POIs.");
        Debug.Log("TEMPORARY_BENEFITS_PASS statistics,renewal,strongest,no-stack,pause,level-up,permanent-upgrade,expiry,orbital,coexistence,disable-enable,one-use,score=6x250,quilombo=50,pressure=1.85,SO-immutable,end-UI,restart,4-resolutions=True");
    }

    private static async Task CheckOtherRunEndings()
    {
        foreach (bool victory in new[] { true, false })
        {
            await FreshMain(CharacterData.All[0]);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var weapons = player.GetComponent<WeaponController>();
            var experience = player.GetComponent<ExperienceSystem>();
            var game = Object.FindFirstObjectByType<GameManager>();
            var zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
            await Complete(Zone(Names[0]));
            weapons.ApplyTemporaryDamage(0.2f,20);
            weapons.ApplyTemporaryAttack(0.15f,20);
            player.ApplyTemporaryMovement(0.15f,15);
            if (victory)
            {
                game.SetElapsedForPractice(game.DurationSeconds);
                await Until(() => game.Result != null);
            }
            else game.ReturnToMenu();
            Require(game.Result != null && game.Result.EndReason == (victory ? GameManager.GameState.Victory : GameManager.GameState.Abandoned) &&
                weapons.DamageBoostRemaining == 0 && weapons.AttackBoostRemaining == 0 && player.MovementBoostRemaining == 0 &&
                experience.CollectionBonusRemaining == 0 && zones.All(z => !z.HasPendingReward && !Notice(z)),
                "Victory/abandonment retained temporary effects or POI UI.");
            if (!victory) await Until(() => SceneManager.GetActiveScene().name == "Menu");
        }
        Debug.Log("POI_RUN_ENDINGS_PASS victory,defeat,abandonment,scene-reload,UI-cleanup=True");
    }

    private static float ActualDamage(WeaponController weapons)
    {
        object first = ((System.Collections.IList)Field(weapons, "equipped"))[0];
        return (float)typeof(WeaponController).GetMethod("GetDamage", Flags).Invoke(weapons, new[] { first });
    }

    private static async Task FreshMain(CharacterData character)
    {
        var previous = Object.FindFirstObjectByType<GameManager>();
        CharacterData.Select(character);
        Time.timeScale = 1;
        SceneManager.LoadScene("Main");
        await Until(() => Object.FindFirstObjectByType<GameManager>() != null &&
            Object.FindFirstObjectByType<GameManager>() != previous &&
            Object.FindFirstObjectByType<GameManager>().ElapsedSeconds > 0);
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<WeaponController>().enabled = false;
        Object.FindFirstObjectByType<PlayerController>().enabled = false;
    }

    private static async Task MoveTo(Vector3 position)
    {
        float fixedTime = Time.fixedTime;
        var body = Object.FindFirstObjectByType<PlayerController>().GetComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        await Until(() => Time.fixedTime > fixedTime);
    }

    private static async Task Complete(RiskZone zone)
    {
        await MoveTo(zone.transform.position);
        if (zone.HasPendingReward) { SetProgress(zone); Invoke(zone, "Update"); }
        Require(!zone.HasPendingReward, "POI did not complete: " + zone.name);
    }

    private static void SetProgress(RiskZone zone) => typeof(RiskZone).GetField("<Progress>k__BackingField", Flags)
        .SetValue(zone, Data(zone).RequiredDuration);
    private static RiskZoneData Data(RiskZone zone) => (RiskZoneData)Field(zone, "data");
    private static bool Notice(RiskZone zone) => (bool)typeof(RiskZone).GetMethod("HasNoticePriority", Flags).Invoke(zone, null);
    private static RiskZone Zone(string name) => Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None).Single(z => z.name == name);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static async Task SetSize(int width, int height) => await (Task)typeof(UiRedesignCheck)
        .GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { width, height });
    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > before && new FileInfo(path).Length > 1000);
    }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Pitch check timed out.");
            await Task.Delay(20);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (expectPlacementFailure && type == LogType.Error && message.StartsWith("No hay una asignación válida de los POIs"))
        { expectedPlacementErrors++; return; }
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { editorSearchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
