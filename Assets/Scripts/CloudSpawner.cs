using System.Collections;
using UnityEngine;

/// <summary>
/// Occasionally spawns drifting, camera-facing cloud billboards at random points inside a box volume.
/// Each cloud fades in, drifts, fades out and is destroyed.
/// </summary>
public class CloudSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Transparent material with the cloud texture.")]
    public Material cloudMaterial;

    [Tooltip("Size of the spawn volume, centered on this transform.")]
    public Vector3 areaSize = new Vector3(10f, 2f, 10f);

    [Tooltip("Clouds spawned immediately on start.")]
    public int initialClouds = 5;

    [Tooltip("Random delay range in seconds between spawns.")]
    public Vector2 spawnInterval = new Vector2(3f, 8f);

    [Tooltip("Random cloud height range in meters (width follows the texture aspect).")]
    public Vector2 cloudSize = new Vector2(1f, 2f);

    [Header("Cloud Settings")]
    [Tooltip("Random cloud lifetime range in seconds.")]
    public Vector2 lifetime = new Vector2(6f, 12f);

    [Tooltip("Fade in/out duration in seconds.")]
    public float fadeDuration = 2f;

    [Tooltip("Peak opacity of a cloud.")]
    [Range(0f, 1f)] public float maxAlpha = 0.5f;

    [Tooltip("Drift speed in meters per second.")]
    public float driftSpeed = 0.2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private float spawnTimer;

    private void Start()
    {
        spawnTimer = Random.Range(spawnInterval.x, spawnInterval.y);

        for (int i = 0; i < initialClouds; i++)
            StartCoroutine(CloudRoutine());
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = Random.Range(spawnInterval.x, spawnInterval.y);
        StartCoroutine(CloudRoutine());
    }

    private IEnumerator CloudRoutine()
    {
        GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cloud.name = "Cloud";
        Destroy(cloud.GetComponent<Collider>());

        Vector3 local = new Vector3(
            Random.Range(-0.5f, 0.5f) * areaSize.x,
            Random.Range(-0.5f, 0.5f) * areaSize.y,
            Random.Range(-0.5f, 0.5f) * areaSize.z);
        cloud.transform.position = transform.TransformPoint(local);

        Texture tex = cloudMaterial.mainTexture;
        float aspect = tex != null ? (float)tex.width / tex.height : 1f;
        float height = Random.Range(cloudSize.x, cloudSize.y);
        cloud.transform.localScale = new Vector3(height * aspect, height, 1f);

        Renderer rend = cloud.GetComponent<Renderer>();
        rend.sharedMaterial = cloudMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color color = cloudMaterial.GetColor(BaseColorId);
        Vector3 drift = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * driftSpeed;
        float life = Random.Range(lifetime.x, lifetime.y);
        float fade = Mathf.Min(fadeDuration, life * 0.5f);

        for (float t = 0f; t < life; t += Time.deltaTime)
        {
            Camera cam = Camera.main;
            if (cam != null) cloud.transform.rotation = cam.transform.rotation;
            cloud.transform.position += drift * Time.deltaTime;

            color.a = maxAlpha * Mathf.Clamp01(Mathf.Min(t, life - t) / fade);
            block.SetColor(BaseColorId, color);
            rend.SetPropertyBlock(block);
            yield return null;
        }

        Destroy(cloud);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.9f, 1f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }
}
