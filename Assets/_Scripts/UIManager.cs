using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public GameObject panelHUD;
    public TextMeshProUGUI txtLevel;
    public TextMeshProUGUI txtTarget;
    public TextMeshProUGUI txtLive;
    public TextMeshProUGUI txtDeath;
    public Button btnMenu;

    [Header("Menu Panel")]
    public GameObject panelMenu;
    public Button btnContinue;
    public Button btnNewGame;
    public Button btnBestScore;
    public Button btnSound;
    public Button btnExit;

    [Header("Best Score Panel")]
    public GameObject panelBestLevel;
    public TextMeshProUGUI txtBestScore;
    public Button btnConfirmBest;

    [Header("Game Win Panel")]
    public GameObject panelGameWin;
    public TextMeshProUGUI txtWinTitle;
    public Button btnNextLevel;

    [Header("Game Lose Panel")]
    public GameObject panelGameLose;
    public TextMeshProUGUI txtLoseTitle;
    public Button btnRetry;

    private void Start()
    {
        // Bind Buttons
        btnMenu.onClick.AddListener(OnMenuClick);
        btnContinue.onClick.AddListener(OnContinueClick);
        btnNewGame.onClick.AddListener(OnNewGameClick);
        btnBestScore.onClick.AddListener(OnBestScoreClick);
        btnSound.onClick.AddListener(OnSoundClick);
        btnExit.onClick.AddListener(OnExitClick);

        btnConfirmBest.onClick.AddListener(OnConfirmBestClick);

        btnNextLevel.onClick.AddListener(OnNextLevelClick);
        btnRetry.onClick.AddListener(OnRetryClick);

        // Initial State
        ShowPanel(panelHUD);
        panelMenu.SetActive(false);
        panelBestLevel.SetActive(false);
        panelGameWin.SetActive(false);
        panelGameLose.SetActive(false);
    }

    public void UpdateHUD(int level, int target, int live, int death)
    {
        txtLevel.text = $"Level: {level}";
        txtTarget.text = $"Target: {target}";
        txtLive.text = $"Live: {live}";
        txtDeath.text = $"Death: {death}";
    }

    public void ShowWinPanel()
    {
        ShowPanel(panelGameWin);
    }

    public void ShowLosePanel()
    {
        ShowPanel(panelGameLose);
    }

    // Button Handlers
    private void OnMenuClick()
    {
        panelMenu.SetActive(true);
        Time.timeScale = 0; // Pause game logic? FDS doesn't specify pause on menu, but usually yes.
    }

    private void OnContinueClick()
    {
        panelMenu.SetActive(false);
        Time.timeScale = 1;
    }

    private void OnNewGameClick()
    {
        GameManager.Instance.RestartGame(); // Reset level to 1
        panelMenu.SetActive(false);
    }

    private void OnBestScoreClick()
    {
        panelMenu.SetActive(false);
        panelBestLevel.SetActive(true);
        int best = PlayerPrefs.GetInt("BestLevel", 1);
        txtBestScore.text = $"Best Level: {best}";
    }

    private void OnSoundClick()
    {
        SoundManager.Instance.ToggleSound();
    }

    private void OnExitClick()
    {
        Application.Quit();
    }

    private void OnConfirmBestClick()
    {
        panelBestLevel.SetActive(false);
        panelMenu.SetActive(true); // Return to menu
    }

    private void OnNextLevelClick()
    {
        GameManager.Instance.NextLevel();
        panelGameWin.SetActive(false);
    }

    private void OnRetryClick()
    {
        GameManager.Instance.RestartLevel(); // Retry current level
        panelGameLose.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        //panelHUD.SetActive(panel == panelHUD); // HUD always visible? FDS "Panel_HUD (Anchor: Top Stretch)". Usually overlays.
        // Assuming Panels occupy center and block game view.
        // But Win/Lose should probably hide HUD or overlay?
        // Let's just SetActive true for the requested panel and false for others (except HUD usually stays).
        // FDS doesn't specify exclusion. I'll just setActive the target.
        panel.SetActive(true);
    }
}
