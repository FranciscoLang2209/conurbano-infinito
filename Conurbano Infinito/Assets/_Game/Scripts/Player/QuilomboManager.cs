using System.Collections.Generic;
using UnityEngine;

public sealed class QuilomboManager : MonoBehaviour
{
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private RiskZone[] elements;
    [SerializeField, Min(0f)] private float minimumPlayerDistance = 15f;
    [SerializeField, Min(0f)] private float minimumElementDistance = 10f;

    private void Start()
    {
        if (playerSpawn == null || spawnPoints == null || elements == null)
        {
            Debug.LogError("QuilomboManager requiere jugador, spawn points y elementos.", this);
            return;
        }

        var available = new List<Transform>();
        float playerDistanceSqr = minimumPlayerDistance * minimumPlayerDistance;
        foreach (Transform point in spawnPoints)
        {
            if (point != null && !available.Contains(point) &&
                ((Vector2)(point.position - playerSpawn.position)).sqrMagnitude >= playerDistanceSqr)
                available.Add(point);
        }

        var placed = new List<Vector2>();
        float elementDistanceSqr = minimumElementDistance * minimumElementDistance;
        foreach (RiskZone element in elements)
        {
            if (element == null) continue;

            var valid = new List<Transform>();
            foreach (Transform point in available)
            {
                Vector2 position = point.position;
                bool farEnough = true;
                foreach (Vector2 other in placed)
                {
                    if ((position - other).sqrMagnitude < elementDistanceSqr)
                    {
                        farEnough = false;
                        break;
                    }
                }
                if (farEnough) valid.Add(point);
            }

            if (valid.Count == 0)
            {
                Debug.LogError($"No hay un spawn point válido para {element.name}.", this);
                element.gameObject.SetActive(false);
                continue;
            }

            Transform selected = valid[Random.Range(0, valid.Count)];
            available.Remove(selected);
            Vector3 position3D = element.transform.position;
            position3D.x = selected.position.x;
            position3D.y = selected.position.y;
            element.transform.position = position3D;
            placed.Add(selected.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.yellow;
        foreach (Transform point in spawnPoints)
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.7f);
    }
}
