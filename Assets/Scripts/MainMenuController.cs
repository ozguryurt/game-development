using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip menuMusic;
    private const float SAMPLE_SCENE_VOLUME = 0.1f; // 10% volume

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        // AudioSource bileşeni yoksa ekle
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Müziği çalmaya başla
        PlayMenuMusic();

        // Scene değişikliğini dinle
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Scene değişikliği dinleyicisini kaldır
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            if (audioSource != null)
            {
                audioSource.volume = SAMPLE_SCENE_VOLUME;
            }
        }
        else if (scene.name == "MainMenu")
        {
            PlayMenuMusic();
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("CharacterSelectionScene");
    }

    private void PlayMenuMusic()
    {
        if (menuMusic != null && audioSource != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}