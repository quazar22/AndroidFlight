using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    //[Header("Options")]
    //[Range(0f, 2f)] public float handleLimit = 1f;

    //[Header("Components")]
    ////public RectTransform background;
    ////public RectTransform handle;
    public Image bgImg;
    public Image joystickimg;
    private Vector3 inVector;

    //public float Horizontal { get { return inVector.x; } }
    //public float Vertical { get { return inVector.y; } }

    private void Start()
    {
        bgImg = GetComponent<Image>();
        joystickimg = transform.GetChild(0).GetComponent<Image>();
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgImg.rectTransform, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos.x = (pos.x / bgImg.rectTransform.sizeDelta.x);
            pos.y = (pos.y / bgImg.rectTransform.sizeDelta.y);
            inVector = new Vector3(pos.x*2 + 1, 0, pos.y*2 - 1);
            inVector = (inVector.magnitude > 1.0f) ? inVector.normalized : inVector;
            joystickimg.rectTransform.anchoredPosition = 
                new Vector3(inVector.x * (bgImg.rectTransform.sizeDelta.x / 2), 
                            inVector.z * (bgImg.rectTransform.sizeDelta.y / 2));
        }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        inVector = Vector3.zero;
        joystickimg.rectTransform.anchoredPosition = Vector3.zero;
    }

    public float Horizontal() {
        if (inVector.x != 0)
            return inVector.x;
        else
            return Input.GetAxis("Horizontal");
    }

    public float Vertical()
    {
        if (inVector.z != 0)
            return inVector.z;
        else
            return Input.GetAxis("Vertical");
    }
}
