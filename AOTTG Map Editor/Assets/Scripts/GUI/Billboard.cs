using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour
{
    //The child 3D text object of this object
    private TextMesh text3D;
    //How high above the object the 3D text will sit
    private const float verticalOffset = 0.25f;
    //The main camera in the scene to face
    private Camera mainCamera;

    void Awake()
    {
        //Get the TextMesh component from the billboard object
        text3D = gameObject.GetComponent<TextMesh>();
        //Get the main camera in the scene
        mainCamera = Camera.main;
    }

    //Update the billboard to follow the parent object and face the camera
    private void LateUpdate()
    {
        //Position the billboard above its parent if it has one
        if (gameObject.transform.parent != null)
        {
            float height = (gameObject.transform.parent.localScale.y / 20f) + verticalOffset;
            text3D.transform.localPosition = new Vector3(0, height, 0);
        }

        //Rotate the billboard to face the camera
        transform.LookAt(mainCamera.transform.position);
    }
}
