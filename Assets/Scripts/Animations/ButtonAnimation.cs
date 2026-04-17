using UnityEngine;
using UnityEngine.EventSystems;

//simple press scale animation for level select buttons
//shrinks slightly on press and returns to normal on release
public class LevelButtonAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}
