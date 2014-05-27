using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace EasySnap
{
	sealed class EasySnapEditorWindow : EditorWindow
	{
	    #region Helpers

	    public enum Axis : byte
	    {
	        None,
	        X,
	        Y,
	        Z,
	    }

	    public enum RoundType : byte
	    {
	        Floor,
	        Nearest,
	        Ceil,
	    }

	    #endregion

	    private EasySnapSettings settings = new EasySnapSettings();
	    private Axis axis;
	    private bool snappingOverride;
        private bool repaintScene;

	    private Texture2D gridSnappingTexEnabled;
	    private Texture2D gridSnappingTexDisabled;
	    private Texture2D visibilityTexEnabled;
	    private Texture2D visibilityTexDisabled;
	    private Texture2D settingsTexEnabled;


	    [MenuItem("Window/EasySnap")]
	    private static void CreateWindow()
	    {
	        EditorWindow.GetWindow<EasySnapEditorWindow>("Grid", false);
	    }

	    private void OnGUI()
	    {
	        minSize = new Vector2(settings.ButtonSize, settings.ButtonSize);

	        const float padding = 3;
	        var buttonWidth     = GUILayout.Width(settings.ButtonSize - padding * 2);
	        var buttonHeight    = GUILayout.Height(settings.ButtonSize - padding * 2);

	        if (GUILayout.Button((settings.IsPositionSnappingEnabled) ? gridSnappingTexEnabled : gridSnappingTexDisabled, buttonWidth, buttonHeight))
	            settings.IsPositionSnappingEnabled = !settings.IsPositionSnappingEnabled;

            if (GUILayout.Button((settings.DrawGrid) ? visibilityTexEnabled : visibilityTexDisabled, buttonWidth, buttonHeight))
                settings.DrawGrid = !settings.DrawGrid;

            if (GUILayout.Button(settingsTexEnabled, buttonWidth, buttonHeight))
                EasySnapSettingsEditorWindow.OpenWindow(settings);

            EditorGUILayout.LabelField("Grid");

	        float minorGrid = EditorGUILayout.FloatField(settings.MinorGridSize.x, buttonWidth);
	        if (minorGrid > 0)
	            settings.MinorGridSize.x = minorGrid;

            minorGrid = EditorGUILayout.FloatField(settings.MinorGridSize.y, buttonWidth);
            if (minorGrid > 0)
                settings.MinorGridSize.y = minorGrid;

            minorGrid = EditorGUILayout.FloatField(settings.MinorGridSize.z, buttonWidth);
            if (minorGrid > 0)
                settings.MinorGridSize.z = minorGrid;

            settings.SelectedUnitIndex = EditorGUILayout.Popup(settings.SelectedUnitIndex, settings.AvailableUnits);

            if (GUI.changed)
                repaintScene = true;

	        //settings.IsAngleSnappingEnabled = EditorGUILayout.Toggle("Snap Angle", settings.IsAngleSnappingEnabled);
	        //float angleSnap = EditorGUILayout.FloatField("Angle Increment", settings.AngleSnapIncrement);

	        //if (angleSnap > 0)
	        //    settings.AngleSnapIncrement = angleSnap;	        

            GUILayout.Space(10);

            if (GUILayout.Button("Align", buttonWidth))
                RealignSelection();

            if (GUI.changed)
                SceneView.RepaintAll();
	    }

	    private void HandleSnapping()
	    {
	        int selectionCount = Selection.transforms.Length;

	        if (EditorApplication.isPlaying ||
	            selectionCount <= 0)
	            return;

            if (Tools.current == Tool.Move && settings.IsPositionSnappingEnabled)
            {
                Vector3 startPos = Tools.handlePosition;
                Vector3 endPos = Handles.PositionHandle(Tools.handlePosition, Tools.handleRotation);
                Vector3 diff = endPos - startPos;

                if (!snappingOverride)
                    diff = Snap(diff, settings.MinorGridSize * settings.SelectedUnit.UnityUnits, RoundType.Nearest);

                if (!Mathf.Approximately(diff.magnitude, 0.0f))
                {
                    Undo.RecordObjects(Selection.transforms, "Move");

                    foreach (var t in Selection.transforms)
                        t.position = t.position + diff;
                }
            }
	    }

	    private void OnEnable()
	    {
	        LoadTextures();

	        SceneView.onSceneGUIDelegate += OnSceneGUI;
	        settings.Load();
	    }

	    private void LoadTextures()
	    {
			gridSnappingTexEnabled = Resources.Load("EasySnapIcons/GridSnapping_Enabled") as Texture2D;
			gridSnappingTexDisabled = Resources.Load("EasySnapIcons/GridSnapping_Disabled") as Texture2D;

			visibilityTexEnabled = Resources.Load("EasySnapIcons/Visibility_Enabled") as Texture2D;
			visibilityTexDisabled = Resources.Load("EasySnapIcons/Visibility_Disabled") as Texture2D;

	        settingsTexEnabled = Resources.Load("EasySnapIcons/Settings_Enabled") as Texture2D;
	    }

	    private void OnDisable()
	    {
	        SceneView.onSceneGUIDelegate -= OnSceneGUI;
	        settings.Save();
	    }

	    private void OnSceneGUI(SceneView sceneView)
	    {
            HandleSnapping();

            if (repaintScene)
            {
                sceneView.Repaint();
                repaintScene = false;
            }

	        KeyCheck();

	        //if (snappingOverride)
	        //    sceneView.Focus();

	        if (!sceneView.camera.isOrthoGraphic)
	            return;

	        Vector3 facingDirection = sceneView.camera.transform.forward;

	        axis =  (Mathf.Approximately(Mathf.Abs(facingDirection.x), 1)) ? Axis.X :
	                (Mathf.Approximately(Mathf.Abs(facingDirection.y), 1)) ? Axis.Y :
	                (Mathf.Approximately(Mathf.Abs(facingDirection.z), 1)) ? Axis.Z :
	                Axis.None;

	        Color majorColour = (axis == Axis.X) ? settings.xColourMajor : (axis == Axis.Y) ? settings.yColourMajor : (axis == Axis.Z) ? settings.zColourMajor : Color.white;
	        Color minorColour = (axis == Axis.X) ? settings.xColourMinor : (axis == Axis.Y) ? settings.yColourMinor : (axis == Axis.Z) ? settings.zColourMinor : Color.white;

	        if (settings.DrawGrid && axis != Axis.None)
	        {
	            DrawGrid(sceneView.camera, settings.MinorGridSize, minorColour);

	            if(settings.IsMajorGridEnabled)
	                DrawGrid(sceneView.camera, Mul(settings.MajorGridSize, settings.MinorGridSize), majorColour, true);
	        }
	    }

	    private void KeyCheck()
	    {
	        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == settings.SnapOverrideKey)
	        {
	            snappingOverride = true;
	            Event.current.Use();
	        }
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == settings.SnapOverrideKey)
            {
                snappingOverride = false;
                Event.current.Use();
            }

	        if (Event.current.type == EventType.KeyUp)
	        {
	            if (Event.current.keyCode == settings.PositionSnappingToggleKey)
	            {
	                settings.IsPositionSnappingEnabled = !settings.IsPositionSnappingEnabled;
	                Event.current.Use();
	                Repaint();
	            }
	            if (Event.current.keyCode == settings.VisibilityToggleKey)
	            {
	                settings.DrawGrid = !settings.DrawGrid;
	                Repaint();
	                Event.current.Use();
	            }
                if (Event.current.keyCode == settings.ReAlignPositionKey)
                    RealignSelection();
	        }
	    }

        private void RealignSelection()
        {
            if (Selection.transforms.Length <= 0)
                return;

            Undo.RecordObjects(Selection.transforms, "Re-align to Grid");

            bool snapPosition = Tools.current != Tool.Rotate;
            //bool snapRotation = Tools.current != Tool.Move;

            Vector3 snapPos = Snap(Selection.activeTransform.position, settings.MinorGridSize * settings.SelectedUnit.UnityUnits, RoundType.Nearest);
            Vector3 diff = snapPos - Selection.activeTransform.position;

            foreach (var t in Selection.transforms)
            {
                if (snapPosition)
                {
                    if (Tools.pivotRotation == PivotRotation.Global)
                        t.position += diff;
                    else
                        t.position = Snap(t.position, settings.MinorGridSize * settings.SelectedUnit.UnityUnits, RoundType.Nearest);
                }

                //if (snapRotation)
                //    t.eulerAngles = Snap(t.eulerAngles, settings.AngleSnapIncrement, RoundType.Nearest);
            }
        }

        private Vector3 Mul(Vector3 a, Vector3 b)
        {
            return new Vector3( a.x * b.x,
                                a.y * b.y,
                                a.z * b.z);
        }

        private Vector3 Mask(Vector3 vector, Vector3 mask)
        {
            if (Mathf.Approximately(Mathf.Abs(mask.x), 1))
                return new Vector3(vector.x, 0, 0);
            if (Mathf.Approximately(Mathf.Abs(mask.y), 1))
                return new Vector3(0, vector.y, 0);
            if (Mathf.Approximately(Mathf.Abs(mask.z), 1))
                return new Vector3(0, 0, vector.z);

            return Vector3.zero;
        }

        private float LargestComponent(Vector3 vector)
        {
            return Mathf.Max(vector.x, vector.y, vector.z);
        }

	    private void DrawGrid(Camera camera, Vector3 incrementSize, Color colour, bool isMajorGrid = false)
	    {
            if (incrementSize.x <= 0 || incrementSize.y <= 0 || incrementSize.z <= 0)
                return;

            incrementSize *= settings.SelectedUnit.UnityUnits;

            Vector3 down = camera.cameraToWorldMatrix.MultiplyVector(new Vector3(0, -1));
            Vector3 right = camera.cameraToWorldMatrix.MultiplyVector(new Vector3(1, 0));

	        Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0, 1, 1));
	        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, 1));
	        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, 1));
	        Vector3 bottomRight = camera.ViewportToWorldPoint(new Vector3(1, 0, 1));

            topLeft = Snap(topLeft, incrementSize) + Mul(-down + -right, incrementSize);
            bottomLeft = Snap(bottomLeft, incrementSize) + Mul(down + -right, incrementSize);
            topRight = Snap(topRight, incrementSize) + Mul(-down + right, incrementSize);
            bottomRight = Snap(bottomRight, incrementSize) + Mul(down + right, incrementSize);

	        float x = camera.orthographicSize * 2 * Screen.width / Screen.height * 1.15f;
	        float y = camera.orthographicSize * 2;

            float xIncrement = LargestComponent(Mask(incrementSize, right));
            float yIncrement = LargestComponent(Mask(incrementSize, down));

	        int xCount = (int)(Mathf.Ceil(x / xIncrement));
	        int yCount = (int)(Mathf.Ceil(y / yIncrement));

	        Handles.color = colour;
            
	        for (int i = 0; i <= xCount; i++)
	        {
	            Vector3 a = topLeft + i * Mul(incrementSize, right);
                Vector3 b = bottomLeft + i * Mul(incrementSize, right);

	            Handles.DrawLine(a, b);
	        }

            for (int j = 0; j <= yCount; j++)
            {
                Vector3 a = topLeft + j * Mul(down, incrementSize);
                Vector3 b = topRight + j * Mul(down, incrementSize);

                Handles.DrawLine(a, b);
            }

	        if (settings.DrawAxes)
	        {
	            Handles.color = Color.red;
	            Handles.DrawLine(new Vector3(topLeft.x, 0, 0), new Vector3(topRight.x, 0, 0));
	            Handles.color = Color.green;
	            Handles.DrawLine(new Vector3(0, topLeft.y, 0), new Vector3(0, bottomLeft.y, 0));
	            Handles.color = Color.blue;
	            Handles.DrawLine(new Vector3(0, 0, topLeft.z), new Vector3(0, 0, bottomRight.z));
	        }
	    }

	    //private void SnapAngle(float incrementSize)
	    //{
	    //    foreach (var t in Selection.transforms)
	    //        t.transform.eulerAngles = Snap(t.transform.eulerAngles, incrementSize, RoundType.Nearest);
	    //}

	    private Vector3 Snap(Vector3 input, Vector3 increment, RoundType roundType = RoundType.Floor)
	    {
	        return new Vector3( Snap(input.x, increment.x, roundType),
	                            Snap(input.y, increment.y, roundType),
	                            Snap(input.z, increment.z, roundType));
	    }

	    private float Snap(float input, float increment, RoundType roundType = RoundType.Floor)
	    {
            float output = input;

	        switch (roundType)
	        {
	            case RoundType.Floor:
	                output = Mathf.Floor(input / increment) * increment;
                    break;
	            case RoundType.Nearest:
	                output = Mathf.Round(input / increment) * increment;
                    break;
	            case RoundType.Ceil:
	                output = Mathf.Ceil(input / increment) * increment;
                    break;
	            default:
	                throw new NotImplementedException("Does not support RoundType: " + roundType);
	        }

            return output;
	    }
	}
}