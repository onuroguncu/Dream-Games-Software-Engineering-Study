using UnityEngine;
using UnityEngine.EventSystems;

//attached to every grid item routes pointer clicks to the correct manager
//blocks input while gamestatemanager. isprocessing is true(so animations in progress)
public class InputHandler : MonoBehaviour, IPointerClickHandler
{
    private GridItem gridItem;

    private void Awake()
    {
        gridItem = GetComponent<GridItem>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameStateManager.Instance.IsProcessing) return;

        if (gridItem is CubeItem cube)
        {
            if (cube.IsRocket || cube.IsTnt)
                SpecialItemManager.Instance.HandleSpecialTap(cube);
            else
                BlastManager.Instance.HandleCubeTap(cube);
        }
    }
}
