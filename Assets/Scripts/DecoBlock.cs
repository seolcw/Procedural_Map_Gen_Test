using UnityEngine;

public class DecoBlock 
{
    public int startX;
    public int startY;
    public int width;
    public int height;
    public CellType type;

    public DecoBlock(int startX, int startY, int width, int height, CellType type)
    {
        this.startX = startX;
        this.startY = startY;
        this.width = width;
        this.height = height;
        this.type = type;
    }
}
