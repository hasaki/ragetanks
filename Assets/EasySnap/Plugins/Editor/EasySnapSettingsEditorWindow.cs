using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace EasySnap
{
	sealed class EasySnapSettingsEditorWindow : EditorWindow
	{
	    private const int MinButtonSize = 5;

	    private EasySnapSettings settings;
	    private bool showColourOptions;
	    private bool showKeyOptions;
        private bool showUnitOptions;
        private Vector2 unitScrollPosition;


	    public static void OpenWindow(EasySnapSettings settings)
	    {
	        var window = EditorWindow.GetWindow<EasySnapSettingsEditorWindow>(true, "EasySnap Settings", true);
	        window.settings = settings;
	    }

	    private void OnGUI()
	    {
	        settings.IsMajorGridEnabled = EditorGUILayout.Toggle("Enable Major Grid", settings.IsMajorGridEnabled);
	        settings.MajorGridSize = EditorGUILayout.Vector3Field("Major Grid Size", settings.MajorGridSize);

	        //settings.AlignToCenterOfCell = EditorGUILayout.Toggle("Snap to Cell Center", settings.AlignToCenterOfCell);
	        settings.DrawAxes = EditorGUILayout.Toggle("Draw Axes", settings.DrawAxes);

	        float buttonSize = EditorGUILayout.FloatField("Button Size", settings.ButtonSize);

	        if (buttonSize > EasySnapSettingsEditorWindow.MinButtonSize)
	            settings.ButtonSize = buttonSize;

	        EditorGUILayout.Space();
	        showKeyOptions = EditorGUILayout.Foldout(showKeyOptions, "Keys");

	        if(showKeyOptions)
	        {
	            KeyCode code = KeyCode.None;

	            if (KeyCodeField("SnappingOverride", "Snapping Override Key", settings.SnapOverrideKey, out code))
	                settings.SnapOverrideKey = code;
                if (KeyCodeField("RealignPosition", "Re-align Position Key", settings.ReAlignPositionKey, out code))
                    settings.ReAlignPositionKey = code;
	            if (KeyCodeField("PositionSnappingToggle", "Snapping Toggle Key", settings.PositionSnappingToggleKey, out code))
	                settings.PositionSnappingToggleKey = code;
	            if (KeyCodeField("VisibilityToggle", "Visibility Toggle Key", settings.VisibilityToggleKey, out code))
	                settings.VisibilityToggleKey = code;
	        }

	        EditorGUILayout.Space();
	        showColourOptions = EditorGUILayout.Foldout(showColourOptions, "Colours");

	        if (showColourOptions)
	        {
	            settings.xColourMinor = EditorGUILayout.ColorField("Minor X", settings.xColourMinor);
	            settings.xColourMajor = EditorGUILayout.ColorField("Major X", settings.xColourMajor);

	            EditorGUILayout.Space();

	            settings.yColourMinor = EditorGUILayout.ColorField("Minor Y", settings.yColourMinor);
	            settings.yColourMajor = EditorGUILayout.ColorField("Major Y", settings.yColourMajor);

	            EditorGUILayout.Space();

	            settings.zColourMinor = EditorGUILayout.ColorField("Minor Z", settings.zColourMinor);
	            settings.zColourMajor = EditorGUILayout.ColorField("Major Z", settings.zColourMajor);
	        }

            EditorGUILayout.Space();
            showUnitOptions = EditorGUILayout.Foldout(showUnitOptions, "Units");

            if (showUnitOptions)
            {
                int toDelete = -1;
                GUI.changed = false;

                unitScrollPosition = GUILayout.BeginScrollView(unitScrollPosition);

                for (int i = 0; i < settings.Units.Count; i++)
                {
                    EasySnapUnit unit = settings.Units.Values.ElementAt(i);

                    GUI.enabled = (i != 0);

                    EditorGUILayout.BeginHorizontal();

                    string name = EditorGUILayout.TextField(unit.UnitName);
                    if (name != unit.UnitName)
                        settings.RenameUnit(unit.UnitName, name);

                    float value = EditorGUILayout.FloatField(unit.Value);

                    if (value > 0)
                        unit.Value = value;

                    string[] unitNames = GetUnitConversionNames(unit.UnitName);
                    int currentSelection = IndexOf(ref unitNames, unit.RelativeUnit);

                    int relative = EditorGUILayout.Popup(currentSelection, unitNames);

                    if (relative != currentSelection)
                    {
                        unit.RelativeUnit = unitNames[relative];
                        unit.Value = settings.Convert(unit.Value, unitNames[currentSelection], unit.RelativeUnit);
                    }

                    if (GUILayout.Button("Delete"))
                        toDelete = i;

                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.EndScrollView();

                GUI.enabled = true;

                if (GUILayout.Button("New.."))
                {
                    EasySnapUnit unit = new EasySnapUnit();
                    unit.UnitName = GetUniqueUnitName();
                    unit.Value = 1;
                    unit.RelativeUnit = EasySnapUnit.DefaultUnitName;

                    settings.Units[unit.UnitName] = unit;

                    GUI.changed = true;
                }

                if (toDelete >= 0)
                    settings.RemoveUnit(toDelete);

                if(GUI.changed)
                    settings.RecalculateUnits();
            }

	        EditorGUILayout.Space();

	        if (GUILayout.Button("Restore Defaults"))
	            settings.RestoreDefaults();

	        if (GUI.changed)
	            SceneView.RepaintAll();
	    }

        private string[] GetUnitConversionNames(string currentUnitName)
        {
            List<string> names = new List<string>(settings.Units.Count - 1);

            foreach (var u in settings.Units.Values)
                if (u.UnitName != currentUnitName && !HasCircularReference(currentUnitName, u.UnitName))
                    names.Add(u.UnitName);

            return names.ToArray();
        }

        private bool HasCircularReference(string unit, string relativeUnit)
        {
            List<string> relativeUnits = new List<string>();
            string u = relativeUnit;

            while (!string.IsNullOrEmpty(u))
            {
                relativeUnits.Add(settings.Units[u].RelativeUnit);
                u = settings.Units[u].RelativeUnit;
            }

            return relativeUnits.Contains(unit);
        }

        private string GetUniqueUnitName()
        {
            int nextID = 1;

            foreach (var unit in settings.Units.Values)
            {
                if (!unit.UnitName.StartsWith("New Unit"))
                {
                    if(nextID < 2)
                        nextID = 2;

                    continue;
                }

                string end = unit.UnitName.Substring(8, unit.UnitName.Length - 8);

                if (string.IsNullOrEmpty(end))
                    continue;

                int ID = 0;

                if (!int.TryParse(end.Trim(), out ID))
                    continue;

                if (ID >= nextID)
                    nextID = ID + 1;
            }

            return (nextID < 2) ? "New Unit" : "New Unit " + nextID;
        }

        private int IndexOf(ref string[] array, string value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                    return i;
            }

            return -1;
        }

	    private bool KeyCodeField(string fieldName, string text, KeyCode currentKeyCode, out KeyCode newKeyCode)
	    {
	        bool hasNewKeyCode = false;
	        newKeyCode = KeyCode.None;

	        GUI.SetNextControlName(fieldName);

	        EditorGUILayout.BeginHorizontal();
	        EditorGUILayout.TextField(text, currentKeyCode.ToString());

	        if (GUILayout.Button("Unbind"))
	        {
	            newKeyCode = KeyCode.None;
	            hasNewKeyCode = true;
	        }

	        EditorGUILayout.EndHorizontal();

	        

	        if (GUI.GetNameOfFocusedControl() == fieldName)
	        {
	            if (Event.current.type == EventType.KeyUp)
	            {
	                newKeyCode = Event.current.keyCode;
	                hasNewKeyCode = true;

	                GUIUtility.keyboardControl = 0;
	                Repaint();
	            }
	        }

	        return hasNewKeyCode;
	    }
	}
}
