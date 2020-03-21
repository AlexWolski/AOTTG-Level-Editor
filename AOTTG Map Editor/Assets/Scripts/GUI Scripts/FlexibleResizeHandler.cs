using System;
using UnityEngine;
using UnityEngine.EventSystems;
using GILES;

public enum HandlerType
{
    Top,
    Bottom,
    Right,
    Left
}

public class FlexibleResizeHandler : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IDragHandler, IEndDragHandler
{
    public HandlerType Type;
    public RectTransform Target;
    public Vector2 MinDimensions;
    public Vector2 MaxDimensions;

    private static Canvas parentCanvas;
    private Vector2 dragStartPoint;
    private Vector2 rectStartSize;
    [SerializeField]
    private Texture2D ewResizeImage;
    private Vector2 cursorHotSpot;
    private static bool resizeBoxPressed;
    private static bool resizeBoxHover;
    
	void Start ()
	{
        dragStartPoint = new Vector2(0, 0);
        rectStartSize = new Vector2(0, 0);
        parentCanvas = GetComponentInParent<Canvas>();
        cursorHotSpot = new Vector2(ewResizeImage.width/2, ewResizeImage.height/2);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        //Don't enter dragging mode if the tool handle is being dragged
        if (!SelectionHandle.InUse())
        {
            Cursor.SetCursor(ewResizeImage, cursorHotSpot, CursorMode.ForceSoftware);
            resizeBoxHover = true;
        }
    }

    public void OnPointerExit(PointerEventData data)
    {
        if(!resizeBoxPressed)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        resizeBoxHover = false;
    }

    public void OnPointerDown(PointerEventData data)
    {
        PointerEventData ped = (PointerEventData)data;
        dragStartPoint.Set(ped.position.x, ped.position.y);
        rectStartSize.Set(Target.rect.width, Target.rect.height);
        resizeBoxPressed = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        resizeBoxPressed = false;
    }

    public void OnDrag(PointerEventData data)
    {
        PointerEventData ped = (PointerEventData) data;
        RectTransform.Edge? horizontalEdge = null;
        RectTransform.Edge? verticalEdge = null;

        switch (Type)
        {
            case HandlerType.Right:
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.Bottom:
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.Left:
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.Top:
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if (horizontalEdge != null)
        {
            if (horizontalEdge == RectTransform.Edge.Right)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 0,
                    Mathf.Clamp(rectStartSize.x - (ped.position.x - dragStartPoint.x)/parentCanvas.scaleFactor, MinDimensions.x, MaxDimensions.x));
            else
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 0,
                    Mathf.Clamp(rectStartSize.x + (ped.position.x - dragStartPoint.x)/ parentCanvas.scaleFactor, MinDimensions.x, MaxDimensions.x));
        }
        if (verticalEdge != null)
        {
            if (verticalEdge == RectTransform.Edge.Top)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Screen.height - Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height - ped.delta.y, MinDimensions.y, MaxDimensions.y));
            else 
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height + ped.delta.y, MinDimensions.y, MaxDimensions.y));
        }
    }

    public void OnEndDrag(PointerEventData data)
    {
        if(!resizeBoxHover)
            Cursor.SetCursor(null, cursorHotSpot, CursorMode.Auto);
    }
}
