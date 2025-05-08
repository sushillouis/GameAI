using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoBehaviour
{
    public static GameMgr inst;
    private GameInputs input;
    private void Awake()    {
        inst = this;
        input = new GameInputs();
        input.Enable();
        Random.InitState(1234); // Set a fixed seed for reproducibility
    }

    // Start is called before the first frame update
    void Start()    {
        Vector3 pos = GridMgr.inst.bottomLeft + new Vector3(GridMgr.inst.cellSize*5, 0, GridMgr.inst.cellSize * 5);
        float x = pos.x;
        float z = pos.z;
        for(int j = 0; j < 2; j++) {
            for(int i = 0; i < 5; i++) {
                Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, pos, Vector3.zero);
                SelectionMgr.inst.SelectEntity(ent);
                pos += new Vector3(GridMgr.inst.cellSize / 2, 0, 0);
            }
            pos = new Vector3(x, 0, z-GridMgr.inst.cellSize / 2);
        }


    }

    public Vector3 position;
    public float spread = 20;
    public float colNum = 10;
    public float initZ;
    // Update is called once per frame
    void Update()
    {
        if (input.Entities.Create100.triggered) {
            initZ = position.z;
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    Entity ent = EntityMgr.inst.CreateEntity(EntityType.PilotVessel, position, Vector3.zero);
                    position.z += spread;
                }
                position.x += spread;
                position.z = initZ;
            }
            DistanceMgr.inst.Initialize();
        }
    }
}
