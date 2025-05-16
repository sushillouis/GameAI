using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GuiMgr : MonoBehaviour
{
    public static GuiMgr instance;
    private void Awake() {
        instance = this;
    }

    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public RectTransform ReadyPanel;
    public TMP_Text readyText;


    public List<Image> livesImages;

    public void SetScore(int score) {
        scoreText.text = score.ToString();
    }
    public void SetHighScore(int score) {
        highScoreText.text = score.ToString();
    }

    public void ShowPanelForTime(RectTransform panel, float time) {
        panel.gameObject.SetActive(true);
        StartCoroutine(ShowTextForTimeCoroutine(panel, time));
    }

    private IEnumerator ShowTextForTimeCoroutine(RectTransform panel, float time) {
        yield return new WaitForSeconds(time);
        panel.gameObject.SetActive(false);
    }

    private IEnumerator WaitForTime(float duration) {
        yield return new WaitForSeconds(duration);
        readyText.text = "Ready";
    }

    public void SetLives(int lives) {
        int i = 0;
        foreach(Image img in livesImages) {
            if(i < lives)
                img.gameObject.SetActive(true);
            else
                img.gameObject.SetActive(false);
            i++;
        }
    }

    public void GameOver() {
        ReadyPanel.gameObject.SetActive(true);
        readyText.text = "Game Over!";
        StartCoroutine(WaitForTime(2f));
    }

    public void NewGame() {
        scoreText.text = "00000";
        foreach(Image img in livesImages) {
            img.gameObject.SetActive(true);
        }
        ReadyPanel.gameObject.SetActive(false);
    }

}
