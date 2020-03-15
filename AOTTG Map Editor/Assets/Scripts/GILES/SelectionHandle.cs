using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GILES
{
    public class SelectionHandle : MonoBehaviour
    {
        #region Data Members
        private Transform _trs;
        private Transform trs { get { if (_trs == null) _trs = gameObject.GetComponent<Transform>(); return _trs; } }
        private Camera _cam;
        private Camera cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }

        const int MAX_DISTANCE_TO_HANDLE = 10;

        static Mesh _HandleLineMesh = null, _HandleTriangleMesh = null;

        [SerializeField]
        private Mesh ConeMesh;   // Used as translation handle cone caps.
        [SerializeField]
        private Mesh CubeMesh;   // Used for scale handle

        private Material HandleOpaqueMaterial
        {
            get { return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_HandleOpaque); }
        }

        private Material RotateLineMaterial
        {
            get { return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_RotateHandle); }
        }

        private Material HandleTransparentMaterial
        {
            get { return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_HandleTransparent); }
        }

        private Mesh HandleLineMesh
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

        private Mesh HandleTriangleMesh
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
        private Vector3 prevPosition;
        private float rotationDisplacement;
        private Vector3 prevScale;
        private Vector3 scale;
        private float prevCursorDist;
        private float currCursorDist;

        ///Persistient variables used by the rotation tool
        //The angle displacement of the rotation handle since the drag started
        private float axisAngle = 0f;
        //Determines if the latest rotation was positive or negative
        private float sign;
        //The vector in screenspace representing the tangent line of the rotation handle that was clicked
        private Vector2 clickTangent;

        ///Persistient variables used by the translation tool
        float cameraDist;

        private Mesh _coneRight, _coneUp, _coneForward;

        const float CAP_SIZE = .07f;

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
        private float maxDistance = 1000000000f;

        private Vector2 mouseOrigin = Vector2.zero;
        private bool draggingHandle;
        //In how many directions is the handle able to move
        private int draggingAxes = 0;
        private pb_Transform handleOrigin = pb_Transform.identity;

        //Determines if the handle should be displayed and interactable
        private bool hidden = false;
        public bool InUse() { return draggingHandle; }

        //The octant of the camera relative ot the tool handle
        public Vector3 viewOctant { get; private set; }
        //The octant of the camera in the previous frame
        private Vector3 previousOctant;

        //Get the octant to display the planes in based on camera position and tool dragging status
        private Vector3 getViewOctant()
        {
            //If the tool is not being dragged, use the current octant
            if (!draggingHandle)
                return EditorMath.getOctant(transform.position, cam.transform.position);

            //If it is being dragged, use the octant the camera was in before the drag
            return previousOctant;
        }

        #endregion

        #region Initialization

        protected void Awake()
        {
            //Hide the hanlde by default
            hidden = true;
            _trs = null;
            _cam = null;
        }

        public void hide()
        {
            hidden = true;
            draggingHandle = false;
        }

        public void show()
        {
            hidden = false;
        }

        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            trs.position = position;
            trs.rotation = rotation;
            trs.localScale = scale;

            RebuildGizmoMatrix();
        }
        #endregion

        #region Delegate

        public delegate void OnHandleMoveEvent(pb_Transform transform);
        public event OnHandleMoveEvent OnHandleMove;

        public delegate void OnHandleBeginEvent(pb_Transform transform);
        public event OnHandleBeginEvent OnHandleBegin;

        public delegate void OnHandleFinishEvent();
        public event OnHandleFinishEvent OnHandleFinish;

        void OnCameraMove()
        {
            RebuildGizmoMesh(Vector3.one);
            RebuildGizmoMatrix();
        }
        #endregion

        #region
        class DragOrientation
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

        DragOrientation drag = new DragOrientation();
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
                if (CommonReferences.editorManager.currentMode != EditorMode.Edit)
                    return;

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

        private void interactHandle()
        {
            //Set the starting point of the drag to the position of the handle
            drag.origin = trs.position;
            //Get the displacement of the cursor on the screen
            Vector2 mouseDisplacement = (Vector2)Input.mousePosition - mouseOrigin;

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

                    default:
                        //Stores the scale factor of each axis
                        Vector3 scaleVector;

                        //If all axes are being dragged, scale based on the distance between the cursor and tool handle
                        if (draggingAxes > 1)
                        {
                            //Get the distance in screen space between the tool handle and the cursor
                            currCursorDist = getMouseHandleDist(Input.mousePosition);
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

                //Reset the mouse origin to get the right displacement next frame
                mouseOrigin = Input.mousePosition;
            }

            //If the current tool is the scale tool, rebuild the handle with the correct scale
            if(tool == Tool.Scale)
                RebuildGizmoMesh(scale);

            if (OnHandleMove != null)
                OnHandleMove(GetTransform());

            RebuildGizmoMatrix();
        }

        //Return the difference between the current handle position and the previous position
        public Vector3 getPosDisplacement()
        {
            return trs.position - prevPosition;
        }

        //Return the angle the handle was rotated and the axis it was rotated around
        public float getRotDisplacement(out Vector3 rotationAxis)
        {
            rotationAxis = drag.worldAxis;
            return rotationDisplacement;
        }

        //Calculte how much each axis was scaled since the last frame and return it
        public Vector3 getScaleDisplacement()
        {
            Vector3 scaleDisplacement = new Vector3();

            for (int axis = 0; axis < 3; axis++)
                scaleDisplacement[axis] = scale[axis] / prevScale[axis];

            return scaleDisplacement;
        }

        //Public getters and setters for the variables of the handle transform component
        public Vector3 getPosition() { return trs.position; }
        public void setPosition(Vector3 newPosition) { trs.position = newPosition; }
        public Quaternion getRotation() { return trs.rotation; }
        public void setRotation(Quaternion newRotation) { trs.rotation = newRotation; }
        public Vector3 getScale() { return trs.localScale; }
        public void setScale(Vector3 newScale) { trs.localScale = newScale; }

        //Find the point the mouse is over on the plane the handle is moving along
        private Vector3 getMovementPlaneHit()
        {
            //Create a ray originating from the camera and passing through the cursor
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            //Find the position the cursor over on the corresponding plane
            if (drag.plane.Raycast(ray, out float distToHit))
                return ray.GetPoint(distToHit);

            //Otherwise return NAN to indicate a failure to get the point
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        private Vector3 getClickVector()
        {
            //A plane representing the axis currently being dragged
            Plane movementPlane = new Plane();
            //The point where the ray intersected the plane
            Vector3 hitPoint;

            //Create a ray originating from the camera and passing through the cursor
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
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
        private Vector3 getOrthInPlane(Vector3 originalVector)
        {
            return Vector3.Cross(drag.worldAxis, originalVector);
        }

        //Find the vector from the handle origin to where the handle was clicked
        private Vector2 getClickTangent()
        {
            //Convert both the handle position to screen space
            Vector2 screenPosHandle = cam.WorldToScreenPoint(trs.position);
            //The 3D vector tangent to the rotation handle at the click point
            Vector3 tangentVector3;

            //Get the vector starting at the hanlde origin and ending at the camera
            Vector3 cameraVector = (cam.transform.position - transform.position).normalized;

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
        private Vector2 projectBontoA(Vector2 B, Vector3 A)
        {
            //The scalar to multiply the onto vector by
            float scalar = Vector2.Dot(A, B) / Mathf.Pow(A.magnitude, 2);

            //Scale the onto vector to represent the projection of the target vector
            return scalar * A;
        }

        //Use the displacement of the mouse in screen space to calculate the corresponding displacement
        //in the local space of the tool handle along the drag axis
        private Vector3 getDragDisplacement(Vector2 mouseDisplacement)
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
        private float getMouseHandleDist(Vector2 mouseDisplacement)
        {
            //Convert the position of the tool handle to screen space
            Vector2 screenHandlePos = cam.WorldToScreenPoint(trs.position);

            //Return the magnitude of the difference between the mouse and the handle position
            return (mouseDisplacement - screenHandlePos).magnitude;
        }

        void checkInteract()
        {
            //Don't check for handle interactions if it is hidden
            if (hidden)
                return;

            Vector3 a, b;
            drag.offset = Vector3.zero;
            Axis plane;

            draggingHandle = CheckHandleActivated(Input.mousePosition, out plane);

            mouseOrigin = Input.mousePosition;
            handleOrigin.SetTRS(trs);

            //Reset the axes being dragged
            drag.worldAxis = Vector3.zero;
            drag.localAxis = Vector3.zero;
            draggingAxes = 0;

            if (draggingHandle)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);

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
                    if (pb_HandleUtility.PointOnLine(new Ray(trs.position, drag.worldAxis), ray, out a, out b))
                        drag.offset = a - trs.position;
                }
                else
                {
                    float hit = 0f;

                    if (drag.plane.Raycast(ray, out hit))
                        drag.offset = ray.GetPoint(hit) - trs.position;
                }

                if (OnHandleBegin != null)
                    OnHandleBegin(GetTransform());

                
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
                    currCursorDist = getMouseHandleDist(Input.mousePosition);
                }
            }
        }

        void OnFinishHandleMovement()
        {
            RebuildGizmoMesh(Vector3.one);
            RebuildGizmoMatrix();

            if (OnHandleFinish != null)
                OnHandleFinish();

            StartCoroutine(SetDraggingFalse());
        }

        IEnumerator SetDraggingFalse()
        {
            yield return new WaitForEndOfFrame();
            draggingHandle = false;
        }
        #endregion

        #region Interface

        public pb_Transform GetTransform()
        {
            return new pb_Transform(
                trs.position,
                trs.rotation,
                scale);
        }

        bool CheckHandleActivated(Vector2 mousePosition, out Axis plane)
        {
            plane = (Axis)0x0;

            if (tool == Tool.Translate || tool == Tool.Scale)
            {
                float sceneHandleSize = pb_HandleUtility.GetHandleSize(trs.position);

                // cen
                Vector2 cen = cam.WorldToScreenPoint(trs.position);

                // up
                Vector2 up = cam.WorldToScreenPoint((trs.position + (trs.up + trs.up * CAP_SIZE * 4f) * (sceneHandleSize * HandleSize)));

                // right
                Vector2 right = cam.WorldToScreenPoint((trs.position + (trs.right + trs.right * CAP_SIZE * 4f) * (sceneHandleSize * HandleSize)));

                // forward
                Vector2 forward = cam.WorldToScreenPoint((trs.position + (trs.forward + trs.forward * CAP_SIZE * 4f) * (sceneHandleSize * HandleSize)));

                // First check if the plane boxes have been activated
                Vector2 p_right = (cen + ((right - cen) * viewOctant.x) * HANDLE_BOX_SIZE);
                Vector2 p_up = (cen + ((up - cen) * viewOctant.y) * HANDLE_BOX_SIZE);
                Vector2 p_forward = (cen + ((forward - cen) * viewOctant.z) * HANDLE_BOX_SIZE);

                // x plane
                if (pb_HandleUtility.PointInPolygon(new Vector2[] {
            cen, p_up,
            p_up, (p_up+p_forward) - cen,
            (p_up+p_forward) - cen, p_forward,
            p_forward, cen
            }, mousePosition))
                    plane = Axis.Y | Axis.Z;
                // y plane
                else if (pb_HandleUtility.PointInPolygon(new Vector2[] {
            cen, p_right,
            p_right, (p_right+p_forward)-cen,
            (p_right+p_forward)-cen, p_forward,
            p_forward, cen
            }, mousePosition))
                    plane = Axis.X | Axis.Z;
                // z plane
                else if (pb_HandleUtility.PointInPolygon(new Vector2[] {
            cen, p_up,
            p_up, (p_up + p_right) - cen,
            (p_up + p_right) - cen, p_right,
            p_right, cen
            }, mousePosition))
                    plane = Axis.X | Axis.Y;
                else
                if (pb_HandleUtility.DistancePointLineSegment(mousePosition, cen, up) < MAX_DISTANCE_TO_HANDLE)
                    plane = Axis.Y;
                else if (pb_HandleUtility.DistancePointLineSegment(mousePosition, cen, right) < MAX_DISTANCE_TO_HANDLE)
                    plane = Axis.X;
                else if (pb_HandleUtility.DistancePointLineSegment(mousePosition, cen, forward) < MAX_DISTANCE_TO_HANDLE)
                    plane = Axis.Z;
                else
                    return false;

                return true;
            }
            else
            {
                Vector3[][] vertices = pb_HandleMesh.GetRotationVertices(16, 1f);

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

                        float dist = pb_HandleUtility.DistancePointLineSegment(mousePosition, prev, cur);

                        if (dist < best && dist < MAX_DISTANCE_TO_HANDLE)
                        {
                            Vector3 viewDir = (handleMatrix.MultiplyPoint3x4((vertices[i][n] + vertices[i][n + 1]) * .5f) - cam.transform.position).normalized;
                            Vector3 nrm = transform.TransformDirection(vertices[i][n]).normalized;

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

                if (best < MAX_DISTANCE_TO_HANDLE + .1f)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Render

        private Matrix4x4 handleMatrix;

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

        void RebuildGizmoMatrix()
        {
            float handleSize = pb_HandleUtility.GetHandleSize(trs.position);
            Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * handleSize * HandleSize);

            handleMatrix = transform.localToWorldMatrix * scale;
        }

        void RebuildGizmoMesh(Vector3 scale)
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

        public void SetTool(Tool tool)
        {
            if (SelectionHandle.tool != tool)
            {
                SelectionHandle.tool = tool;
                RebuildGizmoMesh(Vector3.one);
            }
        }

        public Tool GetTool()
        {
            return tool;
        }

        #endregion

        #region Mesh Generation

        const float HANDLE_BOX_SIZE = .25f;

        private void CreateHandleLineMesh(ref Mesh mesh, Vector3 scale)
        {
            switch (tool)
            {
                case Tool.Translate:
                case Tool.Scale:
                    pb_HandleMesh.CreatePositionLineMesh(ref mesh, trs, scale, viewOctant, cam, HANDLE_BOX_SIZE);
                    break;
                
                case Tool.Rotate:
                    pb_HandleMesh.CreateRotateMesh(ref mesh, 48, 1f);
                    break;

                default:
                    return;
            }
        }

        private void CreateHandleTriangleMesh(ref Mesh mesh, Vector3 scale)
        {
            if (tool == Tool.Translate)
                pb_HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, viewOctant, cam, ConeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
            else if (tool == Tool.Scale)
                pb_HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, viewOctant, cam, CubeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
        }

        #endregion
    }
}