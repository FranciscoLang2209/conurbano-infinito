using UnityEngine;

public sealed class PooledObject : MonoBehaviour
{
    public ObjectPool Owner { get; private set; }
    public GameObject Prefab { get; private set; }

    public void Initialize(ObjectPool owner, GameObject prefab)
    {
        Owner = owner;
        Prefab = prefab;
    }
}
