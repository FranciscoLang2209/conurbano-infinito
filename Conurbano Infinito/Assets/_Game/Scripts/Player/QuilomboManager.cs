using System.Collections.Generic;
using UnityEngine;

public sealed class QuilomboManager : MonoBehaviour
{
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private RiskZone[] elements;
    [SerializeField] private MeshFilter plazaFloor;
    [SerializeField, Min(0f)] private float minimumPlayerDistance = 15f;
    [SerializeField, Min(0f)] private float minimumElementDistance = 10f;

    private void Start()
    {
        if (playerSpawn == null || spawnPoints == null || elements == null || plazaFloor == null)
        {
            Debug.LogError("QuilomboManager requiere jugador, suelo, spawn points y elementos.", this);
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
        for (int i = available.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (available[i], available[other]) = (available[other], available[i]);
        }

        var footprints = new Bounds[elements.Length, available.Count];
        var fits = new bool[elements.Length, available.Count];
        for (int i = 0; i < elements.Length; i++)
        for (int j = 0; j < available.Count; j++)
        {
            if (elements[i] == null) continue;
            footprints[i, j] = Footprint(elements[i], available[j].position);
            fits[i, j] = FitsLawn(elements[i], available[j].position);
        }
        var selected = new int[elements.Length];
        int attempts = 71000; // Eight candidates / six POIs take at most 70,408 checks; larger maps stay bounded.
        if (!Assign(0))
        {
            Debug.LogError("No hay una asignación válida de los POIs en las regiones de césped; " +
                "se omite la colocación completa. Revisá capacidad, footprints y reservas del suelo.", this);
            foreach (RiskZone element in elements)
                if (element != null) element.gameObject.SetActive(false);
            return;
        }
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] == null) continue;
            Vector3 position = elements[i].transform.position;
            Vector3 point = available[selected[i]].position;
            elements[i].transform.position = new Vector3(point.x, point.y, position.z);
        }

        bool Assign(int index)
        {
            if (index == elements.Length) return true;
            if (elements[index] == null) return Assign(index + 1);
            for (int candidate = 0; candidate < available.Count; candidate++)
            {
                if (--attempts < 0) return false;
                if (!fits[index, candidate]) continue;
                bool free = true;
                for (int previous = 0; previous < index; previous++)
                {
                    if (elements[previous] == null) continue;
                    Bounds a = footprints[index, candidate], b = footprints[previous, selected[previous]];
                    if (candidate == selected[previous] ||
                        Vector2.Distance(available[candidate].position, available[selected[previous]].position) < minimumElementDistance ||
                        (a.min.x < b.max.x && a.max.x > b.min.x && a.min.y < b.max.y && a.max.y > b.min.y))
                    {
                        free = false;
                        break;
                    }
                }
                if (!free) continue;
                selected[index] = candidate;
                if (Assign(index + 1)) return true;
            }
            return false;
        }
    }

    private static Bounds Footprint(RiskZone element, Vector2 center)
    {
        var sprites = element.GetComponentsInChildren<SpriteRenderer>();
        Bounds bounds = sprites[0].bounds;
        foreach (SpriteRenderer sprite in sprites) bounds.Encapsulate(sprite.bounds);
        Vector2 delta = center - (Vector2)element.transform.position;
        bounds.center += new Vector3(delta.x, delta.y, 0f);
        bounds.Expand(0.5f); // 0.25 world units around the complete sprite, including child decorations.
        return bounds;
    }

    public bool FitsLawn(RiskZone element, Vector2 center)
    {
        if (plazaFloor == null) return false;
        Bounds footprint = Footprint(element, center);
        Material material = plazaFloor.GetComponent<Renderer>().sharedMaterial;
        Vector2 tiling = material.GetTextureScale("_BaseMap"), offset = material.GetTextureOffset("_BaseMap");
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        Bounds mesh = plazaFloor.sharedMesh.bounds;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 local = plazaFloor.transform.InverseTransformPoint(new Vector3(
                (corner & 1) == 0 ? footprint.min.x : footprint.max.x,
                (corner & 2) == 0 ? footprint.min.y : footprint.max.y, plazaFloor.transform.position.z));
            if (local.x < mesh.min.x || local.x > mesh.max.x || local.y < mesh.min.y || local.y > mesh.max.y)
                return false;
            Vector2 uv = Vector2.Scale((Vector2)local + Vector2.one * 0.5f, tiling) + offset;
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
        // shortcut: authored XY quad UV=local+0.5 and generate_plaza cross; update regions if that floor changes.
        // Roads include the largest wave and border: 43/39 + 3 + 4 pixels, rounded outward.
        Vector2 region = new(Mathf.Floor((min.x + max.x) * 0.5f + 0.5f),
            Mathf.Floor((min.y + max.y) * 0.5f + 0.5f));
        Vector2 halfLawn = new(0.5f - 50f / 512f, 0.5f - 46f / 512f);
        return min.x >= region.x - halfLawn.x && max.x <= region.x + halfLawn.x &&
            min.y >= region.y - halfLawn.y && max.y <= region.y + halfLawn.y;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.yellow;
        foreach (Transform point in spawnPoints)
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.7f);
    }
}
