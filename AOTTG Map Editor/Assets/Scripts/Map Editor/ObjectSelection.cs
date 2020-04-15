using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using OutlineEffect;

namespace MapEditor
{
    //A singleton class for managing the currently selected objects
    public class ObjectSelection : MonoBehaviour
    {
        #region Data Members
        //A self-reference to the singleton instance of this script
        public static ObjectSelection Instance { get; private set; }

        //A list containing the objects that can be selected
        private List<GameObject> selectableObjects = new List<GameObject>();
        //A list containing the objects currently selected
        private List<GameObject> selectedObjects = new List<GameObject>();
        //The average point of all the selected objects
        private Vector3 selectionAverage;
        //The sum of the points of all the selected objects for calculating the average
        private Vector3 positionSum;

        //Determines if objects can be selected by clicking on them
        //False when the tool handle is being dragged or the drag selection box is active
        private bool canSelect = false;
        #endregion

        #region Instantiation
        void Awake()
        {
            //Set this script as the only instance of the ObjectSelection script
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            //Add listners to events in the SelectionHandle class
            SelectionHandle.Instance.OnHandleMove += editSelection;
            SelectionHandle.Instance.OnHandleBegin += disableSelection;
            SelectionHandle.Instance.OnHandleFinish += enableSelection;

            //Add listners to events in the DragSelect class
            DragSelect.Instance.OnDragStart += disableSelection;
            DragSelect.Instance.OnDragEnd += enableSelection;
        }
        #endregion

        #region Drag Event Listners
        private void disableSelection()
        {
            canSelect = true;
        }

        private void enableSelection()
        {
            canSelect = false;
        }
        #endregion

        #region Update Selection Methods
        private void Update()
        {
            //Check for an object selection if in edit mode and nothing is being dragged
            if (EditorManager.Instance.currentMode == EditorMode.Edit && !canSelect)
                checkSelect();
        }

        //Test if any objects were clicked
        private void checkSelect()
        {
            //If the 'A' key was pressed, either select or deselect all based on if anything is currently selected
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (selectedObjects.Count > 0)
                    deselectAll();
                else
                    selectAll();
            }
            //If the mouse was clicked and the cursor is not over the UI, check if any objects were selected
            else if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject(-1))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                //If an object was clicked, select it
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Selectable")))
                {
                    //Select the parent of the object
                    GameObject parentObject = getParent(hit.transform.gameObject);

                    //Only select the object if it is selectable
                    if (selectableObjects.Contains(parentObject))
                    {
                        //If left control is not held, deselect all objects and select the clicked object
                        if (!Input.GetKey(KeyCode.LeftControl))
                        {
                            deselectAll();

                            //Select the object that was clicked on
                            selectObject(parentObject);
                        }
                        //If left control is held, select or deselect the object based on if its currently selected
                        else
                        {
                            if (!selectedObjects.Contains(parentObject))
                                selectObject(parentObject);
                            else
                                deselectObject(parentObject);
                        }
                    }
                }
                //If no objects were clicked and left control is not held, deselect all objects and select
                else
                {
                    if (!Input.GetKey(KeyCode.LeftControl))
                        deselectAll();
                }
            }
        }

        //Update the position, rotation, or scale of the object selections based on the tool handle
        private void editSelection()
        {
            //Determine which tool was used and call the respective transform
            switch (SelectionHandle.Instance.currentTool)
            {
                case Tool.Translate:
                    //Get the position displacement and translate the selected objects
                    Vector3 posDisplacement = SelectionHandle.Instance.getPosDisplacement();
                    TransformTools.TranslateSelection(ref Instance.selectedObjects, posDisplacement);

                    //Update the selection average
                    positionSum += posDisplacement * selectedObjects.Count;
                    selectionAverage += posDisplacement;
                    break;

                case Tool.Rotate:
                    //Get the angle and axis and to rotate around
                    Vector3 rotationAxis;
                    float angle = SelectionHandle.Instance.getRotDisplacement(out rotationAxis);

                    //Rotate the selected objects around the seleciton average
                    TransformTools.RotateSelection(ref Instance.selectedObjects, selectionAverage, rotationAxis, angle);
                    break;

                case Tool.Scale:
                    //Get the scale displacement and scale the selected objects
                    Vector3 scaleDisplacement = SelectionHandle.Instance.getScaleDisplacement();
                    TransformTools.ScaleSelection(ref Instance.selectedObjects, selectionAverage, scaleDisplacement, false);
                    break;
            }
        }
        #endregion

        #region Selection Average Methods
        //Add a point to the total average
        private void addAveragePoint(Vector3 point)
        {
            //Add the point to the total and update the average
            positionSum += point;
            selectionAverage = positionSum / selectedObjects.Count;
            SelectionHandle.Instance.setPosition(selectionAverage);

            //If the tool handle is not active, activate it
            SelectionHandle.Instance.show();
        }

        //Add all selected objects to the total average
        private void addAverageAll()
        {
            //Reset the total
            positionSum = Vector3.zero;

            //Count up the total of all the objects
            foreach (GameObject mapObject in selectedObjects)
                positionSum += mapObject.transform.position;

            //Average the points
            selectionAverage = positionSum / selectableObjects.Count;
            SelectionHandle.Instance.setPosition(selectionAverage);

            //If the tool handle is not active, activate it
            SelectionHandle.Instance.show();
        }

        //Remove a point from the total average
        private void removeAveragePoint(Vector3 point)
        {
            //Subtract the point to the total and update the average
            positionSum -= point;

            //If there are any objects selected, update the handle position
            if (selectedObjects.Count != 0)
            {
                selectionAverage = positionSum / selectedObjects.Count;
                SelectionHandle.Instance.setPosition(selectionAverage);
            }
            //Otherwise, disable the tool handle
            else
                SelectionHandle.Instance.hide();
        }

        //Remove all selected objects from the total average
        private void removeAverageAll()
        {
            //Reset the total and average
            positionSum = Vector3.zero;
            selectionAverage = Vector3.zero;

            //Hide the tool handle
            SelectionHandle.Instance.hide();
        }

        //Return a reference to the selection average
        public ref Vector3 getSelectionAverage()
        {
            return ref Instance.selectionAverage;
        }
        #endregion

        #region Select Objects Methods
        //Return the parent of the given object. If there is no parent, return the given object
        private GameObject getParent(GameObject childObject)
        {
            //The parent object that gets returned
            GameObject parentObject = childObject;
            //The tag of the parent object
            string parentTag = childObject.transform.parent.gameObject.tag;

            //Keep going up the hierarchy until the parent is a map or group
            while (parentTag != "Map" && parentTag != "Group")
            {
                //Move up the hierarchy
                parentObject = parentObject.transform.parent.gameObject;
                //Update the parent tag
                parentTag = parentObject.transform.parent.gameObject.tag;
            }

            return parentObject;
        }

        //Add the given object to the selectable objects list
        public void addSelectable(GameObject objectToAdd)
        {
            selectableObjects.Add(getParent(objectToAdd));
        }

        //Remove the given object from both the selectable and selected objects lists
        public void removeSelectable(GameObject objectToAdd)
        {
            //If the object is selected, deselect it
            if (selectedObjects.Contains(objectToAdd))
                deselectObject(objectToAdd);

            //Remove the object from the selectable objects list
            selectableObjects.Remove(getParent(objectToAdd));
        }

        public void selectObject(GameObject objectToSelect)
        {
            //Get the parent of the object
            GameObject parentObject = getParent(objectToSelect);

            //Select the object
            selectedObjects.Add(parentObject);
            addOutline(parentObject);

            //Update the position of the tool handle
            addAveragePoint(parentObject.transform.position);
            //Reset the rotation on the tool handle
            resetToolHandleRotation();
        }

        public void selectAll()
        {
            //Select all objects by copying the selectedObjects list
            selectedObjects = new List<GameObject>(selectableObjects);

            //Check if any objects are selected
            if (selectableObjects.Count != 0)
            {
                //Add the outline to all of the objects
                foreach (GameObject selectedObject in selectedObjects)
                    addOutline(selectedObject);

                //Update the tool handle position
                addAverageAll();
                //Reset the rotation on the tool handle
                resetToolHandleRotation();
            }
        }

        public void deselectObject(GameObject objectToDeselect)
        {
            //Get the parent of the object
            GameObject parentObject = getParent(objectToDeselect);

            //Deselect the object
            selectedObjects.Remove(parentObject);
            removeOutline(parentObject);

            //Update the position of the tool handle
            removeAveragePoint(parentObject.transform.position);
            //Reset the rotation on the tool handle
            resetToolHandleRotation();
        }

        public void deselectAll()
        {
            //If there are no selected objects, return from the function
            if (selectedObjects.Count == 0)
                return;

            //Remove the outline on all selected objects
            foreach (GameObject selectedObject in selectedObjects)
                removeOutline(selectedObject);

            //Deselect all objects by deleting the selected objects list
            selectedObjects = new List<GameObject>();

            //Update the position of the tool handle
            removeAverageAll();
            //Reset the rotation on the tool handle
            resetToolHandleRotation();
        }

        //Resets both the selected and selectable object lists
        public void resetSelection()
        {
            selectedObjects = new List<GameObject>();
            selectableObjects = new List<GameObject>();
            removeAverageAll();
        }

        //Remove any selected objects from both the selected and selectable objects lists
        //Returns a the selected objects list. Caller is expected to reset it after use
        public ref List<GameObject> removeSelected()
        {
            //If all of the objects are selected, reset just the selectable objects list
            if (selectedObjects.Count == selectableObjects.Count)
                selectableObjects = new List<GameObject>();
            //If a subset of objects are selected, remove just the selected objects from the selectable list
            else
            {
                //Remove all of the selected objects from the selectable list
                foreach (GameObject mapObject in selectedObjects)
                    selectableObjects.Remove(mapObject);
            }

            //Reset the selection average
            removeAverageAll();

            //Return a reference to the selected objects list
            return ref Instance.selectedObjects;
        }

        //Return a reference to the seleceted objects
        public ref List<GameObject> getSelection()
        {
            return ref Instance.selectedObjects;
        }
        #endregion

        #region Tool Methods
        //Set the type of the tool handle
        public void setTool(Tool toolType)
        {
            SelectionHandle.Instance.setTool(toolType);
            resetToolHandleRotation();
        }

        //Set the rotation of the tool handle based on how many objects are selected
        public void resetToolHandleRotation()
        {
            //If the tool handle is in rotate mode and only one object is selected, use the rotation of that object
            if ((SelectionHandle.Instance.currentTool == Tool.Rotate || SelectionHandle.Instance.currentTool == Tool.Scale) && selectedObjects.Count == 1)
                SelectionHandle.Instance.setRotation(selectedObjects[0].transform.rotation);
            //Otherwise reset the rotation
            else
                SelectionHandle.Instance.setRotation(Quaternion.identity);
        }
        #endregion

        #region Outline Methods
        //Add a green outline around a GameObject
        private void addOutline(GameObject objectToAddOutline)
        {
            //Get the outline script of the parent object
            Outline outlineScript = objectToAddOutline.GetComponent<Outline>();

            //If parent has an outline script, enable it
            if (outlineScript != null)
                outlineScript.enabled = true;

            //Go through the children of the object and enable the outline if it is a selectable object
            foreach (Transform child in objectToAddOutline.transform)
                if (child.gameObject.tag == "Selectable Object")
                    child.GetComponent<Outline>().enabled = true;
        }

        //Remove the green outline shader
        private void removeOutline(GameObject objectToRemoveOutline)
        {
            //Get the outline script of the parent object
            Outline outlineScript = objectToRemoveOutline.GetComponent<Outline>();

            //If parent has an outline script, disable it
            if (outlineScript != null)
                outlineScript.enabled = false;

            //Go through the children of the object and disable the outline if it is a selectable object
            foreach (Transform child in objectToRemoveOutline.transform)
                if (child.gameObject.tag == "Selectable Object")
                    child.GetComponent<Outline>().enabled = false;
        }
        #endregion
    }
}