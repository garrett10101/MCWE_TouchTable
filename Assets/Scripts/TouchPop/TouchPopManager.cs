using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the spawning and lifetime of touch targets for a simple whack‑a‑mole style test.
/// This script demonstrates handling multiple simultaneous touches by performing a 2D raycast
/// for every touch that begins. When a target is touched (or clicked with the mouse in the editor),
/// it is destroyed and the score increments. Targets also despawn automatically after a fixed lifetime.
/// </summary>
public class TouchPopManager : MonoBehaviour
{
    [Header("Target Spawning")]
    [Tooltip("Prefab of the target object to spawn. The prefab should have a Collider2D attached.")]
    public GameObject targetPrefab;

    [Tooltip("Area within which targets will spawn. X/Y define the bottom left corner and width/height define the size.")]
    public Rect spawnArea = new Rect(-4f, -3f, 8f, 6f);

    [Tooltip("Minimum interval between spawns in seconds.")]
    public float spawnIntervalMin = 0.5f;

    [Tooltip("Maximum interval between spawns in seconds.")]
    public float spawnIntervalMax = 1.25f;

    [Tooltip("Maximum number of active targets allowed at one time.")]
    public int maxActiveTargets = 5;

    [Tooltip("Lifetime of a spawned target in seconds before it despawns if not touched.")]
    public float targetLifetime = 2f;

    [Header("UI")]
    [Tooltip("UI Text element used to display the current score.")]
    public Text scoreText;

    private class SpawnedTarget
    {
        public GameObject gameObject;
        public float spawnTime;
    }

    private readonly List<SpawnedTarget> activeTargets = new List<SpawnedTarget>();
    private float nextSpawnTime;
    private int score;

    void Start()
    {
        ScheduleNextSpawn();
        UpdateScoreText();
    }

    void Update()
    {
        float time = Time.time;

        // Spawn new targets if it's time and we haven't reached the cap
        if (time >= nextSpawnTime && activeTargets.Count < maxActiveTargets)
        {
            SpawnTarget();
            ScheduleNextSpawn();
        }

        // Check for expired targets
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            SpawnedTarget t = activeTargets[i];
            if (time - t.spawnTime >= targetLifetime)
            {
                Destroy(t.gameObject);
                activeTargets.RemoveAt(i);
            }
        }

        // Handle touch input for multi‑touch devices
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                ProcessTouchAtPosition(touch.position);
            }
        }

        // Handle mouse input in the editor or desktop builds for convenience
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTouchAtPosition(Input.mousePosition);
        }
    }

    /// <summary>
    /// Processes a touch or click at the given screen position. Casts a ray into the 2D world and
    /// checks for a TouchTarget on any hit colliders. If a target is hit, it is removed and the
    /// score increments.
    /// </summary>
    /// <param name="screenPosition">The screen position of the touch/click.</param>
    private void ProcessTouchAtPosition(Vector2 screenPosition)
    {
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pos2D = new Vector2(worldPoint.x, worldPoint.y);
        RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero);
        if (hit.collider != null)
        {
            TouchTarget target = hit.collider.GetComponent<TouchTarget>();
            if (target != null)
            {
                HitTarget(target);
            }
        }
    }

    /// <summary>
    /// Spawns a new target at a random position within the defined spawn area. Keeps track of the
    /// spawned GameObject along with its spawn time so it can be despawned later.
    /// </summary>
    private void SpawnTarget()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("TouchPopManager requires a targetPrefab assigned.");
            return;
        }

        float x = Random.Range(spawnArea.xMin, spawnArea.xMax);
        float y = Random.Range(spawnArea.yMin, spawnArea.yMax);
        Vector2 spawnPos = new Vector2(x, y);
        GameObject obj = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
        TouchTarget touchComponent = obj.GetComponent<TouchTarget>();
        if (touchComponent == null)
        {
            touchComponent = obj.AddComponent<TouchTarget>();
        }
        touchComponent.Initialize(this);
        activeTargets.Add(new SpawnedTarget { gameObject = obj, spawnTime = Time.time });
    }

    /// <summary>
    /// Removes the touched target from the active list, destroys it, and increments the score.
    /// </summary>
    /// <param name="target">The touched target component.</param>
    public void HitTarget(TouchTarget target)
    {
        // Remove from active list
        for (int i = 0; i < activeTargets.Count; i++)
        {
            if (activeTargets[i].gameObject == target.gameObject)
            {
                activeTargets.RemoveAt(i);
                break;
            }
        }
        Destroy(target.gameObject);
        score++;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void ScheduleNextSpawn()
    {
        float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
        nextSpawnTime = Time.time + interval;
    }
}