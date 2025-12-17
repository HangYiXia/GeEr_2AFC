using UnityEngine;
using UnityEngine.EventSystems;

public class SelectableStimulus : MonoBehaviour, IPointerClickHandler
{
    public int myOptionID; // 左边填1，右边填2
    public BaseInputHandler inputHandler; // 引用场景里的 InputHandler

    public void OnPointerClick(PointerEventData eventData)
    {
        // 当被VR射线点击，或者鼠标点击时
        inputHandler.PublicSubmit(myOptionID);
    }
}