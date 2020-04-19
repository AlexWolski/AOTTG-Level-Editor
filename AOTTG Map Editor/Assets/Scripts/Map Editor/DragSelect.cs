using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapEditor
{
    //A singleton class for displaying the drag select box and selecting objects
    public class DragSelect : MonoBehaviour
    {
        #region Data Members
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

        //A reference to the main camera in the scene
        private Camera mainCamera;
        //Cached transformation matricies for the camera
        Matrix4x4 worldToViewMatrix;
        Matrix4x4 projectionMatrix;

        private bool mouseDown = false;
        private bool dragging = false;
        private Vector2 mousePosition;
        private Vector2 dragStartPosition;

        //A dictionary that maps visible game objects to their screen space bounding box (top left and bottom right verticies)
        private Dictionary<GameObject, Tuple<Vector2, Vector2>> boundingBoxTable;
        #endregion

        #region Delegates
        public delegate void OnDragStartEvent();
        public event OnDragStartEvent OnDragStart;

        public delegate void OnDragEndEvent();
        public event OnDragEndEvent OnDragEnd;
        #endregion

        #region Instantiation
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
            //Find and store the main camrea in the scene
            mainCamera = Camera.main;

            boundingBoxTable = new Dictionary<GameObject, Tuple<Vector2, Vector2>>();

            //Get references to components on other game objects
            dragBoxRect = dragSelectBox.GetComponent<RectTransform>();
            canvasComponent = canvas.GetComponent<Canvas>();

            //Add listners to save map object verticies when needed
            MapManager.Instance.OnImport += saveBoundingBoxes;
            MapManager.Instance.OnPaste += saveBoundingBoxes;
            MapManager.Instance.OnDelete += removeBoundingBoxes;
            EditorManager.Instance.OnChangeMode += onModeChange;
            SelectionHandle.Instance.OnHandleFinish += saveSelectedBBs;
        }
        #endregion

        #region Event Handlers
        //Save or clear the bounding boxes based on the new mode
        private void onModeChange(EditorMode prevMode, EditorMode newMode)
        {
            //When the mode changes from fly to edit mode, save the bounding boxes of onscreen objects
            if (prevMode == EditorMode.Fly && newMode == EditorMode.Edit)
            {
                //Store the camera tranformation matricies
                worldToViewMatrix = mainCamera.worldToCameraMatrix;
                projectionMatrix = mainCamera.projectionMatrix;

                //Save the bounding box of all selectable objects
                saveBoundingBoxes(ref ObjectSelection.Instance.getSelectable());
            }
            //If the new mode is fly mode, then clear the verticies
            else if (newMode == EditorMode.Fly)
                clearObjectVerticies();
        }

        //Saves the bounding boxes of the currently updated objects
        private void saveSelectedBBs()
        {
            saveBoundingBoxes(ref ObjectSelection.Instance.getSelection());
        }

        private IEnumerator InvokeOnDragEndEvent()
        {
            //Wait until the end of the frame so that the OnHandleFinish event doesn't overlap with the OnMouseUp event
            yield return new WaitForEndOfFrame();

            //Notify all listners that the selection drag box was disabled
            OnDragEnd?.Invoke();
        }
        #endregion

        #region Update
        private void LateUpdate()
        {
            //If in edit mode and the selection handle is not being dragged, update the drag select box
            if (EditorManager.Instance.currentMode == EditorMode.Edit && !SelectionHandle.Instance.getDragging())
            {
                //Save the position where the mouse was pressed down
                if (Input.GetMouseButtonDown(0))
                {
                    dragStartPosition = Input.mousePosition;
                    mouseDown = true;
                }
                //Disable the drag select when the mouse is released
                else if (Input.GetMouseButtonUp(0))
                {
                    dragSelectBox.SetActive(false);
                    mouseDown = false;
                    dragging = false;
                    StartCoroutine(InvokeOnDragEndEvent());
                }
                //Update the drag select box while the mouse is held down
                else if (Input.GetMouseButton(0))
                {
                    mousePosition = Input.mousePosition;

                    //If the drag select box wasn't enabled yet and the cursor is outside of the dead zone, enable the drag select box
                    if (mouseDown && !dragging && (mousePosition - dragStartPosition).magnitude > deadzone)
                    {
                        dragSelectBox.SetActive(true);
                        dragging = true;
                        OnDragStart?.Invoke();
                    }

                    //If the drag select box is enabled, update the box and check for selected objects
                    if (dragging)
                    {
                        updateDragRect();
                        updateSelection();
                    }
                }
            }
        }

        //Update the position and size of the drag selection box
        private void updateDragRect()
        {
            //Calculate the dimensions of the drag select box
            float width = Mathf.Abs(mousePosition.x - dragStartPosition.x);
            float height = Mathf.Abs(mousePosition.y - dragStartPosition.y);

            //Divide the dimensions of the box by the scale factor of the canvas to correct the width and height
            dragBoxRect.sizeDelta = new Vector2(width, height) / canvasComponent.scaleFactor;
            dragBoxRect.position = (mousePosition + dragStartPosition) / 2f;
        }

        //Check for any objects that were deselected or selected
        private void updateSelection()
        {
            //The bounds of the drag selection box
            float minX, maxX, minY, maxY;

            //Find horizontal bounds
            if(dragStartPosition.x < mousePosition.x)
            {
                minX = dragStartPosition.x;
                maxX = mousePosition.x;
            }
            else
            {
                minX = mousePosition.x;
                maxX = dragStartPosition.x;
            }

            //Find vertical bounds
            if (dragStartPosition.y < mousePosition.y)
            {
                minY = dragStartPosition.y;
                maxY = mousePosition.y;
            }
            else
            {
                minY = mousePosition.y;
                maxY = dragStartPosition.y;
            }

            //Iterate through all onscreen objects
            foreach (KeyValuePair<GameObject, Tuple<Vector2, Vector2>> objectEntry in boundingBoxTable)
            {
                //Get the top left and bottom right of the object's bounding box
                Vector2 topLeft = objectEntry.Value.Item1;
                Vector2 bottomRight = objectEntry.Value.Item2;

                //If the bounding box of the object is inside the drag select box, select the object
                if (topLeft.x > minX && topLeft.y < maxY && bottomRight.x < maxX && bottomRight.y > minY)
                    ObjectSelection.Instance.selectObject(objectEntry.Key);
                //Otherwise deselect it
                else
                    ObjectSelection.Instance.deselectObject(objectEntry.Key);
            }
        }
        #endregion

        #region Bounding Box Methods
        //Store the screen space bounding box of the given map objects
        private void saveBoundingBoxes(ref HashSet<GameObject> mapObjects)
        {
            //Iterate through the selectable objects
            foreach (GameObject mapObject in mapObjects)
                saveBoundingBox(mapObject);
        }

        //Save the screen space bounding of the given map object
        private void saveBoundingBox(GameObject mapObject)
        {
            bool isVisible = false;

            //Loop through all renderers attatched to the object and check if they are visible
            foreach (Renderer renderer in mapObject.GetComponentsInChildren<Renderer>())
            {
                if (renderer.isVisible)
                {
                    isVisible = true;
                    break;
                }
            }

            //Check if the object is visible to the camera
            if (isVisible)
            {
                //Get the verticies of the object in screen space
                Tuple<Vector2, Vector2> boundingBox = get2DBoundingBox(mapObject);

                //If the bounding box is offscreen, skip the object
                if (boundingBox == null)
                    return;

                //If the table contains this item, update the value
                if (boundingBoxTable.ContainsKey(mapObject))
                    boundingBoxTable[mapObject] = boundingBox;
                //Otherwise create a new entry
                else
                    boundingBoxTable.Add(mapObject, boundingBox);
            }
        }

        //Removes the bounding boxes for the given list of map objects 
        private void removeBoundingBoxes(ref HashSet<GameObject> deletedObjects)
        {
            //Remove all of the bounding boxes for the objects that were deleted
            foreach (GameObject mapObject in deletedObjects)
                if (boundingBoxTable.ContainsKey(mapObject))
                    boundingBoxTable.Remove(mapObject);
        }

        //Return a list of screen space verticies for the meshes of the given game object
        private Tuple<Vector2, Vector2> get2DBoundingBox(GameObject mapObject)
        {
            Matrix4x4 localToScreenMatrix = calculateLocalToScreenMatrix(mapObject);

            //Calculate the screen space dimensions
            float scaledWidth = Screen.width / canvasComponent.scaleFactor;
            float scaledHeight = Screen.height / canvasComponent.scaleFactor;

            //Used to find the bounding box of the object
            bool firstVertex = true;
            float minX = 0;
            float maxX = 0;
            float minY = 0;
            float maxY = 0;

            //Iterate through all meshes in the map object and its children
            foreach (MeshFilter meshFilter in mapObject.GetComponentsInChildren<MeshFilter>())
            {
                //Convert the verticies of the mesh to screen space and store the position in a list
                foreach (Vector3 localVertex in meshFilter.mesh.vertices)
                {
                    //Get the vertex in screen space
                    Vector2 screenVertex = localToScreenMatrix.MultiplyPoint(localVertex);

                    //If any of the verticies is offscreen, return null
                    if (screenVertex.x < 0 || screenVertex.x > scaledWidth ||
                        screenVertex.y < 0 || screenVertex.y > scaledHeight)
                        return null;

                    //If this is the first vertex, use it to initialize the bounds
                    if (firstVertex)
                    {
                        firstVertex = false;
                        minX = screenVertex.x;
                        maxX = screenVertex.x;
                        minY = screenVertex.y;
                        maxY = screenVertex.y;
                    }
                    //Check if this vertex is a new minimum or maximum
                    else
                    {
                        if (screenVertex.x < minX)
                            minX = screenVertex.x;
                        else if (screenVertex.x > maxX)
                            maxX = screenVertex.x;

                        if (screenVertex.y < minY)
                            minY = screenVertex.y;
                        else if (screenVertex.y > maxY)
                            maxY = screenVertex.y;
                    }
                }
            }

            //Return the list of screen space verticies
            return new Tuple<Vector2, Vector2>(new Vector2(minX, maxY), new Vector2(maxX, minY));
        }

        //Create a transformation from the local space of the given game object to screen space
        private Matrix4x4 calculateLocalToScreenMatrix(GameObject mapObject)
        {
            //Get the local space to world space matrix
            Matrix4x4 localToWorldMatrix = mapObject.transform.localToWorldMatrix;

            //Used to remap points from the viewport to the screen
            float screenWidth = Screen.width * 0.5f;
            float screenHeight = Screen.height * 0.5f;

            //Calcualte the matrix that converts points in the view port to screen space
            Matrix4x4 viewToScreenMatrix = Matrix4x4.TRS(new Vector3(screenWidth, screenHeight, 0f), Quaternion.identity, new Vector3(screenWidth, screenHeight, 1f));
            
            //Calculate and return the matrix that converts tocal points to screen space
            return viewToScreenMatrix * projectionMatrix * worldToViewMatrix * localToWorldMatrix;
        }
        #endregion

        #region Public Methods
        private void clearObjectVerticies()
        {
            boundingBoxTable = new Dictionary<GameObject, Tuple<Vector2, Vector2>>();
        }

        public bool getDragging()
        {
            return dragging;
        }
        #endregion
    }
}