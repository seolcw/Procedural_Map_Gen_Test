using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CatState
{
    MoveRandomly,
    Idle,
    LooAroundInteraction,
}

public class CatAI : MonoBehaviour
{
    public CatState state = CatState.MoveRandomly; // enum 을 public으로 안두면 인식 못함???
    public MapGenerator mapGen;

    public float catMoveSpeed = 2.0f;
    public float waitTIme = 0.1f;

    public Vector2Int currentCell;
    public Vector2Int preCell;

    bool isCatMoving = false;
    Vector3 targetPosition;

    void Start()
    {
        
    }

    public void InitializeCat(List<Cell> roadCells)
    {
        SetCatStartPosition(roadCells);

        StartCoroutine(MoveRoutine());
    }

    void Update()
    {
        switch (state)
        {
            case CatState.MoveRandomly:
               
                break;
            
            case CatState.Idle:
                break;

            case CatState.LooAroundInteraction:
                break;
        }
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            if(state == CatState.MoveRandomly && !isCatMoving)
            {
                yield return StartCoroutine(MoveToAdjacentRoad());
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator MoveToAdjacentRoad()
    {
        isCatMoving = true;

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.right,
            Vector2Int.left
        };

        List<Vector2Int> candidates = new List<Vector2Int>();

        // 이전 셀을 제외하고 이동 후보 리스트에 넣기
        foreach(var dir in directions)
        {
            Vector2Int next = currentCell + dir;
            if(mapGen.IsRoad(next.x, next.y) && next != preCell) {
                candidates.Add(next);
            }
        }

        // 막다른 길이면 이전 셀 초기화 후 다시 후보 탐색
        if(candidates.Count == 0)
        {
            preCell = new Vector2Int(-1, -1);
            foreach(var dir in directions)
            {
                Vector2Int next = currentCell + dir;
                if (mapGen.IsRoad(next.x, next.y))
                {
                    candidates.Add(next);
                }
            }
        }

        // 막다른 길이 아닌 경우
        if(candidates.Count > 0)
        {
            Vector2Int nextCell = candidates[Random.Range(0, candidates.Count)];
            Vector3 targetPos = new Vector3(nextCell.x, nextCell.y, 0);

            float offsetX = Random.Range(0.1f, 0.4f);
            float offsetY = Random.Range(0.1f, 0.4f);

            // offset을 더한 랜더링용
            Vector3 renderingTargetPos = targetPos + new Vector3(offsetX, offsetY, 0);

            // target 셀의 offset을 더한 상태
            while(Vector3.Distance(this.transform.position, renderingTargetPos) > 0.05f) {
                transform.position = Vector3.MoveTowards(this.transform.position, renderingTargetPos, catMoveSpeed * Time.deltaTime);
                yield return null;
            }

            // ai 계산에서 다시 offset 없는 값으로 이동
            transform.position = renderingTargetPos;
            preCell = currentCell;
            currentCell = nextCell;
        }
        yield return new WaitForSeconds(waitTIme);
        isCatMoving = false;
    }

    public void SetCatStartPosition(List<Cell> roadCells)
    {
        if(roadCells.Count > 0)
        {
            int n = Random.Range(0, roadCells.Count);
            this.transform.position = new Vector3(roadCells[n].x, roadCells[n].y, 0);
            currentCell = new Vector2Int(roadCells[n].x, roadCells[n].y);
            preCell = new Vector2Int(-1, -1);
        }
    }
}
