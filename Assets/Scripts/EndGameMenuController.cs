using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndGameMenuController : MonoBehaviour
{

    public TextMeshProUGUI resultText;
    public TextMeshProUGUI resultTextShadow;

    void Start() {
        if (resultText != null)
        {
            resultText.text = GameResult.playerWon ? "KAZANDIN!" : "KAYBETTIN!";
            resultText.color = GameResult.playerWon ? Color.green : Color.red;
            resultTextShadow.text = GameResult.playerWon ? "KAZANDIN!" : "KAYBETTIN!";
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
