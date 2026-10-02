using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuilomboSliceCheck
{
    [MenuItem("Tools/Conurbano Infinito/Probar +25 Quilombo en Play Mode _F6")]
    private static void AddForPlayMode() =>
        UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>().Add(25f);

    [MenuItem("Tools/Conurbano Infinito/Probar -10 Quilombo en Play Mode _F7")]
    private static void RemoveForPlayMode() =>
        UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>().Remove(10f);

    [MenuItem("Tools/Conurbano Infinito/Probar +25 Quilombo en Play Mode _F6", true)]
    [MenuItem("Tools/Conurbano Infinito/Probar -10 Quilombo en Play Mode _F7", true)]
    private static bool CanChangeInPlayMode() =>
        EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>() != null;

    [MenuItem("Tools/Conurbano Infinito/Comprobar Quilombo")]
    public static void Run()
    {
        GameObject probe = new GameObject("Quilombo check");
        try
        {
            QuilomboSystem system = probe.AddComponent<QuilomboSystem>();
            int changes = 0;
            system.OnQuilomboChanged += _ => changes++;
            system.Add(15f);
            if (system.Current != 15f || changes != 1) throw new Exception("Add o evento falló.");
            system.Add(200f);
            if (system.Current != 100f || changes != 2) throw new Exception("Falta clamp superior.");
            system.Add(1f);
            if (changes != 2) throw new Exception("Se emitió un evento sin cambio.");
            system.Remove(200f);
            system.Add(float.NaN);
            if (system.Current != 0f || changes != 3) throw new Exception("Remove, clamp o NaN falló.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }

        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
        QuilomboSystem quilombo = UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>();
        RiskZone zone = Array.Find(UnityEngine.Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None),
            candidate => candidate.name == "Parrilla del Barrio");
        RiskZone kiosk = Array.Find(UnityEngine.Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None),
            candidate => candidate.name == "Kiosco 24hs");
        GameHud hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        WaveDirector waves = UnityEngine.Object.FindFirstObjectByType<WaveDirector>();
        QuilomboThresholdController thresholds = UnityEngine.Object.FindFirstObjectByType<QuilomboThresholdController>();
        QuilomboManager placement = UnityEngine.Object.FindFirstObjectByType<QuilomboManager>();
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (quilombo == null || zone == null || kiosk == null || hud == null || waves == null || thresholds == null || placement == null || player == null)
            throw new Exception("Falta un componente del slice en Main.");

        var placementFields = new SerializedObject(placement);
        SerializedProperty points = placementFields.FindProperty("spawnPoints");
        SerializedProperty elements = placementFields.FindProperty("elements");
        float playerDistance = placementFields.FindProperty("minimumPlayerDistance").floatValue;
        float elementDistance = placementFields.FindProperty("minimumElementDistance").floatValue;
        if (points.arraySize < 8 ||
            placementFields.FindProperty("playerSpawn").objectReferenceValue != player.transform ||
            elements.arraySize < 2 ||
            elements.GetArrayElementAtIndex(0).objectReferenceValue != zone ||
            elements.GetArrayElementAtIndex(1).objectReferenceValue != kiosk)
            throw new Exception("QuilomboManager no tiene los puntos, el jugador o los elementos configurados.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        for (int run = 0; run < 20; run++)
        {
            typeof(QuilomboManager).GetMethod("Start", flags).Invoke(placement, null);
            if (Vector2.Distance(zone.transform.position, player.transform.position) < playerDistance ||
                Vector2.Distance(kiosk.transform.position, player.transform.position) < playerDistance ||
                Vector2.Distance(zone.transform.position, kiosk.transform.position) < elementDistance)
                throw new Exception("Las zonas no respetan las distancias mínimas.");

            bool zoneOnPoint = false;
            bool kioskOnPoint = false;
            for (int i = 0; i < points.arraySize; i++)
            {
                Transform point = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (point == null) throw new Exception("Hay un spawn point vacío.");
                zoneOnPoint |= Vector2.Distance(zone.transform.position, point.position) < 0.01f;
                kioskOnPoint |= Vector2.Distance(kiosk.transform.position, point.position) < 0.01f;
            }
            if (!zoneOnPoint || !kioskOnPoint || zone.transform.position == kiosk.transform.position)
                throw new Exception("Las zonas no quedaron en puntos distintos.");
        }

        var zoneFields = new SerializedObject(zone);
        RiskZoneData data = zoneFields.FindProperty("data").objectReferenceValue as RiskZoneData;
        CircleCollider2D area = zone.GetComponent<CircleCollider2D>();
        if (data == null || data.RequiredDuration != 5f || data.QuilomboReward != 15f ||
            !Mathf.Approximately(data.DamageMultiplier, 1.2f) ||
            !area.isTrigger ||
            zoneFields.FindProperty("quilombo").objectReferenceValue != quilombo ||
            new SerializedObject(hud).FindProperty("quilombo").objectReferenceValue != quilombo ||
            new SerializedObject(waves).FindProperty("quilombo").objectReferenceValue != quilombo ||
            new SerializedObject(thresholds).FindProperty("quilombo").objectReferenceValue != quilombo ||
            new SerializedObject(thresholds).FindProperty("waves").objectReferenceValue != waves ||
            new SerializedObject(thresholds).FindProperty("hud").objectReferenceValue != hud)
            throw new Exception("La Parrilla, el HUD o WaveDirector no están configurados.");

        // Edit Mode does not run this gameplay listener automatically.
        typeof(QuilomboThresholdController).GetMethod("OnEnable", flags).Invoke(thresholds, null);
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        WeaponController weapons = player.GetComponent<WeaponController>();
        if (weapons == null || weapons.DamageMultiplier != 1f)
            throw new Exception("La partida no empieza con daño normal.");
        typeof(RiskZone).GetMethod("OnTriggerEnter2D", flags).Invoke(zone, new object[] { playerCollider });
        if (zone.State != RiskZone.ZoneState.Active) throw new Exception("La entrada no activa la zona.");
        typeof(RiskZone).GetMethod("OnTriggerExit2D", flags).Invoke(zone, new object[] { playerCollider });
        if (zone.State != RiskZone.ZoneState.Inactive || zone.Progress != 0f || quilombo.Current != 0f)
            throw new Exception("La salida temprana no reinició la zona.");

        typeof(RiskZone).GetMethod("OnTriggerEnter2D", flags).Invoke(zone, new object[] { playerCollider });
        typeof(RiskZone).GetField("<Progress>k__BackingField", flags)
            .SetValue(zone, data.RequiredDuration);
        typeof(RiskZone).GetMethod("Update", flags).Invoke(zone, null);
        if (zone.State != RiskZone.ZoneState.Completed || quilombo.Current != 15f ||
            !Mathf.Approximately(weapons.DamageMultiplier, 1.2f))
            throw new Exception("La Parrilla no otorgó ambas recompensas.");
        typeof(RiskZone).GetMethod("OnTriggerExit2D", flags).Invoke(zone, new object[] { playerCollider });
        typeof(RiskZone).GetMethod("OnTriggerEnter2D", flags).Invoke(zone, new object[] { playerCollider });
        typeof(RiskZone).GetMethod("Update", flags).Invoke(zone, null);
        if (quilombo.Current != 15f || !Mathf.Approximately(weapons.DamageMultiplier, 1.2f))
            throw new Exception("La Parrilla otorgó la recompensa más de una vez.");

        ExperienceSystem experience = player.GetComponent<ExperienceSystem>();
        float originalRadius = experience.CollectionRadius;
        RiskZoneData kioskData = new SerializedObject(kiosk).FindProperty("data")
            .objectReferenceValue as RiskZoneData;
        if (kioskData == null || kioskData.RequiredDuration != 0f ||
            kioskData.QuilomboReward != 10f || kioskData.RewardDuration != 10f)
            throw new Exception("El Kiosco no tiene la configuración esperada.");
        typeof(RiskZone).GetMethod("OnTriggerEnter2D", flags).Invoke(kiosk, new object[] { playerCollider });
        if (kiosk.State != RiskZone.ZoneState.Active || quilombo.Current != 25f ||
            !Mathf.Approximately(experience.CollectionRadius, originalRadius * 1.25f))
            throw new Exception("El Kiosco no activó el bonus de inmediato.");
        typeof(RiskZone).GetMethod("OnTriggerExit2D", flags).Invoke(kiosk, new object[] { playerCollider });
        typeof(RiskZone).GetMethod("OnTriggerEnter2D", flags).Invoke(kiosk, new object[] { playerCollider });
        if (quilombo.Current != 25f) throw new Exception("El Kiosco se activó dos veces.");
        if ((float)typeof(WaveDirector).GetField("quilomboThresholdBonus", flags).GetValue(waves) != 0.15f ||
            (string)typeof(GameHud).GetField("feedbackMessage", flags).GetValue(hud) != "EL BARRIO SE ESTÁ PICANDO")
            throw new Exception("El threshold de 25 no activó spawn o feedback.");
        quilombo.Remove(10f);
        quilombo.Add(10f);
        if ((string)typeof(GameHud).GetField("queuedFeedback", flags).GetValue(hud) != null)
            throw new Exception("El threshold de 25 se repitió.");
        typeof(ExperienceSystem).GetField("collectionBonusUntil", flags).SetValue(experience, Time.time - 1f);
        typeof(RiskZone).GetField("rewardEndTime", flags).SetValue(kiosk, Time.time - 1f);
        typeof(RiskZone).GetMethod("Update", flags).Invoke(kiosk, null);
        if (kiosk.State != RiskZone.ZoneState.Completed || experience.CollectionRadius != originalRadius ||
            !Mathf.Approximately(weapons.DamageMultiplier, 1.2f))
            throw new Exception("El Kiosco no restauró el radio o alteró la Parrilla.");

        GameObject target = new GameObject("Daño fraccional check");
        try
        {
            Health health = target.AddComponent<Health>();
            var targetData = new SerializedObject(health);
            targetData.FindProperty("maximumHealth").intValue = 12;
            targetData.ApplyModifiedPropertiesWithoutUndo();
            health.ResetForReuse();
            for (int i = 0; i < 9; i++) health.TakeDamage(1.2f);
            if (health.IsDead) throw new Exception("El bonus mata antes de tiempo.");
            health.TakeDamage(1.2f);
            if (!health.IsDead) throw new Exception("El daño fraccional no se acumuló.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }

        quilombo.Add(25f);
        if ((float)typeof(WaveDirector).GetField("quilomboThresholdBonus", flags).GetValue(waves) != 0.35f ||
            !(bool)typeof(WaveDirector).GetField("elitesEnabled", flags).GetValue(waves) ||
            (string)typeof(GameHud).GetField("queuedFeedback", flags).GetValue(hud) != "SE ESTÁ YENDO TODO AL CARAJO")
            throw new Exception("El threshold de 50 no activó spawn, hook elite o feedback.");
        quilombo.Remove(10f);
        quilombo.Add(10f);
        if ((float)typeof(WaveDirector).GetField("quilomboThresholdBonus", flags).GetValue(waves) != 0.35f)
            throw new Exception("El threshold de 50 se repitió.");

        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
        if (UnityEngine.Object.FindFirstObjectByType<QuilomboSystem>().Current != 0f ||
            (bool)typeof(QuilomboThresholdController).GetField("reached25", flags)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<QuilomboThresholdController>()))
            throw new Exception("Reiniciar no restauró los thresholds.");

        Debug.Log("Quilombo: zonas, thresholds 25/50, presión de spawn y reinicio correctos.");
    }
}
