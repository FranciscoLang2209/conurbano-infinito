using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool : MonoBehaviour
{
    private readonly Dictionary<GameObject, Stack<GameObject>> available = new();

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!available.TryGetValue(prefab, out Stack<GameObject> idle))
        {
            idle = new Stack<GameObject>();
            available.Add(prefab, idle);
        }

        GameObject instance = null;
        while (idle.Count > 0 && instance == null) instance = idle.Pop();
        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
            instance.AddComponent<PooledObject>().Initialize(this, prefab);
            return instance;
        }

        instance.transform.SetParent(null);
        instance.transform.SetPositionAndRotation(position, rotation);
        if (instance.TryGetComponent(out Rigidbody2D body))
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
        if (instance.TryGetComponent(out Health health)) health.ResetForReuse();
        instance.SetActive(true);
        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance.TryGetComponent(out PooledObject pooled) && pooled.Owner != null)
        {
            pooled.Owner.Return(pooled);
        }
        else
        {
            Destroy(instance);
        }
    }

    private void Return(PooledObject pooled)
    {
        GameObject instance = pooled.gameObject;
        if (!instance.activeSelf) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform);
        // ponytail: retiene el pico de instancias; limitar el stack si la memoria medida lo exige.
        available[pooled.Prefab].Push(instance);
    }
}
