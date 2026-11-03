using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClubFeaturesSetup
{
    private const string Root = "Assets/_Game/";

    [MenuItem("Tools/Conurbano Infinito/Integrar personajes del Club")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Guardá las escenas abiertas antes de integrar personajes.");
        AssetDatabase.Refresh();
        Sprite idle = Import(Root + "Art/Sprites/Player/LaPibaDelClub.png");
        Sprite[] frames = Enumerable.Range(1, 6)
            .Select(i => Import(Root + "Art/Animations/Frames/PibaStride" + i + ".png")).ToArray();
        var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "Art/Animations/Player.controller");
        const string controllerPath = Root + "Art/Animations/LaPibaDelClub.overrideController";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
        if (controller == null)
        {
            controller = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(controller, controllerPath);
        }
        controller["PlayerIdle"] = Clip("PibaIdle", new[] { idle });
        controller["PlayerWalk"] = Clip("PibaWalk", frames);
        EditorUtility.SetDirty(controller);

        if (!AssetDatabase.IsValidFolder(Root + "Resources/Characters"))
            AssetDatabase.CreateFolder(Root + "Resources", "Characters");
        var termo = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Termo Balístico.asset");
        var acquire = AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Equipar Termo Balístico.asset");
        if (acquire == null)
        {
            acquire = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(acquire, Root + "Data/Equipar Termo Balístico.asset");
        }
        var upgrade = new SerializedObject(acquire);
        upgrade.FindProperty("displayName").stringValue = "Termo Balístico";
        upgrade.FindProperty("description").stringValue = "Equipá Termo Balístico.";
        upgrade.FindProperty("maximumLevel").intValue = 1;
        upgrade.FindProperty("effect").enumValueIndex = (int)UpgradeEffect.EquipWeapon;
        upgrade.FindProperty("targetWeapon").objectReferenceValue = termo;
        upgrade.ApplyModifiedPropertiesWithoutUndo();
        Profile("ElPibeDelBarrio", "El Pibe del Barrio", "El clásico: aguante y ataques a distancia.", 5, 5f,
            termo, AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Equipar Aerosol.asset"), baseController);
        Profile("LaPibaDelClub", "La Piba del Club", "Más rápida, menos aguante. Buscá espacio.", 4, 5.5f,
            AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Boleadora.asset"), acquire, controller);

        Scene scene = EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
        var selection = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<UpgradeSelection>());
        SerializedProperty upgrades = selection.FindProperty("upgrades");
        if (!Enumerable.Range(0, upgrades.arraySize).Any(i => upgrades.GetArrayElementAtIndex(i).objectReferenceValue == acquire))
        {
            upgrades.InsertArrayElementAtIndex(upgrades.arraySize);
            upgrades.GetArrayElementAtIndex(upgrades.arraySize - 1).objectReferenceValue = acquire;
            selection.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CLUB_CHARACTERS_SETUP_PASS profiles=2 piba=idle+6-walk starting-weapons=Termo,Boleadora");
    }

    [MenuItem("Tools/Conurbano Infinito/Integrar Pelota de Potrero")]
    public static void ApplyBall()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Guardá las escenas abiertas antes de integrar.");
        AssetDatabase.Refresh();
        Sprite sprite = Import(Root + "Art/Sprites/Weapons/PelotaDePotrero.png", true);
        string path = Root + "Data/Pelota de Potrero.asset";
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (weapon == null)
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(weapon, path);
        }
        var data = new SerializedObject(weapon);
        data.FindProperty("pattern").enumValueIndex = (int)WeaponPattern.Ricochet;
        data.FindProperty("damage").intValue = 2;
        data.FindProperty("fireInterval").floatValue = 1.8f;
        data.FindProperty("projectileSpeed").floatValue = 8f;
        data.FindProperty("projectileCount").intValue = 1;
        data.FindProperty("pierce").intValue = 2;
        data.FindProperty("range").floatValue = 5f;
        data.FindProperty("spreadDegrees").floatValue = 0f;
        data.FindProperty("projectileSprite").objectReferenceValue = sprite;
        data.FindProperty("projectileColor").colorValue = Color.white;
        data.FindProperty("projectileScale").vector2Value = Vector2.one * 0.35f;
        data.ApplyModifiedPropertiesWithoutUndo();
        UpgradeData[] cards = {
            BallCard("Equipar Pelota de Potrero", "Pelota de Potrero", "Hasta 2 rebotes entre enemigos cercanos.",
                UpgradeEffect.EquipWeapon, 1, 1, weapon),
            BallCard("Pelota - Daño", "Pelota · Daño", "+1 de daño por impacto.",
                UpgradeEffect.WeaponDamage, 1, 3, weapon),
            BallCard("Pelota - Cadencia", "Pelota · Cadencia", "10% menos de espera entre pelotazos.",
                UpgradeEffect.WeaponFireInterval, 0.9f, 3, weapon),
            BallCard("Pelota - Rebotes", "Pelota · Rebotes", "Un rebote más entre enemigos.",
                UpgradeEffect.WeaponRicochets, 1, 3, weapon)
        };
        Scene scene = EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
        var selection = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<UpgradeSelection>());
        SerializedProperty upgrades = selection.FindProperty("upgrades");
        bool changed = false;
        foreach (UpgradeData card in cards)
        {
            if (Enumerable.Range(0, upgrades.arraySize).Any(i => upgrades.GetArrayElementAtIndex(i).objectReferenceValue == card)) continue;
            upgrades.InsertArrayElementAtIndex(upgrades.arraySize);
            upgrades.GetArrayElementAtIndex(upgrades.arraySize - 1).objectReferenceValue = card;
            changed = true;
        }
        if (changed)
        {
            selection.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CLUB_BALL_SETUP_PASS weapon=Ricochet cards=4 damage=2 interval=1.8 bounces=2 range=5");
    }

    [MenuItem("Tools/Conurbano Infinito/Integrar Canchita del Club")]
    public static void ApplyPitch()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Guardá las escenas abiertas antes de integrar.");
        AssetDatabase.Refresh();
        Sprite sprite = Import(Root + "Art/Sprites/Environment/CanchitaDelClub.png", true);
        string path = Root + "Data/Canchita del Club.asset";
        var data = AssetDatabase.LoadAssetAtPath<RiskZoneData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<RiskZoneData>();
            AssetDatabase.CreateAsset(data, path);
        }
        var fields = new SerializedObject(data);
        fields.FindProperty("displayName").stringValue = "Canchita del Club";
        fields.FindProperty("completionMessage").stringValue = "¡TERCER TIEMPO!";
        fields.FindProperty("requiredDuration").floatValue = 6;
        fields.FindProperty("quilomboReward").floatValue = 10;
        fields.FindProperty("reward").enumValueIndex = (int)RiskZoneReward.Healing;
        fields.FindProperty("healingAmount").intValue = 2;
        fields.FindProperty("rewardDuration").floatValue = 0;
        fields.ApplyModifiedPropertiesWithoutUndo();

        Scene scene = EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
        var pitch = GameObject.Find("Canchita del Club");
        if (pitch == null)
        {
            pitch = new GameObject("Canchita del Club");
            pitch.transform.position = new Vector3(0, -20, 1);
            pitch.AddComponent<SpriteRenderer>();
            pitch.AddComponent<CircleCollider2D>();
            pitch.AddComponent<RiskZone>();
            Undo.RegisterCreatedObjectUndo(pitch, "Integrar Canchita");
        }
        var renderer = pitch.GetComponent<SpriteRenderer>();
        var area = pitch.GetComponent<CircleCollider2D>();
        Undo.RecordObjects(new UnityEngine.Object[] { renderer, area }, "Configurar Canchita");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 0;
        area.isTrigger = true;
        area.radius = 1.1f;
        area.offset = new Vector2(0, -2f / 48);
        var zone = new SerializedObject(pitch.GetComponent<RiskZone>());
        zone.FindProperty("data").objectReferenceValue = data;
        zone.FindProperty("quilombo").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>();
        zone.FindProperty("gameCamera").objectReferenceValue = Camera.main;
        zone.ApplyModifiedProperties();
        var placement = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<QuilomboManager>());
        var elements = placement.FindProperty("elements");
        if (!Enumerable.Range(0, elements.arraySize).Any(i => elements.GetArrayElementAtIndex(i).objectReferenceValue == pitch.GetComponent<RiskZone>()))
        {
            elements.InsertArrayElementAtIndex(elements.arraySize);
            elements.GetArrayElementAtIndex(elements.arraySize - 1).objectReferenceValue = pitch.GetComponent<RiskZone>();
            placement.ApplyModifiedProperties();
        }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CLUB_PITCH_SETUP_PASS POIs=3 duration=6 healing=2 quilombo=10 radius=1.1");
    }

    private static UpgradeData BallCard(string name, string title, string description, UpgradeEffect effect,
        float amount, int levels, WeaponData weapon)
    {
        string path = Root + "Data/" + name + ".asset";
        var card = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (card == null)
        {
            card = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(card, path);
        }
        var data = new SerializedObject(card);
        data.FindProperty("displayName").stringValue = title;
        data.FindProperty("description").stringValue = description;
        data.FindProperty("effect").enumValueIndex = (int)effect;
        data.FindProperty("amount").floatValue = amount;
        data.FindProperty("maximumLevel").intValue = levels;
        data.FindProperty("targetWeapon").objectReferenceValue = weapon;
        data.ApplyModifiedPropertiesWithoutUndo();
        return card;
    }

    private static Sprite Import(string path, bool centered = false)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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
        settings.spritePivot = new Vector2(0.5f, centered ? 0.5f : 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AnimationClip Clip(string name, Sprite[] sprites)
    {
        string path = Root + "Art/Animations/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 10;
        var frames = Enumerable.Range(0, sprites.Length + 1)
            .Select(i => new ObjectReferenceKeyframe { time = i / 10f, value = sprites[i % sprites.Length] }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), frames);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void Profile(string name, string display, string description, int health, float speed,
        WeaponData weapon, UpgradeData upgrade, RuntimeAnimatorController controller)
    {
        string path = Root + "Resources/Characters/" + name + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(profile, path);
        }
        var data = new SerializedObject(profile);
        data.FindProperty("displayName").stringValue = display;
        data.FindProperty("description").stringValue = description;
        data.FindProperty("startingHealth").intValue = health;
        data.FindProperty("movementSpeed").floatValue = speed;
        data.FindProperty("startingWeapon").objectReferenceValue = weapon;
        data.FindProperty("firstWeaponUpgrade").objectReferenceValue = upgrade;
        data.FindProperty("animatorController").objectReferenceValue = controller;
        data.FindProperty("portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(
            Root + "Resources/CharacterPortraits/" + name + ".png");
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
