using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockHandler : MonoBehaviour
{
    private void Awake() {
        rectTransform = transform.GetComponent<RectTransform>();
        blockButton = transform.GetComponent<Button>();
        blockButtonText = transform.GetComponentInChildren<TMP_Text>();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public RectTransform rectTransform;

    public Button blockButton;
    public TMP_Text blockButtonText;

    public int number;
    public Vector2Int rowCol;
    public Vector2 position;

    public void SetText(string msg) {
        blockButtonText.text = msg;
    }

    public void SetAnchoredPosition(Vector2 inPosition) {
        position = new Vector2(inPosition.x, inPosition.y);
        rectTransform.anchoredPosition = position;
    }

    public void TryMove() {
        BlocksMgr.instance.TryMove(rowCol);
    }


}
