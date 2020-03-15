using UnityEngine;

public class EditorManager : MonoBehaviour
{
    //Determines if the user is in fly more or edit mode. Default mode is edit
    public EditorMode currentMode;

    void Awake()
    {
        Screen.fullScreen = false;
        Screen.SetResolution(800, 600, false);
    }

    //Load the assets from RC mod and set the window settings
    [System.Obsolete]
    void Start()
    {
        StartCoroutine(AssetManager.LoadRCAssets());
    }

    void Update()
    {
        //If the x key is pressed and the tool handle is not being dragged,
        //toggle between edit and fly mode
        if (Input.GetKeyDown(KeyCode.X) && !CommonReferences.selectionHandle.InUse())
        {
            if(currentMode == EditorMode.Fly)
            {
                currentMode = EditorMode.Edit;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if(currentMode == EditorMode.Edit)
            {
                currentMode = EditorMode.Fly;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void LateUpdate()
    {
        CommonReferences.selectionHandle.lateUpdate();
        CommonReferences.objectSelection.lateUpdate();
    }
}