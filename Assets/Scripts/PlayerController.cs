using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public MapGenerator mapGen;

    Rigidbody2D rigid;

    public float moveSpeed = 5.0f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        SetPlayerStartPosition(mapGen.GetRoadCells());
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(moveX, moveY) * moveSpeed * Time.deltaTime;
        Vector2 targetPos = rigid.position + movement;

        int cellX = Mathf.RoundToInt(targetPos.x);
        int cellY = Mathf.RoundToInt(targetPos.y);

        if(mapGen.IsRoad(cellX, cellY))
        {
            rigid.MovePosition(targetPos);
        }
    }

    public void SetPlayerStartPosition(List<Cell> roadCells)
    {
        if (roadCells.Count > 0)
        {
            int n = Random.Range(0, roadCells.Count);
            this.transform.position = new Vector3(roadCells[n].x, roadCells[n].y, 0);
        }
    }
}
