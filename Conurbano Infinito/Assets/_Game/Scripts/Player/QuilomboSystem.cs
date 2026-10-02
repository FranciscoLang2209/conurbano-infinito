using System;
using UnityEngine;

public sealed class QuilomboSystem : MonoBehaviour
{
    public float Current { get; private set; }
    public float Max => 100f;

    public event Action<float> OnQuilomboChanged;

    public void Add(float amount)
    {
        if (!(amount > 0f) || float.IsInfinity(amount)) return;
        Set(Mathf.Clamp(Current + amount, 0f, Max));
    }

    public void Remove(float amount)
    {
        if (!(amount > 0f) || float.IsInfinity(amount)) return;
        Set(Mathf.Clamp(Current - amount, 0f, Max));
    }

    private void Set(float value)
    {
        if (Current == value) return;
        Current = value;
        OnQuilomboChanged?.Invoke(Current);
    }
}
