using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("-------Audio Source--------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("-------Background Music--------")]
    public AudioClip background;
    public AudioClip wizardDeath;
    public AudioClip warriorDeath;
    public AudioClip ninjaDeath;
    public AudioClip walk;
    public AudioClip defence;
    public AudioClip dash;
    public AudioClip jump;
    public AudioClip wizardAttack;
    public AudioClip warriorAttack;
    public AudioClip ninjaAttack;
    public AudioClip getHealth;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
