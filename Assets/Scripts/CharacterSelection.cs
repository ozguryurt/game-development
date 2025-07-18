using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance;

    public string selectedCharacter; // örn: "Ninja_Player"
    public string botCharacter;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Scene geçişlerinde kaybolmasın
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
