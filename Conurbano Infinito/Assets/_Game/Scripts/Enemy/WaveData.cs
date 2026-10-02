using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Oleadas", menuName = "Conurbano Infinito/Oleadas")]
public sealed class WaveData : ScriptableObject
{
    [Serializable]
    public struct SpawnOption
    {
        public GameObject prefab;
        [Min(1)] public int weight;
    }

    [Serializable]
    public struct Phase
    {
        [Min(0f)] public float startSeconds;
        [Min(0.1f)] public float spawnIntervalSeconds;
        [Min(1)] public int maximumAlive;
        public SpawnOption[] enemies;
    }

    [SerializeField] private Phase[] phases;
    [SerializeField, Min(0f)] private float bossSpawnSeconds = 300f;
    [SerializeField] private GameObject bossPrefab;

    public Phase[] Phases => phases;
    public float BossSpawnSeconds => bossSpawnSeconds;
    public GameObject BossPrefab => bossPrefab;

    public int PhaseIndexAt(float elapsedSeconds)
    {
        int index = 0;
        for (int i = 1; i < phases.Length && elapsedSeconds >= phases[i].startSeconds; i++)
        {
            index = i;
        }
        return index;
    }
}
