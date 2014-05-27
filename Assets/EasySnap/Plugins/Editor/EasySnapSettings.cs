using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace EasySnap
{
	[Serializable]
	sealed class EasySnapSettings
	{
	    #region Preference Keys

	    private const string XColourMinorKey            = "EasySnap_xColourMinor";
	    private const string YColourMinorKey            = "EasySnap_yColourMinor";
	    private const string ZColourMinorKey            = "EasySnap_zColourMinor";
	    private const string XColourMajorKey            = "EasySnap_xColourMajor";
	    private const string YColourMajorKey            = "EasySnap_yColourMajor";
	    private const string ZColourMajorKey            = "EasySnap_zColourMajor";
	    private const string MajorGridEnabledKey        = "EasySnap_IsMajorGridEnabled";
	    private const string MinorGridSizeKey           = "EasySnap_MinorGridSize";
	    private const string MajorGridSizeKey           = "EasySnap_MajorGridSize";
	    private const string PositionSnappingEnabledKey = "EasySnap_IsPositionSnappingEnabled";
	    private const string DrawGridKey                = "EasySnap_DrawGrid";
	    //private const string AngleSnappingEnabledKey    = "EasySnap_IsAngleSnappingEnabled";
	    //private const string AngleSnapIncrementKey      = "EasySnap_AngleSnapIncrement";
	    private const string SnapOverrideKeyCodeKey     = "EasySnap_SnapOverrideKey";
	    private const string DrawAxesKey                = "EasySnap_DrawAxisCrossSection";
	    private const string ButtonSizeKey              = "EasySnap_ButtonSize";
	    private const string PositionSnapToggleKey      = "EasySnap_PositionSnapToggleKey";
	    private const string GridVisibilityToggleKey    = "EasySnap_GridVisibilityToggleKey";
        private const string ReAlignPositionKeyCodeKey  = "EasySnap_ReAlignPositionKey";
	    //private const string AlignToCenterKey           = "EasySnap_AlignToCenterKey";
        private const string UnitsKey                   = "EasySnap_Units";

	    #endregion

	    public Color xColourMinor;
	    public Color yColourMinor;
	    public Color zColourMinor;
	    public Color xColourMajor;
	    public Color yColourMajor;
	    public Color zColourMajor;
	    public bool IsMajorGridEnabled;
	    public Vector3 MajorGridSize;
	    public Vector3 MinorGridSize;
	    public bool IsPositionSnappingEnabled;
	    //public bool IsAngleSnappingEnabled;
	    //public float AngleSnapIncrement;
	    public bool DrawGrid;
	    public KeyCode SnapOverrideKey;
	    public bool DrawAxes;
	    public float ButtonSize;
	    public KeyCode PositionSnappingToggleKey;
	    public KeyCode VisibilityToggleKey;
        public KeyCode ReAlignPositionKey;
	    //public bool AlignToCenterOfCell;
        public readonly Dictionary<string, EasySnapUnit> Units = new Dictionary<string, EasySnapUnit>();
        public EasySnapUnit SelectedUnit { get { return Units[AvailableUnits[SelectedUnitIndex]]; } }
        public int SelectedUnitIndex;
        public string[] AvailableUnits;


	    public EasySnapSettings()
	    {
	        RestoreDefaults();
	    }

	    public void RestoreDefaults()
	    {
	        xColourMinor = new Color(0.5f, 0.0f, 0.0f);
	        yColourMinor = new Color(0.0f, 0.5f, 0.0f);
	        zColourMinor = new Color(0.0f, 0.0f, 0.5f);
	        xColourMajor = new Color(0.8f, 0.0f, 0.0f);
	        yColourMajor = new Color(0.0f, 0.8f, 0.0f);
	        zColourMajor = new Color(0.0f, 0.0f, 0.8f);

	        IsMajorGridEnabled = true;
            MinorGridSize = new Vector3(1, 1, 1);
            MajorGridSize = new Vector3(5, 5, 5);
	        IsPositionSnappingEnabled = true;
	        //IsAngleSnappingEnabled = true;
	        //AngleSnapIncrement = 15;
	        DrawGrid = true;
	        SnapOverrideKey = KeyCode.Space;
            ReAlignPositionKey = KeyCode.Return;
	        DrawAxes = true;
	        ButtonSize = 58;
	        PositionSnappingToggleKey = KeyCode.None;
	        VisibilityToggleKey = KeyCode.None;
	        //AlignToCenterOfCell = false;

            Units.Clear();
            Units[EasySnapUnit.DefaultUnitName] = new EasySnapUnit() { UnitName = EasySnapUnit.DefaultUnitName, Value = 1.0f, RelativeUnit = null };
            AddUnit("Meters", 1, "Unity");
            AddUnit("Centimeters", 0.01f, "Meters");
            AddUnit("Inches", 2.54f, "Centimeters");
            AddUnit("Feet", 12, "Inches");
            AddUnit("Yards", 3, "Feet");
            SelectedUnitIndex = 0;

            RecalculateUnits();
	    }

        private void AddUnit(string name, float value, string relative)
        {
            EasySnapUnit unit = new EasySnapUnit();
            unit.UnitName = name;
            unit.Value = value;
            unit.RelativeUnit = relative;

            Units[name] = unit;
        }

	    public void Save()
	    {
	        EditorPrefs.SetBool("EasySnap_Settings", true);

	        SetColour(EasySnapSettings.XColourMinorKey, xColourMinor);
	        SetColour(EasySnapSettings.YColourMinorKey, yColourMinor);
	        SetColour(EasySnapSettings.ZColourMinorKey, zColourMinor);

	        SetColour(EasySnapSettings.XColourMajorKey, xColourMajor);
	        SetColour(EasySnapSettings.YColourMajorKey, yColourMajor);
	        SetColour(EasySnapSettings.ZColourMajorKey, zColourMajor);

	        EditorPrefs.SetBool(EasySnapSettings.MajorGridEnabledKey, IsMajorGridEnabled);
            SetVector(EasySnapSettings.MajorGridSizeKey, MajorGridSize);
	        EditorPrefs.SetBool(EasySnapSettings.PositionSnappingEnabledKey, IsPositionSnappingEnabled);
            SetVector(EasySnapSettings.MinorGridSizeKey, MinorGridSize);
	        EditorPrefs.SetBool(EasySnapSettings.DrawGridKey, DrawGrid);
	        //EditorPrefs.SetBool(EasySnapSettings.AngleSnappingEnabledKey, IsAngleSnappingEnabled);
	        //EditorPrefs.SetFloat(EasySnapSettings.AngleSnapIncrementKey, AngleSnapIncrement);
	        EditorPrefs.SetInt(EasySnapSettings.SnapOverrideKeyCodeKey, (int)SnapOverrideKey);
	        EditorPrefs.SetBool(EasySnapSettings.DrawAxesKey, DrawAxes);
	        EditorPrefs.SetFloat(EasySnapSettings.ButtonSizeKey, ButtonSize);
	        EditorPrefs.SetInt(EasySnapSettings.PositionSnapToggleKey, (int)PositionSnappingToggleKey);
	        EditorPrefs.SetInt(EasySnapSettings.GridVisibilityToggleKey, (int)VisibilityToggleKey);
	        //EditorPrefs.SetBool(EasySnapSettings.AlignToCenterKey, AlignToCenterOfCell);
            EditorPrefs.SetInt(EasySnapSettings.ReAlignPositionKeyCodeKey, (int)ReAlignPositionKey);

            SaveUnits();
	    }

	    public void Load()
	    {
	        if (!EditorPrefs.HasKey("EasySnap_Settings"))
	        {
	            Save();
	            return;
	        }

	        xColourMinor = GetColour(EasySnapSettings.XColourMinorKey);
	        yColourMinor = GetColour(EasySnapSettings.YColourMinorKey);
	        zColourMinor = GetColour(EasySnapSettings.ZColourMinorKey);

	        xColourMajor = GetColour(EasySnapSettings.XColourMajorKey);
	        yColourMajor = GetColour(EasySnapSettings.YColourMajorKey);
	        zColourMajor = GetColour(EasySnapSettings.ZColourMajorKey);

	        IsMajorGridEnabled = EditorPrefs.GetBool(EasySnapSettings.MajorGridEnabledKey);
            MajorGridSize = GetVector(EasySnapSettings.MajorGridSizeKey);
	        IsPositionSnappingEnabled = EditorPrefs.GetBool(EasySnapSettings.PositionSnappingEnabledKey);
            MinorGridSize = GetVector(EasySnapSettings.MinorGridSizeKey);
	        DrawGrid = EditorPrefs.GetBool(EasySnapSettings.DrawGridKey);
	        //IsAngleSnappingEnabled = EditorPrefs.GetBool(EasySnapSettings.AngleSnappingEnabledKey);
	        //AngleSnapIncrement = EditorPrefs.GetFloat(EasySnapSettings.AngleSnapIncrementKey);
	        SnapOverrideKey = (KeyCode)EditorPrefs.GetInt(EasySnapSettings.SnapOverrideKeyCodeKey);
	        DrawAxes = EditorPrefs.GetBool(EasySnapSettings.DrawAxesKey);
	        ButtonSize = EditorPrefs.GetFloat(EasySnapSettings.ButtonSizeKey);
	        PositionSnappingToggleKey = (KeyCode)EditorPrefs.GetInt(EasySnapSettings.PositionSnapToggleKey);
	        VisibilityToggleKey = (KeyCode)EditorPrefs.GetInt(EasySnapSettings.GridVisibilityToggleKey);
	        //AlignToCenterOfCell = EditorPrefs.GetBool(EasySnapSettings.AlignToCenterKey);
            ReAlignPositionKey = (KeyCode)EditorPrefs.GetInt(EasySnapSettings.ReAlignPositionKeyCodeKey);

            Units.Clear();
            LoadUnits();

            RecalculateUnits();
	    }

        public void RecalculateUnits()
        {
            foreach (var u in Units.Values)
                u.UnityUnits = 0;

            foreach (var u in Units.Values)
                RecalculateUnit(u);

            AvailableUnits = Units.Keys.ToArray();
        }

        private void RecalculateUnit(EasySnapUnit unit)
        {
            EasySnapUnit relative = (string.IsNullOrEmpty(unit.RelativeUnit)) ? null : Units[unit.RelativeUnit];

            if (relative != null && relative.UnityUnits == 0)
                RecalculateUnit(relative);

            if (unit.UnitName == EasySnapUnit.DefaultUnitName)
            {
                unit.UnityUnits = 1;
                return;
            }

            float value = unit.Value;
            string from = unit.RelativeUnit;
            string to = Units[unit.RelativeUnit].RelativeUnit;

            while (!string.IsNullOrEmpty(to))
            {
                value = Convert(value, from, to);
                from = to;
                to = Units[to].RelativeUnit;
            }

            unit.UnityUnits = value;
        }

        public float Convert(float value, string from, string to)
        {
            if (string.IsNullOrEmpty(from))
                from = EasySnapUnit.DefaultUnitName;
            if (string.IsNullOrEmpty(to))
                to = EasySnapUnit.DefaultUnitName;

            float unityUnits = value *  Units[from].UnityUnits;
            return unityUnits / Units[to].UnityUnits;
        }

        public bool RenameUnit(string currentName, string newName)
        {
            if (Units.ContainsKey(newName))
                return false;

            EasySnapUnit unit = Units[currentName];
            Units.Remove(currentName);
            Units[newName] = unit;

            unit.UnitName = newName;

            foreach (var u in Units.Values)
                if (u.RelativeUnit == currentName)
                    u.RelativeUnit = newName;

            return true;
        }

        public void RemoveUnit(int index)
        {
            string unitName = Units.Keys.ElementAt(index);

            Units.Values.ElementAt(index).Delete(EasySnapSettings.UnitsKey + "_" + index);
            Units.Remove(unitName);

            if (SelectedUnitIndex == index)
                SelectedUnitIndex = 0;

            foreach (var u in Units.Values)
                if (u.RelativeUnit == unitName)
                {
                    u.RelativeUnit = "Unity";
                    u.Value = u.UnityUnits;
                }
        }

        private void SaveUnits()
        {
            EditorPrefs.SetInt(EasySnapSettings.UnitsKey, Units.Count);

            for (int i = 0; i < Units.Count; i++)
                Units.Values.ElementAt(i).Save(EasySnapSettings.UnitsKey + "_" + i);

            EditorPrefs.SetInt(EasySnapSettings.UnitsKey + "_CURRENT", SelectedUnitIndex);
        }

        private void LoadUnits()
        {
            int count = EditorPrefs.GetInt(EasySnapSettings.UnitsKey);

            Units.Clear();
            for (int i = 0; i < count; i++)
            {
                EasySnapUnit unit = EasySnapUnit.Load(EasySnapSettings.UnitsKey + "_" + i);
                Units[unit.UnitName] = unit;
            }

            SelectedUnitIndex = EditorPrefs.GetInt(EasySnapSettings.UnitsKey + "_CURRENT");
        }

        private void SetVector(string key, Vector3 vector)
        {
            EditorPrefs.SetFloat(key + "_X", vector.x);
            EditorPrefs.SetFloat(key + "_Y", vector.y);
            EditorPrefs.SetFloat(key + "_Z", vector.z);
        }

        private Vector3 GetVector(string key)
        {
            Vector3 vector = new Vector3();

            vector.x = EditorPrefs.GetFloat(key + "_X");
            vector.y = EditorPrefs.GetFloat(key + "_Y");
            vector.z = EditorPrefs.GetFloat(key + "_Z");

            return vector;
        }

	    private void SetColour(string key, Color colour)
	    {
	        EditorPrefs.SetFloat(key + "_R", colour.r);
	        EditorPrefs.SetFloat(key + "_G", colour.g);
	        EditorPrefs.SetFloat(key + "_B", colour.b);
            EditorPrefs.SetFloat(key + "_A", colour.a);
	    }

	    private Color GetColour(string key)
	    {
	        Color colour = new Color();

	        colour.r = EditorPrefs.GetFloat(key + "_R");
	        colour.g = EditorPrefs.GetFloat(key + "_G");
	        colour.b = EditorPrefs.GetFloat(key + "_B");
            colour.a = EditorPrefs.GetFloat(key + "_A", 1);

	        return colour;
	    }
	}
}
