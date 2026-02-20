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

    private void AdjustWalls()
    {
        if (wallsRoot == null) return;

        // Assuming 4 children: Top, Bottom, Left, Right
        // Or specific names. Let's just fit them to bounds.
        // Needs Cube Colliders.
        // Top Wall
        Transform top = wallsRoot.GetChild(0);
        top.position = new Vector3((_mapMin.x + _mapMax.x)/2, 0, _mapMax.y);
        top.localScale = new Vector3(_mapMax.x - _mapMin.x, 5, 1);

        // Bottom Wall
        Transform bottom = wallsRoot.GetChild(1);
        bottom.position = new Vector3((_mapMin.x + _mapMax.x)/2, 0, _mapMin.y);
        bottom.localScale = new Vector3(_mapMax.x - _mapMin.x, 5, 1);

        // Left Wall
        Transform left = wallsRoot.GetChild(2);
        left.position = new Vector3(_mapMin.x, 0, (_mapMin.y + _mapMax.y)/2);
        left.localScale = new Vector3(1, 5, _mapMax.y - _mapMin.y);

        // Right Wall
        Transform right = wallsRoot.GetChild(3);
        right.position = new Vector3(_mapMax.x, 0, (_mapMin.y + _mapMax.y)/2);
        right.localScale = new Vector3(1, 5, _mapMax.y - _mapMin.y);
    }

    public void StartLevel()
    {
        _liveCount = 0;
        _deathCount = 0;
        _targetCount = Mathf.Max(1, (int)(startSheepCount * 0.8f)); // Target logic? FDS doesn't say. 80% survival?
        // Or "TargetCount calculation". Maybe fixed per level?
        // Let's say Target = SheepCount - 1.
        // Assume SheepCount = Level + 2.
        int sheepToSpawn = startSheepCount + (currentLevel - 1);
        _targetCount = sheepToSpawn - 1;

        _isGameActive = true;
        Time.timeScale = 1;

        // Cleanup
        foreach (var agent in FindObjectsOfType<SheepController>()) Destroy(agent.gameObject);
        foreach (var agent in FindObjectsOfType<WolfController>()) Destroy(agent.gameObject);
        foreach (var zone in FindObjectsOfType<TargetZone>()) Destroy(zone.gameObject);

        // Adjust Walls
        AdjustWalls();

        // Spawn Zone
        SpawnTargetZone();

        // Spawn Sheep
        for (int i = 0; i < sheepToSpawn; i++)
        {
            SpawnEntity(sheepPrefab);
        }

        // Spawn Wolf
        int wolfCount = startWolfCount + (currentLevel / 5);
        for (int i = 0; i < wolfCount; i++)
        {
            SpawnEntity(wolfPrefab);
        }

        // UI
        if (uiManager != null)
        {
            uiManager.UpdateHUD(currentLevel, _targetCount, _liveCount, _deathCount);
            // Hide Win/Lose panels? Handled by UIManager Start or Reset.
            // But we should ensure they are closed here.
            uiManager.panelGameWin.SetActive(false);
            uiManager.panelGameLose.SetActive(false);
        }
    }

    private void SpawnTargetZone()
    {
        // Random position within bounds, padded by radius
        // Assume radius 2?
        float padding = 3f;
        float x = Random.Range(_mapMin.x + padding, _mapMax.x - padding);
        float z = Random.Range(_mapMin.y + padding, _mapMax.y - padding);
        Instantiate(targetZonePrefab, new Vector3(x, 0, z), Quaternion.identity);
    }

    private void SpawnEntity(GameObject prefab)
    {
        float padding = 1f;
        float x = Random.Range(_mapMin.x + padding, _mapMax.x - padding);
        float z = Random.Range(_mapMin.y + padding, _mapMax.y - padding);
        Instantiate(prefab, new Vector3(x, 0, z), Quaternion.identity);
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
