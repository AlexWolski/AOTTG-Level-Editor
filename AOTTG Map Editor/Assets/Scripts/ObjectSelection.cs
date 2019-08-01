using UnityEngine;
using System.Collections;

public class ObjectSelection : MonoBehaviour
{
    //A reference to the main object so the EditorManager is accessable
    public GameObject mainObject;
    //A reference to the editorManager
    private EditorManager editorManager;

    //Get the EditorManager from the main object
    void Start()
    {
        editorManager = mainObject.GetComponent<EditorManager>();
    }

    //
    void Update()
    {
        checkSelect();
    }

    //Check if an object was clicked
    private void checkSelect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //If an object was clicked on, either select or deselect it based on if it is currently selected
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                if (!editorManager.isSelected(hit.transform.gameObject))
                {
                    editorManager.selectObject(hit.transform.gameObject);
                }
                else
                {
                    editorManager.deselectObject(hit.transform.gameObject);
                }
            }

        }
    }
}
