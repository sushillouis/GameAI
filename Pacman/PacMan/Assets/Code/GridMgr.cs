using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMgr : MonoBehaviour {
    public static GridMgr instance;
    private void Awake() {
        instance = this;
    }

    //vars
    public TextAsset mazefile;
    public GameObject TileRoot;

    public GameObject smallPelletTile;
    public GameObject powerPelletTile;

    public GameObject blackTile;
    public GameObject pinkTile;
    public GameObject pathTile;

    public GameObject topLeftTile;
    public GameObject topRightTile;
    public GameObject bottomLeftTile;
    public GameObject bottomRightTile;
    public GameObject horizontalTile;
    public GameObject verticalTile;

    public List<Pellet> pellets = new List<Pellet>();
    public LayerMask pelletLayerMask;
    public List<Pellet> navNodes = new List<Pellet>();

    void Start() {
        //CreateGridTiles();
        SetNavNodes();
    }

    [ContextMenu("Create Grid Tiles")]
    void CreateGridTiles() {
        
        pellets.Clear();

        string[] lines = mazefile.text.Split('\n');
        for(int y = 0; y < lines.Length; y++) {
            string line = lines[y];
            Vector3Int lowerLeft = new Vector3Int(-line.Length / 2, lines.Length / 2, 0);
            for(int x = 0; x < line.Length; x++) {
                char tileType = line[x];
                Vector3Int position = new Vector3Int(x, -y, 0);
                GameObject tilePrefab = GetTilePrefab(tileType);
                if(tilePrefab != null) {
                    GameObject go = Instantiate(tilePrefab, lowerLeft + position, Quaternion.identity, TileRoot.transform);
                    Pellet pellet = go.GetComponent<Pellet>();
                    if(pellet != null) {
                        pellets.Add(pellet);
                    }
                }
            }
        }
        SetNavNodes();
    }



    [ContextMenu("Delete All Tiles")]
    public void DeleteAllTiles() {
#if UNITY_EDITOR
        pellets.Clear();
        Transform[] children = new Transform[TileRoot.transform.childCount];
        for(int i = 0; i < TileRoot.transform.childCount; i++) {
            children[i] = TileRoot.transform.GetChild(i);
        }

        foreach(Transform child in children)
            DestroyImmediate(child.gameObject);
#endif
    }



    public void ActivatePellets() {
        foreach(Pellet pellet in pellets) {
            pellet.gameObject.SetActive(true);
            pellet.isEaten = false;
            pellet.spriteRenderer.enabled = true;
        }
    }

    public LayerMask obsLayerMask;
    [ContextMenu("Set Nav Nodes")]
    public void SetNavNodes() {
        navNodes.Clear();
        foreach(Pellet pellet in pellets) {
            List<Vector2Int> directions = Utils.FindAvailableDirections(pellet.transform.position, obsLayerMask);
            if(directions.Count > 2) {
                pellet.SetNavNode(directions);
                Debug.Log("Pellet @ " + pellet.transform.position + " has " + directions.Count + " directions");
                navNodes.Add(pellet);
            } else if(directions.Count == 2) {
                if(!IsOppositeDirections(directions[0], directions[1])) {
                    Debug.Log("Pellet @ " + pellet.transform.position + " is a corner with 2 directions");
                    pellet.SetNavNode(directions);
                    navNodes.Add(pellet);
                }
            }
        }
    }
    
    bool IsOppositeDirections(Vector2Int dir1, Vector2Int dir2) {
        return (dir1 == -dir2);
    }

    GameObject GetTilePrefab(char tileType) {
        switch(tileType) {
            case '.':
                return smallPelletTile;
            case 'o':
                return powerPelletTile;
            case 'X':
                return blackTile;
            case '-':
                return pinkTile;
            case '=':
                return horizontalTile;
            case '|':
                return verticalTile;
            case '/':
                return topLeftTile;
            case '\\':
                return topRightTile;
            case 'L':
                return bottomLeftTile;
            case 'R':
                return bottomRightTile;

            default:
                return null;
        }

    }
}
