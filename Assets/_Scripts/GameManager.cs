using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Settings")]
    public int currentLevel = 1;
    public int startSheepCount = 5;
    public int startWolfCount = 1;
    public int deathLimit = 3;

    [Header("References")]
    public UIManager uiManager;
    public Transform wallsRoot;
    public GameObject sheepPrefab;
    public GameObject wolfPrefab;
    public GameObject targetZonePrefab;
    public LayerMask agentLayer;
    public LayerMask groundLayer;

    // State
    private int _liveCount;
    private int _deathCount;
    private int _targetCount;
    private Vector2 _mapMin;
    private Vector2 _mapMax;
    private bool _isGameActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CalculateMapBounds();
        StartLevel();
    }

    private void CalculateMapBounds()
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.y); // Assuming Y-up TopDown
        // Viewport 0,0 to 1,1
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, depth));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, depth));

        _mapMin = new Vector2(min.x, min.z);
        _mapMax = new Vector2(max.x, max.z);

        Debug.Log($"Map Bounds: {_mapMin} to {_mapMax}");
    }

    public void StartLevel()
    {
        _liveCount = 0;
        _deathCount = 0; // Constraint: Reset death count

        // Constraint: Sheep Count Formula
        // Level 1: 2. Level N: 2 + (N-1). Max 10.
        int totalSheep = Mathf.Min(2 + (currentLevel - 1), 10);

        // Constraint: Target Count Formula (Floor(Total * 0.7))
        _targetCount = Mathf.FloorToInt(totalSheep * 0.7f);

        // Constraint: Wolf Count Formula
        // Level 1-4: 0. Level 5+: 1 + (N-5). Max 5.
        int wolfCount = 0;
        if (currentLevel >= 5)
        {
            wolfCount = Mathf.Min(1 + (currentLevel - 5), 5);
        }

        _isGameActive = true;
        Time.timeScale = 1;

        // Cleanup
        foreach (var agent in FindObjectsOfType<SheepController>()) Destroy(agent.gameObject);
        foreach (var agent in FindObjectsOfType<WolfController>()) Destroy(agent.gameObject);
        foreach (var zone in FindObjectsOfType<TargetZone>()) Destroy(zone.gameObject);

        // Spawn Zone
        SpawnTargetZone();

        // Spawn Sheep
        for (int i = 0; i < totalSheep; i++)
        {
            SpawnEntity(sheepPrefab);
        }

        // Spawn Wolf
        for (int i = 0; i < wolfCount; i++)
        {
            SpawnEntity(wolfPrefab);
        }

        // UI
        if (uiManager != null)
        {
            uiManager.UpdateHUD(currentLevel, _targetCount, _liveCount, _deathCount);
            uiManager.panelGameWin.SetActive(false);
            uiManager.panelGameLose.SetActive(false);
        }
    }

    private void SpawnTargetZone()
    {
        // Random position within bounds, padded by radius
        float padding = 3f;
        float x = Random.Range(_mapMin.x + padding, _mapMax.x - padding);
        float z = Random.Range(_mapMin.y + padding, _mapMax.y - padding);

        // Constraint: Y = 0.01f
        GameObject zone = Instantiate(targetZonePrefab, new Vector3(x, 0.01f, z), Quaternion.identity);

        // Constraint: Scale increases with level (clamped to 3x). Y fixed at 0.05f.
        // Base scale assumed X/Z = 2.
        float baseScaleXZ = 2f;
        float scaleFactor = Mathf.Min(1f + (currentLevel - 1) * 0.2f, 3f);
        float targetScaleXZ = baseScaleXZ * scaleFactor;

        zone.transform.localScale = new Vector3(targetScaleXZ, 0.05f, targetScaleXZ);
    }

    private void SpawnEntity(GameObject prefab)
    {
        float padding = 1f;
        Vector3 spawnPos = Vector3.zero;
        int maxAttempts = 30;
        float checkRadius = 1.0f; // Adjust based on agent size
        bool validPos = false;

        // Constraint: Check against Agent AND Zone layer
        int combinedLayerMask = agentLayer | (1 << LayerMask.NameToLayer("Zone"));

        // Constraint: Safe spawn logic with max attempts
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(_mapMin.x + padding, _mapMax.x - padding);
            float z = Random.Range(_mapMin.y + padding, _mapMax.y - padding);
            spawnPos = new Vector3(x, 0, z); // Agents usually on Y=0 or adjusted by physics

            if (!Physics.CheckSphere(spawnPos, checkRadius, combinedLayerMask))
            {
                validPos = true;
                break; // Found valid spot
            }
        }

        if (validPos)
        {
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Fallback: Just spawn somewhere if we can't find a spot
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    private void Update()
    {
        if (!_isGameActive) return;

        CheckGameState();
        HandleInput();
    }

    private void CheckGameState()
    {
        if (uiManager != null) uiManager.UpdateHUD(currentLevel, _targetCount, _liveCount, _deathCount);

        if (_liveCount >= _targetCount)
        {
            GameWin();
        }
        else if (_deathCount >= deathLimit)
        {
            GameLose();
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Step 1: Agent
            if (Physics.Raycast(ray, out RaycastHit hitAgent, 100f, agentLayer))
            {
                IAgent agent = hitAgent.collider.GetComponentInParent<IAgent>(); // Check parent first for Container logic
                if (agent == null) agent = hitAgent.collider.GetComponent<IAgent>();

                if (agent != null)
                {
                    agent.OnDirectClick();
                    return; // Stop if hit agent
                }
            }

            // Step 2: Ground
            if (Physics.Raycast(ray, out RaycastHit hitGround, 100f, groundLayer))
            {
                float radius = 2f; // Base radius? FDS says "Radius * 3.0". Assuming base 1?
                // Or define constant.
                float checkRadius = 3.0f;

                Collider[] colliders = Physics.OverlapSphere(hitGround.point, checkRadius, agentLayer);
                foreach (var col in colliders)
                {
                    IAgent agent = col.GetComponentInParent<IAgent>();
                    if (agent == null) agent = col.GetComponent<IAgent>();

                    if (agent != null)
                    {
                        agent.OnNearClick(hitGround.point);
                    }
                }
            }
        }
    }

    private void GameWin()
    {
        _isGameActive = false;
        Time.timeScale = 0;

        // Save Best Level
        int best = PlayerPrefs.GetInt("BestLevel", 1);
        if (currentLevel > best)
        {
            PlayerPrefs.SetInt("BestLevel", currentLevel);
            PlayerPrefs.Save();
        }

        if (uiManager != null) uiManager.ShowWinPanel();
    }

    private void GameLose()
    {
        _isGameActive = false;
        Time.timeScale = 0;
        if (uiManager != null) uiManager.ShowLosePanel();
    }

    public void AddLiveCount()
    {
        _liveCount++;
    }

    public void AddDeathCount()
    {
        _deathCount++;
    }

    public void NextLevel()
    {
        currentLevel++;
        StartLevel();
    }

    public void RestartLevel()
    {
        StartLevel();
    }

    public void RestartGame()
    {
        currentLevel = 1;
        StartLevel();
    }
}
