using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private const string IS_MUTED_KEY = "IsMuted";

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

        // Initialize sound settings
        bool isMuted = PlayerPrefs.GetInt(IS_MUTED_KEY, 0) == 1;
        AudioListener.volume = isMuted ? 0 : 1;
    }

    public void ToggleSound()
    {
        bool isMuted = PlayerPrefs.GetInt(IS_MUTED_KEY, 0) == 1;
        isMuted = !isMuted;

        PlayerPrefs.SetInt(IS_MUTED_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        AudioListener.volume = isMuted ? 0 : 1;
    }
}
