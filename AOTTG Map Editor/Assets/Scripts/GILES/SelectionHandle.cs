using UnityEngine;
using UnityEngine.EventSystems;
//Used for manipulating the cursor position (Windows)
using System.Runtime.InteropServices;
using System.Drawing;

namespace GILES
{
    public class SelectionHandle : MonoBehaviour
    {
        #region Data Members
        //A self-reference to the singleton instance of this script
        public static SelectionHandle Instance { get; private set; }

        private static Transform _trs;
        private static Transform trs { get { if (_trs == null) _trs = Instance.gameObject.GetComponent<Transform>(); return _trs; } }
        private static Camera _cam;
        private static Camera cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }

        private static Mesh _HandleLineMesh = null, _HandleTriangleMesh = null;

        //Used as translation handle cone caps.
        [SerializeField]
        private Mesh ConeMesh;
        // Used for scale handle
        [SerializeField]
        private Mesh CubeMesh;

        //Materials used for the tool handle gizmo
        [SerializeField]
        private Material HandleOpaqueMaterial;
        [SerializeField]
        private Material RotateLineMaterial;
        [SerializeField]
        private Material HandleTransparentMaterial;

        //The width around the tool handle that can be interacted with
        [SerializeField]
        const int handleInteractWidth = 10;
        //The padding around the edges of the window past which the mouse will be moved back into the window
        [SerializeField]
        const int windowPadding = 5;

        private static Mesh HandleLineMesh
        {
            get
            {
                if (_HandleLineMesh == null)
                {
                    _HandleLineMesh = new Mesh();
                    CreateHandleLineMesh(ref _HandleLineMesh, Vector3.one);
                }
                return _HandleLineMesh;
            }
        }

        private static Mesh HandleTriangleMesh
        {
            get
            {
                if (_HandleTriangleMesh == null)
                {
                    _HandleTriangleMesh = new Mesh();
                    CreateHandleTriangleMesh(ref _HandleTriangleMesh, Vector3.one);
                }
                return _HandleTriangleMesh;
            }
        }

        //The current tool. Default is translate tool
        public static Tool tool { get; private set; } = Tool.Translate;

        //Save the handle displacements for when they need to be returned
        private static Vector3 prevPosition;
        private static float rotationDisplacement;
        private static Vector3 prevScale;
        private static Vector3 scale;
        private static float prevCursorDist;
        private static float currCursorDist;

        //The current position of the mouse. Used instead of Input.mousePosition because it can be modified
        private static Vector2 currentMousePosition;
        //Used to keep track of the displacement of the mouse between frames
        private static Vector2 prevMousePosition = Vector2.zero;
        //The distance the mouse moved between the previous and the current frame
        private static Vector2 mouseDisplacement = Vector2.zero;
        //The amount to offset the onscreen cursor to get the hypothetical unconstrained position. Used in the plane drag and scale all tools
        private static Vector2 mouseOffest = Vector2.zero;

        ///Persistient variables used by the rotation tool
        //The angle displacement of the rotation handle since the drag started
        private static float axisAngle = 0f;
        //Determines if the latest rotation was positive or negative
        private static float sign;
        //The vector in screenspace representing the tangent line of the rotation handle that was clicked
        private static Vector2 clickTangent;

        ///Persistient variables used by the translation tool
        public static float cameraDist;
        public const float CAP_SIZE = .07f;

        [SerializeField]
        private float HandleSize = 90f;
        //Adjusts the speed of handle movement when rotating or translating 
        [SerializeField]
        private float rotationSpeed = 3f;
        [SerializeField]
        private float translationSpeed = 1f;
        [SerializeField]
        private float scaleSpeed = 1f;

        //The maximum distance away from the origin an object can be
        [SerializeField]
        private static float maxDistance = 1000000000f;

        //Determines if the handle is being interacted with or not
        private static bool draggingHandle;
        //In how many directions is the handle able to move
        private static int draggingAxes = 0;
        private static TransformData handleOrigin = TransformData.identity;

        //Determines if the handle should be displayed and interactable
        private static bool hidden = false;
        public static bool InUse() { return draggingHandle; }

        //The octant of the camera relative ot the tool handle
        public static Vector3 viewOctant { get; private set; }
        //The octant of the camera in the previous frame
        private static Vector3 previousOctant;

        //Get the octant to display the planes in based on camera position and tool dragging status
        private static Vector3 getViewOctant()
        {
            //If the tool is not being dragged, use the current octant
            if (!draggingHandle)
                return HandleUtility.getViewOctant(trs.position, cam.transform.position);

            //If it is being dragged, use the octant the camera was in before the drag
            return previousOctant;
        }

        [SerializeField]
        private const float HANDLE_BOX_SIZE = .25f;

        #endregion

        #region Initialization

        protected void Awake()
        {
            //Set this script as the only instance of the SelectionHandle script
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            //Hide the hanlde by default
            hidden = true;
            _trs = null;
            _cam = null;
        }

        public static void hide()
        {
            hidden = true;
            draggingHandle = false;
        }

        public static void show()
        {
            hidden = false;
        }

        public static void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            trs.position = position;
            trs.rotation = rotation;
            trs.localScale = scale;

            RebuildGizmoMatrix();
        }
        #endregion

        #region Delegate

        public delegate void OnHandleMoveEvent();
        public static event OnHandleMoveEvent OnHandleMove;

        public delegate void OnHandleBeginEvent();
        public static event OnHandleBeginEvent OnHandleBegin;

        public delegate void OnHandleFinishEvent();
        public static event OnHandleFinishEvent OnHandleFinish;

        private static void OnCameraMove()
        {
            RebuildGizmoMesh(Vector3.one);
            RebuildGizmoMatrix();
        }
        #endregion

        #region Imported Functions
        //Import external windows functions for gettting and setting the cursor position
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pos);
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);
        #endregion

        #region Drag Orientation Class
        private class DragOrientation
        {
            public Vector3 origin;
            //The primary axis the handle is being dragged along in local coordiantes (x, y, or z)
            public Vector3 localAxis;
            //The arbitrary axis the handle is being dragged along in world coordinates
            public Vector3 worldAxis;
            public Vector3 offset;
            public Plane plane;

            public DragOrientation()
            {
                origin = Vector3.zero;
                worldAxis = Vector3.zero;
                offset = Vector3.zero;
                plane = new Plane(Vector3.up, Vector3.zero);
            }
        }

        private static DragOrientation drag = new DragOrientation();
        #endregion Drag Class

        #region Update
        public void lateUpdate()
        {
            //Don't display or interact with the handle if it is hidden
            if (!hidden)
            {
                //Update the octant the camera is in relative to the tool handle
                previousOctant = viewOctant;
                viewOctant = getViewOctant();

                //Rebuild the gizmo meshes and matricies when the camera moves
                OnCameraMove();

                //Don't check for handle interactions if the handle is hidden or the editor is not in edit mode
                if (EditorManager.currentMode != EditorMode.Edit)
                    return;

                //Save the current mouse position
                currentMousePosition = Input.mousePosition;
                //Calculate the mouse displacement
                mouseDisplacement = currentMousePosition - prevMousePosition;
                //Save the posision of the mouse for the next frame
                prevMousePosition = currentMousePosition;

                //While the tool handle is being dragged, make sure the mouse stays within the window bounds
                if (InUse() && !Input.GetMouseButtonUp(0))
                    constrainMouse();
                //If the mouse is pressed, check if the handle was clicked
                if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl))
                    checkInteract();
                //If the mouse is released, finish interacting with the handle
                if (Input.GetMouseButtonUp(0))
                    OnFinishHandleMovement();
                //If the mouse is pressed and dragging the handle, interact with the handle
                else if (draggingHandle && Input.GetMouseButton(0))
                    interactHandle();
            }
        }

        //If the mouse moves too close to the edges of the window, move it to the opposite side
        private void constrainMouse()
        {
            //Get the position of the cursor relative to the window
            Point mousePosition = new Point((int)currentMousePosition.x, (int)currentMousePosition.y);
            //The amount the cursor needs to be moved to keep it inside the window
            Vector2 positionOffset = Vector2.zero;

            //Check if the mouse is out of the window horizontally
            if (mousePosition.X < windowPadding)
                positionOffset.x = Screen.width - (2 * windowPadding) - positionOffset.x;
            else if (mousePosition.X > Screen.width - windowPadding)
                positionOffset.x = -Screen.width + (2 * windowPadding) + positionOffset.x;

            //Check if the mouse is out of window vertically
            if (mousePosition.Y < windowPadding)
                positionOffset.y = Screen.height - (2 * windowPadding) - positionOffset.y;
            else if (mousePosition.Y > Screen.height - windowPadding)
                positionOffset.y = -Screen.height + (2 * windowPadding) + positionOffset.y;

            //If the mouse needs to be moved, set its position and calculate the mouse displcement
            if (positionOffset.x != 0 || positionOffset.y != 0)
            {
                //The location of the cursor relative to the screen
                Point cursorLocation;

                //Get the the cursor location relative to the screen, add the offset, and set the new mouse position
                GetCursorPos(out cursorLocation);
                SetCursorPos(cursorLocation.X + (int)positionOffset.x, cursorLocation.Y - (int)positionOffset.y);

                //Add the offset to the mouse position so that the mouse displacement doesn't include the mouse repositioning
                currentMousePosition += positionOffset;
                prevMousePosition = currentMousePosition;

                //Store the total offset between the onscreen cursor and the hypothetical unconstrained cursor
                mouseOffest -= positionOffset;
            }
        }

        private void interactHandle()
        {
            //Set the starting point of the drag to the position of the handle
            drag.origin = trs.position;

            //Reset the persistient variables of each tool
            prevPosition = trs.position;
            rotationDisplacement = 0f;
            prevScale = scale;
            prevCursorDist = currCursorDist;

            //Only rotate the hanlde if the mouse was moved
            if (mouseDisplacement.magnitude > 0f)
            {
                switch (tool)
                {
                    case Tool.Translate:
                        //If the plane translate is selected, use the whole hit point as the position of the handle
                        if (draggingAxes > 1)
                        {
                            //Get the position under the cursor but on the movement plane
                            Vector3 planeHit = getMovementPlaneHit();

                            //If the position is not valid, don't move the tool handle
                            for (int axis = 0; axis < 3; axis++)
                                if (float.IsNaN(planeHit[axis]))
                                    return;

                            //If the point is valid, move the tool handle to the point under the cursor
                            trs.position = planeHit - drag.offset;
                        }
                        //If only one axis is selected, use the component of the mosue displacement parallel to the drag axis
                        else
                        {
                            //Get the displcement of the mouse in the handle's local space along the drag axis
                            Vector3 translationVector = getDragDisplacement(mouseDisplacement);
                            //Scale the translation vector by the translation speed and distance to camera
                            translationVector *= translationSpeed * cameraDist / 1000;

                            //Translate the tool handle
                            trs.Translate(translationVector, Space.Self);

                            //If any of the axes of the object went out of bounds, set it back to the maximum valid value
                            for(int axis = 0; axis < 3; axis++)
                            {
                                if(Mathf.Abs(trs.position[axis]) > maxDistance)
                                {
                                    //Get the sign of the current position
                                    float positionSign = Mathf.Sign(trs.position[axis]);

                                    //Set the position to back in bounds
                                    Vector3 fixedPosition = trs.position;
                                    fixedPosition[axis] = maxDistance * positionSign;
                                    trs.position = fixedPosition;
                                }
                            }
                        }
                        break;

                    case Tool.Rotate:
                        //Project the mouse displacement onto the tangent vector to get the component tangent to the rotation handle
                        Vector2 tangentDisplacement = projectBontoA(mouseDisplacement, clickTangent);
                        //Use the dot product between the tangent displacement and click tangent to get the sign of the rotation
                        sign = Vector2.Dot(tangentDisplacement, clickTangent) > 0 ? 1f : -1f;

                        //Use the magnitude of the displacement as the angle displacement
                        float angleDisplacement = tangentDisplacement.magnitude * sign;
                        //Add the displacement to the angle after scaling it by the rotation speed
                        rotationDisplacement = angleDisplacement / 10 * rotationSpeed;
                        axisAngle += rotationDisplacement;

                        //Rotate the tool handle
                        trs.rotation = Quaternion.AngleAxis(axisAngle, drag.worldAxis) * handleOrigin.rotation;

                        break;

                    //If the tool isn't translate or rotate, it has to be scale
                    default:
                        //Stores the scale factor of each axis
                        Vector3 scaleVector;

                        //If all axes are being dragged, scale based on the distance between the cursor and tool handle
                        if (draggingAxes > 1)
                        {
                            //Get the distance in screen space between the tool handle and the cursor
                            currCursorDist = getMouseHandleDist(currentMousePosition + mouseOffest);
                            //Calculate the displacement of the distance since last frame
                            float displacement = currCursorDist - prevCursorDist;
                            //Multiply the drag axis by the displacement
                            scaleVector = new Vector3(displacement, displacement, displacement);
                        }
                        //If only only axis is being dragged, only use the mouse displacement parallel to the axis being dragged
                        else
                            scaleVector = getDragDisplacement(mouseDisplacement);

                        //Scale the vector by the translation speed and distance to camera
                        scaleVector *= scaleSpeed / 100;

                        //Scale the axis of the scale vector by the scale displacement
                        for (int axis = 0; axis < 3; axis++)
                            if (scaleVector[axis] != 0)
                                scale[axis] += scaleVector[axis];

                        break;
                }
            }

            //If the current tool is the scale tool, rebuild the handle with the correct scale
            if(tool == Tool.Scale)
                RebuildGizmoMesh(scale);

            //Notify all listners that the handle was moved
            OnHandleMove?.Invoke();

            RebuildGizmoMatrix();
        }

        //Return the difference between the current handle position and the previous position
        public static Vector3 getPosDisplacement()
        {
            return trs.position - prevPosition;
        }

        //Return the angle the handle was rotated and the axis it was rotated around
        public static float getRotDisplacement(out Vector3 rotationAxis)
        {
            rotationAxis = drag.worldAxis;
            return rotationDisplacement;
        }

        //Calculte how much each axis was scaled since the last frame and return it
        public static Vector3 getScaleDisplacement()
        {
            Vector3 scaleDisplacement = new Vector3();

            for (int axis = 0; axis < 3; axis++)
                scaleDisplacement[axis] = scale[axis] / prevScale[axis];

            return scaleDisplacement;
        }

        //Public getters and setters for the variables of the handle transform component
        public static Vector3 getPosition() { return trs.position; }
        public static void setPosition(Vector3 newPosition) { trs.position = newPosition; }
        public static Quaternion getRotation() { return trs.rotation; }
        public static void setRotation(Quaternion newRotation) { trs.rotation = newRotation; }
        public static Vector3 getScale() { return trs.localScale; }
        public static void setScale(Vector3 newScale) { trs.localScale = newScale; }

        //Find the point the mouse is over on the plane the handle is moving along
        private static Vector3 getMovementPlaneHit()
        {
            //Create a ray originating from the camera and passing through the cursor
            Ray ray = cam.ScreenPointToRay(currentMousePosition + mouseOffest);

            //Find the position the cursor over on the corresponding plane
            if (drag.plane.Raycast(ray, out float distToHit))
                return ray.GetPoint(distToHit);

            //Otherwise return NAN to indicate a failure to get the point
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        private static Vector3 getClickVector()
        {
            //A plane representing the axis currently being dragged
            Plane movementPlane = new Plane();
            //The point where the ray intersected the plane
            Vector3 hitPoint;

            //Create a ray originating from the camera and passing through the cursor
            Ray ray = cam.ScreenPointToRay(currentMousePosition);
            //The distance from the camera to the hit point
            float distToHit;

            //Set the movemnet plane based on the axis being dragged
            movementPlane.SetNormalAndPosition(drag.worldAxis, trs.position);

            //Find the plane hit point
            if (movementPlane.Raycast(ray, out distToHit))
                hitPoint = ray.GetPoint(distToHit);
            //If the pland and ray don't intersect, return a zero vector
            else
                return Vector3.zero;

            //Return the position of hit point relative to the tool handle
            return hitPoint - trs.position;
        }

        //Find the vector orthogonal to the given vector but in the same plane (counter-clockwise)
        private static Vector3 getOrthInPlane(Vector3 originalVector)
        {
            return Vector3.Cross(drag.worldAxis, originalVector);
        }

        //Find the vector from the handle origin to where the handle was clicked
        private static Vector2 getClickTangent()
        {
            //Convert both the handle position to screen space
            Vector2 screenPosHandle = cam.WorldToScreenPoint(trs.position);
            //The 3D vector tangent to the rotation handle at the click point
            Vector3 tangentVector3;

            //Get the vector starting at the hanlde origin and ending at the camera
            Vector3 cameraVector = (cam.transform.position - trs.position).normalized;

            //If the dragging plane is nearly orthogonal to the camera, calculate the tangent vector using the drag plane normal
            //Temporary fix until parts of the rotation handle further from the camera aren't interactable
            if (Mathf.Abs(Vector3.Dot(cameraVector, drag.worldAxis)) <= 0.05f)
            {
                //Use the vector orthogonal to both the camera vector and movement plane normal as the tangent vector
                tangentVector3 = Vector3.Cross(drag.worldAxis, cameraVector);
            }
            //Otherwise calculate the tangent vector using the click vector
            else
            {
                //Get the 3D vector representing the point on the rotation handle that was clicked
                Vector3 clickVector3 = getClickVector();
                //Find the vector that is tangent to the rotation handle and in the movement plane
                tangentVector3 = getOrthInPlane(clickVector3);
            }

            //Calculate the position of the end of hte tangent vector in screen space
            Vector2 screenPosTangent = cam.WorldToScreenPoint(trs.position + tangentVector3);
            //Get the tangent vector in screen space by subtracting the screen tangent position by the screen handle position
            return screenPosTangent - screenPosHandle;
        }

        //Calculate the projection of one 2D vector onto another
        private static Vector2 projectBontoA(Vector2 B, Vector3 A)
        {
            //The scalar to multiply the onto vector by
            float scalar = Vector2.Dot(A, B) / Mathf.Pow(A.magnitude, 2);

            //Scale the onto vector to represent the projection of the target vector
            return scalar * A;
        }

        //Use the displacement of the mouse in screen space to calculate the corresponding displacement
        //in the local space of the tool handle along the drag axis
        private static Vector3 getDragDisplacement(Vector2 mouseDisplacement)
        {
            //Convert the position of the tool handle to screen space
            Vector2 screenHandlePos = cam.WorldToScreenPoint(trs.position);
            //Convert the drag axis to screen space
            Vector2 screenDragAxis = cam.WorldToScreenPoint(trs.position + drag.worldAxis);

            //Get the drag axis vector in screen space by subtracting the drag axis tail point from the head point
            Vector2 screenDragVector = screenDragAxis - screenHandlePos;
            //Get the component of the mouse displacement parallel to the drag axis
            Vector2 screenDisplacement = projectBontoA(mouseDisplacement, screenDragVector);
            //Use the dot product between the drag vector and the screen displacement to get the sign of the translation
            float displacementSign = Vector2.Dot(screenDragVector, screenDisplacement) > 0 ? 1f : -1f;

            //Multiply the drag axis by the displacement magnitude to get the vector to translate by and return the result
            return drag.localAxis * screenDisplacement.magnitude * displacementSign;
        }

        //Return the distance between the tool handle and the mouse in screen space
        private static float getMouseHandleDist(Vector2 mouseDisplacement)
        {
            //Convert the position of the tool handle to screen space
            Vector2 screenHandlePos = cam.WorldToScreenPoint(trs.position);

            //Return the magnitude of the difference between the mouse and the handle position
            return (mouseDisplacement - screenHandlePos).magnitude;
        }

        private static void checkInteract()
        {
            //Don't check for handle interactions if it is hidden or if the cursor is over the UI
            if (hidden || EventSystem.current.IsPointerOverGameObject(-1))
                return;

            //The axis or axes along which the tool handle is being dragged
            Axis plane;

            //Check if the tool handle was clicked
            draggingHandle = CheckHandleActivated(currentMousePosition, out plane);

            //If the tool handle wasn't clicked, don't start the interaction
            if (!draggingHandle)
                return;

            //Reset the axes being dragged
            drag.worldAxis = Vector3.zero;
            drag.localAxis = Vector3.zero;
            draggingAxes = 0;

            //Set the relevant variables based on the drag plane
            setDragData(plane);

            //Save the distance from the camera to the tool handle
            if (tool == Tool.Translate)
            {
                cameraDist = (cam.transform.position - trs.position).magnitude;
            }
            //Reset the total displacement and save the angle of the point clicked on
            if (tool == Tool.Rotate)
            {
                axisAngle = 0f;
                clickTangent = getClickTangent();
            }
            //Reset the handle size and prime it for scaling
            else 
            {
                prevScale = Vector3.one;
                scale = Vector3.one;
                currCursorDist = getMouseHandleDist(currentMousePosition);
            }

            //Notify all listners that the tool handle was activated
            OnHandleBegin?.Invoke();
        }

        //Sets the appropriate variables according to which axes are being dragged
        private static void setDragData(Axis plane)
        {
            Vector3 a, b;
            drag.offset = Vector3.zero;

            Ray ray = cam.ScreenPointToRay(currentMousePosition);

            if ((plane & Axis.X) == Axis.X)
            {
                draggingAxes++;
                drag.worldAxis = trs.right.normalized;
                drag.localAxis = Vector3.right;
                drag.plane.SetNormalAndPosition(trs.right.normalized, trs.position);
            }

            if ((plane & Axis.Y) == Axis.Y)
            {
                draggingAxes++;

                if (draggingAxes > 1)
                    drag.plane.SetNormalAndPosition(Vector3.Cross(drag.worldAxis, trs.up).normalized, trs.position);
                else
                    drag.plane.SetNormalAndPosition(trs.up.normalized, trs.position);

                drag.worldAxis += trs.up.normalized;
                drag.localAxis += Vector3.up;
            }

            if ((plane & Axis.Z) == Axis.Z)
            {
                draggingAxes++;
                if (draggingAxes > 1)
                    drag.plane.SetNormalAndPosition(Vector3.Cross(drag.worldAxis, trs.forward).normalized, trs.position);
                else
                    drag.plane.SetNormalAndPosition(trs.forward.normalized, trs.position);

                drag.worldAxis += trs.forward.normalized;
                drag.localAxis += Vector3.forward;
            }

            if (draggingAxes < 2)
            {
                if (HandleUtility.PointOnLine(new Ray(trs.position, drag.worldAxis), ray, out a, out b))
                    drag.offset = a - trs.position;
            }
            else
            {
                if (drag.plane.Raycast(ray, out float hit))
                    drag.offset = ray.GetPoint(hit) - trs.position;
            }
        }

        private static void OnFinishHandleMovement()
        {
            RebuildGizmoMesh(Vector3.one);
            RebuildGizmoMatrix();

            draggingHandle = false;
            //Reset the offset between the onscreen mouse position and its hypotheical unclamped position
            mouseOffest = Vector2.zero;

            //Notify all listners that the handle is no longer being interacted with
            OnHandleFinish?.Invoke();
        }

        #endregion

        #region Interface

        public static TransformData GetTransform()
        {
            return new TransformData(trs.position, trs.rotation, scale);
        }

        private static bool CheckHandleActivated(Vector2 mousePosition, out Axis plane)
        {
            plane = (Axis)0x0;

            if (tool == Tool.Translate || tool == Tool.Scale)
            {
                float sceneHandleSize = HandleUtility.GetHandleSize(trs.position);

                // cen
                Vector2 cen = cam.WorldToScreenPoint(trs.position);
                // up
                Vector2 up = cam.WorldToScreenPoint((trs.position + (trs.up + trs.up * CAP_SIZE * 4f) * (sceneHandleSize * Instance.HandleSize)));
                // right
                Vector2 right = cam.WorldToScreenPoint((trs.position + (trs.right + trs.right * CAP_SIZE * 4f) * (sceneHandleSize * Instance.HandleSize)));
                // forward
                Vector2 forward = cam.WorldToScreenPoint((trs.position + (trs.forward + trs.forward * CAP_SIZE * 4f) * (sceneHandleSize * Instance.HandleSize)));
                // First check if the plane boxes have been activated
                Vector2 p_right = (cen + ((right - cen) * viewOctant.x) * HANDLE_BOX_SIZE);
                Vector2 p_up = (cen + ((up - cen) * viewOctant.y) * HANDLE_BOX_SIZE);
                Vector2 p_forward = (cen + ((forward - cen) * viewOctant.z) * HANDLE_BOX_SIZE);

                //x plane
                if (HandleUtility.PointInPolygon(new Vector2[] { cen, p_up, p_up, (p_up+p_forward) - cen,
                                                                (p_up + p_forward) - cen, p_forward, p_forward, cen },
                                                                mousePosition))
                {
                    plane = Axis.Y | Axis.Z;
                }
                //y plane
                else if (HandleUtility.PointInPolygon(new Vector2[] { cen, p_right, p_right, (p_right+p_forward)-cen,
                                                                    (p_right + p_forward)-cen, p_forward, p_forward, cen },
                                                                    mousePosition))
                {
                    plane = Axis.X | Axis.Z;
                }
                //z plane
                else if (HandleUtility.PointInPolygon(new Vector2[] { cen, p_up, p_up, (p_up + p_right) - cen,
                                                                    (p_up + p_right) - cen, p_right, p_right, cen },
                                                                    mousePosition))
                {
                    plane = Axis.X | Axis.Y;
                }
                //x axis
                else if (HandleUtility.DistancePointLineSegment(mousePosition, cen, up) < handleInteractWidth)
                    plane = Axis.Y;
                //y axis
                else if (HandleUtility.DistancePointLineSegment(mousePosition, cen, right) < handleInteractWidth)
                    plane = Axis.X;
                //z axis
                else if (HandleUtility.DistancePointLineSegment(mousePosition, cen, forward) < handleInteractWidth)
                    plane = Axis.Z;
                else
                    return false;

                return true;
            }
            else
            {
                Vector3[][] vertices = HandleMesh.GetRotationVertices(16, 1f);

                float best = Mathf.Infinity;

                Vector2 cur, prev = Vector2.zero;
                plane = Axis.X;

                for (int i = 0; i < 3; i++)
                {
                    cur = cam.WorldToScreenPoint(vertices[i][0]);

                    for (int n = 0; n < vertices[i].Length - 1; n++)
                    {
                        prev = cur;
                        cur = cam.WorldToScreenPoint(handleMatrix.MultiplyPoint3x4(vertices[i][n + 1]));

                        float dist = HandleUtility.DistancePointLineSegment(mousePosition, prev, cur);

                        if (dist < best && dist < handleInteractWidth)
                        {
                            Vector3 viewDir = (handleMatrix.MultiplyPoint3x4((vertices[i][n] + vertices[i][n + 1]) * .5f) - cam.transform.position).normalized;
                            Vector3 nrm = trs.TransformDirection(vertices[i][n]).normalized;

                            if (Vector3.Dot(nrm, viewDir) > .5f)
                                continue;

                            best = dist;

                            switch (i)
                            {
                                case 0: // Y
                                    plane = Axis.Y; // Axis.X | Axis.Z;
                                    break;

                                case 1: // Z
                                    plane = Axis.Z;// Axis.X | Axis.Y;
                                    break;

                                case 2: // X
                                    plane = Axis.X;// Axis.Y | Axis.Z;
                                    break;
                            }
                        }
                    }
                }

                if (best < handleInteractWidth + .1f)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Render

        private static Matrix4x4 handleMatrix;

        void OnRenderObject()
        {
            //Don't render the handle if it is hidden or this is not the designated camera
            if (hidden || Camera.current != cam)
                return;

            switch (tool)
            {
                case Tool.Translate:
                case Tool.Scale:
                    HandleOpaqueMaterial.SetPass(0);
                    Graphics.DrawMeshNow(HandleLineMesh, handleMatrix);
                    Graphics.DrawMeshNow(HandleTriangleMesh, handleMatrix, 1);  // Cones

                    HandleTransparentMaterial.SetPass(0);
                    Graphics.DrawMeshNow(HandleTriangleMesh, handleMatrix, 0);  // Box
                    break;

                case Tool.Rotate:
                    RotateLineMaterial.SetPass(0);
                    Graphics.DrawMeshNow(HandleLineMesh, handleMatrix);
                    break;
            }
        }

        private static void RebuildGizmoMatrix()
        {
            float handleSize = HandleUtility.GetHandleSize(trs.position);
            Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * handleSize * Instance.HandleSize);

            handleMatrix = trs.localToWorldMatrix * scale;
        }

        private static void RebuildGizmoMesh(Vector3 scale)
        {
            if (_HandleLineMesh == null)
                _HandleLineMesh = new Mesh();

            if (_HandleTriangleMesh == null)
                _HandleTriangleMesh = new Mesh();

            CreateHandleLineMesh(ref _HandleLineMesh, scale);
            CreateHandleTriangleMesh(ref _HandleTriangleMesh, scale);
        }
        #endregion

        #region Set Functionality

        public static void SetTool(Tool tool)
        {
            if (SelectionHandle.tool != tool)
            {
                SelectionHandle.tool = tool;
                RebuildGizmoMesh(Vector3.one);
            }
        }

        public static Tool GetTool()
        {
            return tool;
        }

        #endregion

        #region Mesh Generation
        private static void CreateHandleLineMesh(ref Mesh mesh, Vector3 scale)
        {
            switch (tool)
            {
                case Tool.Translate:
                case Tool.Scale:
                    HandleMesh.CreatePositionLineMesh(ref mesh, trs, scale, viewOctant, cam, HANDLE_BOX_SIZE);
                    break;
                
                case Tool.Rotate:
                    HandleMesh.CreateRotateMesh(ref mesh, 48, 1f);
                    break;

                default:
                    return;
            }
        }

        private static void CreateHandleTriangleMesh(ref Mesh mesh, Vector3 scale)
        {
            if (tool == Tool.Translate)
                HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, viewOctant, cam, Instance.ConeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
            else if (tool == Tool.Scale)
                HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, viewOctant, cam, Instance.CubeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
        }

        #endregion
    }
}