using UnityEngine;
using System.Collections;

public class ObjectSelection : MonoBehaviour
{
    //A reference to the main object so the EditorManager is accessable
    public GameObject mainObject;
    //A reference to the editorManager
    private EditorManager editorManager;
    //The shader that adds an outline to an object
    public Shader outlineShader;
    //The defailt shader
    public Shader defaultShader;

    //Get the EditorManager from the main object
    void Start()
    {
        editorManager = mainObject.GetComponent<EditorManager>();
    }

    //If the editor is in edit mode, check for selections
    void Update()
    {
        if (editorManager.editMode == "edit")
            checkSelect();
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

                //Either select or deselect the object depending on if it is currently selected
                if (!editorManager.isSelected(hitObject))
                {
                    applyOutline(hitObject);
                    editorManager.selectObject(hitObject);
                }
                else
                {
                    removeOutline(hitObject);
                    editorManager.deselectObject(hitObject);
                }
            }

        }
    }

    //Add a green outline around a GameObject
    private void applyOutline(GameObject objectToOutline)
    {
        //Iterate through all of the children in the GameObject
        foreach (MeshRenderer renderer in objectToOutline.GetComponentsInChildren<MeshRenderer>())
        {
            //Apply the outline shader and color it green
            renderer.material.shader = outlineShader;
            renderer.sharedMaterial.SetColor("_OutlineColor", Color.green);
        }
    }

    private void removeOutline(GameObject objectToRemove)
    {
        foreach (MeshRenderer renderer in objectToRemove.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material.shader = defaultShader;
        }
    }
}
