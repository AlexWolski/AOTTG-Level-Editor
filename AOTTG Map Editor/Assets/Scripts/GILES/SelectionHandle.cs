using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        public Mesh ConeMesh;   // Used as translation handle cone caps.
        public Mesh CubeMesh;   // Used for scale handle

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
        //The tool handle status for position, rotation, and scale
        private Vector3 prevPosition;
        private float rotationDisplacement;
        private Vector3 scale;

        private Mesh _coneRight, _coneUp, _coneForward;

        const float CAP_SIZE = .07f;

        [SerializeField]
        private float HandleSize = 90f;
        [SerializeField]
        private float rotationSpeed = 3f;

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

        #region Update

        class DragOrientation
        {
            public Vector3 origin;
            //The primary axis the handle is being dragged along in local coordiantes (x, y, or z)
            public Vector3 localAxis;
            //The arbitrary axis the handle is being dragged along in world coordinates
            public Vector3 worldAxis;
            public Vector3 mouse;
            public Vector3 cross;
            public Vector3 offset;
            public Plane plane;

            public DragOrientation()
            {
                origin = Vector3.zero;
                worldAxis = Vector3.zero;
                mouse = Vector3.zero;
                cross = Vector3.zero;
                offset = Vector3.zero;
                plane = new Plane(Vector3.up, Vector3.zero);
            }
        }

        DragOrientation drag = new DragOrientation();

        //Using Update instead of LateUpdate so that the dragginHandle bool can update before ObjectSelection.cs call LateUpdate
        void Update()
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
                    OnMouseDown();
                //If the mouse is released, finish interacting with the handle
                if (Input.GetMouseButtonUp(0))
                    OnFinishHandleMovement();
                //If the mouse is pressed and dragging the handle, interact with the handle
                else if (draggingHandle && Input.GetMouseButton(0))
                    interactHandle();
            }

            //After checking for interactions, let the object selection script check for selections
            //To-do: move this to an input manager class
            ObjectSelection.updateSelection();
        }

        private void interactHandle()
        {
            //Set the starting point of the drag to the position of the handle
            drag.origin = trs.position;

            switch (tool)
            {
                case Tool.Translate:
                    //Get the point on the movement plane the cursor is over
                    Vector3 planeHit = getMovementPlaneHit();
                    //The plane hit relative to the local coordinate system of the tool handle
                    Vector3 localPlaneHit;

                    //Save the old position
                    prevPosition = trs.position;

                    //If the plane translate is selected, use the whole hit point as the position of the handle
                    if (draggingAxes > 1)
                        trs.position = planeHit - drag.offset;
                    //If only one axis is selected, use the corresponding component of the hit point in the position of the handle
                    else
                    {
                        //Convert the plane hit to local coordinates
                        localPlaneHit = trs.InverseTransformPoint(planeHit - drag.offset);

                        //Erase the unneeded components of the displacement
                        for (int axis = 0; axis < 3; axis++)
                            localPlaneHit[axis] *= drag.localAxis[axis];

                        //Translate the tool handle so it matches the position of the plane hit
                        trs.Translate(localPlaneHit, Space.Self);
                    }
                    break;

                case Tool.Rotate:
                    //Get the displacement of the cursor on the screen
                    Vector2 mouseDisplacement = (Vector2)Input.mousePosition - mouseOrigin;

                    //Project the mouse displacement onto the tangent vector to get the component tangent to the rotation handle
                    Vector2 tangentDisplacement = projectBontoA(mouseDisplacement, clickTangent);
                    //Use the dot product between the tangent displacement and click tangent to get the sign of the rotation
                    sign = Vector2.Dot(tangentDisplacement, clickTangent) > 0 ? 1f : -1f;

                    //If the camera is on the wrong side of the movement plane, invert the displacement angle
                    if (!drag.plane.GetSide(cam.transform.position))
                        sign *= -1;

                    //Use the magnitude of the displacement as the angle displacement
                    float angleDisplacement = tangentDisplacement.magnitude * sign;
                    //Add the displacement to the angle after scaling it by the rotation speed
                    rotationDisplacement = angleDisplacement / 10 * rotationSpeed;
                    axisAngle += rotationDisplacement;

                    //Rotate the tool handle
                    trs.rotation = Quaternion.AngleAxis(axisAngle, drag.worldAxis) * handleOrigin.rotation;

                    //Resest the mouse origin to get the right displacement next frame
                    mouseOrigin = Input.mousePosition;
                    break;

                //case Tool.Scale:
                //    //Convert the plane hit to local coordinates
                //    localPlaneHit = trs.InverseTransformPoint(planeHit - drag.offset);

                //    float handleDisplacement;

                //    if (drag.localAxis.x != 0)
                //        handleDisplacement = localPlaneHit.x;
                //    else if (drag.localAxis.y != 0)
                //        handleDisplacement = localPlaneHit.y;
                //    else
                //        handleDisplacement = localPlaneHit.z;

                //    //If the entire object is being scaled, scale all three axis
                //    if (draggingAxes > 1)
                //        scale = new Vector3(handleDisplacement, handleDisplacement, handleDisplacement);
                //    //Otherwise scale the axis handle being iteracted with
                //    else
                //        scale = (drag.localAxis * handleDisplacement) + Vector3.one;

                //    //Add the default scale to the displacement to get the amount to scale the object
                //    RebuildGizmoMesh(scale);
                //    break;
            }

            if (OnHandleMove != null)
                OnHandleMove(GetTransform());

            RebuildGizmoMatrix();
        }

        //Return the displacement of the tool handle
        public Vector3 getDisplacement()
        {
            switch (tool)
            {
                case Tool.Translate:
                    return trs.position - prevPosition;

                case Tool.Rotate:
                    return drag.localAxis * rotationDisplacement;
                default:
                    return scale;
            }
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
            //The distance between the origin of the ray and the intersection with the plane
            float distToHit = 0f;

            //A plane to interesct the ray casted from the camera. The plane must face the camera
            Plane movementPlane = new Plane();

            //If the handle is moving along two axes, use the dragging plane
            if (draggingAxes >= 2)
                movementPlane = drag.plane;
            //Otherwise, determine the plane to use based on the camera angle
            else
            {
                //Get the rotation of the camera in Euler angles relative to the handle rotation
                Vector3 camRot = cam.transform.rotation.eulerAngles;

                //If the drag axis is x or z and the x angle is 45 degrees away from default, use the y axis
                if (drag.localAxis.y == 0 && (45f < camRot.x && camRot.x < 90f || 270f < camRot.x && camRot.x < 315f))
                    movementPlane.SetNormalAndPosition(trs.up, trs.position);
                //If the drag axis is the y, use either the x or z plane based on the camera's angle
                else if (drag.localAxis.y != 0)
                {
                    if (45 < camRot.y && camRot.y < 135 ||
                        225 < camRot.y && camRot.y < 315)
                        movementPlane.SetNormalAndPosition(trs.right, trs.position);
                    else
                        movementPlane.SetNormalAndPosition(trs.forward, trs.position);
                }
                //Otherwise use the plane of the axis being dragged
                else
                {
                    if (drag.localAxis.x != 0)
                        movementPlane.SetNormalAndPosition(trs.forward, trs.position);
                    else
                        movementPlane.SetNormalAndPosition(trs.right, trs.position);
                }
            }

            //Find the position the cursor over on the corresponding plane
            if (movementPlane.Raycast(ray, out distToHit))
                return ray.GetPoint(distToHit);

            //If the ray didn't hit anything, return an empty vector
            return new Vector3();
        }

        //The angle displacement of the rotation handle since the drag started
        private float axisAngle = 0f;
        //Determines if the latest rotation was positive or negative
        private float sign;
        //A vector in screenspace starting at the hanlde origin and ending at the click location
        private Vector2 clickVector;
        //The vector in screenspace perpendicular to the click vector
        private Vector2 clickTangent;

        //Find the vector from the handle origin to where the handle was clicked
        private Vector2 getClickVector()
        {
            //Get the location on the screen that was clicked
            Vector2 clickPos = Input.mousePosition;
            //Convert the position of the tool handle in world space into screen space
            Vector2 handleOrigin = cam.WorldToScreenPoint(trs.position);
            //Calculate the click vector by finding the click position relative to the handle origin
            return clickPos - handleOrigin;
        }

        //Find the vector clockwise orthogonal to the given vector in 2D space
        private Vector2 getOrth2D(Vector2 originalVector)
        {
            return new Vector2(originalVector.y, -originalVector.x);
        }

        //Calculate the projection of one 2D vector onto another
        private Vector2 projectBontoA(Vector2 B, Vector3 A)
        {
            //The scalar to multiply the onto vector by
            float scalar = Vector2.Dot(A, B) / Mathf.Pow(A.magnitude, 2);

            //Scale the onto vector to represent the projection of the target vector
            return scalar * A;
        }

        void OnMouseDown()
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

                    float hit = 0f;

                    if (drag.plane.Raycast(ray, out hit))
                    {
                        drag.mouse = (ray.GetPoint(hit) - trs.position).normalized;
                        drag.cross = Vector3.Cross(drag.worldAxis, drag.mouse);
                    }
                }
                else
                {
                    float hit = 0f;

                    if (drag.plane.Raycast(ray, out hit))
                    {
                        drag.offset = ray.GetPoint(hit) - trs.position;
                        drag.mouse = (ray.GetPoint(hit) - trs.position).normalized;
                        drag.cross = Vector3.Cross(drag.worldAxis, drag.mouse);
                    }
                }

                if (OnHandleBegin != null)
                    OnHandleBegin(GetTransform());

                //If the scale handle was just clicked, reset the handle size and prime it for scaling
                if (tool == Tool.Scale)
                {
                    scale = Vector3.one;
                }
                //Reset the total displacement and save the angle of the point clicked on
                else if (tool == Tool.Rotate)
                {
                    axisAngle = 0f;
                    clickVector = getClickVector();
                    clickTangent = getOrth2D(clickVector);
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

        Vector2 screenToGUIPoint(Vector2 v)
        {
            v.y = Screen.height - v.y;
            return v;
        }

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