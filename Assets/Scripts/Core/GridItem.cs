using UnityEngine;

//base class for every item that can occupy a grid cell (cube, stone, vase, chalice box)
//holds grid coordinates and defines the contract all items must implement
public abstract class GridItem : MonoBehaviour
{
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsEmpty => false;

    public virtual void Init(int row, int col)
    {
        Row = row;
        Col = col;
    }

    //returns true if this item can fall when cells below it are empty, obstacles return false
    public abstract bool CanFall();

    //apply damage, each subclass defines its own destruction logic
    public abstract void TakeDamage(int amount);

    //returns true if the item has been destroyed, used by levelmanager for win checks
    public abstract bool IsDestroyed();
}
