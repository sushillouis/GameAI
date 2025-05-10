using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PuzzleState {
    public int size;
    public int[,] board;
    public Vector2Int spacePos;
    public PuzzleState parent;
    public string boardString;
    public Vector2Int movedFrom;

    public PuzzleState(int size, int[,] board, Vector2Int emptyPos, Vector2Int movedFrom, PuzzleState parent = null) {

        this.size = size;
        this.board = board;
        this.spacePos = emptyPos;
        this.movedFrom = movedFrom;
        this.parent = parent;
        boardString = ToString();

    }
    public string GetStateKey() {
        string key = "";
        for(int i = 0; i < size; i++) {
            for(int j = 0; j < size; j++) {
                key += board[i, j] + "_";
            }
        }
        return key;
    }

    public bool IsGoalState() {
        int n = 1;
        for(int i = 0; i < size; i++) {
            for(int j = 0; j < size; j++) {
                if(board[i, j] != n)
                    return false;
                n++;
            }
        }
        return true;
    }

    public PuzzleState(List<BlockHandler> blocks, Vector2Int spacePos, int size) {
        this.size = size;
        this.spacePos = new Vector2Int(spacePos.x, spacePos.y);
        board = new int[size, size];
        foreach(BlockHandler bh in blocks) {
            board[bh.rowCol.x, bh.rowCol.y] = bh.number;
        }
        boardString = ToString();
        movedFrom = new Vector2Int(-1, -1);
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder("Board:\n");
        int spaceNum = BlocksMgr.instance.puzzleSize * BlocksMgr.instance.puzzleSize;
        for(int i = 0; i < size; i++) {
            sb.Append("| ");
            for(int j = 0; j < size; j++) {
                if(board[i, j] == spaceNum)
                    sb.Append("_ | ");
                else 
                    sb.Append(board[i, j] + " | ");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }

}

[Serializable]
public class TreeNode {
    public PuzzleState state;
    public List<TreeNode> children;

    public TreeNode(PuzzleState state) {
        this.state = state;
        children = new List<TreeNode>();

    }
}

public class BreadthFirstSearch : MonoBehaviour
{
    public PuzzleState initialState;
    public Button SolveButton;

    // Start is called before the first frame update
    void Start()
    {
        SolveButton.onClick.AddListener(Solve);
        InitDirections();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Vector2Int> directions = new List<Vector2Int>();

    void InitDirections() {
        Vector2Int dir = new Vector2Int(0, 1);
        directions.Add(dir);
        dir = new Vector2Int(0, -1);
        directions.Add(dir);
        dir = new Vector2Int(1, 0);
        directions.Add(dir);
        dir = new Vector2Int(-1, 0);
        directions.Add(dir);
    }
    public void Solve() {
        initialState = new PuzzleState(BlocksMgr.instance.blocks, 
            BlocksMgr.instance.spaceBlockRowCol, BlocksMgr.instance.puzzleSize);
        Debug.Log(initialState.ToString());

        Queue<TreeNode> queue = new Queue<TreeNode>();
        HashSet<string> visitedStates = new HashSet<string>();

        TreeNode RootNode = new TreeNode(initialState);
        queue.Enqueue(RootNode);
        visitedStates.Add(initialState.GetStateKey());

        while(queue.Count > 0) {
            TreeNode currentNode = queue.Dequeue();
            PuzzleState currentState = currentNode.state;
            if(currentState.IsGoalState()) {
                MoveToSolve(currentState);
                return;
            } else {
                foreach(Vector2Int dir in directions) {
                    Vector2Int newSpacePos = new Vector2Int(currentState.spacePos.x + dir.x, currentState.spacePos.y + dir.y);
                    if(IsValidPosition(newSpacePos)) {
                        int[,] newBoard = SwapTiles(currentState.board, currentState.spacePos,  newSpacePos);
                        PuzzleState newState = new PuzzleState(BlocksMgr.instance.puzzleSize, newBoard, newSpacePos, newSpacePos, currentState);
                        if(!visitedStates.Contains(newState.GetStateKey())) {
                            TreeNode newNode = new TreeNode(newState);
                            currentNode.children.Add(newNode);
                            queue.Enqueue(newNode);
                            visitedStates.Add(newState.GetStateKey());
                        }
                    }
                }
            }
        }
        Debug.Log("NO SOLUTION FOUND!");
    }

    int[,] SwapTiles(int[,] board, Vector2Int spacePos, Vector2Int newSpacePos) {
        int[,] newBoard = (int[,]) board.Clone();
        int tmp = newBoard[spacePos.x, spacePos.y];
        newBoard[spacePos.x, spacePos.y] = newBoard[newSpacePos.x, newSpacePos.y];
        newBoard[newSpacePos.x, newSpacePos.y] = tmp; //better be 9
        return newBoard;
    }

    bool IsValidPosition(Vector2Int pos) {
        return (pos.x >= 0 && pos.y >= 0 && pos.x < BlocksMgr.instance.puzzleSize && pos.y < BlocksMgr.instance.puzzleSize);
    }

    public void MoveToSolve(PuzzleState goalState) {
        Debug.Log("SOLVED!----------------\n" + goalState.ToString());

        List<PuzzleState> path = new List<PuzzleState>();
        for(PuzzleState state = goalState; state != null; state = state.parent) 
            path.Add(state);
        path.Reverse();

        StartCoroutine(AnimateSolution(path));


    }

    IEnumerator AnimateSolution(List<PuzzleState> path) {
        foreach(PuzzleState state in path) {
            BlocksMgr.instance.TryMove(state.movedFrom);
            yield return new WaitForSeconds(0.5f);
        }
        BlocksMgr.instance.OnPuzzleSolved();
    }


}
