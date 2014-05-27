using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace EasySnap
{
    [Serializable]
    sealed class EasySnapUnit
    {
        public const string DefaultUnitName = "Unity";

        public string UnitName;
        public float Value;
        public string RelativeUnit;

        public float UnityUnits { get; internal set; }


        public void Save(string key)
        {
            EditorPrefs.SetString(key + "_NAME", UnitName);
            EditorPrefs.SetFloat(key + "_VALUE", Value);
            EditorPrefs.SetString(key + "_RELATIVE", RelativeUnit);
        }

        public void Delete(string key)
        {
            EditorPrefs.DeleteKey(key + "_NAME");
            EditorPrefs.DeleteKey(key + "_VALUE");
            EditorPrefs.DeleteKey(key + "_RELATIVE");
        }

        public static EasySnapUnit Load(string key)
        {
            EasySnapUnit unit = new EasySnapUnit();

            unit.UnitName = EditorPrefs.GetString(key + "_NAME");
            unit.Value = EditorPrefs.GetFloat(key + "_VALUE");
            unit.RelativeUnit = EditorPrefs.GetString(key + "_RELATIVE");

            return unit;
        }
    }
}
