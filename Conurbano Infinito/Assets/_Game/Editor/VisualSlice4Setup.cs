using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class VisualSlice4Setup
{
    private const string Art = "Assets/_Game/Art/";
    private const string Animations = Art + "Animations/";
    private const string Prefabs = "Assets/_Game/Prefabs/";
    private static readonly string[] Enemies =
        { "MosquitoGigante", "PalomaDePlaza", "BolsaVoladora", "LaJefa" };

    [MenuItem("Tools/Conurbano Infinito/Integrar cuarta slice visual")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar animaciones.");

        Sprite player = Sprite("Sprites/Player/ElPibeDelBarrio.png");
        AnimationClip idle = Clip("PlayerIdle", 3f, player, Frame("PlayerIdle2"));
        AnimationClip walk = Clip("PlayerWalk", 8f, Frame("PlayerWalk1"),
            Frame("PlayerWalk2"), Frame("PlayerWalk3"), Frame("PlayerWalk4"));
        AnimatorController playerController = Controller("Player", idle, walk);
        WirePrefab("Player", playerController, false);

        foreach (string enemy in Enemies)
        {
            AnimationClip move = Clip(enemy + "Move", 6f,
                Sprite("Sprites/Enemies/" + enemy + ".png"), Frame(enemy + "Move2"));
            WirePrefab(enemy, Controller(enemy, move, null), true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Cuarta slice visual: Player y cuatro enemigos animados e integrados.");
    }

    private static Sprite Frame(string name)
    {
        string path = Animations + "Frames/" + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Falta cuadro: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 48;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return Sprite("Animations/Frames/" + name + ".png");
    }

    private static Sprite Sprite(string relativePath) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(Art + relativePath) ??
        throw new Exception("Falta sprite: " + Art + relativePath);

    private static AnimationClip Clip(string name, float framesPerSecond, params Sprite[] frames)
    {
        string path = Animations + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = framesPerSecond;
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / framesPerSecond, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController Controller(string name, AnimationClip first, AnimationClip second)
    {
        string path = Animations + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        var firstState = machine.AddState(first.name);
        firstState.motion = first;
        machine.defaultState = firstState;
        if (second != null)
        {
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var secondState = machine.AddState(second.name);
            secondState.motion = second;
            var toWalk = firstState.AddTransition(secondState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
            var toIdle = secondState.AddTransition(firstState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
        }
        return controller;
    }

    private static void WirePrefab(string name, AnimatorController controller, bool enemy)
    {
        string path = Prefabs + name + ".prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (prefab.GetComponent<SpriteRenderer>() == null)
                throw new Exception("Falta SpriteRenderer en " + name);
            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null) animator = prefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            if (enemy && prefab.GetComponent<EnemyVisual>() == null) prefab.AddComponent<EnemyVisual>();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar cuarta slice en Play Mode")]
    public static async void CheckPlayMode()
    {
        if (!Application.isPlaying || Time.timeScale != 1f)
            throw new Exception("Iniciá Play Mode en Main, sin pausa.");
        var player = Object.FindFirstObjectByType<PlayerController>();
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        if (player == null || pool == null) throw new Exception("Falta Player o pool.");
        if (player.GetComponent<Animator>()?.runtimeAnimatorController == null)
            throw new Exception("Player no tiene Animator configurado.");

        try
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) throw new Exception("No hay teclado para comprobar Player/Move.");
            var playerAnimator = player.GetComponent<Animator>();
            var playerRenderer = player.GetComponent<SpriteRenderer>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            await Task.Delay(80);
            if (!playerAnimator.GetBool("Walking") || !playerRenderer.flipX)
                throw new Exception("Player no pasó a Walk mirando a la izquierda.");
            Sprite walkingFrame = playerRenderer.sprite;
            await Task.Delay(160);
            if (playerRenderer.sprite == walkingFrame)
                throw new Exception("Walk no cambió visiblemente de cuadro.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            await Task.Delay(80);
            if (!playerAnimator.GetBool("Walking") || playerRenderer.flipX)
                throw new Exception("Player no miró a la derecha en Walk.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            await Task.Delay(80);
            if (playerAnimator.GetBool("Walking"))
                throw new Exception("Player no volvió a Idle sin input.");

            foreach (string name in Enemies)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                Vector3 position = player.transform.position + Vector3.right * 4f;
                GameObject first = pool.Spawn(prefab, position, Quaternion.identity);
                var visual = first.GetComponent<EnemyVisual>();
                var animator = first.GetComponent<Animator>();
                var renderer = first.GetComponent<SpriteRenderer>();
                if (visual == null || animator?.runtimeAnimatorController == null)
                    throw new Exception(name + " no tiene animación.");
                visual.SetTarget(player.transform);
                Sprite initial = renderer.sprite;
                await Task.Delay(240);
                if (renderer.sprite == initial || !renderer.flipX)
                    throw new Exception(name + " no cambió de cuadro o no miró al Player.");
                renderer.flipX = true;
                ObjectPool.Release(first);
                GameObject reused = pool.Spawn(prefab, position, Quaternion.identity);
                if (reused != first || renderer.flipX ||
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f)
                    throw new Exception(name + " conservó estado visual al reutilizarse.");
                ObjectPool.Release(reused);
            }
            Debug.Log("Cuarta slice Play Mode: Player Idle/Walk/flip y cuatro enemigos animados con reset del Animator al reutilizar pool correctos.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            if (Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                InputSystem.Update();
            }
        }
    }
}
