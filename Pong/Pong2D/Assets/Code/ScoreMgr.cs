using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Collections;
public class ScoreMgr : MonoBehaviour
{
    public static ScoreMgr instance;

    private void Awake() {
        instance = this;
        winPanel.gameObject.SetActive(false);
    }
    public TMP_Text playerScoreText;
    public TMP_Text AIScoreText;
    public BallController ballController;

    int playerScore = 0;
    int AIScore = 0;
    int maxScore = 21;
    public void IncScore(bool isPlayer) {
        if(isPlayer) {
            playerScore = int.Parse(playerScoreText.text);
            playerScore++;
            playerScoreText.text = playerScore.ToString();
        } else {
            AIScore = int.Parse(AIScoreText.text);
            AIScore++;
            AIScoreText.text = AIScore.ToString();
        }
        CheckWin();
    }

    void CheckWin() {
        if(playerScore >= maxScore) {
            Debug.Log("Player wins!");
            // Reset scores
            playerScore = 0;
            AIScore = 0;
            playerScoreText.text = playerScore.ToString();
            AIScoreText.text = AIScore.ToString();
            StartCoroutine(ReLaunch(true));
        } else if(AIScore >= maxScore) {
            Debug.Log("AI wins!");
            // Reset scores
            playerScore = 0;
            AIScore = 0;
            playerScoreText.text = playerScore.ToString();
            AIScoreText.text = AIScore.ToString();
            StartCoroutine(ReLaunch(false));
        }
        // Reset ball position

    }

    public RectTransform winPanel;
    public TMP_Text winText;
    public GameObject Table;
    IEnumerator ReLaunch(bool playerWin) {
        ballController.transform.position = new Vector2(0, 0);
        ballController.speed = 0f;
        SetWinOutput(playerWin);

        winPanel.gameObject.SetActive(true);
        Table.SetActive(false);
        yield return new WaitForSeconds(5f);

        Table.SetActive(true);
        winPanel.gameObject.SetActive(false);
        ballController.LaunchBall();
    }

    void SetWinOutput(bool playerWin) {
        if(playerWin) {
            winText.text = "Player wins!";
        } else {
            winText.text = "AI wins!";
        }
    }

}
