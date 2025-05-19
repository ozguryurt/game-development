using UnityEngine;

public class MainMenuSound : MonoBehaviour
{
    [Header("-------Audio Source--------")]
    [SerializeField] AudioSource musicSource;
    
    [Header("-------Background Music--------")]
    public AudioClip background;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        musicSource.clip = background;
        musicSource.Play();
    }
}
