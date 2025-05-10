using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BlocksRow {
    public int row;
    public List<BlockHandler> blockHandlers;

    public BlocksRow(int r) {
        row = r;
        blockHandlers = new List<BlockHandler>();
    }
}

public class BlocksMgr : MonoBehaviour
{
    public static BlocksMgr instance;
    private void Awake() {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(1234);

        ResetBlocks();

        ResettButton.onClick.AddListener(ResetBlocks);
        ShuffleButton.onClick.AddListener(Shuffle);
        SizeSlider.onValueChanged.AddListener(SliderChange);
        WinPanel.gameObject.SetActive(false);
    }

    void ResetBlocks() {
        panelSize = BlocksParentPanel.GetComponent<RectTransform>().rect.width;
        buttonSize = (panelSize / puzzleSize) - spacing;
        foreach(BlockHandler bh in blocks) {
            Destroy(bh.gameObject);
        }
        blocks.Clear();
        GenerateBlocks(puzzleSize);
    }

    public RectTransform WinPanel;
    public Transform BlocksParentPanel;
    public BlockHandler BlocksPrefab;
    public Slider SizeSlider;
    public TMP_Text SliderValueText;
    public Button ResettButton;
    public Button ShuffleButton;

    public float buttonSize;
    public float spacing;
    public float panelSize;

    public int puzzleSize = 3;


    public List<BlockHandler> blocks = new List<BlockHandler>();

    public Vector2Int spaceBlockRowCol;

    public void GenerateBlocks(int size) {
        float startX = -((size - 1) * (buttonSize + spacing)) / 2;
        float startY = ((size - 1) * (buttonSize + spacing)) / 2;
        int n = 1;
        blocks.Clear();
        for(int row = 0; row < size; row++) {
            for(int col = 0; col < size; col++) {
                Vector2 position = new Vector2(startX + col * (buttonSize + spacing),
                                               startY - row * (buttonSize + spacing));
                BlockHandler blockHandler = CreateBlock(row, col, n, size, position);
                n++;
                blocks.Add(blockHandler);
            }
        }

        spaceBlockRowCol = new Vector2Int(size - 1, size -1);


    }

    void SetButtonSize(Button button, float buttonSize) {
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(buttonSize, buttonSize);
    }

    BlockHandler CreateBlock(int row, int col, int num, int size, Vector2 position) {
        BlockHandler blockHandler = Instantiate(BlocksPrefab, BlocksParentPanel);
        SetButtonSize(blockHandler.blockButton, buttonSize);
        blockHandler.SetAnchoredPosition(position);
        Vector2Int rowCol = new Vector2Int(row, col);
        blockHandler.rowCol = rowCol;
        blockHandler.number = num;
        if(row == size - 1 && col == size - 1) {
            blockHandler.SetText("");
        } else {
            blockHandler.SetText(num.ToString());
        }
        blockHandler.blockButton.onClick.AddListener(blockHandler.TryMove);
        return blockHandler;
    }

    /// <summary>
    /// Called from BlockHandler.TryMove with current grid position in BlockHandler
    /// </summary>
    /// <param name="gridRowCol"></param>
    public void TryMove(Vector2Int gridRowCol) {
        if(IsAdjacent(gridRowCol, spaceBlockRowCol))
            MoveBlock(gridRowCol);
    }

    bool IsAdjacent(Vector2Int a, Vector2Int b) {
        return (Mathf.Abs(a.x - b.x) == 1 && (a.y == b.y)) || (Mathf.Abs(a.y - b.y) == 1 && (b.x == a.x)) ;
    }

    void MoveBlock(Vector2Int gridRowCol) {
        BlockHandler blockHandler = blocks.Find(x=>x.rowCol.x == gridRowCol.x && x.rowCol.y == gridRowCol.y);
        Vector2Int tmpRC = blockHandler.rowCol;

        BlockHandler spaceBlockHandler = blocks.Find(x => x.rowCol.x == spaceBlockRowCol.x && x.rowCol.y == spaceBlockRowCol.y);

        blockHandler.rowCol = spaceBlockRowCol;
        spaceBlockHandler.rowCol = tmpRC;

        spaceBlockRowCol = tmpRC;

        Vector2 blockPosition = blockHandler.position;
        blockHandler.SetAnchoredPosition(spaceBlockHandler.position);
        spaceBlockHandler.SetAnchoredPosition(blockPosition);
        /*
        if(IsWinningCondition()) {
            Debug.Log("UI: Puzzle Solved!");
            OnPuzzleSolved();
        }
        */
    }

    public bool IsWinningCondition() {
        int n = 0;
        for(int row = 0; row < puzzleSize; row++) {
            for(int col = 0; col < puzzleSize; col++) {
                BlockHandler bhn = blocks[n];
                if(bhn.rowCol.x != row || bhn.rowCol.y != col)
                    return false;
                n++;
            }
        }
        return true;
    }


    public bool IsSpaceBlock(BlockHandler bh) {
        return bh.number == puzzleSize * puzzleSize;
    }

    public int nShuffles = 10;
    public List<BlockHandler> possibleMoves = new List<BlockHandler>();
    public void Shuffle() {

        for(int i = 0; i < nShuffles; i++) {
            possibleMoves.Clear();
            foreach(BlockHandler bh in blocks) {
                if(IsSpaceBlock(bh))
                    continue;
                if(IsAdjacent(bh.rowCol, spaceBlockRowCol))
                    possibleMoves.Add(bh);
            }
            if(possibleMoves.Count > 0) {
                int r = UnityEngine.Random.Range(0, possibleMoves.Count);
                MoveBlock(possibleMoves[r].rowCol);
            }
        }

    }

    public void SliderChange(float value) {
        //SliderValueText.text = value.ToString();
        switch((int) value) {
            case 3:
                SliderValueText.text = "Easy";
                break;
            case 4:
                SliderValueText.text = "Medium";
                break;
            case 5:
                SliderValueText.text = "Hard";
                break;
            default:
                SliderValueText.text = "Unknown";
                break;
        }
        puzzleSize = (int) value;
        ResetBlocks();
    }

    public void OnPuzzleSolved() {
        WinPanel.gameObject.SetActive(true);
        StartCoroutine(TimedHide(2f));
        
    }

    private IEnumerator TimedHide(float time) {
        yield return new WaitForSeconds(time);
        WinPanel.gameObject.SetActive(false);
    }
}
