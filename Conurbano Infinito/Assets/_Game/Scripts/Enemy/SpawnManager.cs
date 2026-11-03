using UnityEngine;

public sealed class SpawnManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera spawnCamera;
    [SerializeField, Min(0f)] private float minimumPlayerDistance = 7f;
    [SerializeField, Min(0.1f)] private float offscreenPadding = 1.5f;
    private ObjectPool pool;

    private void Awake()
    {
        pool = GetComponent<ObjectPool>();
        if (player == null || spawnCamera == null || !spawnCamera.orthographic || pool == null)
        {
            Debug.LogError("SpawnManager requiere Player, cámara ortográfica y ObjectPool.", this);
            enabled = false;
        }
    }

    public Health Spawn(GameObject enemyPrefab)
    {
        if (!enabled || enemyPrefab == null)
        {
            return null;
        }

        Vector2 playerPosition = player.position;
        Vector2 cameraPosition = spawnCamera.transform.position;
        float halfHeight = spawnCamera.orthographicSize + offscreenPadding;
        float halfWidth = spawnCamera.orthographicSize * spawnCamera.aspect + offscreenPadding;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));

        float distanceToX = direction.x > 0f
            ? (cameraPosition.x + halfWidth - playerPosition.x) / direction.x
            : (cameraPosition.x - halfWidth - playerPosition.x) / direction.x;
        float distanceToY = direction.y > 0f
            ? (cameraPosition.y + halfHeight - playerPosition.y) / direction.y
            : (cameraPosition.y - halfHeight - playerPosition.y) / direction.y;
        float distance = Mathf.Max(minimumPlayerDistance, Mathf.Min(distanceToX, distanceToY));

        GameObject enemy = pool.Spawn(enemyPrefab, playerPosition + direction * distance,
            Quaternion.identity);
        if (enemy.TryGetComponent(out MosquitoChase mosquito)) mosquito.SetTarget(player);
        if (enemy.TryGetComponent(out EnemyMovement movement)) movement.SetTarget(player);
        if (enemy.TryGetComponent(out EnemyCharge charge)) charge.SetTarget(player);
        if (enemy.TryGetComponent(out EnemyRanged ranged)) ranged.SetTarget(player);
        if (enemy.TryGetComponent(out EnemyVisual visual)) visual.SetTarget(player);
        if (enemy.TryGetComponent(out BossAttack attack)) attack.SetTarget(player);
        return enemy.GetComponent<Health>();
    }
}
