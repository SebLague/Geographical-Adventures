using System.Collections.Generic;
using PathCreation;
using PathCreation.Utility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PathCreationEditor {
    /// Editor class for the creation of Bezier and Vertex paths

    [CustomEditor (typeof (PathCreator))]
    public class PathEditor : Editor {

        #region Fields

        // Interaction:
        const float segmentSelectDistanceThreshold = 10f;
        const float screenPolylineMaxAngleError = .3f;
        const float screenPolylineMinVertexDst = .01f;

        // Help messages:
        const string helpInfo = "Shift-click to add or insert new points. Control-click to delete points. For more detailed infomation, please refer to the documentation.";
        static readonly string[] spaceNames = { "3D (xyz)", "2D (xy)", "Top-down (xz)" };
        static readonly string[] tabNames = { "Bézier Path", "Vertex Path" };
        const string constantSizeTooltip = "If true, anchor and control points will keep a constant size when zooming in the editor.";

        // Display
        const int inspectorSectionSpacing = 10;
        const float constantHandleScale = .01f;
        const float normalsSpacing = .2f;
        GUIStyle boldFoldoutStyle;

        // References:
        PathCreator creator;
        Editor globalDisplaySettingsEditor;
        ScreenSpacePolyLine screenSpaceLine;
        ScreenSpacePolyLine.MouseInfo pathMouseInfo;
        GlobalDisplaySettings globalDisplaySettings;
        PathHandle.HandleColours splineAnchorColours;
        PathHandle.HandleColours splineControlColours;
        Dictionary<GlobalDisplaySettings.HandleType, Handles.CapFunction> capFunctions;
        ArcHandle anchorAngleHandle = new ArcHandle ();
        VertexPath normalsVertexPath;

        // State variables:
        int selectedSegmentIndex;
        int draggingHandleIndex;
        int mouseOverHandleIndex;
        int handleIndexToDisplayAsTransform;

        bool shiftLastFrame;
        bool hasUpdatedScreenSpaceLine;
        bool hasUpdatedNormalsVertexPath;
        bool editingNormalsOld;

        Vector3 transformPos;
        Vector3 transformScale;
        Quaternion transformRot;

        Color handlesStartCol;

        // Constants
        const int bezierPathTab = 0;
        const int vertexPathTab = 1;

        #endregion

        #region Inspectors

        public override void OnInspectorGUI () {
            // Initialize GUI styles
            if (boldFoldoutStyle == null) {
                boldFoldoutStyle = new GUIStyle (EditorStyles.foldout);
                boldFoldoutStyle.fontStyle = FontStyle.Bold;
            }

            Undo.RecordObject (creator, "Path settings changed");

            // Draw Bezier and Vertex tabs
            int tabIndex = GUILayout.Toolbar (data.tabIndex, tabNames);
            if (tabIndex != data.tabIndex) {
                data.tabIndex = tabIndex;
                TabChanged ();
            }

            // Draw inspector for active tab
            switch (data.tabIndex) {
                case bezierPathTab:
                    DrawBezierPathInspector ();
                    break;
                case vertexPathTab:
                    DrawVertexPathInspector ();
                    break;
            }

            // Notify of undo/redo that might modify the path
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
                data.PathModifiedByUndo ();
            }
        }

        void DrawBezierPathInspector () {
            using (var check = new EditorGUI.ChangeCheckScope ()) {
                // Path options:
                data.showPathOptions = EditorGUILayout.Foldout (data.showPathOptions, new GUIContent ("Bézier Path Options"), true, boldFoldoutStyle);
                if (data.showPathOptions) {
                    bezierPath.Space = (PathSpace) EditorGUILayout.Popup ("Space", (int) bezierPath.Space, spaceNames);
                    bezierPath.ControlPointMode = (BezierPath.ControlMode) EditorGUILayout.EnumPopup (new GUIContent ("Control Mode"), bezierPath.ControlPointMode);
                    if (bezierPath.ControlPointMode == BezierPath.ControlMode.Automatic) {
                        bezierPath.AutoControlLength = EditorGUILayout.Slider (new GUIContent ("Control Spacing"), bezierPath.AutoControlLength, 0, 1);
                    }

                    bezierPath.IsClosed = EditorGUILayout.Toggle ("Closed Path", bezierPath.IsClosed);
                    data.showTransformTool = EditorGUILayout.Toggle (new GUIContent ("Enable Transforms"), data.showTransformTool);

                    Tools.hidden = !data.showTransformTool;

                    // Check if out of bounds (can occur after undo operations)
                    if (handleIndexToDisplayAsTransform >= bezierPath.NumPoints) {
                        handleIndexToDisplayAsTransform = -1;
                    }

                    // If a point has been selected
                    if (handleIndexToDisplayAsTransform != -1) {
                        EditorGUILayout.LabelField ("Selected Point:");

                        using (new EditorGUI.IndentLevelScope ()) {
                            var currentPosition = creator.bezierPath[handleIndexToDisplayAsTransform];
                            var newPosition = EditorGUILayout.Vector3Field ("Position", currentPosition);
                            if (newPosition != currentPosition) {
                                Undo.RecordObject (creator, "Move point");
                                creator.bezierPath.MovePoint (handleIndexToDisplayAsTransform, newPosition);
                            }
                            // Don't draw the angle field if we aren't selecting an anchor point/not in 3d space
                            if (handleIndexToDisplayAsTransform % 3 == 0 && creator.bezierPath.Space == PathSpace.xyz) {
                                var anchorIndex = handleIndexToDisplayAsTransform / 3;
                                var currentAngle = creator.bezierPath.GetAnchorNormalAngle (anchorIndex);
                                var newAngle = EditorGUILayout.FloatField ("Angle", currentAngle);
                                if (newAngle != currentAngle) {
                                    Undo.RecordObject (creator, "Set Angle");
                                    creator.bezierPath.SetAnchorNormalAngle (anchorIndex, newAngle);
                                }
                            }
                        }
                    }

                    if (data.showTransformTool & (handleIndexToDisplayAsTransform == -1)) {
                        if (GUILayout.Button ("Centre Transform")) {

                            Vector3 worldCentre = bezierPath.CalculateBoundsWithTransform (creator.transform).center;
                            Vector3 transformPos = creator.transform.position;
                            if (bezierPath.Space == PathSpace.xy) {
                                transformPos = new Vector3 (transformPos.x, transformPos.y, 0);
                            } else if (bezierPath.Space == PathSpace.xz) {
                                transformPos = new Vector3 (transformPos.x, 0, transformPos.z);
                            }
                            Vector3 worldCentreToTransform = transformPos - worldCentre;

                            if (worldCentre != creator.transform.position) {
                                //Undo.RecordObject (creator, "Centralize Transform");
                                if (worldCentreToTransform != Vector3.zero) {
                                    Vector3 localCentreToTransform = MathUtility.InverseTransformVector (worldCentreToTransform, creator.transform, bezierPath.Space);
                                    for (int i = 0; i < bezierPath.NumPoints; i++) {
                                        bezierPath.SetPoint (i, bezierPath.GetPoint (i) + localCentreToTransform, true);
                                    }
                                }

                                creator.transform.position = worldCentre;
                                bezierPath.NotifyPathModified ();
                            }
                        }
                    }

                    if (GUILayout.Button ("Reset Path")) {
                        Undo.RecordObject (creator, "Reset Path");
                        bool in2DEditorMode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
                        data.ResetBezierPath (creator.transform.position, in2DEditorMode);
                        EditorApplication.QueuePlayerLoopUpdate ();
                    }

                    GUILayout.Space (inspectorSectionSpacing);
                }

                data.showNormals = EditorGUILayout.Foldout (data.showNormals, new GUIContent ("Normals Options"), true, boldFoldoutStyle);
                if (data.showNormals) {
                    bezierPath.FlipNormals = EditorGUILayout.Toggle (new GUIContent ("Flip Normals"), bezierPath.FlipNormals);
                    if (bezierPath.Space == PathSpace.xyz) {
                        bezierPath.GlobalNormalsAngle = EditorGUILayout.Slider (new GUIContent ("Global Angle"), bezierPath.GlobalNormalsAngle, 0, 360);

                        if (GUILayout.Button ("Reset Normals")) {
                            Undo.RecordObject (creator, "Reset Normals");
                            bezierPath.FlipNormals = false;
                            bezierPath.ResetNormalAngles ();
                        }
                    }
                    GUILayout.Space (inspectorSectionSpacing);
                }

                // Editor display options
                data.showDisplayOptions = EditorGUILayout.Foldout (data.showDisplayOptions, new GUIContent ("Display Options"), true, boldFoldoutStyle);
                if (data.showDisplayOptions) {
                    data.showPathBounds = GUILayout.Toggle (data.showPathBounds, new GUIContent ("Show Path Bounds"));
                    data.showPerSegmentBounds = GUILayout.Toggle (data.showPerSegmentBounds, new GUIContent ("Show Segment Bounds"));
                    data.displayAnchorPoints = GUILayout.Toggle (data.displayAnchorPoints, new GUIContent ("Show Anchor Points"));
                    if (!(bezierPath.ControlPointMode == BezierPath.ControlMode.Automatic && globalDisplaySettings.hideAutoControls)) {
                        data.displayControlPoints = GUILayout.Toggle (data.displayControlPoints, new GUIContent ("Show Control Points"));
                    }
                    data.keepConstantHandleSize = GUILayout.Toggle (data.keepConstantHandleSize, new GUIContent ("Constant Point Size", constantSizeTooltip));
                    data.bezierHandleScale = Mathf.Max (0, EditorGUILayout.FloatField (new GUIContent ("Handle Scale"), data.bezierHandleScale));
                    DrawGlobalDisplaySettingsInspector ();
                }

                if (check.changed) {
                    SceneView.RepaintAll ();
                    EditorApplication.QueuePlayerLoopUpdate ();
                }
            }
        }

        void DrawVertexPathInspector () {

            GUILayout.Space (inspectorSectionSpacing);
            EditorGUILayout.LabelField ("Vertex count: " + creator.path.NumPoints);
            GUILayout.Space (inspectorSectionSpacing);

            data.showVertexPathOptions = EditorGUILayout.Foldout (data.showVertexPathOptions, new GUIContent ("Vertex Path Options"), true, boldFoldoutStyle);
            if (data.showVertexPathOptions) {
                using (var check = new EditorGUI.ChangeCheckScope ()) {
                    data.vertexPathMaxAngleError = EditorGUILayout.Slider (new GUIContent ("Max Angle Error"), data.vertexPathMaxAngleError, 0, 45);
                    data.vertexPathMinVertexSpacing = EditorGUILayout.Slider (new GUIContent ("Min Vertex Dst"), data.vertexPathMinVertexSpacing, 0, 1);

                    GUILayout.Space (inspectorSectionSpacing);
                    if (check.changed) {
                        data.VertexPathSettingsChanged ();
                        SceneView.RepaintAll ();
                        EditorApplication.QueuePlayerLoopUpdate ();
                    }
                }
            }

            data.showVertexPathDisplayOptions = EditorGUILayout.Foldout (data.showVertexPathDisplayOptions, new GUIContent ("Display Options"), true, boldFoldoutStyle);
            if (data.showVertexPathDisplayOptions) {
                using (var check = new EditorGUI.ChangeCheckScope ()) {
                    data.showNormalsInVertexMode = GUILayout.Toggle (data.showNormalsInVertexMode, new GUIContent ("Show Normals"));
                    data.showBezierPathInVertexMode = GUILayout.Toggle (data.showBezierPathInVertexMode, new GUIContent ("Show Bezier Path"));

                    if (check.changed) {
                        SceneView.RepaintAll ();
                        EditorApplication.QueuePlayerLoopUpdate ();
                    }
                }
                DrawGlobalDisplaySettingsInspector ();
            }
        }

        void DrawGlobalDisplaySettingsInspector () {
            using (var check = new EditorGUI.ChangeCheckScope ()) {
                data.globalDisplaySettingsFoldout = EditorGUILayout.InspectorTitlebar (data.globalDisplaySettingsFoldout, globalDisplaySettings);
                if (data.globalDisplaySettingsFoldout) {
                    CreateCachedEditor (globalDisplaySettings, null, ref globalDisplaySettingsEditor);
                    globalDisplaySettingsEditor.OnInspectorGUI ();
                }
                if (check.changed) {
                    UpdateGlobalDisplaySettings ();
                    SceneView.RepaintAll ();
                }
            }
        }

        #endregion

        #region Scene GUI

        void OnSceneGUI () {
            if (!globalDisplaySettings.visibleBehindObjects) {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            }

            EventType eventType = Event.current.type;

            using (var check = new EditorGUI.ChangeCheckScope ()) {
                handlesStartCol = Handles.color;
                switch (data.tabIndex) {
                    case bezierPathTab:
                        if (eventType != EventType.Repaint && eventType != EventType.Layout) {
                            ProcessBezierPathInput (Event.current);
                        }

                        DrawBezierPathSceneEditor ();
                        break;
                    case vertexPathTab:
                        if (eventType == EventType.Repaint) {
                            DrawVertexPathSceneEditor ();
                        }
                        break;
                }

                // Don't allow clicking over empty space to deselect the object
                if (eventType == EventType.Layout) {
                    HandleUtility.AddDefaultControl (0);
                }

                if (check.changed) {
                    EditorApplication.QueuePlayerLoopUpdate ();
                }
            }

            SetTransformState ();
        }

        void DrawVertexPathSceneEditor () {

            Color bezierCol = globalDisplaySettings.bezierPath;
            bezierCol.a *= .5f;

            if (data.showBezierPathInVertexMode) {
                for (int i = 0; i < bezierPath.NumSegments; i++) {
                    Vector3[] points = bezierPath.GetPointsInSegment (i);
                    for (int j = 0; j < points.Length; j++) {
                        points[j] = MathUtility.TransformPoint (points[j], creator.transform, bezierPath.Space);
                    }
                    Handles.DrawBezier (points[0], points[3], points[1], points[2], bezierCol, null, 2);
                }
            }

            Handles.color = globalDisplaySettings.vertexPath;

            for (int i = 0; i < creator.path.NumPoints; i++) {
                int nextIndex = (i + 1) % creator.path.NumPoints;
                if (nextIndex != 0 || bezierPath.IsClosed) {
                    Handles.DrawLine (creator.path.GetPoint (i), creator.path.GetPoint (nextIndex));
                }
            }

            if (data.showNormalsInVertexMode) {
                Handles.color = globalDisplaySettings.normals;
                Vector3[] normalLines = new Vector3[creator.path.NumPoints * 2];
                for (int i = 0; i < creator.path.NumPoints; i++) {
                    normalLines[i * 2] = creator.path.GetPoint (i);
                    normalLines[i * 2 + 1] = creator.path.GetPoint (i) + creator.path.localNormals[i] * globalDisplaySettings.normalsLength;
                }
                Handles.DrawLines (normalLines);
            }
        }

        void ProcessBezierPathInput (Event e) {
            // Find which handle mouse is over. Start by looking at previous handle index first, as most likely to still be closest to mouse
            int previousMouseOverHandleIndex = (mouseOverHandleIndex == -1) ? 0 : mouseOverHandleIndex;
            mouseOverHandleIndex = -1;
            for (int i = 0; i < bezierPath.NumPoints; i += 3) {

                int handleIndex = (previousMouseOverHandleIndex + i) % bezierPath.NumPoints;
                float handleRadius = GetHandleDiameter (globalDisplaySettings.anchorSize * data.bezierHandleScale, bezierPath[handleIndex]) / 2f;
                Vector3 pos = MathUtility.TransformPoint (bezierPath[handleIndex], creator.transform, bezierPath.Space);
                float dst = HandleUtility.DistanceToCircle (pos, handleRadius);
                if (dst == 0) {
                    mouseOverHandleIndex = handleIndex;
                    break;
                }
            }

            // Shift-left click (when mouse not over a handle) to split or add segment
            if (mouseOverHandleIndex == -1) {
                if (e.type == EventType.MouseDown && e.button == 0 && e.shift) {
                    UpdatePathMouseInfo ();
                    // Insert point along selected segment
                    if (selectedSegmentIndex != -1 && selectedSegmentIndex < bezierPath.NumSegments) {
                        Vector3 newPathPoint = pathMouseInfo.closestWorldPointToMouse;
                        newPathPoint = MathUtility.InverseTransformPoint (newPathPoint, creator.transform, bezierPath.Space);
                        Undo.RecordObject (creator, "Split segment");
                        bezierPath.SplitSegment (newPathPoint, selectedSegmentIndex, pathMouseInfo.timeOnBezierSegment);
                    }
                    // If path is not a closed loop, add new point on to the end of the path
                    else if (!bezierPath.IsClosed)
                    {
                        // If control/command are held down, the point gets pre-pended, so we want to check distance
                        // to the endpoint we are adding to
                        var pointIdx = e.control || e.command ? 0 : bezierPath.NumPoints - 1;
                        // insert new point at same dst from scene camera as the point that comes before it (for a 3d path)
                        var endPointLocal = bezierPath[pointIdx];
                        var endPointGlobal =
                            MathUtility.TransformPoint(endPointLocal, creator.transform, bezierPath.Space);
                        var distanceCameraToEndpoint = (Camera.current.transform.position - endPointGlobal).magnitude;
                        var newPointGlobal = 
                            MouseUtility.GetMouseWorldPosition (bezierPath.Space, distanceCameraToEndpoint);
                        var newPointLocal = 
                            MathUtility.InverseTransformPoint (newPointGlobal, creator.transform, bezierPath.Space);

                        Undo.RecordObject (creator, "Add segment");
                        if (e.control || e.command) {
                            bezierPath.AddSegmentToStart (newPointLocal);
                        } else {
                            bezierPath.AddSegmentToEnd (newPointLocal);
                        }

                    }

                }
            }

            // Control click or backspace/delete to remove point
            if (e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete || ((e.control || e.command) && e.type == EventType.MouseDown && e.button == 0)) {

                if (mouseOverHandleIndex != -1) {
                    Undo.RecordObject (creator, "Delete segment");
                    bezierPath.DeleteSegment (mouseOverHandleIndex);
                    if (mouseOverHandleIndex == handleIndexToDisplayAsTransform) {
                        handleIndexToDisplayAsTransform = -1;
                    }
                    mouseOverHandleIndex = -1;
                    Repaint ();
                }
            }

            // Holding shift and moving mouse (but mouse not over a handle/dragging a handle)
            if (draggingHandleIndex == -1 && mouseOverHandleIndex == -1) {
                bool shiftDown = e.shift && !shiftLastFrame;
                if (shiftDown || ((e.type == EventType.MouseMove || e.type == EventType.MouseDrag) && e.shift)) {

                    UpdatePathMouseInfo ();

                    if (pathMouseInfo.mouseDstToLine < segmentSelectDistanceThreshold) {
                        if (pathMouseInfo.closestSegmentIndex != selectedSegmentIndex) {
                            selectedSegmentIndex = pathMouseInfo.closestSegmentIndex;
                            HandleUtility.Repaint ();
                        }
                    } else {
                        selectedSegmentIndex = -1;
                        HandleUtility.Repaint ();
                    }

                }
            }

            shiftLastFrame = e.shift;

        }

        void DrawBezierPathSceneEditor () {

            bool displayControlPoints = data.displayControlPoints && (bezierPath.ControlPointMode != BezierPath.ControlMode.Automatic || !globalDisplaySettings.hideAutoControls);
            Bounds bounds = bezierPath.CalculateBoundsWithTransform (creator.transform);

            if (Event.current.type == EventType.Repaint) {
                for (int i = 0; i < bezierPath.NumSegments; i++) {
                    Vector3[] points = bezierPath.GetPointsInSegment (i);
                    for (int j = 0; j < points.Length; j++) {
                        points[j] = MathUtility.TransformPoint (points[j], creator.transform, bezierPath.Space);
                    }

                    if (data.showPerSegmentBounds) {
                        Bounds segmentBounds = CubicBezierUtility.CalculateSegmentBounds (points[0], points[1], points[2], points[3]);
                        Handles.color = globalDisplaySettings.segmentBounds;
                        Handles.DrawWireCube (segmentBounds.center, segmentBounds.size);
                    }

                    // Draw lines between control points
                    if (displayControlPoints) {
                        Handles.color = (bezierPath.ControlPointMode == BezierPath.ControlMode.Automatic) ? globalDisplaySettings.handleDisabled : globalDisplaySettings.controlLine;
                        Handles.DrawLine (points[1], points[0]);
                        Handles.DrawLine (points[2], points[3]);
                    }

                    // Draw path
                    bool highlightSegment = (i == selectedSegmentIndex && Event.current.shift && draggingHandleIndex == -1 && mouseOverHandleIndex == -1);
                    Color segmentCol = (highlightSegment) ? globalDisplaySettings.highlightedPath : globalDisplaySettings.bezierPath;
                    Handles.DrawBezier (points[0], points[3], points[1], points[2], segmentCol, null, 2);
                }

                if (data.showPathBounds) {
                    Handles.color = globalDisplaySettings.bounds;
                    Handles.DrawWireCube (bounds.center, bounds.size);
                }

                // Draw normals
                if (data.showNormals) {
                    if (!hasUpdatedNormalsVertexPath) {
                        normalsVertexPath = new VertexPath (bezierPath, creator.transform, normalsSpacing);
                        hasUpdatedNormalsVertexPath = true;
                    }

                    if (editingNormalsOld != data.showNormals) {
                        editingNormalsOld = data.showNormals;
                        Repaint ();
                    }

                    Vector3[] normalLines = new Vector3[normalsVertexPath.NumPoints * 2];
                    Handles.color = globalDisplaySettings.normals;
                    for (int i = 0; i < normalsVertexPath.NumPoints; i++) {
                        normalLines[i * 2] = normalsVertexPath.GetPoint (i);
                        normalLines[i * 2 + 1] = normalsVertexPath.GetPoint (i) + normalsVertexPath.GetNormal (i) * globalDisplaySettings.normalsLength;
                    }
                    Handles.DrawLines (normalLines);
                }
            }

            if (data.displayAnchorPoints) {
                for (int i = 0; i < bezierPath.NumPoints; i += 3) {
                    DrawHandle (i);
                }
            }
            if (displayControlPoints) {
                for (int i = 1; i < bezierPath.NumPoints - 1; i += 3) {
                    DrawHandle (i);
                    DrawHandle (i + 1);
                }
            }
        }

        void DrawHandle (int i) {
            Vector3 handlePosition = MathUtility.TransformPoint (bezierPath[i], creator.transform, bezierPath.Space);

            float anchorHandleSize = GetHandleDiameter (globalDisplaySettings.anchorSize * data.bezierHandleScale, bezierPath[i]);
            float controlHandleSize = GetHandleDiameter (globalDisplaySettings.controlSize * data.bezierHandleScale, bezierPath[i]);

            bool isAnchorPoint = i % 3 == 0;
            bool isInteractive = isAnchorPoint || bezierPath.ControlPointMode != BezierPath.ControlMode.Automatic;
            float handleSize = (isAnchorPoint) ? anchorHandleSize : controlHandleSize;
            bool doTransformHandle = i == handleIndexToDisplayAsTransform;

            PathHandle.HandleColours handleColours = (isAnchorPoint) ? splineAnchorColours : splineControlColours;
            if (i == handleIndexToDisplayAsTransform) {
                handleColours.defaultColour = (isAnchorPoint) ? globalDisplaySettings.anchorSelected : globalDisplaySettings.controlSelected;
            }
            var cap = capFunctions[(isAnchorPoint) ? globalDisplaySettings.anchorShape : globalDisplaySettings.controlShape];
            PathHandle.HandleInputType handleInputType;
            handlePosition = PathHandle.DrawHandle (handlePosition, bezierPath.Space, isInteractive, handleSize, cap, handleColours, out handleInputType, i);

            if (doTransformHandle) {
                // Show normals rotate tool 
                if (data.showNormals && Tools.current == Tool.Rotate && isAnchorPoint && bezierPath.Space == PathSpace.xyz) {
                    Handles.color = handlesStartCol;

                    int attachedControlIndex = (i == bezierPath.NumPoints - 1) ? i - 1 : i + 1;
                    Vector3 dir = (bezierPath[attachedControlIndex] - handlePosition).normalized;
                    float handleRotOffset = (360 + bezierPath.GlobalNormalsAngle) % 360;
                    anchorAngleHandle.radius = handleSize * 3;
                    anchorAngleHandle.angle = handleRotOffset + bezierPath.GetAnchorNormalAngle (i / 3);
                    Vector3 handleDirection = Vector3.Cross (dir, Vector3.up);
                    Matrix4x4 handleMatrix = Matrix4x4.TRS (
                        handlePosition,
                        Quaternion.LookRotation (handleDirection, dir),
                        Vector3.one
                    );

                    using (new Handles.DrawingScope (handleMatrix)) {
                        // draw the handle
                        EditorGUI.BeginChangeCheck ();
                        anchorAngleHandle.DrawHandle ();
                        if (EditorGUI.EndChangeCheck ()) {
                            Undo.RecordObject (creator, "Set angle");
                            bezierPath.SetAnchorNormalAngle (i / 3, anchorAngleHandle.angle - handleRotOffset);
                        }
                    }

                } else {
                    handlePosition = Handles.DoPositionHandle (handlePosition, Quaternion.identity);
                }

            }

            switch (handleInputType) {
                case PathHandle.HandleInputType.LMBDrag:
                    draggingHandleIndex = i;
                    handleIndexToDisplayAsTransform = -1;
                    Repaint ();
                    break;
                case PathHandle.HandleInputType.LMBRelease:
                    draggingHandleIndex = -1;
                    handleIndexToDisplayAsTransform = -1;
                    Repaint ();
                    break;
                case PathHandle.HandleInputType.LMBClick:
                    draggingHandleIndex = -1;
                    if (Event.current.shift) {
                        handleIndexToDisplayAsTransform = -1; // disable move tool if new point added
                    } else {
                        if (handleIndexToDisplayAsTransform == i) {
                            handleIndexToDisplayAsTransform = -1; // disable move tool if clicking on point under move tool
                        } else {
                            handleIndexToDisplayAsTransform = i;
                        }
                    }
                    Repaint ();
                    break;
                case PathHandle.HandleInputType.LMBPress:
                    if (handleIndexToDisplayAsTransform != i) {
                        handleIndexToDisplayAsTransform = -1;
                        Repaint ();
                    }
                    break;
            }

            Vector3 localHandlePosition = MathUtility.InverseTransformPoint (handlePosition, creator.transform, bezierPath.Space);

            if (bezierPath[i] != localHandlePosition) {
                Undo.RecordObject (creator, "Move point");
                bezierPath.MovePoint (i, localHandlePosition);

            }

        }

        #endregion

        #region Internal methods

        void OnDisable () {
            Tools.hidden = false;
        }

        void OnEnable () {
            creator = (PathCreator) target;
            bool in2DEditorMode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
            creator.InitializeEditorData (in2DEditorMode);

            data.bezierCreated -= ResetState;
            data.bezierCreated += ResetState;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            LoadDisplaySettings ();
            UpdateGlobalDisplaySettings ();
            ResetState ();
            SetTransformState (true);
        }

        void SetTransformState (bool initialize = false) {
            Transform t = creator.transform;
            if (!initialize) {
                if (transformPos != t.position || t.localScale != transformScale || t.rotation != transformRot) {
                    data.PathTransformed ();
                }
            }
            transformPos = t.position;
            transformScale = t.localScale;
            transformRot = t.rotation;
        }

        void OnUndoRedo () {
            hasUpdatedScreenSpaceLine = false;
            hasUpdatedNormalsVertexPath = false;
            selectedSegmentIndex = -1;

            Repaint ();
        }

        void TabChanged () {
            SceneView.RepaintAll ();
            RepaintUnfocusedSceneViews ();
        }

        void LoadDisplaySettings () {
            globalDisplaySettings = GlobalDisplaySettings.Load ();

            capFunctions = new Dictionary<GlobalDisplaySettings.HandleType, Handles.CapFunction> ();
            capFunctions.Add (GlobalDisplaySettings.HandleType.Circle, Handles.CylinderHandleCap);
            capFunctions.Add (GlobalDisplaySettings.HandleType.Sphere, Handles.SphereHandleCap);
            capFunctions.Add (GlobalDisplaySettings.HandleType.Square, Handles.CubeHandleCap);
        }

        void UpdateGlobalDisplaySettings () {
            var gds = globalDisplaySettings;
            splineAnchorColours = new PathHandle.HandleColours (gds.anchor, gds.anchorHighlighted, gds.anchorSelected, gds.handleDisabled);
            splineControlColours = new PathHandle.HandleColours (gds.control, gds.controlHighlighted, gds.controlSelected, gds.handleDisabled);

            anchorAngleHandle.fillColor = new Color (1, 1, 1, .05f);
            anchorAngleHandle.wireframeColor = Color.grey;
            anchorAngleHandle.radiusHandleColor = Color.clear;
            anchorAngleHandle.angleHandleColor = Color.white;
        }

        void ResetState () {
            selectedSegmentIndex = -1;
            draggingHandleIndex = -1;
            mouseOverHandleIndex = -1;
            handleIndexToDisplayAsTransform = -1;
            hasUpdatedScreenSpaceLine = false;
            hasUpdatedNormalsVertexPath = false;

            bezierPath.OnModified -= OnPathModifed;
            bezierPath.OnModified += OnPathModifed;

            SceneView.RepaintAll ();
            EditorApplication.QueuePlayerLoopUpdate ();
        }

        void OnPathModifed () {
            hasUpdatedScreenSpaceLine = false;
            hasUpdatedNormalsVertexPath = false;

            RepaintUnfocusedSceneViews ();
        }

        void RepaintUnfocusedSceneViews () {
            // If multiple scene views are open, repaint those which do not have focus.
            if (SceneView.sceneViews.Count > 1) {
                foreach (SceneView sv in SceneView.sceneViews) {
                    if (EditorWindow.focusedWindow != (EditorWindow) sv) {
                        sv.Repaint ();
                    }
                }
            }
        }

        void UpdatePathMouseInfo () {

            if (!hasUpdatedScreenSpaceLine || (screenSpaceLine != null && screenSpaceLine.TransformIsOutOfDate ())) {
                screenSpaceLine = new ScreenSpacePolyLine (bezierPath, creator.transform, screenPolylineMaxAngleError, screenPolylineMinVertexDst);
                hasUpdatedScreenSpaceLine = true;
            }
            pathMouseInfo = screenSpaceLine.CalculateMouseInfo ();
        }

        float GetHandleDiameter (float diameter, Vector3 handlePosition) {
            float scaledDiameter = diameter * constantHandleScale;
            if (data.keepConstantHandleSize) {
                scaledDiameter *= HandleUtility.GetHandleSize (handlePosition) * 2.5f;
            }
            return scaledDiameter;
        }

        BezierPath bezierPath {
            get {
                return data.bezierPath;
            }
        }

        PathCreatorData data {
            get {
                return creator.EditorData;
            }
        }

        bool editingNormals {
            get {
                return Tools.current == Tool.Rotate && handleIndexToDisplayAsTransform % 3 == 0 && bezierPath.Space == PathSpace.xyz;
            }
        }

        #endregion

    }

}