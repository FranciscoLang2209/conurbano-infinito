using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CombatFeaturesSetup
{
    private const string Root = "Assets/_Game/";
    private const string Art = Root + "Art/";
    private const string Animations = Art + "Animations/";

    [MenuItem("Tools/Conurbano Infinito/Integrar animaciones y Vecino Gomero")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar.");
        Sprite[] stride = Enumerable.Range(1, 6).Select(i => Frame("PlayerStride" + i, false)).ToArray();
        Clip("PlayerWalk", 10f, stride);
        AnimatorController dog = PhaseController("PerroCallejero", "ChargePhase",
            Clip("DogRun", 8f, Frame("DogRun1"), Frame("DogRun2")),
            Clip("DogAim", 6f, Frame("DogAim1"), Frame("DogAim2")),
            Clip("DogDash", 12f, Frame("DogDash1"), Frame("DogDash2")),
            Clip("DogRest", 4f, Frame("DogRest1"), Frame("DogRest2")));
        string dogPath = Root + "Prefabs/PerroCallejero.prefab";
        GameObject dogPrefab = PrefabUtility.LoadPrefabContents(dogPath);
        try
        {
            Animator animator = dogPrefab.GetComponent<Animator>();
            if (animator == null) animator = dogPrefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = dog;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(dogPrefab, dogPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(dogPrefab); }

        Sprite stoneSprite = Import(Art + "Sprites/Enemies/PiedraGomera.png", new Vector2(0.5f, 0.5f));
        var stone = new GameObject("PiedraGomera");
        GameObject stoneAsset;
        try
        {
            var renderer = stone.AddComponent<SpriteRenderer>();
            renderer.sprite = stoneSprite;
            renderer.sortingOrder = 3;
            stone.AddComponent<EnemyStone>();
            stoneAsset = PrefabUtility.SaveAsPrefabAsset(stone, Root + "Prefabs/PiedraGomera.prefab");
        }
        finally { Object.DestroyImmediate(stone); }

        AnimatorController vecino = PhaseController("VecinoGomero", "RangedPhase",
            Clip("VecinoWalk", 6f, Frame("VecinoWalk1"), Frame("VecinoWalk2")),
            Clip("VecinoAim", 4f, Frame("VecinoAim")),
            Clip("VecinoRest", 4f, Frame("VecinoRest")));
        var enemy = new GameObject("VecinoGomero");
        GameObject enemyAsset;
        try
        {
            enemy.layer = LayerMask.NameToLayer("Enemy");
            var renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = Frame("VecinoRest");
            renderer.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            var body = enemy.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var collider = enemy.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
            var health = new SerializedObject(enemy.AddComponent<Health>());
            health.FindProperty("maximumHealth").intValue = 12;
            health.FindProperty("destroyOnDeath").boolValue = true;
            health.ApplyModifiedPropertiesWithoutUndo();
            enemy.AddComponent<ContactDamage>();
            var drop = new SerializedObject(enemy.AddComponent<ExperienceDrop>());
            drop.FindProperty("gemPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ExperienceGem.prefab").GetComponent<ExperienceGem>();
            drop.FindProperty("experienceValue").intValue = 3;
            drop.ApplyModifiedPropertiesWithoutUndo();
            enemy.AddComponent<Animator>().runtimeAnimatorController = vecino;
            enemy.AddComponent<HitFlash>();
            var ranged = new SerializedObject(enemy.AddComponent<EnemyRanged>());
            ranged.FindProperty("stonePrefab").objectReferenceValue = stoneAsset.GetComponent<EnemyStone>();
            ranged.ApplyModifiedPropertiesWithoutUndo();
            enemyAsset = PrefabUtility.SaveAsPrefabAsset(enemy, Root + "Prefabs/VecinoGomero.prefab");
        }
        finally { Object.DestroyImmediate(enemy); }

        WaveData waves = AssetDatabase.LoadAssetAtPath<WaveData>(Root + "Data/Oleadas.asset");
        for (int index = 0; index < waves.Phases.Length; index++)
        {
            WaveData.Phase phase = waves.Phases[index];
            if (phase.startSeconds < 120f) continue;
            WaveData.Phase updated = phase;
            if (!phase.enemies.Any(option => option.prefab == enemyAsset))
                updated.enemies = phase.enemies.Concat(new[] { new WaveData.SpawnOption { prefab = enemyAsset, weight = 1 } }).ToArray();
            if (phase.startSeconds == 120f) updated.title = "Gomeras: salí de la línea roja";
            waves.Phases[index] = updated;
        }
        EditorUtility.SetDirty(waves);
        AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_FEATURES_SETUP_PASS Player=6-frames Dog=4-phases Vecino=from-120s");
    }

    private static Sprite Frame(string name, bool centered = true) =>
        Import(Animations + "Frames/" + name + ".png", new Vector2(0.5f, centered ? 0.5f : 0f));

    private static Sprite Import(string path, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Falta sprite: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 48;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new Exception("Sprite inválido: " + path);
    }

    private static AnimationClip Clip(string name, float fps, params Sprite[] frames)
    {
        string path = Animations + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = fps;
        // Repeat the first frame at the boundary so every pose gets one full frame.
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i % frames.Length] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController PhaseController(string name, string parameter, params AnimationClip[] clips)
    {
        string path = Animations + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        else if (controller.layers.Length > 0) return controller;
        else controller.AddLayer("Base Layer");
        controller.AddParameter(parameter, AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimatorState state = machine.AddState(clips[i].name);
            state.motion = clips[i];
            if (i == 0) machine.defaultState = state;
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, i, parameter);
        }
        return controller;
    }
}
