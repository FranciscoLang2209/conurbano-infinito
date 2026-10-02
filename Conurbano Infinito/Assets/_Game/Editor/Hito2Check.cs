using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Hito2Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 2")]
    public static void Run()
    {
        var first = new GameObject("Health check");
        var second = new GameObject("Death check");
        try
        {
            var health = first.AddComponent<Health>();
            typeof(Health).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(health, null);
            var settings = new SerializedObject(health);
            settings.FindProperty("invulnerabilitySeconds").floatValue = 1f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (health.CurrentHealth != 5 || !health.TakeDamage(2) ||
                health.CurrentHealth != 3 || health.TakeDamage(2))
            {
                throw new System.Exception("Health no respeta el daño o la invulnerabilidad.");
            }

            var lethalHealth = second.AddComponent<Health>();
            typeof(Health).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(lethalHealth, null);
            bool died = false;
            lethalHealth.Died += () => died = true;
            if (!lethalHealth.TakeDamage(5) || !died || !lethalHealth.IsDead ||
                lethalHealth.CurrentHealth != 0 || lethalHealth.TakeDamage(1))
            {
                throw new System.Exception("Health no resuelve correctamente la muerte.");
            }

            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
            var mosquito = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/MosquitoGigante.prefab");
            if (player == null || player.GetComponent<Health>() == null ||
                player.GetComponent<PlayerSurvival>() == null || mosquito == null ||
                mosquito.layer != LayerMask.NameToLayer("Enemy") ||
                mosquito.GetComponent<MosquitoChase>() == null ||
                mosquito.GetComponent<ContactDamage>() == null ||
                mosquito.GetComponent<Health>() == null ||
                !mosquito.GetComponent<CircleCollider2D>().isTrigger)
            {
                throw new System.Exception("Falta configurar un prefab del hito 2.");
            }

            Debug.Log("Hito 2: comprobación de vida y prefabs correcta.");
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }
}
