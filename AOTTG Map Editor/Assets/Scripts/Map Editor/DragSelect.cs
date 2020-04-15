using System.Collections;
using UnityEngine;

namespace MapEditor
{
    //A singleton class for displaying the drag select box and selecting objects
    public class DragSelect : MonoBehaviour
    {
        //A self-reference to the singleton instance of this script
        public static DragSelect Instance { get; private set; }

        //The canvas game object
        [SerializeField] private GameObject canvas;
        //The Game Object that contains the selection box
        [SerializeField] private GameObject dragSelectBox;
        //How many pixels the cursor has to move after the drag starts before the drag select starts
        [SerializeField] private float deadzone = 5f;

        //The RectTransform component of the drag select box game object
        private RectTransform dragBoxRect;
        //A reference to the Canvas component
        private Canvas canvasComponent;

        private bool dragging = false;
        private Vector2 dragStartPosition;
        private Vector2 mousePosition;

        //Delegates to notify listners when the drag select box is activated or deactivated
        public delegate void OnDragStartEvent();
        public event OnDragStartEvent OnDragStart;
        public delegate void OnDragEndEvent();
        public event OnDragEndEvent OnDragEnd;

        private void Awake()
        {
            //Set this script as the only instance of the ObjectSelection script
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            //Get references to components on other game objects
            dragBoxRect = dragSelectBox.GetComponent<RectTransform>();
            canvasComponent = canvas.GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            //If in edit mode and the selection handle is not being dragged, update the drag select box
            if (EditorManager.Instance.currentMode == EditorMode.Edit && !SelectionHandle.Instance.getDragging())
                updateSelectionBox();
        }

        private void updateSelectionBox()
        {
            //Save the position where the mouse was pressed down
            if (Input.GetMouseButtonDown(0))
                dragStartPosition = Input.mousePosition;
            //Disable the drag select when the mouse is released
            else if (Input.GetMouseButtonUp(0))
            {
                dragSelectBox.SetActive(false);
                dragging = false;
                StartCoroutine(InvokeOnDragEndEvent());
            }
            //Update the drag select box while the mouse is held down
            else if (Input.GetMouseButton(0))
            {
                mousePosition = Input.mousePosition;

                //If the drag select box wasn't enabled yet and the cursor is outside of the dead zone, enable the drag select box
                if (!dragging && (mousePosition - dragStartPosition).magnitude > deadzone)
                {
                    dragSelectBox.SetActive(true);
                    dragging = true;
                    OnDragStart?.Invoke();
                }

                //If the drag select box is enabled, update the box and check for selected objects
                if (dragging)
                    updateDragSelectBox();
            }
        }

        private void updateDragSelectBox()
        {
            float scaleFactor = canvasComponent.scaleFactor;
            float width = Mathf.Abs(mousePosition.x - dragStartPosition.x) / scaleFactor;
            float height = Mathf.Abs(mousePosition.y - dragStartPosition.y) / scaleFactor;

            dragBoxRect.position = (mousePosition + dragStartPosition) / 2f;
            dragBoxRect.sizeDelta = new Vector2(width, height);
        }

        public bool getDragging()
        {
            return dragging;
        }

        private IEnumerator InvokeOnDragEndEvent()
        {
            //Wait until the end of the frame so that the OnHandleFinish event doesn't overlap with the OnMouseUp event
            yield return new WaitForEndOfFrame();

            //Notify all listners that the selection drag box was disabled
            OnDragEnd?.Invoke();
        }
    }
}