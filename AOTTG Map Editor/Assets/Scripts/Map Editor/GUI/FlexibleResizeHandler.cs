using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MapEditor
{
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
        public HandlerType type;
        public RectTransform target;
        public Vector2 minDimensions;
        public Vector2 maxDimensions;

        private static Canvas parentCanvas;
        private Vector2 dragStartPoint;
        private Vector2 rectStartSize;
        [SerializeField]
        private Texture2D ewResizeImage;
        private Vector2 cursorHotSpot;
        private static bool resizeBoxPressed;
        private static bool resizeBoxHover;

        void Start()
        {
            dragStartPoint = new Vector2(0, 0);
            rectStartSize = new Vector2(0, 0);
            parentCanvas = GetComponentInParent<Canvas>();
            cursorHotSpot = new Vector2(ewResizeImage.width / 2, ewResizeImage.height / 2);

            //Listen for when the cursor is released
            EditorManager.Instance.OnCursorReleased += onCursorReleased;
        }

        //If the game loses focus, reset the cursor and window
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                resizeBoxHover = false;
                resizeBoxPressed = false;
            }
        }

        //If the cursor is over the resize box when released from another class, set the cursor
        private void onCursorReleased()
        {
            if(resizeBoxHover)
                Cursor.SetCursor(ewResizeImage, cursorHotSpot, CursorMode.ForceSoftware);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            resizeBoxHover = true;

            //Don't change the cursor if the tool handle is being dragged
            if (EditorManager.Instance.CursorAvailable)
                Cursor.SetCursor(ewResizeImage, cursorHotSpot, CursorMode.ForceSoftware);
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (!resizeBoxPressed)
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            resizeBoxHover = false;
        }

        public void OnPointerDown(PointerEventData data)
        {
            PointerEventData ped = (PointerEventData)data;
            dragStartPoint.Set(ped.position.x, ped.position.y);
            rectStartSize.Set(target.rect.width, target.rect.height);
            resizeBoxPressed = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            resizeBoxPressed = false;
        }

        public void OnDrag(PointerEventData data)
        {
            PointerEventData ped = (PointerEventData)data;
            RectTransform.Edge? horizontalEdge = null;
            RectTransform.Edge? verticalEdge = null;

            switch (type)
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
                    target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 0,
                        Mathf.Clamp(rectStartSize.x - (ped.position.x - dragStartPoint.x) / parentCanvas.scaleFactor, minDimensions.x, maxDimensions.x));
                else
                    target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 0,
                        Mathf.Clamp(rectStartSize.x + (ped.position.x - dragStartPoint.x) / parentCanvas.scaleFactor, minDimensions.x, maxDimensions.x));
            }
            if (verticalEdge != null)
            {
                if (verticalEdge == RectTransform.Edge.Top)
                    target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge,
                        Screen.height - target.position.y - target.pivot.y * target.rect.height,
                        Mathf.Clamp(target.rect.height - ped.delta.y, minDimensions.y, maxDimensions.y));
                else
                    target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge,
                        target.position.y - target.pivot.y * target.rect.height,
                        Mathf.Clamp(target.rect.height + ped.delta.y, minDimensions.y, maxDimensions.y));
            }
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!resizeBoxHover)
                Cursor.SetCursor(null, cursorHotSpot, CursorMode.Auto);
        }
    }
}