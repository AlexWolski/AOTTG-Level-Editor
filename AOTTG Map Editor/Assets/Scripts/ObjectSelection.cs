using UnityEngine;
using System.Collections.Generic;

public class ObjectSelection : MonoBehaviour
{
    //A reference to the main object
    public GameObject mainObject;
    //The shader that adds an outline to an object
    public Shader outlineShader;
    //The defailt shader
    public Shader defaultShader;
    //A reference to the editorManager
    private EditorManager editorManager;
    //A list containing the objects currently selected
    private List<GameObject> selectedObjects = new List<GameObject>();

    //Get references from other scripts
    void Start()
    {
        editorManager = mainObject.GetComponent<EditorManager>();
    }

    //If the editor is in edit mode, check for selections
    void Update()
    {
        if (editorManager.currentMode == "edit")
            checkSelect();
    }

    //Return a reference to the seleceted objects
    public List<GameObject> getSelectedObjects()
    {
        return selectedObjects;
    }

    //Select of deselect objects clicked on
    private void checkSelect()
    {
        //Check if the mouse was clicked
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //Test if any objects were clicked on
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                //The object that was clicked on
                GameObject hitObject = hit.transform.gameObject;

                //Select the parent object instead of a child
                while (hitObject.transform.parent != null)
                    hitObject = hitObject.transform.parent.gameObject;

                //If left control is not held, deselect all objects and select the clicked object
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    //Remove the outline on all selected objects
                    foreach (GameObject selectedObject in selectedObjects)
                        removeOutline(selectedObject);

                    //Deselect all objects by deleting the selected objects list
                    selectedObjects = new List<GameObject>();

                    //Select the object that was clicked on
                    selectObject(hitObject);
                }
                //If left control is held, select or deselect the object based on if its currently selected
                else
                {
                    if (!selectedObjects.Contains(hitObject))
                        selectObject(hitObject);
                    else
                        deselectObject(hitObject);
                }
            }

        }
    }

    //Add an object to the list of selected objects
    public void selectObject(GameObject objectToAdd)
    {
        selectedObjects.Add(objectToAdd);
        applyOutline(objectToAdd);
    }

    //Remove an object to the list of selected objects
    public void deselectObject(GameObject objectToRemove)
    {
        selectedObjects.Remove(objectToRemove);
        removeOutline(objectToRemove);
    }

    //Add a green outline around a GameObject
    private void applyOutline(GameObject objectToOutline)
    {
        //Iterate through all of the children in the GameObject and apply a green outline
        foreach (MeshRenderer renderer in objectToOutline.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material.shader = outlineShader;
            renderer.sharedMaterial.SetColor("_OutlineColor", Color.green);
        }
    }

    //Remove the green outline shader
    private void removeOutline(GameObject objectToRemoveOutline)
    {
        foreach (MeshRenderer renderer in objectToRemoveOutline.GetComponentsInChildren<MeshRenderer>())
            renderer.material.shader = defaultShader;
    }
}
