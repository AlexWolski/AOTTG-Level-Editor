using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public enum HandlerType
{
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    TopLeft,
    Top
}

[RequireComponent(typeof(EventTrigger))]
public class FlexibleResizeHandler : MonoBehaviour
{
    public HandlerType Type;
    public RectTransform Target;
    public Vector2 MinimumDimmensions = new Vector2(50, 50);
    public Vector2 MaximumDimmensions = new Vector2(800, 800);

    private static Canvas parentCanvas;
    private EventTrigger _eventTrigger;
    private Vector2 dragStartPoint;
    private Vector2 rectStartSize;
    private static Texture2D ewResizeImage;
    private static Vector2 cursorHotSpot;
    private static bool resizeBoxPressed;
    
	void Start ()
	{
	    _eventTrigger = GetComponent<EventTrigger>();
        _eventTrigger.AddEventTrigger(OnMouseEnter, EventTriggerType.PointerEnter);
        _eventTrigger.AddEventTrigger(OnMouseExit, EventTriggerType.PointerExit);
        _eventTrigger.AddEventTrigger(OnMouseDown, EventTriggerType.PointerDown);
        _eventTrigger.AddEventTrigger(OnMouseUp, EventTriggerType.PointerUp);
        _eventTrigger.AddEventTrigger(OnDrag, EventTriggerType.Drag);
        _eventTrigger.AddEventTrigger(OnEndDrag, EventTriggerType.EndDrag);
        dragStartPoint = new Vector2(0, 0);
        rectStartSize = new Vector2(0, 0);
        parentCanvas = GetComponentInParent<Canvas>();
        ewResizeImage = (Texture2D)Resources.Load("GUI/ew-resize", typeof(Texture2D));
        cursorHotSpot = new Vector2(ewResizeImage.width/2, ewResizeImage.height/2);
    }

    void OnMouseEnter(BaseEventData data)
    {
        Cursor.SetCursor(ewResizeImage, cursorHotSpot, CursorMode.ForceSoftware);
    }

    void OnMouseExit(BaseEventData data)
    {
        if(!resizeBoxPressed)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void OnMouseDown(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;
        dragStartPoint.Set(ped.position.x, ped.position.y);
        rectStartSize.Set(Target.rect.width, Target.rect.height);
        resizeBoxPressed = true;
    }

    void OnMouseUp(BaseEventData data)
    {
        resizeBoxPressed = false;
    }

    void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData) data;
        RectTransform.Edge? horizontalEdge = null;
        RectTransform.Edge? verticalEdge = null;

        switch (Type)
        {
            case HandlerType.TopRight:
                horizontalEdge = RectTransform.Edge.Left;
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            case HandlerType.Right:
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.BottomRight:
                horizontalEdge = RectTransform.Edge.Left;
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.Bottom:
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.BottomLeft:
                horizontalEdge = RectTransform.Edge.Right;
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.Left:
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.TopLeft:
                horizontalEdge = RectTransform.Edge.Right;
                verticalEdge = RectTransform.Edge.Bottom;
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
                    Mathf.Clamp(rectStartSize.x - (ped.position.x - dragStartPoint.x)/parentCanvas.scaleFactor, MinimumDimmensions.x, MaximumDimmensions.x));
            else
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 0,
                    Mathf.Clamp(rectStartSize.x + (ped.position.x - dragStartPoint.x)/ parentCanvas.scaleFactor, MinimumDimmensions.x, MaximumDimmensions.x));
        }
        if (verticalEdge != null)
        {
            if (verticalEdge == RectTransform.Edge.Top)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Screen.height - Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height - ped.delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
            else 
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height + ped.delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
        }
    }

    void OnEndDrag(BaseEventData data)
    {
        Cursor.SetCursor(null, cursorHotSpot, CursorMode.Auto);
    }
}
