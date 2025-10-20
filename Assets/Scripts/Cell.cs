using UnityEngine;

public class Cell 
    // Monobehaviour 를 상속하면 -> Unity가 관리하는 컴포넌트로 분류
    // 따라서 new 키워드로 객체 생성 불가능, 가능방식) Cell a = AddComponent<Cell>();
{
    public int x;
    public int y;
    public CellType type;

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.type = CellType.Empty;
    }
}
