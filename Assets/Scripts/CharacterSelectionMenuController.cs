using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectionMenuController : MonoBehaviour
{
    public void SelectCharacter(string characterName)
    {
        Debug.Log("Selected character: " + characterName);
        CharacterSelection.Instance.selectedCharacter = characterName;
        SceneManager.LoadScene("SampleScene");
    }
}
