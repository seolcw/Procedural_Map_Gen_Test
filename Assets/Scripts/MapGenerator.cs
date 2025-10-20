using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public enum CellType
{
    Road,
    House,
    Empty,
    Park, // DecoBlock
    Playground // DecoBlock
}

public class MapGenerator : MonoBehaviour
{

    private enum Dir
    {
        Up,
        Down,
        Left,
        Right
    }

    Cell[,] cellGrid;

    // 처음 플레이어 생성 (맵 생성 이후 플레이어 생성)
    public PlayerController player;
    public CatAI cat;

    public GameObject housePrefab;
    public GameObject roadPrefab;
    public GameObject emptyPrefab;
    public GameObject parkPrefab;
    public GameObject playgroundPrefab;

    // 도로
    public GameObject straightRoadPrefab;
    public GameObject lshapeRoadPrefab;
    public GameObject tShapeInteractionRoadPrefab;
    public GameObject fourWayInteractionRoadPrefab;
    public GameObject deadEndRoadPrefab;

    int mapWidth = 30;
    int mapHeight = 30;
    int houseWidth = 1;
    int houseHeight = 1;

    public int numberOfHouses = 30;
    public int numberOfHouseBlocks = 5;

    public List<Cell> roadCells = new List<Cell>();

    List<Cell> houseCells = new List<Cell>();
    List<Cell> emptyCells = new List<Cell>();
    List<Cell> decoCells = new List<Cell>();

    List<HouseBlock> houseBlocks = new List<HouseBlock>();


    void Start()
    {
        CreateGrid(); // 전체적인 맵 크기 설정 함수

        for (int n = 0; n < numberOfHouseBlocks; n++)
        {
            PlaceHouseBlocks(); // 모여있는 집
        }

        for (int n = 0; n < numberOfHouses; n++)
        {
            PlaceHouses(); // cellType = empty 인곳 랜덤으로 정해서 배치 함수
        }

        PlaceDeco(CellType.Park, 4, 4, 2);
        PlaceDeco(CellType.Playground, 4, 3, 2);

        ClassifiedCells(); // 셀 순환하며 각 cellType 별로 분류 하는 함수
        SelectingRoadCells(); // house 셀 주위로 road 셀 배치하는 과정
        ConnectRoads(); // BFS로 떨어진 집끼리 도로 연결
        ConnectDecoCellsAroundRoadToRoad(); // deco셀 또한 bfs로 도로 연결

        SpawnPrefabs(); // 셀 직접 프린트 하는 함수

        player.SetPlayerStartPosition(roadCells);
        cat.InitializeCat(roadCells);
        
    }

    void Update()
    {

    }

    public List<Cell> GetRoadCells()
    {
        return roadCells;
    }

    private void CreateGrid()
    {
        cellGrid = new Cell[mapHeight, mapWidth];
        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                cellGrid[row, col] = new Cell(col, row); // row, col 위치 주의
            }
        }
    }

    private void PlaceHouses() // 단일 집 
    {
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int houseX = Random.Range(0, mapWidth - houseWidth);
            int houseY = Random.Range(0, mapHeight - houseHeight);

            if (cellGrid[houseY, houseX].type == CellType.Empty)
            {
                cellGrid[houseY, houseX].type = CellType.House;
                return;
            }
            //else if (cellGrid[houseY, houseX].type != CellType.Empty)
            //{
            //    PlaceHouses();
            //} // 재귀문제, 만약 빈 집이 없으면 무한 루프 가능성s -> maxAttempt 사용
        }
    }

    private void PlaceHouseBlocks() // 여러집 모여있는 곳
    {
        int blockWidth = Random.Range(1, 5);// 넓이 1-4
        int blockHeight = Random.Range(1, 3); // 높이 1-2

        int startX = Random.Range(0, mapWidth - blockWidth);
        int startY = Random.Range(0, mapHeight - blockHeight);

        // houseBlock 중에 type != empty가 있으면 겹치기 때문에 return
        for (int y = 0; y < blockHeight; y++)
        {
            for (int x = 0; x < blockWidth; x++)
            {
                if (cellGrid[startY + y, startX + x].type != CellType.Empty)
                {
                    return;
                }
            }
        }

        for (int y = 0; y < blockHeight; y++)
        {
            for (int x = 0; x < blockWidth; x++)
            {
                cellGrid[startY + y, startX + x].type = CellType.House;
            }
        }
    }

    private void PlaceDeco(CellType type, int width, int height, int count)
    {
        int attempts = 0;
        int maxAttempts = 10;

        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            while (!placed && attempts < maxAttempts)
            {
                int startX = Random.Range(0, mapWidth - width);
                int startY = Random.Range(0, mapHeight - height);
                attempts++;

                bool canPlace = true;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (cellGrid[startY + y, startX + x].type != CellType.Empty)
                        {
                            canPlace = false;
                            break;
                        }
                    }
                    if (!canPlace)
                    {
                        break;
                    }
                }

                if (canPlace)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            cellGrid[startY + y, startX + x].type = type;
                        }
                    }
                    placed = true;
                }
            }
        }
    }

    private void ClassifiedCells()
    {
        houseCells.Clear();
        roadCells.Clear();
        emptyCells.Clear();
        decoCells.Clear();

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                Cell cell = cellGrid[row, col];
                switch (cell.type)
                {
                    case CellType.House:
                        houseCells.Add(cell);
                        break;
                    case CellType.Road:
                        roadCells.Add(cell);
                        break;
                    case CellType.Empty:
                        emptyCells.Add(cell);
                        break;
                    case CellType.Playground:
                        decoCells.Add(cell);
                        break;
                    case CellType.Park:
                        decoCells.Add(cell);
                        break;
                }
            }
        }
    }

    private void SpawnPrefabs()
    {
        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                Vector3 pos = new Vector3(col, row, 0);
                // Vector3 사용이유 : Instantiate의 매개변수로 생성 객체의 Vector3 위치를 요구하기 때문
                switch (cellGrid[row, col].type)
                {
                    case CellType.House:
                        //Quaternion houseRotation = Quaternion.identity;
                        //Dir dir = DetermineHouseRotation(col, row);

                        //switch (dir) // 지금은 방향으로 해놓고 나중에 피리팹별 적용하기
                        //{
                        //    case Dir.Up:
                        //        houseRotation = Quaternion.identity;
                        //        break;
                        //    case Dir.Down:
                        //        houseRotation = Quaternion.Euler(0, 0, 180);
                        //        break;
                        //    case Dir.Right:
                        //        houseRotation = Quaternion.Euler(0, 0, -90);
                        //        break;
                        //    case Dir.Left:
                        //        houseRotation = Quaternion.Euler(0, 0, 90);
                        //        break;
                        //}
                        //Instantiate(housePrefab, pos, houseRotation);
                        Instantiate(housePrefab, pos, Quaternion.identity);
                        break;

                    case CellType.Road:
                        // 도로 방향을 경정하는 함수 SetRoadPrefabRotation
                        GameObject prefab;
                        Quaternion rot;
                        SetRoadPrefabRoatation(cellGrid[row, col], out prefab, out rot);
                        //Instantiate(roadPrefab, pos, Quaternion.identity);
                        Instantiate(prefab, pos, rot);
                        break;

                    case CellType.Empty:
                        Instantiate(emptyPrefab, pos, Quaternion.identity);
                        break;

                    case CellType.Park:
                        Instantiate(parkPrefab, pos, Quaternion.identity);
                        break;

                    case CellType.Playground:
                        Instantiate(playgroundPrefab, pos, Quaternion.identity);
                        break;
                }
            }
        }
    }

    // 셀 순회하면서 celltype 변경되면 각 타입 LIst 삭제/제거해야함
    private void SelectingRoadCells()
    {
        int[] aroundX = { 1, 1, 1, 0, -1, -1, -1, 0 };
        int[] aroundY = { 1, 0, -1, -1, -1, 0, 1, 1 };
        // 상하좌우대각선의 8방향 좌표, 

        foreach (Cell houseC in houseCells)
        {
            int hX = houseC.x;
            int hY = houseC.y;


            for (int i = 0; i < 8; i++)
            {
                int rX = hX + aroundX[i];
                int rY = hY + aroundY[i];

                // 전체 Cell의 범위 내부에 있는지? -> 건너뜀
                if (0 > rX || rX >= mapWidth || rY < 0 || rY >= mapHeight)
                {
                    continue;
                }

                Cell tCell = cellGrid[rY, rX];

                // road 배치할 곳의 cellTYpe 이 empty 이면 road로 변경, 리스트 갱신도
                if (tCell.type == CellType.Empty)
                {
                    tCell.type = CellType.Road;
                    emptyCells.Remove(tCell);
                    roadCells.Add(tCell);
                }
            }
        }

        foreach (Cell decoC in decoCells)
        {
            int decoX = decoC.x;
            int decoY = decoC.y;

            for (int i = 0; i < 8; i++)
            {
                int rX = decoX + aroundX[i];
                int rY = decoY + aroundY[i];

                if (rX < 0 || rX >= mapWidth || rY < 0 || rY >= mapHeight)
                {
                    continue;
                }

                Cell tCell = cellGrid[rY, rX];

                if (tCell.type == CellType.Empty)
                {
                    tCell.type = CellType.Road;
                    emptyCells.Remove(tCell);
                    roadCells.Add(tCell);
                }
            }
        }
    }

    private void ConnectRoads()
    {
        List<List<Cell>> roadGroups = new List<List<Cell>>();
        HashSet<Cell> isVisited = new HashSet<Cell>(); // 참조 타입 비교 -> 같은 좌표라도 다른 객체로 인식
                                                       // HaseSet 사용이유? -> 키 값 입력하면 시간 복잡도 O(1)로 바로 방문여부 확인 가능


        foreach (Cell roadC in roadCells)
        {
            if (isVisited.Contains(roadC)) // 방문여부 확인
            {
                continue;
            }

            List<Cell> group = new List<Cell>();
            Queue<Cell> queue = new Queue<Cell>();
            // BFS는 queue 사용, 같은 단계 노드를 먼저 탐색해서 시작점으로 부터 "가까운 것부터 먼저 탐색"하는 FIFO 방식
            queue.Enqueue(roadC); // BFS 로 그룹 분할 queue 사용, 시작점

            while (queue.Count > 0)
            {
                Cell curr = queue.Dequeue();
                if (isVisited.Contains(curr))
                {
                    continue;
                }

                isVisited.Add(curr);
                group.Add(curr);

                int[] dX = { 1, 0, -1, 0 };
                int[] dY = { 0, -1, 0, 1 };

                for (int i = 0; i < 4; i++)
                {
                    int nX = curr.x + dX[i];
                    int nY = curr.y + dY[i];

                    // 전체 셀 범위 확인용
                    if (nX < 0 || nX >= mapWidth || nY < 0 || nY >= mapHeight)
                    {
                        continue;
                    }

                    // 이웃 celltype == road 이고 방문X 셀이면 다시 큐에 집에 넣고 bfs 반복
                    Cell neighbor = cellGrid[nY, nX];
                    if (neighbor.type == CellType.Road && !isVisited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            roadGroups.Add(group); // 하나의 도로 섬 그룹 반환
        }

        Debug.Log($"전체 road그룹 개수: {roadGroups.Count}");
        // roadGroup이 1개이면 할 필요 없지롱
        while (roadGroups.Count > 1)
        {
            List<Cell> groupA = roadGroups[0];
            List<Cell> groupB = roadGroups[1];

            // 2개의 roadGroup을 선정해서 가장 가까운 Cell 찾기
            Cell closeA = null;
            Cell closeB = null;
            float minDistance = float.MaxValue;

            foreach (Cell a in groupA)
            {
                foreach (Cell b in groupB)
                {
                    float distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                    // 맨해튼 거리 기법 사용
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closeA = a;
                        closeB = b;
                    }
                }
            }

            int x0 = closeA.x;
            int y0 = closeA.y;
            int x1 = closeB.x;
            int y1 = closeB.y;

            int lineX = x1 > x0 ? 1 : -1;
            for (int x = x0; x != x1; x += lineX)
            {
                SetRoadCell(x, y0);
            }

            int lineY = y1 > y0 ? 1 : -1;
            for (int y = y0; y != y1; y += lineY)
            {
                SetRoadCell(x1, y);
            }

            groupA.AddRange(groupB);
            roadGroups.RemoveAt(1); // 두 그룹간 연결 이후 리스트 줄이고 연결 반복

            // 찾은 그룹별 가장 가까운 셀 closeA, close B 사이 도로 생성
            // x좌표 부터 실행하여 ㄱ 자 모양으로 만듬
            // x,y 좌표 번갈아 가며 실행하면 계단모양으로 나옴
        }
    }

    private void SetRoadCell(int x, int y)
    {
        Cell cell = cellGrid[y, x];
        if (cell.type == CellType.Empty)
        {
            cell.type = CellType.Road;
            emptyCells.Remove(cell);
            roadCells.Add(cell);
        }
    }

    private void ConnectDecoCellsAroundRoadToRoad()
    {
        int[] dX = { 1, 0, -1, 0 };
        int[] dY = { 0, -1, 0, 1 };

        foreach (Cell decoC in decoCells)
        {
            List<Cell> startRoads = new List<Cell>();
            // decoCells 주변 type == road 후보

            for (int i = 0; i < 4; i++)
            {
                int nX = decoC.x + dX[i];
                int nY = decoC.y + dY[i];

                if (nX < 0 || nX >= mapWidth || nY < 0 || nY >= mapHeight)
                {
                    continue;
                }

                Cell neighbor = cellGrid[nY, nX];

                if (neighbor.type == CellType.Road)
                {
                    startRoads.Add(neighbor);
                }
            }

            // BFS로 가까운 도로 찾기

            HashSet<Cell> visited = new HashSet<Cell>();
            Queue<Cell> queue = new Queue<Cell>();
            Dictionary<Cell, Cell> parentMap = new Dictionary<Cell, Cell>();

            foreach (var start in startRoads)
            {
                queue.Enqueue(start);
                visited.Add(start);
            }

            Cell found = null;
            while (queue.Count > 0 && found == null)
            {
                Cell curr = queue.Dequeue();

                foreach (int v in new int[] { 0, 1, 2, 3 })
                {
                    int rX = curr.x + dX[v];
                    int rY = curr.y + dY[v];

                    if (rX < 0 || rX >= mapWidth || rY < 0 || rY >= mapHeight)
                    {
                        continue;
                    }

                    Cell neighbor = cellGrid[rY, rX];
                    if (visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);

                    parentMap[neighbor] = curr; // 경로 역추적에 사용

                    if (roadCells.Contains(neighbor))
                    {
                        found = neighbor;
                        break;
                    }

                    if (neighbor.type == CellType.Empty || neighbor.type == CellType.Road)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (found != null)
            {
                Cell curr = found;
                while (!startRoads.Contains(curr))
                {
                    SetRoadCell(curr.x, curr.y);
                    curr = parentMap[curr];
                }
            }
        }
    }

    private void SetRoadPrefabRoatation(Cell cell, out GameObject prefab, out Quaternion rot)
    {
        bool right = IsRoad(cell.x + 1, cell.y);
        bool down = IsRoad(cell.x, cell.y - 1);
        bool left = IsRoad(cell.x - 1, cell.y);
        bool up = IsRoad(cell.x, cell.y + 1);

        int connectCount = 0;
        if (right) connectCount++;
        if (down) connectCount++;
        if (left) connectCount++;
        if (up) connectCount++;

        prefab = straightRoadPrefab;
        rot = Quaternion.identity;

        if (connectCount == 4)
        {
            prefab = fourWayInteractionRoadPrefab;
            rot = Quaternion.identity;
        }

        else if (connectCount == 3)
        {
            prefab = tShapeInteractionRoadPrefab;
            if (!up)
            {
                rot = Quaternion.identity; // 기준
            }
            else if (!right)
            {
                rot = Quaternion.Euler(0, 0, -90);
            }
            else if (!down)
            {
                rot = Quaternion.Euler(0, 0, -180);
            }
            else if (!left)
            {
                rot = Quaternion.Euler(0, 0, -270);
            }
        }

        else if (connectCount == 2)
        {
            if ((up && down) || (left && right))
            {
                prefab = straightRoadPrefab;
                if (up && down)
                {
                    rot = Quaternion.Euler(0, 0, -90);
                }
                else if (right && left) // 기준
                {
                    rot = Quaternion.identity;

                }
            }
            else if ((right && up) || (right && down) || (down && left) || (left && up))
            {
                prefab = lshapeRoadPrefab;

                if (right && down)
                {
                    rot = Quaternion.Euler(0, 0, -90);
                }
                else if (down && left)
                {
                    rot = Quaternion.Euler(0, 0, 180);
                }
                else if (left && up)
                {
                    rot = Quaternion.Euler(0, 0, 90);
                }
                else if (up && right) // 기준
                {
                    rot = Quaternion.identity;
                }
            }
        }

        else if (connectCount == 1)
        {
            prefab = deadEndRoadPrefab;

            if (left) // 기준
            {
                rot = Quaternion.identity;
            }
            else if (up)
            {
                rot = Quaternion.Euler(0, 0, -90);
            }
            else if (right)
            {
                rot = Quaternion.Euler(0, 0, -180);
            }
            else if (down)
            {
                rot = Quaternion.Euler(0, 0, 90);
            }
        }
    }

    public bool IsRoad(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return false;
        }
        return cellGrid[y, x].type == CellType.Road;
    }

    private bool IsHouse(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return false;
        }
        return cellGrid[y, x].type == CellType.House;
    }

    private Dir DetermineHouseRotation(int x, int y)
    {
        bool isUpRoad = IsRoad(x, y + 1);
        bool isDownRoad = IsRoad(x, y - 1);
        bool isRightRoad = IsRoad(x + 1, y);
        bool isLeftRoad = IsRoad(x - 1, y);

        bool isUpHouse = IsHouse(x, y + 1);
        bool isDownHouse = IsHouse(x, y - 1);
        bool isRightHouse = IsHouse(x + 1, y);
        bool isLeftHouse = IsHouse(x - 1, y);

        int howManyRoadsAroundHouse = (isUpRoad ? 1 : 0) + (isDownRoad ? 1 : 0) + (isRightRoad ? 1 : 0) + (isLeftRoad ? 1 : 0);

        if (howManyRoadsAroundHouse == 1)
        {
            if (isUpRoad) return Dir.Up;
            if (isDownRoad) return Dir.Down;
            if (isRightRoad) return Dir.Right;
            if (isLeftRoad) return Dir.Left;
        }

        else if (howManyRoadsAroundHouse > 1)
        {
            // 가중치 : Up > Down > Left >= Right
            if (isDownRoad) return Dir.Down; // 2d 게임 특성상 아래쪽 가중치
            if (isUpRoad) return Dir.Up;
            if (isLeftRoad && isRightRoad)
            {
                return (Random.value > 0.5f) ? Dir.Left : Dir.Right;
            }
            if (isLeftRoad) return Dir.Left;
            if (isRightRoad) return Dir.Right;
        }

        // house 끼리 마주보지 않게
        if (isUpHouse) return Dir.Down;
        if (isDownHouse) return Dir.Up;
        if (isRightHouse) return Dir.Left;
        if (isLeftHouse) return Dir.Right;

        // 4방향이 모두 집인 경우
        if (isUpHouse && isRightHouse && isLeftHouse && isDownHouse)
        {
            return Dir.Down; //////////////////사방이 막혔을때 다른 프리팹 적용
        }

        // Road, House로도 감싸지지 않은 경우
        return Dir.Down; ////////////////// 아무것도 없을때 다른 프리팹 적용


    }
}

