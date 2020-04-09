using UnityEngine;

namespace MapEditor
{
    public class CameraController : MonoBehaviour
    {
        #region Data Members
        //The default speed the camrea moves at
        [SerializeField] private float defaultMovementSpeed = 100f;
        //The factor with which the movement speeds up or slows down when a speed modifier key is held
        [SerializeField] private float speedMultiplier = 3f;
        //Stores the three different speeds the camera can move at
        private AdjustableSpeed movementSpeed;

        //The speed the camera rotates at
        [SerializeField] private float rotateSpeed = 100f;
        #endregion

        #region Instnatiation
        //Disable the fog on distant objects
        void OnPreRender()
        {
            RenderSettings.fog = false;
        }

        //Instantiate the adjustable speed class
        private void Awake()
        {
            movementSpeed = new AdjustableSpeed(defaultMovementSpeed, speedMultiplier);
        }
        #endregion

        //If the editor is in fly mode, translate and rotate the camera
        private void LateUpdate()
        {
            if (EditorManager.Instance.currentMode == EditorMode.Fly)
            {
                translateCamera();
                rotateCamera();
            }
        }

        private void translateCamera()
        {
            //Get the amount to translate on the x and z axis
            float xDisplacement = Input.GetAxisRaw("Horizontal") * movementSpeed.getSpeed() * Time.deltaTime;
            float zDisplacement = Input.GetAxisRaw("Vertical") * movementSpeed.getSpeed() * Time.deltaTime;

            //Get the amount to translate on the y axis
            float yDisplacement = 0;

            //If only the left mouse button is pressed, move the camera down
            if (Input.GetButton("Fire1") && !Input.GetButton("Fire2"))
                yDisplacement = -movementSpeed.getSpeed() * Time.deltaTime;
            //If only the right mouse button is pressed, move the camera up
            else if (Input.GetButton("Fire2") && !Input.GetButton("Fire1"))
                yDisplacement = movementSpeed.getSpeed() * Time.deltaTime;

            //Translate the camera on the x and z axes in self space
            transform.Translate(xDisplacement, 0, zDisplacement, Space.Self);
            //Translate the camera on the y axis in world space
            transform.Translate(0, yDisplacement, 0, Space.World);
        }

        private void rotateCamera()
        {
            //Find how much the camera should be rotated on the x and y axes, then add the current rotations to them
            float xRotation = (Input.GetAxis("Mouse Y") * -rotateSpeed * Time.deltaTime) + transform.rotation.eulerAngles.x;
            float yRotation = (Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime) + transform.rotation.eulerAngles.y;

            //Restrict the camera angle so it doesn't flip
            if (xRotation > 90 && xRotation < 180)
                xRotation = 90;
            if (xRotation < 270 && xRotation > 180)
                xRotation = 270;

            //Set the new rotation of the camera
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        }
    }
}