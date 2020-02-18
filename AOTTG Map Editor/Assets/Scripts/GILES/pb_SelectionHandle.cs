using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GILES
{
    public class pb_SelectionHandle : MonoBehaviour
    {
        #region Member

        private Transform _trs;
        private Transform trs { get { if (_trs == null) _trs = gameObject.GetComponent<Transform>(); return _trs; } }
        private Camera _cam;
        private Camera cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }

        const int MAX_DISTANCE_TO_HANDLE = 15;

        static Mesh _HandleLineMesh = null, _HandleTriangleMesh = null;

        public Mesh ConeMesh;   // Used as translation handle cone caps.
        public Mesh CubeMesh;   // Used for scale handle

        private Material HandleOpaqueMaterial
        {
            get
            {
                return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_HandleOpaque);
            }
        }

        private Material RotateLineMaterial
        {
            get
            {
                return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_RotateHandle);
            }
        }

        private Material HandleTransparentMaterial
        {
            get
            {
                return pb_BuiltinResource.GetMaterial(pb_BuiltinResource.mat_HandleTransparent);
            }
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

        //The current tool. Default is translate tool.
        private Tool tool = Tool.Translate;

        private Mesh _coneRight, _coneUp, _coneForward;

        const float CAP_SIZE = .07f;

        public float HandleSize = 90f;

        private Vector2 mouseOrigin = Vector2.zero;
        public bool draggingHandle { get; private set; }
        private int draggingAxes = 0;   // In how many directions is the handle able to move
        private Vector3 scale = Vector3.one;
        private pb_Transform handleOrigin = pb_Transform.identity;

        public bool isHidden { get; private set; }
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
            _trs = null;
            _cam = null;
            //base.Awake();
        }

        void Start()
        {
            //pb_SceneCamera.AddOnCameraMoveDelegate(this.OnCameraMove);

            //SetIsHidden(true);
            SetIsHidden(false);
        }

        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            trs.position = position;
            trs.localRotation = rotation;
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
            public Vector3 axis;
            public Vector3 mouse;
            public Vector3 cross;
            public Vector3 offset;
            public Plane plane;

            public DragOrientation()
            {
                origin = Vector3.zero;
                axis = Vector3.zero;
                mouse = Vector3.zero;
                cross = Vector3.zero;
                offset = Vector3.zero;
                plane = new Plane(Vector3.up, Vector3.zero);
            }
        }

        DragOrientation drag = new DragOrientation();

        void LateUpdate()
        {
            //Update the octant the camera is in relative to the tool handle
            previousOctant = viewOctant;
            viewOctant = getViewOctant();

            //Rebuild the gizmo meshes and matricies when the camera moves
            OnCameraMove();

            //Don't check for handle interactions if the handle is hidden
            if (isHidden)
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

        private void interactHandle()
        {
            //Get the point on the movement plane the cursor is over
            Vector3 planeHit = getMovementPlaneHit();

            //Set the starting point of the drag to the position of the handle
            drag.origin = trs.position;

            switch (tool)
            {
                case Tool.Translate:
                    //If the plane translate is selected, use the whole hit point as the position of the handle
                    if (draggingAxes > 1)
                        trs.position = planeHit - drag.offset;
                    //If only an axis is selected, use the corresponding axis of the hit point in the position of the handle
                    else
                    {
                        //Get the component of the ray hit position corresponding to the axis of movement
                        Vector3 positionAlongAxis = new Vector3(drag.axis.x * planeHit.x, drag.axis.y * planeHit.y, drag.axis.z * planeHit.z);

                        if(positionAlongAxis.x != 0)
                            trs.position = new Vector3(positionAlongAxis.x, trs.position.y, trs.position.z) - drag.offset;
                        if (positionAlongAxis.y != 0)
                            trs.position = new Vector3(trs.position.x, positionAlongAxis.y, trs.position.z) - drag.offset;
                        if (positionAlongAxis.z != 0)
                            trs.position = new Vector3(trs.position.x, trs.position.y, positionAlongAxis.z) - drag.offset;
                    }
                    break;

                case Tool.Rotate:
                    Vector2 delta = (Vector2)Input.mousePosition - mouseOrigin;
                    mouseOrigin = Input.mousePosition;
                    float sign = pb_HandleUtility.CalcMouseDeltaSignWithAxes(cam, drag.origin, drag.axis, drag.cross, delta);
                    axisAngle += delta.magnitude * sign;
                    trs.localRotation = Quaternion.AngleAxis(axisAngle, drag.axis) * handleOrigin.rotation;// trs.localRotation;
                    break;

                case Tool.Scale:
                    Vector3 v;

                    //If the entire object is being scaled, enlargen all three axis handles
                    if (draggingAxes > 1)
                        v = SetUniformMagnitude(((planeHit - drag.offset) - trs.position));
                    //Otherwise enlargen the axis handle being iteracted with
                    else
                    {
                        //v = Quaternion.Inverse(handleOrigin.rotation) * ((planeHit - drag.offset) - trs.position);
                        v = new Vector3(1, 2, 3);
                    }

                    v += Vector3.one;
                    scale = v;
                    RebuildGizmoMesh(scale);
                    break;
            }

            if (OnHandleMove != null)
                OnHandleMove(GetTransform());

            RebuildGizmoMatrix();
        }

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
            //Otherwise, determine the plane to use
            else
            {
                //If the handle is moving along either the x or z axes, use an x-z plane
                if (drag.axis.x != 0 || drag.axis.z != 0)
                    movementPlane.SetNormalAndPosition(trs.up.normalized, trs.position);
            }

            //Find the position the cursor over on the corresponding plane
            if (movementPlane.Raycast(ray, out distToHit))
                return ray.GetPoint(distToHit);

            //If the ray didn't hit anything, return an empty vector
            return new Vector3();
        }

        float axisAngle = 0f;

        /**
            * Sets all the components of a vector to the component with the largest magnitude.
            */
        Vector3 SetUniformMagnitude(Vector3 a)
        {
            float max = Mathf.Abs(a.x) > Mathf.Abs(a.y) && Mathf.Abs(a.x) > Mathf.Abs(a.z) ? a.x : Mathf.Abs(a.y) > Mathf.Abs(a.z) ? a.y : a.z;

            a.x = max;
            a.y = max;
            a.z = max;

            return a;
        }

        void OnMouseDown()
        {
            scale = Vector3.one;

            Vector3 a, b;
            drag.offset = Vector3.zero;
            Axis plane;

            axisAngle = 0f;

            draggingHandle = CheckHandleActivated(Input.mousePosition, out plane);

            mouseOrigin = Input.mousePosition;
            handleOrigin.SetTRS(trs);

            drag.axis = Vector3.zero;
            draggingAxes = 0;

            if (draggingHandle)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);

                if ((plane & Axis.X) == Axis.X)
                {
                    draggingAxes++;
                    drag.axis = trs.right;
                    drag.plane.SetNormalAndPosition(trs.right.normalized, trs.position);
                }

                if ((plane & Axis.Y) == Axis.Y)
                {
                    draggingAxes++;
                    if (draggingAxes > 1)
                        drag.plane.SetNormalAndPosition(Vector3.Cross(drag.axis, trs.up).normalized, trs.position);
                    else
                        drag.plane.SetNormalAndPosition(trs.up.normalized, trs.position);
                    drag.axis += trs.up;
                }

                if ((plane & Axis.Z) == Axis.Z)
                {
                    draggingAxes++;
                    if (draggingAxes > 1)
                        drag.plane.SetNormalAndPosition(Vector3.Cross(drag.axis, trs.forward).normalized, trs.position);
                    else
                        drag.plane.SetNormalAndPosition(trs.forward.normalized, trs.position);
                    drag.axis += trs.forward;
                }

                if (draggingAxes < 2)
                {
                    if (pb_HandleUtility.PointOnLine(new Ray(trs.position, drag.axis), ray, out a, out b))
                        drag.offset = a - trs.position;

                    float hit = 0f;

                    if (drag.plane.Raycast(ray, out hit))
                    {
                        drag.mouse = (ray.GetPoint(hit) - trs.position).normalized;
                        drag.cross = Vector3.Cross(drag.axis, drag.mouse);
                    }
                }
                else
                {
                    float hit = 0f;

                    if (drag.plane.Raycast(ray, out hit))
                    {
                        drag.offset = ray.GetPoint(hit) - trs.position;
                        drag.mouse = (ray.GetPoint(hit) - trs.position).normalized;
                        drag.cross = Vector3.Cross(drag.axis, drag.mouse);
                    }
                }

                if (OnHandleBegin != null)
                    OnHandleBegin(GetTransform());
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
                trs.localRotation,
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
            if (isHidden || Camera.current != cam)
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
            if (this.tool != tool)
            {
                this.tool = tool;
                RebuildGizmoMesh(Vector3.one);
            }
        }

        public Tool GetTool()
        {
            return tool;
        }

        public void SetIsHidden(bool isHidden)
        {
            draggingHandle = false;
            this.isHidden = isHidden;
        }

        public bool GetIsHidden()
        {
            return this.isHidden;
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
                    pb_HandleMesh.CreatePositionLineMesh(ref mesh, trs, scale, cam, HANDLE_BOX_SIZE);
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
                pb_HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, cam, ConeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
            else if (tool == Tool.Scale)
                pb_HandleMesh.CreateTriangleMesh(ref mesh, trs, scale, cam, CubeMesh, HANDLE_BOX_SIZE, CAP_SIZE);
        }

        #endregion
    }
}