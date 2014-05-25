/*
------------------------------------------------
    Hierarchy2 for Unity3d by VietLabs
------------------------------------------------
    version : 1.3.7
    release : 10 May 2013
    require : Unity3d 4.3+
    website : http://vietlabs.net/hierarchy2
--------------------------------------------------

Powerful extension to add the most demanding features
to Hierarchy panel packed in a single, lightweight,
concise and commented C# source code that fully 
integrated into Unity Editor 

--------------------------------------------------
*/

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace vietlabs {

[InitializeOnLoad]
class Hierarchy2
{
    //------------------------------ CONFIG -----------------------------------------

    // Highlight Hierarchy items
    internal static bool HLEnabled				= true; 
    internal static bool HLFull					= false;

	internal static bool AllowRenameLockedGO	= true;
    internal static bool AllowDragLockedGO      = false;

	internal static float IconsOffset			= 0f;
    
	internal static bool AllowAltShortcuts		= true;
	internal static bool AllowShiftShortcuts	= true;
	internal static bool AllowOtherShortcuts	= true;

	//Ignore (don't show greenbars) for scripts in these folders
	internal static string[] IgnoreScriptPaths	= {
		".dll",
		"Daikon Forge",
		"FlipbookGames",
		"iTween",
		"NGUI",
		"PlayMaker",
		"TK2DROOT",
		"VietLabs"
	};

    internal static readonly Color[] HLColors = { Color.red, Color.yellow, Color.green, Color.blue, Color.cyan, Color.magenta};
	

    /*[MenuItem("Window/Hierarchy2/Toggle Highlight")]
    static void ToggleHighlight() { HLEnabled = !HLEnabled; }

    [MenuItem("Window/Hierarchy2/Toggle Highlight Full")]
    static void ToggleHighlightMode() { HLEnabled = true; HLFull = !HLFull; }*/

	[MenuItem("Window/Hierarchy2/Reset")] 
	static void Reset() {
		Hierarchy2Api.RootGOList.ForEach(rootGO => {
			rootGO.hideFlags = 0;
			rootGO.ForeachChild(child => {
				child.hideFlags = 0;
			});
		});
	}

    //---------------------------- ROOT CACHE ---------------------------------------

    static Hierarchy2() {
		EditorApplication.hierarchyWindowChanged	+= Hierarchy2Api.UpdateRoot;
        EditorApplication.hierarchyWindowItemOnGUI	+= HierarchyItemCB;
	    EditorApplication.playmodeStateChanged		+= OnPlayModeChanged;
		
        Undo.undoRedoPerformed						+= () => {
			//TODO : narrow down & only perform on correct undo targets

			//BUGFIXED : Quick + dirty patch to force Hierarchy refresh to show correct children
			Hierarchy2Api.RootGOList.ForEach(rootGO => rootGO.ForeachChild(child => {
				child.ToggleFlag(HideFlags.NotEditable);
				child.ToggleFlag(HideFlags.NotEditable);
			}, true));

	        vlbEditor.HierarchyWindow.Repaint();
        };
    }
	static void OnPlayModeChanged() {
		if (!EditorApplication.isPlaying && !EditorApplication.isPaused) { //stop playing
			Hierarchy2Api.ltCamera = null;
			Hierarchy2Api.ltCameraInfo = null;
		}
	}

	//-------------------------------- CONTEXT ---------------------------------------

	internal static void Context_BuiltIn(GenericMenu menu, GameObject go, string category = "") {
		menu.Add(category + "Copy %C", () => {
			Selection.activeGameObject = go;
			Unsupported.CopyGameObjectsToPasteboard();
		});

		menu.Add(category + "Paste %V", () => {
			Selection.activeGameObject = go;
			Unsupported.PasteGameObjectsFromPasteboard();
		});

		menu.AddSep(category);
		menu.Add(category + "Rename _F2", () => {
			Selection.activeGameObject = go;
			go.Rename();
		});
		menu.Add(category + "Duplicate %D", () => {
			Selection.activeGameObject = go;
			Unsupported.DuplicateGameObjectsUsingPasteboard();
		});

		menu.Add(category + "Delete _Delete", () => {
			Selection.activeGameObject = go;
			Unsupported.DeleteGameObjectSelection();
		});
	}
	internal static void Context_Basic(GenericMenu menu, GameObject go, string category = "Edit/") {
		//basic tools
		menu.Add(category + "Lock _L",				() => go.SetSmartLock(false, false), go.IsLock());
		menu.Add(category + "Visible _A , V",	    () => go.ToggleActive(false),		 go.activeSelf); 
		menu.Add(category + "Combine Children _C",	() => go.ToggleCombine(),			 go.IsCombined());

		//goto tools
        menu.AddSep(category);
		menu.Add(category + "Goto Parent _[",	() => go.transform.PingParent());
		menu.Add(category + "Goto Child _]",	() => go.transform.PingChild());
		menu.Add(category + "Goto Sibling _\\",	() => go.transform.PingSibling());

		//transform tools
		Context_Transform(menu, go, category);
	}
	internal static void Context_Transform(GenericMenu menu, GameObject go, string category = "Transform/") {
		var lcPos = go.transform.localPosition != Vector3.zero;
		var lcScl = go.transform.localScale != Vector3.one;
		var lcRot = go.transform.localRotation != Quaternion.identity;

		var cnt = (lcPos ? 1 : 0) + (lcScl ? 1 : 0) + (lcRot ? 1 : 0);
        if (cnt > 0) menu.AddSep(category);

		menu.AddIf(lcPos, category + "Reset Position #P", () => Hierarchy2Api.ResetLocalPosition(go));
		menu.AddIf(lcRot, category + "Reset Rotation #R", () => Hierarchy2Api.ResetLocalRotation(go));
		menu.AddIf(lcScl, category + "Reset Scale #S",	() => Hierarchy2Api.ResetLocalScale(go));

        if (cnt > 0) menu.AddSep(category);
		menu.AddIf(cnt > 0, category + "Reset Transform #T", () => Hierarchy2Api.ResetTransform(go));
	}
	internal static void Context_Special(GenericMenu menu, GameObject go, string category = "Edit/") {
		// Prefab specific
		//var isPrefab = PrefabUtility.GetPrefabObject(go) != null;

		//Debug.Log("--->"+PrefabUtility.GetPrefabObject(go) +":"+ isPrefab);

		var t = PrefabUtility.GetPrefabType(go); 

		if (t != PrefabType.None) {
            menu.AddSep(category);
			menu.Add(category + "Select Prefab", (t == PrefabType.MissingPrefabInstance) ? (Action)null : go.SelectPrefab);
			menu.Add(category + "Break Prefab #B", () => go.BreakPrefab());
		}
		
		// Camera specific
		var cam = go.GetComponent<Camera>();
		if (cam != null) {
            menu.AddSep(category);
			menu.Add(category + ((Hierarchy2Api.ltCamera != null) ? "Stop ":"") +"Look through #L", () => Hierarchy2Api.ToggleLookThroughCamera(cam));
			menu.Add(category + "Capture SceneView #C", () => Hierarchy2Api.CameraCaptureSceneView(cam));
		}
	}

	internal static void Context_Components(GenericMenu menu, GameObject go) {
		var listTemp = go.GetComponents<Component>().ToList();
		var scripts = new List<MonoBehaviour>();
		var compList = new List<Component>();
		var missing = 0;

		foreach (var c in listTemp) {
			if (c is Transform) continue;

			if (c == null) {
				missing++;
				continue;
			}

			if (c is MonoBehaviour) {
				scripts.Add((MonoBehaviour)c);
				continue;
			}

			compList.Add(c);
		}

		var total = scripts.Count + compList.Count + missing;
		var prefix = "Components [" + (total) + "]/";

		if (scripts.Count > 0) {
			foreach (var script in scripts) {
				var behaviour = script;
				var title = prefix + behaviour.GetTitle() +"/";
				menu.Add(title + "Reveal", script.Ping);
				menu.Add(title + "Edit", script.OpenScript);
                menu.AddSep(title);
				menu.Add(title + "Isolate", () => Hierarchy2Api.Isolate_Component(behaviour));
			}
		}

		if (compList.Count > 0) {
            if (scripts.Count > 0) menu.AddSep(prefix);

			foreach (var c in compList) {
				var comp = c;
				menu.Add(prefix + comp.GetTitle(), () => Hierarchy2Api.Isolate_Component(comp));
			}
		}

		if (missing > 0) {
			if (compList.Count + scripts.Count>0) {
                menu.AddSep(prefix);
				menu.Add(prefix +"+"+ missing + " Missing Behaviour" + (missing > 1 ? "s" : ""), null);
			} else {
				menu.Add("+"+missing + " Missing Behaviour" + (missing > 1 ? "s" : ""), null);
			}
		}
	}
	internal static void Context_Create(GenericMenu menu, GameObject go, string category = "Create/") {
		menu.Add("New Empty Child #N", () => Hierarchy2Api.CreateEmptyChild(go));
		menu.Add("New Empty Sibling", () => Hierarchy2Api.CreateEmptySibling(go));
		menu.Add(category + "Parent", () => Hierarchy2Api.CreateParentAtMyPosition(go));
		menu.Add(category + "Parent at Origin", () => Hierarchy2Api.CreateParentAtOrigin(go));

        menu.AddSep(category);

		var list = new[] { "Quad", "Plane", "Cube", "Cylinder", "Capsule", "Sphere" };
		var key = new[] { " #1", " #2", " #3", " #4", " #5", " #6" };
		var types = new[] {
			PrimitiveType.Quad,
			PrimitiveType.Cube,
			PrimitiveType.Sphere,
			PrimitiveType.Plane,
			PrimitiveType.Cylinder,
			PrimitiveType.Capsule
		};

		for (var i = 0; i < types.Length; i++)
		{
			var type = types[i];
			var name = list[i];

			menu.Add(category + name + key[i], () =>
			{
				Selection.activeGameObject = go;
				vlbEditor.NewPrimity(
					type,
					"New".GetNewName(go.transform, name),
					"New" + name, go.transform
				);//.transform.Ping();
			});

			//menu.Add("Create Child Primity / " + name, () => {
			//	Selection.activeGameObject = go;
			//	Hierarchy2Utils.NewPrimity(
			//		type,
			//		go.name.GetNewName(go.transform, "_child" + name),
			//		"New" + name, go.transform
			//	).transform.Ping();
			//});
		}
	}
	internal static void Context_Isolate(GenericMenu menu, GameObject go, string category = "Isolate/") {
		menu.Add(category + "Missing Behaviours &M", ()=>Hierarchy2Api.Isolate_MissingBehaviours());
		menu.Add(category + "Has Behaviour &B", () => Hierarchy2Api.Isolate_ObjectsHasScript());
		if (Selection.instanceIDs.Length > 1) menu.Add(category + "Selected Objects &S", () => Hierarchy2Api.Isolate_SelectedObjects());
        menu.AddSep(category);
		menu.Add(category + "Locked Objects &L", () => Hierarchy2Api.Isolate_LockedObjects());
		menu.Add(category + "InActive Objects &I", () => Hierarchy2Api.Isolate_InActiveObjects());
		menu.Add(category + "Combined Objects &Y", () => Hierarchy2Api.Isolate_CombinedObjects());
        menu.AddSep(category);

		var type	= "UnityEditorInternal.InternalEditorUtility".GetTypeByName();
		var layers	= (string[])(type.GetProperty("layers", BindingFlags.Static | BindingFlags.Public).GetValue(null,null));
		var tags	= (string[])(type.GetProperty("tags", BindingFlags.Static | BindingFlags.Public).GetValue(null,null));

		for (var i = 0; i < layers.Length; i++) {
			var idx = i;
			menu.Add(category + "Layer/" + layers[idx], () => Hierarchy2Api.Isolate_Layer(layers[idx]));
		}

		for (var i = 0; i < tags.Length; i++) {
			var idx = i;
			menu.Add(category + "Tag/" + tags[idx], () => Hierarchy2Api.Isolate_Tag(tags[idx]));
		}
	}

	//-------------------------------- SHORTCUTS ---------------------------------------

	static void Key_Handler(Event evt, Transform t) {
		var go = t.gameObject;

		switch (evt.keyCode) { //TO PARENT AND BACK
			case KeyCode.Comma			:
			case KeyCode.LeftBracket	: t.PingParent(true); break;
			case KeyCode.Period			:
			case KeyCode.RightBracket	: t.PingChild(true); break;
			case KeyCode.Backslash		: t.PingSibling(true); break;

			case KeyCode.L	: 
				go.SetSmartLock(false, false);
				Event.current.Use();
				vlbEditor.InspectorWindow.Repaint();
			break;

			case KeyCode.A	: 
			case KeyCode.V	:
				Event.current.Use();
				go.ToggleActive(true);
			break;

			case KeyCode.C	: 
				Event.current.Use();
				go.ToggleCombine();
				Selection.activeGameObject = t.gameObject;
				//vlbEditor.HierarchyWindow.Focus();
			break;
		}
	}
	static void ShiftKey_Handler(Event evt, GameObject go) {

		var dict = new Dictionary<KeyCode, PrimitiveType> {
			{KeyCode.Alpha1,		PrimitiveType.Quad},
			{KeyCode.Alpha2,		PrimitiveType.Plane},
			{KeyCode.Alpha3,		PrimitiveType.Cube},
			{KeyCode.Alpha4,		PrimitiveType.Cylinder},
			{KeyCode.Alpha5,		PrimitiveType.Capsule},
			{KeyCode.Alpha6,		PrimitiveType.Sphere}
		};

		if (dict.ContainsKey(evt.keyCode)) {
			go.RevealChildrenInHierarchy();

			var primity = dict[evt.keyCode];
			vlbEditor.NewPrimity(
				primity,
				"New".GetNewName(go.transform, primity.ToString()),
				"New" + primity + "Child",
				go.transform
			).transform.PingAndUseEvent();
			return;
		}

		switch (evt.keyCode) {
			case KeyCode.N: Hierarchy2Api.CreateEmptyChild(go, true); break;

			case KeyCode.P: Hierarchy2Api.ResetLocalPosition(go); break;
			case KeyCode.R: Hierarchy2Api.ResetLocalRotation(go); break;
			case KeyCode.S: Hierarchy2Api.ResetLocalScale(go); break;
			case KeyCode.T: Hierarchy2Api.ResetTransform(go); break;
			case KeyCode.B: go.BreakPrefab(); break;
		}

		//if (evt.type == EventType.used) {
			//Selection.activeGameObject = null;
			//Hierarchy2Utils.RefreshInspector();
			//Selection.activeGameObject = go;
		//}
	}
	static void AltKey_Handler(Event evt, GameObject go) {//commands

		switch (evt.keyCode) {//TO PARENT AND BACK
			case KeyCode.M	: Hierarchy2Api.Isolate_MissingBehaviours(true); break;
			case KeyCode.B	: Hierarchy2Api.Isolate_ObjectsHasScript(true); break;
			case KeyCode.S	: Hierarchy2Api.Isolate_SelectedObjects(true); break;

			case KeyCode.L	: Hierarchy2Api.Isolate_LockedObjects(true); break;
			case KeyCode.I	: 
			case KeyCode.V	: Hierarchy2Api.Isolate_InActiveObjects(true); break;

			case KeyCode.Y	: Hierarchy2Api.Isolate_CombinedObjects(true); break;
		}
	}
	
    //-------------------------------- FUNCTIONS ---------------------------------------

    internal static void EditLock(Rect r, GameObject go) {
        const HideFlags flag = HideFlags.NotEditable;
        var isSet	= go.GetFlag(flag);

		GUI.DrawTexture(r, vlbGUISkin.icoLock(isSet));

	    var lm = r.GetLeftMouseDown();
	    if (lm != -1) {
			Event.current.Use();

			switch ((vlbMouseFlags)lm) {
				case vlbMouseFlags.None		: go.SetSmartLock(false, false); break; //auto-lock children
				case vlbMouseFlags.Ctrl		:
					if (Selection.gameObjects.Contains(go) && Selection.gameObjects.Length>1) {
						go.ToggleLock();
					} else {
						go.SetSmartLock(false, true);
					}
				break;
				case vlbMouseFlags.Alt		:
					if (Selection.gameObjects.Contains(go) && Selection.gameObjects.Length > 1) {
						go.SetSmartLock(true, false);
					} else {
						go.ToggleSiblingLock();
					}
				break;
			}

			vlbEditor.InspectorWindow.Repaint();
	    }

		var rm = r.GetRightMouseDown();
	    if (rm == 0) {//right-Click
			Event.current.Use();
		    var menu = new GenericMenu();

		    menu.Add("Toggle Lock",		go.InvertLock);
			//menu.Add("Toggle Lock Children",	go.InvertLock);
            menu.AddSep("");
		    menu.Add(Hierarchy2Api.RootGOList.HasFlag(flag),
					"Recursive Unlock",
					"Recursive Lock",
					has => Hierarchy2Api.RecursiveLock(!has));
			menu.ShowAsContext();
	    }
    }
	internal static void EditActive(Rect r, GameObject go)
    {
        var isSet	= go.activeSelf;
	    GUI.DrawTexture(r, vlbGUISkin.icoEye(isSet));

		var lm = r.GetLeftMouseDown();
		
		if (lm != -1) {
			Event.current.Use();

			switch ((vlbMouseFlags)lm) {
				case vlbMouseFlags.None : go.ToggleActive(false); break;
				case vlbMouseFlags.Ctrl :
					if (Selection.gameObjects.Contains(go) && Selection.gameObjects.Length > 1) {
						//go.ToggleActive(false);
						//Toggle active me only
					} else {
						go.SetActiveChildren(!isSet, false);	
					}
				break;

				case vlbMouseFlags.Alt	:
					if (Selection.gameObjects.Contains(go) && Selection.gameObjects.Length > 1)
					{
						go.ToggleActive(true);
					}
					else
					{
						go.SetActiveSibling(isSet, false);
					}
					
				break;
			}
		}

		var rm = r.GetRightMouseDown();
		if (rm == 0)
		{
			Event.current.Use();
			var menu = new GenericMenu();
			if (go.HasChild()) {
				menu.Add(go.HasActiveChild(), "Hide children", "Show children", has => go.SetActiveChildren(!has, false));
				menu.AddIf(go.HasGrandChild(), go.HasActiveChild(), "Hide all children", "Show all children", has => go.SetActiveChildren(!has, false));
			}
			menu.AddIf(go.HasSibling(Hierarchy2Api.RootGOList), go.HasActiveSibling(Hierarchy2Api.RootGOList), "Hide siblings", "Show siblings", (has) => go.ForeachSibling(Hierarchy2Api.RootGOList, item => item.SetActive(!has)));
            if (menu.GetItemCount() > 0) menu.AddSep(null);
			if (go.transform.parent != null) {
				menu.Add(go.HasActiveParent(), "Hide parents", "Show parents", (has) => go.SetActiveParents(!has));
			}
			menu.Add(Hierarchy2Api.RootGOList.HasActive(), "Recursive Hide", "Recursive Show", (has) => Hierarchy2Api.RootGOList.SetActive(!has, true));
			menu.ShowAsContext();
		}
    }
    internal static void EditCombine(Rect r, GameObject go)
    {
        const HideFlags flag = HideFlags.HideInHierarchy;
        var count = go.transform.childCount;
        if (count == 0) return; //don't display childCount if GO does not contains child

		//calculate size needed for display childCount text
        var isSet = go.HasFlagChild(flag);
        var w = (count < 10 ? 14 : count < 100 ? 18 : count < 1000 ? 28 : 33) + (isSet ? 6 : 0);
        r.x += r.width - w;
        r.width = w;
        var countStr = count < 1000 ? (string.Empty + count) : "999+";

		if (isSet) {
			GUI.Label(r, countStr, EditorStyles.miniButtonMid);
		} else {
			GUI.Label(r, countStr);
		}

		var lm = r.GetLeftMouseDown();
		
		if (lm != -1) {
			Event.current.Use();
			switch ((vlbMouseFlags)lm) {
				case vlbMouseFlags.None : go.ToggleCombine(); break;
				case vlbMouseFlags.Ctrl : go.ToggleCombineChildren(); break;
				case vlbMouseFlags.Alt	: go.SetCombineSibling(!isSet); break;
			}
		}

		var rm = r.GetRightMouseDown();
	    if (rm == 0) {
		    Event.current.Use();
		    var menu = new GenericMenu();
		    menu.Add(Hierarchy2Api.RootGOList.HasFlagChild(flag),
				"Recursive Expand",
				"Recursive Combine",
				has => Hierarchy2Api.RecursiveCombine(!has)
			);
			menu.ShowAsContext();
	    }
    }
    internal static void EditLevelStrip(Rect r, GameObject go) {
        var c   = go.ParentCount() - (HLFull ? 1 : 0);

        if (c < 0) return; //don't highlight level 0 on full mode

		if (go.numScriptMissing() > 0 || go.numScript() > 0){
            var color = go.numScriptMissing() > 0 ? Color.red : Color.green;
			GUI.DrawTexture(r.AddX(-26f).SetWidth(36f), vlbGUISkin.GetColor(color, EditorGUIUtility.isProSkin ? 0.2f : 0.3f, 0f));
		}
		
        if (HLFull){
            r.width = r.x + r.width;
            r.x = 0;    
        } else{
            var w = 18 * 4 + c * 5f;
            r.x = r.x + r.width - w;
            r.width = 5f;
        }

		GUI.DrawTexture(r, vlbGUISkin.GetColor(
            HLColors[c % HLColors.Length], HLFull ? 0.05f : 0.5f, HLFull ? 0f : 0.3f
        ));
    }

    //-------------------------------- GUI ---------------------------------------
	
	private static Camera LookThroughCam;
	private static GameObject RenameUnlock;

    static void HierarchyItemCB(int instanceID, Rect selectionRect)
    {
        var ofocus = EditorWindow.focusedWindow;

		var evt = Event.current;
        var r	= selectionRect.AddX(selectionRect.width).SetWidth(16f).SetHeight(16f);
        var go	= EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        var hasFocus = EditorWindow.focusedWindow != null &&
                       EditorWindow.focusedWindow.name == "UnityEditor.HierarchyWindow";

		if (vlbGUI.renameGO != null && go.GetInstanceID() == vlbGUI.renameGO.GetInstanceID()) {
			go.Rename(); 
		}

		if (HLEnabled) EditLevelStrip(r.AddX(-8f - IconsOffset), go);
		
        //fixed for deactivated root GO can not be found by FindObjectOfType
		Hierarchy2Api.CheckRoot(go);
		if (AllowRenameLockedGO && go == Selection.activeGameObject && hasFocus) {
			if (go.GetFlag(HideFlags.NotEditable) && vlbEditor.IsRenaming()) {
				RenameUnlock = go;
				go.SetFlag(HideFlags.NotEditable, false);
			} else if (RenameUnlock != null && !vlbEditor.IsRenaming()) {
				RenameUnlock.SetFlag(HideFlags.NotEditable, true);
				RenameUnlock = null;
			}
	    }

		EditLock(r.AddX(-16f - IconsOffset), go);
		EditActive(r.AddX(-32f - IconsOffset), go);
		EditCombine(r.AddX(-50f - IconsOffset), go);

        if (hasFocus && Selection.activeGameObject != null && evt.type == EventType.keyUp && !vlbEditor.IsRenaming()) //evt.shift && 
	    {
		    var t = Selection.activeGameObject.transform;
		    if (Event.current.control) {
				//ignore Ctrl 
			} else if (Event.current.alt) {
				if (AllowAltShortcuts && !Event.current.shift) {
					AltKey_Handler(evt, go);
				}
		    } else if (Event.current.shift) {
				if (AllowShiftShortcuts) ShiftKey_Handler(evt, t.gameObject);
		    } else if (AllowOtherShortcuts){
				Key_Handler(evt, t);
			}
	    }

        if (!AllowDragLockedGO && go.GetFlag(HideFlags.NotEditable) && selectionRect.Contains(Event.current.mousePosition)) {
			if (selectionRect.GetLeftMouseDown() == 0) {
				if (!Selection.instanceIDs.Contains(go.GetInstanceID())) {
					Selection.activeGameObject = go;
				}
				Event.current.Use();
			}
			if (evt.type == EventType.mouseDrag) Event.current.Use();
		}

	    var rect = selectionRect.MoveLeft(-selectionRect.x).AddWidth(-55f);
		//GUI.DrawTexture(rect, vlbGUISkin.GetColor(Color.red, 0.1f, 0f));

		int rightMouse = rect.GetRightMouseDown();

		if (rightMouse == -1) return;

	    if (rightMouse == 0) {
		    evt.Use();
			//DefaultContext(new GenericMenu(), go).ShowAsContext(); 
			var menu = new GenericMenu(); 
			Context_BuiltIn(menu, go);
			Context_Special(menu, go, "");
            menu.AddSep("");

			Context_Basic(menu, go, "Edit/");
			Context_Isolate(menu, go);
			Context_Components(menu, go);
            menu.AddSep("");
			Context_Create(menu, go);

			menu.ShowAsContext();
	    }

        if (ofocus != EditorWindow.focusedWindow) {
            Debug.LogWarning("Focus changed :: " + ofocus + ":::::>>>" + EditorWindow.focusedWindow);
        }
    }
}

internal static class Hierarchy2Utils
{
	static BindingFlags _flags;
	static internal SceneView current {
		get {
			if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(SceneView)) {
				return (SceneView)EditorWindow.focusedWindow;
			}
			_flags = BindingFlags.Instance | BindingFlags.NonPublic;
			return SceneView.lastActiveSceneView ?? (SceneView)SceneView.sceneViews[0];
		}
	}
	static internal Camera SceneCamera { get { return current.camera; } }
	static public void Refresh() {//hacky way to force SceneView increase drawing frame
		Transform t = Selection.activeTransform;
		if (t == null) {
			t = (Camera.main != null) ? Camera.main.transform : new GameObject("$t3mp$").transform;
		}

		Vector3 op = t.position;
		t.position += new Vector3(1, 1, 1); //make some dirty
		t.position = op;

		if (t.name == "$t3mp$") MonoBehaviour.DestroyImmediate(t.gameObject, true);
	}

	static T GetAnimT<T>(string name) {
		if (current == null) return default(T);
		object animT = typeof(SceneView).GetField(name, _flags).GetValue(current);
		return (T)animT.GetType().GetField("m_Value", _flags).GetValue(animT);
	}
	static void SetAnimT<T>(string name, T value) {
		if (current == null) return;
		object animT = typeof(SceneView).GetField(name, _flags).GetValue(current);
		var info = animT.GetType().GetField("m_Value", _flags);

		animT.GetType().GetMethod("BeginAnimating", _flags).Invoke(animT, new object[] { value, (T)info.GetValue(animT) });
	}

	static public Vector3 m_Position {
		get { return GetAnimT<Vector3>("m_Position"); }
		set { SetAnimT<Vector3>("m_Position", value.FixNaN()); }
	}
	static public Quaternion m_Rotation {
		get { return GetAnimT<Quaternion>("m_Rotation"); }
		set { SetAnimT<Quaternion>("m_Rotation", value); }
	}

	static public float cameraDistance {
		get { return (float) current.GetProperty("cameraDistance"); }
		}
	static public bool orthographic {
		get { return current.camera.orthographic; }
		set {
			//current.camera.orthographic = value;
			SetAnimT<Single>("m_Ortho", value ? 1f : 0f);
		}
	}
	static public float m_Size {
		get { return GetAnimT<float>("m_Size"); }
		set { SetAnimT<float>("m_Size", (Single.IsInfinity(value) || (Single.IsNaN(value)) || (value == 0)) ? 100f : value); }
	}

    //-------------------------------- FLAG ----------------------------

    internal static void SetDeepFlag(this GameObject go, HideFlags flag, bool value, bool includeMe = true, bool recursive = true)
    {
        if (includeMe) go.SetFlag(flag, value);
        foreach (Transform t in go.transform)
        {	
            if (recursive)
            {
                SetDeepFlag(t.gameObject, flag, value);
            }
            else
            {
                t.gameObject.SetFlag(flag, value);
            }
        }
    }
    internal static bool HasFlagChild(this GameObject go, HideFlags flag)
    {
	    return go.GetChildren().Any(item => item.GetFlag(flag));
    }
    internal static bool HasFlagChild(this List<GameObject> list, HideFlags flag) {
	    //var has = false;

        /*foreach (var child in list)
        {
            child.ForeachChild2(child2 =>
            {
                has = child2.GetFlag(flag);
                return !has;
            });
            if (has) break;
        }*/

	    return list.Any(item => item.HasFlagChild(flag));
    }
	internal static bool HasFlag(this List<GameObject> list, HideFlags flag)
    {
        var hasActive = false;
        foreach (var go in list)
        {
            hasActive = go.GetFlag(flag);
            if (hasActive) break;
        }
        return hasActive;
    }
    internal static void SetDeepFlag(this List<GameObject> list, bool value, HideFlags flag, bool includeMe)
    {
        foreach (var go in list)
        {
            go.SetDeepFlag(flag, value, includeMe);
        }
    }

    //-------------------------------- ACTIVE ----------------------------

    internal static bool HasChild(this GameObject go)
    {
        return go.transform.childCount > 0;
    }
    internal static bool HasActiveChild(this GameObject go)
    {
        var has = false;
        go.ForeachChild2(child => {
            has = child.activeSelf;
            return !has;
        });
        return has;
    }
    internal static bool HasGrandChild(this GameObject go)
    {
        var has = false;
        go.ForeachChild2(child =>
        {
            has = child.transform.childCount > 0;
            return !has;
        });
        return has;
    }
    
    internal static bool HasSibling(this GameObject go, List<GameObject> rootGOList)
    {
        var p = go.transform.parent;
        return p != null ? (p.childCount > 1) : (rootGOList.Count > 1);
    }
    internal static bool HasActiveSibling(this GameObject go, List<GameObject> rootGOList)
    {
        var has = false;
        go.ForeachSibling2(rootGOList, sibl =>
        {
            has = sibl.activeSelf;
            return !has;
        });
        return has;
    }
    internal static bool HasActiveParent(this GameObject go)
    {
        var has = false;
        go.ForeachParent2(p =>
        {
            has = p.activeSelf;
            return !has;
        });
        return has;
    }
    
    internal static bool HasActive(this List<GameObject> list)
    {
        bool hasActive = false;
        foreach (var go in list)
        {
            hasActive = go.activeSelf;
            if (hasActive) break;
        }
        return hasActive;
    }
    internal static void SetActive(this List<GameObject> list, bool value, bool deep)
    {
        foreach (var go in list)
        {
            if (deep) go.SetActiveChildren(value, false);
            if (go.activeSelf != value) go.SetActive(value);
        }
    }

    //-------------------------------- FLAG ----------------------------





    //------------------------------- FOREACH --------------------------

    internal static int ParentCount(this GameObject go)
    {
        var p = go.transform.parent;
        var cnt = 0;

        while (p != null)
        {
            cnt++;
            p = p.parent;
        }
        return cnt;
    }
    internal static void ForeachSibling(this GameObject go, List<GameObject> rootList, Action<GameObject> func)
    {
        ForeachSibling2(go, rootList, (item) => { func(item); return true; });
    }
    internal static void ForeachSibling2(this GameObject go, List<GameObject> rootList, Func<GameObject, bool> func)
    {
        var p = go.transform.parent;

        if (p != null)
        {
            foreach (Transform t in go.transform.parent)
            {
                if (t.gameObject != go)
                {
                    if (!func(t.gameObject)) break;
                }
            }
        }
        else
        {
            foreach (var child in rootList)
            {
                if (child != go)
                {
                    if (!func(child)) break;
                }
            }
        }
    }
    internal static void ForeachParent(this GameObject go, Action<GameObject> func)
    {
        ForeachParent2(go, (item) => { func(item); return true; });
    }
    internal static void ForeachParent2(this GameObject go, Func<GameObject, bool> func)
    {
        var p = go.transform.parent;
        while (p != null)
        {
            if (!func(p.gameObject)) break;
            p = p.parent;
        }
    }
	internal static void ForeachSelected(this GameObject go, Action<GameObject, int> func) {
		var selected = Selection.objects;
		if (selected.Length == 0 || !selected.Contains(go) || (selected.Length == 1 && selected.Contains(go))) {
			func(go, -1);
			return;
		}

		var cnt = 0;
		foreach (var item in selected) {
			if (item is GameObject) func((GameObject)item, cnt++);
		}
	}

	internal static void ForeachChild<T>(this Transform p, Func<T, bool> action, bool deep = false) where T : Component { 
		foreach (Transform child in p) {
			var t = child.GetComponent<T>();
			if (deep) child.ForeachChild(action, true);

			if (t != null) {
				if (!action(t)) return; //stop if enough
			}
		}
	}
	internal static void ForeachChild2(this GameObject go, Func<GameObject, bool> action, bool deep = false) {
		go.transform.ForeachChild<Transform>(t => action(t.gameObject), deep);	
	}
	internal static void ForeachChild(this GameObject go, Action<GameObject> action, bool deep = false) {
		go.transform.ForeachChild<Transform>(t => {
			action(t.gameObject);
			return true;
		}, deep);
	}

    //------------------------------- MISC --------------------------

    internal static void SetSearchFilter(this EditorWindow window, string term){
        //var type    = Types.GetType("UnityEditor.SearchableEditorWindow", "UnityEditor");
        //var method = SearchableWindowType.GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        //method.Invoke(window, new object[] { term, SearchableEditorWindow.SearchMode.All, true});

		//Debug.Log("UnityEditor.SearchableEditorWindow".GetTypeByName() + ":::::::::::");

	    var sWindow = "UnityEditor.SearchableEditorWindow".GetTypeByName();
		window.Invoke(sWindow, "SetSearchFilter", null, new object[] { term, SearchableEditorWindow.SearchMode.All, true });
		window.SetField("m_HasSearchFilterFocus", true, null, sWindow);

		//var field = SearchableWindowType.GetField("m_HasSearchFilterFocus", BindingFlags.NonPublic | BindingFlags.Instance);
		//field.SetValue(window, true);
        EditorGUI.FocusTextInControl("SearchFilter"); 
        window.Repaint();
    }
    internal static void SetSearchFilter(this EditorWindow window, int[] instanceIDs, string title)
    {
		if (window == null) {
			//Reset();
			vlbEditor.ClearDefinitionCache();
			window = vlbEditor.HierarchyWindow;
		}

	    if (instanceIDs.Length == 0) {
			//Debug.Log("There are no " + title + " Game Object in the scene");
		    window.Invoke("SetSearchFilter", new object[]{"Hierarchy2tempfixemptysearch", SearchableEditorWindow.SearchMode.All, false});//(str, SearchableEditorWindow.SearchMode.All, false);
			window.SetSearchFilter("iso:" + title);
		    return; 
	    }
		
		var TBaseProjectWindow = "UnityEditor.BaseProjectWindow".GetTypeByName();
		var TFilteredHierarchy = "UnityEditor.FilteredHierarchy".GetTypeByName();

        window.SetSearchFilter("iso:" + title);

        var instIDsParams = new object[] { instanceIDs };
		var fh	= window.GetField("m_FilteredHierarchy", null, TBaseProjectWindow);
		var sf = (SearchFilter)fh.GetField("m_SearchFilter", null, TFilteredHierarchy);

        sf.ClearSearch();
        sf.referencingInstanceIDs = instanceIDs;
		fh.Invoke(TFilteredHierarchy, "SetResults", null, instIDsParams);

		var arr = (object[])fh.GetProperty("results", null, TFilteredHierarchy);//(FilteredHierarchyType.GetProperty("results").GetValue(fh, null));
        var list = new List<int>();

		//patch
	    var nMissing = 0;
        foreach (var t in arr) {
	        if (t == null) {
		        nMissing++;
		        continue;
	        }
	        var id = (int) t.GetField("instanceID");
	        if (!list.Contains(id)) list.Add(id);
        }

		if (nMissing>0) Debug.LogWarning("Filtered result may not be correct, missing "+ nMissing + " results, please help report it to unity3d@vietlab.net");
	    instanceIDs = list.ToArray();

		//reapply
		sf.ClearSearch();
		sf.referencingInstanceIDs = instanceIDs;
		fh.Invoke(TFilteredHierarchy, "SetResults", null, new object[] { instanceIDs });

        window.Repaint();
    }
	internal static void Ping(this Object obj)
	{
		EditorGUIUtility.PingObject(obj);

		if (obj is MonoBehaviour)
		{
			EditorGUIUtility.PingObject(MonoScript.FromMonoBehaviour(obj as MonoBehaviour));
		} 
		else if (obj is ScriptableObject)
		{
			EditorGUIUtility.PingObject(MonoScript.FromScriptableObject(obj as ScriptableObject));
		}
	}
	internal static void PingAndUseEvent(this Transform obj, bool ping = true, bool useEvent = true)
	{
		if (obj == null) return;
		var go = obj.gameObject;

		if (useEvent) Event.current.Use();
		if (!ping) return;

		if (go != null && !go.GetFlag(HideFlags.HideInHierarchy)) {
			Selection.activeObject = go;
			EditorGUIUtility.PingObject(go);
		} else {
			//Debug.Log("Can not ping a null or hidden target ---> " + go + ":" + go.hideFlags);
		}
	}
	internal static Transform NextSibling(this Transform t, List<GameObject> rootList)
	{
		if (t == null) {
			Debug.LogWarning("Transform should not be null ");
			return null;
		}

		var p = t.parent;
		if (t.parent != null) {
			var cnt = 0;
			while (p.GetChild(cnt) != t) cnt++;
			return (cnt < p.childCount - 1) ? p.GetChild(cnt + 1) : p.GetChild(0);
		}

		var idx = rootList.IndexOf(t.gameObject);
		if (idx != -1) return rootList[(idx < rootList.Count - 1) ? idx + 1 : 0].transform;
		Debug.LogWarning("Root Object not in RootList " + t + ":" + rootList);
		return t;
	}

	internal static bool IsExpanded(this GameObject go) {
		var mExpand = (int[])vlbEditor.HierarchyWindow.GetField("m_ExpandedArray", null, "UnityEditor.BaseProjectWindow".GetTypeByName());
		return mExpand.Contains(go.GetInstanceID());
	}
	internal static void OpenScript(this Object obj) {
		AssetDatabase.OpenAsset(MonoScript.FromMonoBehaviour((MonoBehaviour)obj).GetInstanceID());
	}

	internal static int numScript(this GameObject go) {
		var list = go.GetComponents<MonoBehaviour>();
		if (list.Length == 0) return 0;

		var paths = Hierarchy2.IgnoreScriptPaths;
		var cnt = 0;
		for (int i = 0; i < list.Length; i++) {
			var mono = MonoScript.FromMonoBehaviour(list[i]);
			var monoPath = AssetDatabase.GetAssetPath(mono);

			for (var j = 0; j < paths.Length; j++) {
				if (monoPath.Contains(paths[j])) {
					list[i] = null;
					//Debug.Log("Ignoring ... " + monoPath);
					break;
				}
			}

			if (list[i] != null) cnt++;
		}

		return cnt;
	}
    internal static int numScriptMissing(this GameObject go){
        var list =  go.GetComponents<MonoBehaviour>();
        var cnt = 0;
        if (list.Any(item => item == null)) cnt++;
        return cnt;
    }
    internal static void AppendChildren(Transform t, ref List<GameObject> list, bool deep = false){
		list.Add(t.gameObject);
        foreach (Transform child in t){
            if (child != t){
                list.Add(child.gameObject);
                if (deep && child.childCount>0) AppendChildren(child, ref list, true);
            }
        }
    }

    internal static int[] GetFilterInstanceIDs(this List<GameObject> rootList, Func<GameObject, bool> func){
        var list = new List<GameObject>();
        foreach (var child in rootList) {
            AppendChildren(child.transform, ref list, true);
        }

	    return (from item in list where func(item) select item.GetInstanceID()).ToArray(); 
    }
    internal static string GetNewName(this string baseName, Transform p, string suffix = "")
    {
		var name = baseName.Contains(suffix) ? baseName : (baseName + suffix);
        if (p == null) return name;
        var namesList = new string[p.childCount];
        for (var i = 0; i < namesList.Length; i++)
        {
            namesList[i] = p.GetChild(i).name;
        }

        if (!namesList.Contains(name)) return name;
        var counter = 1;
        while (namesList.Contains(name + counter)) counter++;
        return name + counter;
    }

    //-------------------------------- UNDO SUPPORTED INSTANTIATE -----------------------------------

	static internal bool IsCombined(this GameObject go) {
		return go.HasFlagChild(HideFlags.HideInHierarchy);
	}
	static internal bool IsLock(this GameObject go) {
		return go.GetFlag(HideFlags.NotEditable);
	}
	static internal void RecordUndo(this Object go, string undoKey, bool full = false) {
		if (string.IsNullOrEmpty(undoKey)) return;
		if (full) {
			Undo.RegisterCompleteObjectUndo(go, undoKey);
		} else {
			Undo.RecordObject(go, undoKey);	
		}
	}
}

static class Hierarchy2Api {

	///----------------------------------- TRANSFORM ------------------------------------------------
	static private bool _rootDirty;
	static private List<GameObject> _rootList;
	static internal List<GameObject> RootGOList {
		get {
			if (_rootList != null && !_rootDirty) return _rootList;

			_rootList = new List<GameObject>();
			_rootDirty = false;

			foreach (var o in Object.FindObjectsOfType(typeof(GameObject))) {
				var obj = (GameObject) o;
				if (obj != null && obj.transform != null && obj.transform.parent == null) RootGOList.Add(obj);
			}

			return _rootList;
		}
	}
	static internal void CheckRoot(GameObject go) {
		if (go.transform.parent == null && (!RootGOList.Contains(go))) {
			_rootList.Add(go);
		}
	}
	static internal void UpdateRoot() { _rootDirty = true; /*Debug.Log("----> refreshed " + vlbGUI.renameGO);*/ }

	///----------------------------------- LOCK ------------------------------------------------
	static internal void SetLock(this GameObject go, bool value, bool deep = false, string undoKey = "h@-auto") {
		if (undoKey == "h@-auto") undoKey = value ? "Lock" : "UnLock";

		go.RecordUndo(undoKey, true);
		go.SetFlag(HideFlags.NotEditable, value);

		foreach (var c in go.GetComponents<Component>()) {
			if (!(c is Transform)) {
				c.SetFlag(HideFlags.NotEditable, value);
				c.SetFlag(HideFlags.HideInHierarchy, value);	
			}
		}
		
		if (deep) {
			go.ForeachChild(child => {
				child.RecordUndo(undoKey, true);
				child.SetFlag(HideFlags.NotEditable, value);
				foreach (var c in child.GetComponents<Component>()) {
					if (!(c is Transform)) {
						c.SetFlag(HideFlags.NotEditable, value);
						c.SetFlag(HideFlags.HideInHierarchy, value);
					}
				}
			}, true);
		}
	}
	static internal void ToggleLock(this GameObject go, string undoKey = "h@-auto") {
		go.SetLock(!go.GetFlag(HideFlags.NotEditable), false, undoKey);
	}

	/*static internal void SetNaiveLock(this GameObject go, bool value, bool deep, bool invertMe) {
		var isLock = go.GetFlag(HideFlags.NotEditable);
		go.ForeachSelected((item, idx) => SetLock(item,
			(!invertMe || (item == go)) ? !isLock : isLock, deep)
		);
	}*/
	static internal void SetSmartLock(this GameObject go, bool invertMe, bool smartInvert) {//smart mode : auto-deepLock
		var isLock = go.GetFlag(HideFlags.NotEditable);
		var key = isLock ? "Lock" : "Unlock";

		go.ForeachSelected((item, idx) => item.SetLock(
			(!invertMe || (item == go)) ? !isLock : isLock,	// invert lock 
			idx == -1 && smartInvert == isLock,				// deep-lock if isLock=true
			key
		));
	}
	static internal void InvertLock(this GameObject go) {
		go.ForeachSelected((item, idx) => item.ToggleLock("Invert Lock"));
	}
	static internal void ToggleSiblingLock(this GameObject go, bool deep = false) {
		var isLock = go.GetFlag(HideFlags.NotEditable);
		var key = isLock ? "Lock siblings" : "Unlock siblings";

		go.ToggleLock(key);
		go.ForeachSibling(RootGOList, sibl => sibl.ToggleLock(key));
	}
	static internal void RecursiveLock(bool value) {
		var key = value ? "Recursive Lock" : "Recursive Unlock";
		RootGOList.ForEach(
			rootGO => rootGO.SetLock(value, true, key)
		);
	}

	///----------------------------------- ACTIVE ------------------------------------------------
	static internal void SetGOActive(this GameObject go, bool value, bool? activeParents = null, string undoKey = "h@-auto") {
		//activeParents == null : activeParents if setActive==true
		if (undoKey == "h@-auto") undoKey = value ? "Show GameObject" : "Hide GameObject";
		
		//if (!string.IsNullOrEmpty(undoKey)) Undo.RecordObject(go, undoKey);
		go.RecordUndo(undoKey);
		go.SetActive(value);

		if ((activeParents ?? value) && !go.activeInHierarchy) {
			go.ForeachParent2(p => {
				//if (!string.IsNullOrEmpty(undoKey)) Undo.RecordObject(p, undoKey);
				p.RecordUndo(undoKey);
				p.SetActive(true);
				return !p.activeInHierarchy;
			});
		}
	}
	static internal void ToggleActive(this GameObject go, bool invertMe, bool? activeParents = null)
	{
		var isActive	= go.activeSelf;
		var key			= isActive ? "Hide Selected GameObjects" : "Show Selected GameObjects";

		go.ForeachSelected(
			(item, idx) => item.SetGOActive((!invertMe || (item == go)) ? !isActive : isActive, activeParents, key)
		);
	}
	static internal void SetActiveChildren(this GameObject go, bool value, bool? activeParents) {
		var key = value ? "Show Children" : "Hide Children";
		go.ForeachChild(child => child.SetGOActive(value, activeParents, key), true);
		go.SetGOActive(value, false, key);
	}
	static internal void SetActiveSibling(this GameObject go, bool value, bool? activeParents = null) {
		var key = value ? "Show siblings" : "Hide siblings";
		go.ForeachSibling(RootGOList, item=>item.SetGOActive(value, activeParents, key));
		go.SetGOActive(!value, false, key);
	}
	static internal void SetActiveParents(this GameObject go, bool value)
	{
		var p = go.transform.parent;
		var key = value ? "Show Parents" : "Hide Parents";
		//if (go.activeSelf != value) go.SetActive(value);

		while (p != null) {
			if (p.gameObject.activeSelf != value) {
				p.gameObject.RecordUndo(key);
				p.gameObject.SetActive(value);
			}
			p = p.parent;
		}
	}

	///---------------------------------- SIBLINGS ------------------------------------------------
	static internal void SetCombine(this GameObject go, bool value, bool deep = false, string undoKey = "h@-auto") {
		if (undoKey == "h@-auto") undoKey = value ? "Combine GameObject" : "Expand GameObject";
		go.ForeachChild(child => {
			//Undo.RegisterCompleteObjectUndo(child, undoKey);
			child.RecordUndo(undoKey, true);
			child.SetFlag(HideFlags.HideInHierarchy, value);
		}, deep);
	}
	static internal void ToggleCombine(this GameObject go, bool deep = false) {
		var isCombined	= go.IsCombined();
		var key = isCombined ? "Combine Selected GameObjects" : "Expand Selected GameObjects";
		go.ForeachSelected((item,index) => item.SetCombine(!isCombined, deep, key));
		go.transform.GetChild(0).Ping();
	}
	static internal void ToggleCombineChildren(this GameObject go) {
		var val = false;

		go.ForeachChild2(child => {
			val = child.IsCombined();
			return !val;
		});

		var key = val ? "Expand Children" : "Combine Children";
		go.SetCombine(false, false, key);
		go.ForeachChild(child => child.SetCombine(!val, false, key));
	}
	static internal void SetCombineSibling(this GameObject go, bool value) {
		var key = value ? "Expand Siblings" : "Combine siblings";

		go.SetCombine(value, false, key);
		go.ForeachSibling(RootGOList, sibl => sibl.SetCombine(!value, false, key));
		if (!value) go.RevealChildrenInHierarchy(true);
	}
	static internal void RecursiveCombine(bool value) {
		var key = value ? "Recursive Combine" : "Recursive Expand";
		RootGOList.ForEach(rootGO => {
			var list = rootGO.GetChildren(true);
			foreach (var child in list) child.RecordUndo(key, true);
			rootGO.SetDeepFlag(HideFlags.HideInHierarchy, value);
		});
	}
	
	///------------------------ GOTO : SIBLING / PARENT / CHILD -------------------------------
	
	private static List<Transform> _pingList;

	static internal void PingParent(this Transform t, bool useEvent = false) {
		var p = t.parent;
		if (p == null) return;

		//clear history when select other GO
		if (_pingList == null || (_pingList.Count > 0 && _pingList[_pingList.Count - 1].parent != t)) {
			_pingList = new List<Transform>();
		} 

		_pingList.Add(t);
		p.PingAndUseEvent(true, useEvent);
	}
	static internal void PingChild(this Transform t, bool useEvent = false) {
		Transform pingT = null;

		if (_pingList == null) _pingList = new List<Transform>();

		if (_pingList.Count > 0) {
			var idx = _pingList.Count - 1;
			var c = _pingList[idx];
			_pingList.Remove(c);

			pingT = c;
		} else if (t.childCount > 0) {
			pingT = t.GetChild(0);
		}

		if (pingT != null) pingT.PingAndUseEvent(true, useEvent);
	}
	static internal void PingSibling(this Transform t, bool useEvent = false) {
		t.NextSibling(RootGOList).PingAndUseEvent(true, useEvent);
	}

	///----------------------------------- CAMERA ---------------------------------------------------

	public static Camera ltCamera;
	public static CameraInfo ltCameraInfo;

	static void SceneCameraApply(bool orthor, Vector3 pos, Quaternion rot) {
		Hierarchy2Utils.orthographic	= orthor;
		Hierarchy2Utils.m_Rotation		= rot;
		Hierarchy2Utils.m_Position		= pos;
		Hierarchy2Utils.Refresh();
	}
	static void UpdateLookThrough() {
		if (ltCamera != null) {
			if (EditorApplication.isPaused) return;

			var sceneCam	= Hierarchy2Utils.SceneCamera;
			var hasChanged = ltCamera.transform.position != sceneCam.transform.position ||
							ltCamera.orthographic != sceneCam.orthographic ||
							ltCamera.transform.rotation != sceneCam.transform.rotation;

			 if (hasChanged) CameraLookThrough(ltCamera);
		} else {
			EditorApplication.update -= UpdateLookThrough;
		}
	}

	static internal void CameraLookThrough(Camera cam) {
		//Undo.RecordObject(cam, "LookThrough");
		var sceneCam	= Hierarchy2Utils.SceneCamera;

		if (EditorApplication.isPlaying) {
			if (ltCameraInfo == null) {
				ltCameraInfo = new CameraInfo {
					orthor		= Hierarchy2Utils.orthographic,
					mRotation	= Hierarchy2Utils.m_Rotation,
					mPosition	= Hierarchy2Utils.m_Position
				};

				EditorApplication.update -= UpdateLookThrough;
				EditorApplication.update += UpdateLookThrough;
			}
		} else {
			ltCameraInfo = null;
			ltCamera = null;
		}

		sceneCam.CopyFrom(cam);
		var distance = Hierarchy2Utils.cameraDistance;
		SceneCameraApply(
			cam.orthographic,
			cam.transform.position - (cam.transform.rotation * new Vector3(0f, 0f, -distance)),
			cam.transform.rotation
		);

		//Hierarchy2Utils.orthographic = cam.orthographic;
		//Hierarchy2Utils.m_Rotation = cam.transform.rotation;
		//Hierarchy2Utils.m_Position = cam.transform.position - (cam.transform.rotation * new Vector3(0f, 0f, -distance));
		//Hierarchy2Utils.Refresh();
	}
	static internal void CameraCaptureSceneView(Camera cam) {
		ltCamera = null;
		ltCameraInfo = null;

		//Undo.RecordObject(cam, "CaptureSceneCamera");
		cam.RecordUndo("Capture Scene Camera");
		cam.CopyFrom(Hierarchy2Utils.SceneCamera);
	}
	static internal void ToggleLookThroughCamera(Camera c) {
		ltCamera = ltCamera == c ? null : c;

		if (ltCamera != null) { 
			CameraLookThrough(ltCamera);
		} else if (ltCameraInfo != null) {
			SceneCameraApply(ltCameraInfo.orthor, ltCameraInfo.mPosition, ltCameraInfo.mRotation);
			ltCameraInfo = null;
			ltCamera = null;
		}
	}

	///----------------------------------- ISOLATE ---------------------------------------------------

	internal static void Isolate_MissingBehaviours(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => item.numScriptMissing() > 0), "Missing"
		);
		if (useEvent) Event.current.Use();
	}
	internal static void Isolate_ObjectsHasScript(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => item.numScript() > 0), "Script"
		);
		if (useEvent) Event.current.Use();
	}
	internal static void Isolate_SelectedObjects(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(Selection.instanceIDs, "Selected");
		if (useEvent) Event.current.Use();
	}
	internal static void Isolate_LockedObjects(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => item.GetFlag(HideFlags.NotEditable)), "Locked"
		);
		if (useEvent) Event.current.Use();
	}
	internal static void Isolate_InActiveObjects(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => !item.activeSelf), "InActive"
		);
		if (useEvent) Event.current.Use();
	}
	internal static void Isolate_CombinedObjects(bool useEvent = false) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => item.HasFlagChild(HideFlags.HideInHierarchy)), "Combined"
		);
		if (useEvent) Event.current.Use();
	}

	internal static void Isolate_ComponentType(Type t) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => (item.GetComponent(t) != null)), t.ToString()
		);
	}
	internal static void Isolate_Component(Component c) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => (item.GetComponent(c.GetType()) != null)), c.GetTitle(false)
		);
	}
	internal static void Isolate_Layer(string layerName) {
		var layer = LayerMask.NameToLayer(layerName);
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => item.layer == layer), layerName
		);
	}
	internal static void Isolate_Tag(string tagName) {
		vlbEditor.HierarchyWindow.SetSearchFilter(
			RootGOList.GetFilterInstanceIDs(item => (item.tag == tagName)), tagName
		);
	}

	///----------------------------------- RESET TRANSFORM -------------------------------------------

	internal static void ResetLocalPosition(GameObject go) {
		Selection.activeGameObject = go;
		go.transform.ResetLocalPosition("ResetPosition");
	}
	internal static void ResetLocalRotation(GameObject go) {
		Selection.activeGameObject = go;
		go.transform.ResetLocalRotation("ResetRotation");
	}
	internal static void ResetLocalScale(GameObject go) {
		Selection.activeGameObject = go;
		go.transform.ResetLocalScale("ResetScale");
	}
	internal static void ResetTransform(GameObject go) {
		Selection.activeGameObject = go;
		go.transform.ResetLocalTransform("ResetTransform");
	}

	///----------------------------------- CREATE GO -------------------------------------------

	internal static void CreateEmptyChild(GameObject go, bool useEvent = false) {
		//var willPing = go.transform.childCount == 0 || !go.IsExpanded();

		vlbEditor.NewTransform(
			name: "New".GetNewName(go.transform, "Empty"),
			undo: "NewEmptyChild",
			p	: go.transform
		);//.PingAndUseEvent(willPing, useEvent);

        if (useEvent) Event.current.Use();
	}
	internal static void CreateEmptySibling(GameObject go, bool useEvent = false) {
		vlbEditor.NewTransform(
			name: "New".GetNewName(go.transform, "Empty"),
			undo: "NewEmptySibling",
			p: go.transform.parent
		);//.PingAndUseEvent(false, useEvent);
        if (useEvent) Event.current.Use();
	}
	internal static void CreateParentAtMyPosition(GameObject go, bool useEvent = false) {
		Selection.activeGameObject = go;
		var goT = go.transform;
		var p = vlbEditor.NewTransform(
			name: "NewEmpty".GetNewName(goT.parent, "_parent"),
			undo: "NewParent1",
			p: goT.parent,
			pos: goT.localPosition,
			scl: goT.localScale,
			rot: goT.localEulerAngles
		);

		goT.Reparent("NewParent1", p);
		//p.gameObject.RevealChildrenInHierarchy();

		if (useEvent) Event.current.Use();
	}
	internal static void CreateParentAtOrigin(GameObject go, bool useEvent = false) {
		Selection.activeGameObject = go;
		var goT = go.transform;
		var p = vlbEditor.NewTransform(
			name: "NewEmpty".GetNewName(goT.parent, "_parent"),
			undo: "NewParent2",
			p: goT.parent
		);

		goT.Reparent("NewParent2", p);
		//p.gameObject.RevealChildrenInHierarchy();
		//p.Ping();
		if (useEvent) Event.current.Use();
	}
}

class CameraInfo
{
	public bool orthor;
	public Vector3 mPosition;
	public Quaternion mRotation;
}

///------------------------------------ CORE ------------------------------------------------------

static internal class vlbEvent {
	internal static vlbMouseFlags CurrentEventModifier {
		get {
			var evt = Event.current;
			return (evt.control ? vlbMouseFlags.Ctrl : 0) |
					(evt.alt ? vlbMouseFlags.Alt : 0) |
					(evt.shift ? vlbMouseFlags.Shift : 0);
		}
	}

	internal static int GetMouse(Rect r, int button, bool down, bool useEvent = false) {
		var evt = Event.current;
		var result = evt.type == (down ? EventType.mouseDown : EventType.mouseUp) && evt.button == button && r.Contains(evt.mousePosition);
		if (!result) return -1;
		if (useEvent) evt.Use();
		return (int)CurrentEventModifier;
	}
	internal static int GetLeftMouseDown(this Rect r, bool useEvent = false) { return GetMouse(r, 0, true, useEvent); }
	internal static int GetLeftMouseUp(this Rect r, bool useEvent = false) { return GetMouse(r, 0, false, useEvent); }
	internal static int GetRightMouseDown(this Rect r, bool useEvent = false) { return GetMouse(r, 1, true, useEvent); }
	internal static int IsDown(this KeyCode c, bool useEvent = false) {
		var evt = Event.current;
		if (evt == null || evt.type != EventType.keyDown || evt.keyCode != c) return -1;
		if (useEvent) evt.Use();
		return (int)CurrentEventModifier;
	}
	internal static int IsUp(this KeyCode c, bool useEvent = false) {
		var evt = Event.current;
		if (evt == null || evt.type != EventType.keyUp || evt.keyCode != c) return -1;
		if (useEvent) evt.Use();
		return (int)CurrentEventModifier;
	}
}

[Flags]
public enum vlbMouseFlags { None = 0, Ctrl = 1, Alt = 2, Shift = 4 }

static internal class vlbReflection {

	//TODO : cache fields / properties / methods

	private const BindingFlags AllFlags = BindingFlags.Default
		//| BindingFlags.ExactBinding
										| BindingFlags.FlattenHierarchy
		//| BindingFlags.DeclaredOnly
		//| BindingFlags.CreateInstance
		//| BindingFlags.GetField
		//| BindingFlags.GetProperty
		//| BindingFlags.IgnoreCase
		//| BindingFlags.IgnoreReturn
		//| BindingFlags.SuppressChangeType
		//| BindingFlags.InvokeMethod
										| BindingFlags.NonPublic
										| BindingFlags.Public
		//| BindingFlags.OptionalParamBinding
		//| BindingFlags.PutDispProperty
		//| BindingFlags.PutRefDispProperty
		//| BindingFlags.SetField
		//| BindingFlags.SetProperty
										| BindingFlags.Instance
										| BindingFlags.Static;

	public static bool HasMethod(this Type type, string methodName, BindingFlags? flags = null) {
		//if (obj == null || string.IsNullOrEmpty(methodName)) return false;
		//var type = obj.GetType();
		return type.GetMethod(methodName, flags ?? AllFlags) != null;
	}

	public static bool HasMethod(this object obj, string methodName, BindingFlags? flags = null) {
		if (obj == null || string.IsNullOrEmpty(methodName)) return false;
		var type = obj.GetType();
		return type.GetMethod(methodName, flags ?? AllFlags) != null;
	}

	private static Dictionary<string, Type> TypeDict;
	static internal Type GetTypeByName(this string className, string pck = "UnityEditor") {
		if (TypeDict == null) TypeDict = new Dictionary<string, Type>();
		var hasCache = TypeDict.ContainsKey(className);
		var def = hasCache ? TypeDict[className] : null;

		if (hasCache) {
			if (def != null) return def;
			TypeDict.Remove(className);
		}

		def = Types.GetType(className, pck);
		if (def != null) {
			TypeDict.Add(className, def);
		} else {
			Debug.LogWarning(string.Format("Type <{0}> not found in package <{1}>", className, pck));
		}

		return def;
	}

	public static object Invoke(this object obj, string methodName, params object[] parameters) {
		return obj.Invoke(null, methodName, null, parameters);
	}

	public static object Invoke(this object obj, Type type, string methodName, BindingFlags? flags = null, params object[] parameters) {
		if (obj == null || string.IsNullOrEmpty(methodName)) return null;

		if (type == null) type = obj.GetType();
		var f = type.GetMethod(methodName, flags ?? AllFlags);
		if (f != null) return f.Invoke(obj, parameters);
		Debug.LogWarning(string.Format("Invoke Error : <{0}> is not a method of type <{1}>", methodName, type));
		return null;
	}

	#region GET / SET FIELDS
	public static bool HasField(this object obj, string name, BindingFlags? flags = null, Type type = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return false;
		if (type == null) type = obj.GetType();
		return type.GetField(name, flags ?? AllFlags) != null;
	}
	public static object GetField(this object obj, string name, BindingFlags? flags = null, Type type = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return false;

		if (type == null) type = obj.GetType();
		var field = type.GetField(name, flags ?? AllFlags);
		if (field == null) {
			Debug.LogWarning(string.Format(
				"GetField Error : <{0}> does not contains a field with name <{1}>",
				type, name
			));
			return null;
		}

		return field.GetValue(obj);
	}
	public static void SetField(this object obj, string name, object value, BindingFlags? flags = null, Type type = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return;

		if (type == null) type = obj.GetType();
		var field = type.GetField(name, flags ?? AllFlags);

		if (field == null) {
			Debug.LogWarning(string.Format(
				"SetField Error : <{0}> does not contains a field with name <{1}>",
				type, name
			));
			return;
		}

		field.SetValue(obj, value);
	}
	#endregion

	#region GET / SET PROPERTY
	public static bool HasProperty(this object obj, string name, BindingFlags? flags = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return false;

		var type = obj.GetType();
		return type.GetProperty(name, flags ?? AllFlags) != null;
	}
	public static void SetProperty(this object obj, string name, object value, BindingFlags? flags = null, Type type = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return;

		if (type == null) type = obj.GetType();
		var property = type.GetProperty(name, flags ?? AllFlags);

		if (property == null) {
			Debug.LogWarning(string.Format(
				"SetProperty Error : <{0}> does not contains a property with name <{1}>",
				obj, name
			));
			return;
		}

		property.SetValue(obj, value, null);
	}
	public static object GetProperty(this object obj, string name, BindingFlags? flags = null, Type type = null) {
		if (obj == null || string.IsNullOrEmpty(name)) return null;

		if (type == null) type = obj.GetType();
		var property = type.GetProperty(name, flags ?? AllFlags);
		if (property != null) return property.GetValue(obj, null);

		Debug.LogWarning(string.Format(
			"GetProperty Error : <{0}> does not contains a property with name <{1}>",
			type, name
		));
		return null;
	}
	#endregion
}

static internal class vlbSerialized {
	internal static SerializedProperty[] GetSerializedProperties(this Object go) {
		var so = new SerializedObject(go);
		so.Update();
		var result = new List<SerializedProperty>();

		var iterator = so.GetIterator();
		while (iterator.NextVisible(true)) result.Add(iterator.Copy());
		return result.ToArray();
	}
	internal static Dictionary<string, object> GetDump(this SerializedObject obj) {
		var iterator = obj.GetIterator();
		var first = true;
		var result = new Dictionary<string, object>();

		var isHidden = obj.targetObject.GetFlag(HideFlags.HideInInspector);
		if (isHidden) Debug.Log(obj + ": is Hidden");

		while (iterator.NextVisible(first)) {
			first = false;
			result.Add(iterator.name, iterator.propertyType);
		}

		return result;
	}
}

static class MathUtils {
	internal static float LerpTo(this float from, float to, float min = 0.5f, float frac = 0.1f) {
		var d = to - from;
		if (d >= -min && d <= min) return to;
		return Mathf.Lerp(from, to, frac);
	}
	public static Vector3 FixNaN(this Vector3 v) {
		v.x = Single.IsNaN(v.x) ? 0 : v.x;
		v.y = Single.IsNaN(v.y) ? 0 : v.y;
		v.z = Single.IsNaN(v.z) ? 0 : v.z;
		return v;
	}
}

static class ArrayUtils {
	public static string Join<T>(this T[] list) {
		var s = typeof(T) + "[" + list.Length + "]{";

		for (var i = 0; i < list.Length; i++) {
			s += (i == 0 ? "" : ";") + list[i];
		}
		s += "}";
		return s;
	}
	public static T[] ToArray<T>(this ICollection list) {
		var result = new T[list.Count];
		return result;
	}
	public static T[] Concat<T>(this T[] list, ICollection list2) {
		var arr = new ArrayList();
		if (list != null && list.Length > 0) arr.AddRange(list);
		if (list2 != null && list2.Count > 0) arr.AddRange(list2);
		return (T[])arr.ToArray(typeof(T));
	}
	public static T[] Concat<T>(this T[] list, T item) {
		var arr = new ArrayList();
		if (list != null && list.Length > 0) arr.AddRange(list);
		if (item != null) arr.Add(item);
		return (T[])arr.ToArray(typeof(T));
	}
	public static T[] RemoveAt<T>(this T[] source, int index) {
		var dest = new T[source.Length - 1];
		if (index > 0)
			Array.Copy(source, 0, dest, 0, index);

		if (index < source.Length - 1)
			Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

		return dest;
	}
	public static int IndexOf<T>(this T[] list, T item) where T : class {
		for (int i = 0; i < list.Length; i++) {
			if (item is string) {//bugfixed :: item == list[i] always return false if T is string
				var str1 = (string)Convert.ChangeType(list[i], typeof(string));
				var str2 = (string)Convert.ChangeType(item, typeof(string));
				if (str1 == str2) return i;
			} else {
				if (list[i] == item) return i;
			}
		}
		return -1;
	}

	public static T[] Remove<T>(this T[] list, T item, bool checkUnique = false) {
		if (list == null) {
			MonoBehaviour.print("Can not remove item from an empty list");
			return list;
		}

		var arr = new ArrayList();
		arr.AddRange(list);
		arr.Remove(item);

		return (T[])arr.ToArray(typeof(T));
	}
	public static T[] Add<T>(this T[] list, T item, bool checkUnique = false) {
		var tail = new[] { item, };
		var result = checkUnique ? list.Union(tail) : list.Concat(tail);

		return result.ToArray();
	}
	public static T[] AddRange<T>(this T[] list, ICollection items, bool checkUnique = false) {// where T : class
		var arr = new ArrayList();
		if (list != null && list.Length > 0) arr.AddRange(list);

		if (checkUnique) {
			foreach (T obj in items) {
				if (arr.Contains(obj)) continue;
				arr.Add(obj);
			}
		} else {
			arr.AddRange(items);
		}

		return (T[])arr.ToArray(typeof(T));
	}
	public static T2[] Map<T1, T2>(this T1[] list, Func<int, T2> action)
		where T1 : class
		where T2 : class {
		var arr = new ArrayList();

		for (int i = 0; i < list.Length; i++) {
			T2 tmp = action(i);
			if (tmp != null) arr.Add(tmp);
		}

		return (T2[])arr.ToArray(typeof(T2));
	}
	public static T[] Compact<T>(this T[] list) {
		var arr = new ArrayList();

		for (int i = 0; i < list.Length; i++) {
			if (list[i] != null) {
				arr.Add(list[i]);
			}
		}

		return (T[])arr.ToArray(typeof(T));
	}

	public static void ApplyAction<T>(this T[] list, Action<T> act) {
		foreach (var item in list) { act(item); }
	}
}

static class GameObjectUtils {
	internal static bool GetFlag(this Object go, HideFlags flag) { return (go.hideFlags & flag) > 0; }
	internal static void SetFlag(this Object go, HideFlags flag, bool value)
	{
	    if (go == null) return;
		if (value) go.hideFlags |= flag;
		else go.hideFlags &= ~flag;
	}
	internal static void ToggleFlag(this Object go, HideFlags flag) {
		go.SetFlag(flag, !go.GetFlag(flag));
	}

	internal static void SetFlag(this Object[] list, HideFlags flag, Func<int, Object, bool> func) {
		for (var i = 0; i < list.Length; i++) {
			if (list[i] != null) list[i].SetFlag(flag, func(i, list[i]));
		}
	}

	internal static Vector3 FixIfNaN(this Vector3 v, Vector3? defaultValue = null) {
		var v2 = defaultValue ?? Vector3.zero;
		if (float.IsNaN(v.x)) v.x = v2.x;
		if (float.IsNaN(v.y)) v.y = v2.y;
		if (float.IsNaN(v.z)) v.z = v2.z;
		return v;
	}

	private static Transform[] GetChildrenTransform(Transform t, bool deep = false, bool includeMe = false, bool activeOnly = false) {
		var children = new ArrayList();

		if (includeMe && (t.gameObject.activeSelf || !activeOnly)) children.Add(t);

		foreach (Transform child in t) {
			if (deep) children.AddRange(GetChildrenTransform(child, deep, includeMe, activeOnly));
			if ((!deep || !includeMe) && (child.gameObject.activeSelf || !activeOnly)) children.Add(child);
		}

		return (Transform[])children.ToArray(typeof(Transform));
	}

	public static GameObject[] GetChildren(this GameObject go, bool deep = false, bool includeMe = false, bool activeOnly = false) {
		Transform[] children = GetChildrenTransform(go.transform, deep, includeMe, activeOnly);
		return children.Map(i => children[i].gameObject);
	}
	public static T[] GetChildren<T>(this Component comp, bool deep = false, bool includeMe = false, bool activeOnly = false) where T : Component {
		Transform[] children = GetChildrenTransform(comp.transform);
		return children.Map(i => children[i].GetComponent<T>());
	}

	public static T[] GetSiblings<T>(this Component comp, GameObject[] rootList) where T : Component {
		if (comp.transform.parent == null) return rootList.Select(item => item.GetComponent<T>()).ToArray();
		var list = GetChildrenTransform(comp.transform.parent);
		list.Remove(comp.transform);
		return list.Select(item => item.GetComponent<T>()).ToArray();
	}

	public static GameObject[] GetSiblings(this GameObject go, GameObject[] rootList) {
		if (go.transform.parent == null) return rootList;
		var list = GetChildrenTransform(go.transform.parent);
		list.Remove(go.transform);
		return list.Select(item=>item.gameObject).ToArray();
	}

	public static GameObject[] GetParents(this GameObject go) {
		var p = go.transform.parent;
		var list = new List<GameObject>();
		while (p != null) {
			list.Add(p.gameObject);
			p = p.parent;
		}
		return list.ToArray();
	}
}

static class StringUtils {
	public static ArrayList Append(this ICollection src, ICollection tar) {
		//if (src == null) src = new ArrayList();

		var arr = new ArrayList();
		if (src != null) arr.AddRange(src);
		arr.AddRange(tar);
		return arr;
	}

	// public static int IndexOf(this Array array, object value){
	// 	return Array.IndexOf(array, value);
	// }

	public static string ReplaceAll(this string src, string oldStr, string newStr) {
		while (src.IndexOf(oldStr) != -1) {
			src = src.Replace(oldStr, newStr);
		}
		return src;
	}
	public static string ReplaceAll(this string src, ICollection oldStrArr, string newStr) {
		foreach (string str in oldStrArr) {
			while (src.IndexOf(str) != -1) {
				src = src.Replace(str, newStr);
			}
		}
		return src;
	}
	public static string[] Split(this string src, string spliter, bool removeEmpty = false) {
		return src.Split(new string[] { spliter }, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
	}
	public static string Duplicate(this string str, int nTimes) {
		var result = "";
		for (var i = 0; i < nTimes; i++) { result += str; }
		return result;
	}
}

static class ColorUtils {
	internal static Color ToColor(this int colorValue) {
		var a = (colorValue >> 24);
		var r = (colorValue >> 16) & 255;
		var g = (colorValue >> 8) & 255;
		var b = colorValue & 255;
		return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
	}
	internal static int ToInt(this Color c) {
		Color32 c32 = c;
		return (c32.a << 24) | (c32.r << 16) | (c32.g << 8) | c32.b;
	}
}

static class RectUtils {
	internal static float dx(this Rect rect1, Rect rect2) { return rect2.x - rect1.x; }
	internal static float dy(this Rect rect1, Rect rect2) { return rect2.y - rect1.y; }
	internal static float dw(this Rect rect1, Rect rect2) { return rect2.width - rect1.width; }
	internal static float dh(this Rect rect1, Rect rect2) { return rect2.height - rect1.height; }

	internal static float absDx(this Rect rect1, Rect rect2) { return Mathf.Abs(rect2.x - rect1.x); }
	internal static float absDy(this Rect rect1, Rect rect2) { return Mathf.Abs(rect2.y - rect1.y); }
	internal static float absDw(this Rect rect1, Rect rect2) { return Mathf.Abs(rect2.width - rect1.width); }
	internal static float absDh(this Rect rect1, Rect rect2) { return Mathf.Abs(rect2.height - rect1.height); }

	internal static bool IsDifference(this Rect rect1, Rect rect2, float tollerant = 0.5f) {
		return (rect1.absDx(rect2) + rect1.absDy(rect2) + rect1.absDw(rect2) + rect1.absDh(rect2)) > tollerant;
	}
	internal static Rect Lerp(this Rect rect1, Rect rect2, float snap = 0.5f) {
		rect1.x = rect1.x.LerpTo(rect2.x);
		rect1.y = rect1.y.LerpTo(rect2.y);
		rect1.width = rect1.width.LerpTo(rect2.width);
		rect1.height = rect1.height.LerpTo(rect2.height);
		return rect1;
	}

	internal static Rect SetXY(this Rect rect, float x, float y) {
		rect.x = x;
		rect.y = y;
		return rect;
	}
	internal static Rect SetWH(this Rect rect, float w, float h) {
		rect.width = w;
		rect.height = h;
		return rect;
	}
	internal static Rect AddXY(this Rect rect, float x, float y) {
		rect.x += x;
		rect.y += y;
		return rect;
	}

	internal static Rect SetX(this Rect rect, float value) {
		rect.x = value;
		return rect;
	}
	internal static Rect SetY(this Rect rect, float value) {
		rect.y = value;
		return rect;
	}
	internal static Rect SetWidth(this Rect rect, float value) {
		rect.width = value;
		return rect;
	}
	internal static Rect SetHeight(this Rect rect, float value) {
		rect.height = value;
		return rect;
	}

	internal static Rect AddX(this Rect rect, float value) {
		rect.x += value;
		return rect;
	}
	internal static Rect AddY(this Rect rect, float value) {
		rect.y += value;
		return rect;
	}
	internal static Rect AddWidth(this Rect rect, float value) {
		rect.width += value;
		return rect;
	}
	internal static Rect AddHeight(this Rect rect, float value) {
		rect.height += value;
		return rect;
	}
	internal static Rect AddWH(this Rect rect, float w, float h) {
		rect.width += w;
		rect.height += h;
		return rect;
	}

	internal static Rect MoveLeft(this Rect rect, float value) {
		rect.x += value;
		rect.width -= value;
		return rect;
	}
	internal static Rect MoveTop(this Rect rect, float value) {
		rect.y += value;
		rect.height -= value;
		return rect;
	}
	internal static Rect MoveLeftUntilWidth(this Rect rect, float value) {
		rect.x += rect.width - value;
		rect.width = value;
		return rect;
	}
	internal static Rect MoveTopUntilHeight(this Rect rect, float value) {
		rect.y += rect.height - value;
		rect.height = value;
		return rect;
	}
	internal static Rect MoveTLUntilWH(this Rect rect, float w, float h) {
		rect.x += rect.width - w;
		rect.y += rect.height - h;
		rect.width = w;
		rect.height = h;
		return rect;
	}

	internal static Rect Slide(this Rect rect, float pctX = 0, float pctY = 0) {
		rect.x += pctX * rect.width;
		rect.y += pctY * rect.height;
		return rect;
	}
	internal static Rect Expand(this Rect rect, int px, int py) {
		rect.x -= px;
		rect.y -= py;
		rect.width += 2 * px;
		rect.height += 2 * py;
		return rect;
	}
}


///------------------------------------ EDITOR ------------------------------------------------------

static internal class vlbEditor {
	internal static void Move(this Component c, int delta) {
		while (delta > 0) {
			ComponentUtility.MoveComponentDown(c);
			delta--;
		}

		while (delta < 0) {
			ComponentUtility.MoveComponentUp(c);
			delta++;
		}
	}
	internal static Editor[] InspectorComponentEditors {
		get {
			return ActiveEditorTracker.sharedTracker.activeEditors;
		}
	}

	internal static bool GetEditorFlag(this Object obj, HideFlags flag) {
		return (obj as Editor).target.GetFlag(flag);
	}
	internal static void SetEditorFlag(this Object obj, HideFlags flag, bool value) {
		(obj as Editor).target.SetFlag(flag, value);
	}

	internal static void RevealChildrenInHierarchy(this GameObject go, bool pingMe = false) {
		if (go.transform.childCount == 0) return;
		foreach (Transform child in go.transform) {
			if (child == go.transform) continue;
			HierarchyWindow.Invoke("PingTargetObject", new object[] { child.GetInstanceID() });
			if (pingMe) HierarchyWindow.Invoke("PingTargetObject", new object[] { go.GetInstanceID() });
			return;
		}
	}
	internal static void SetEditorEnable(this Object editor, bool isEnable) {
		EditorUtility.SetObjectEnabled((editor as Editor).target, isEnable);
	}
	internal static bool GetEditorEnable(this Object editor) {
		if (editor == null) return false;
		return EditorUtility.GetObjectEnabled((editor as Editor).target) == 1;
	}
	internal static void ToggleEditorEnable(this Object editor) {
		if (editor != null) editor.SetEditorEnable(!editor.GetEditorEnable());
	}

	internal static void BreakPrefab(this GameObject go, string tempName = "vlb_dummy.prefab") {
		var go2 = PrefabUtility.FindRootGameObjectWithSameParentPrefab(go);

		PrefabUtility.DisconnectPrefabInstance(go2);
		var prefab = PrefabUtility.CreateEmptyPrefab("Assets/" + tempName);
		PrefabUtility.ReplacePrefab(go2, prefab, ReplacePrefabOptions.ConnectToPrefab);
		PrefabUtility.DisconnectPrefabInstance(go2);
		AssetDatabase.DeleteAsset("Assets/" + tempName);

		//temp fix to hide Inspector's dirty looks
		Selection.instanceIDs = new int[] { };
	}

	internal static void SelectPrefab(this GameObject go) {
		var prefab = PrefabUtility.GetPrefabParent(PrefabUtility.FindRootGameObjectWithSameParentPrefab(go));
		Selection.activeObject = prefab;
		EditorGUIUtility.PingObject(prefab.GetInstanceID());
	}
	internal static void Reparent(this Transform t, string undo, Transform parent) {
		if (t == null || t == parent || t.parent == parent) return;
		t.gameObject.layer = parent.gameObject.layer;
		Undo.SetTransformParent(t.transform, parent, undo);
	}
	internal static string Enum2String(this Enum e) {
		if (e == null) {
			Debug.Log(e);
			return "";
		}

		var names = Enum.GetNames(e.GetType());
		var values = Enum.GetValues(e.GetType());
		return names[Array.IndexOf(values, e)];
	}
	internal static void SetLocalTransform(this Transform t, string undo, Vector3? pos = null, Vector3? scl = null, Vector3? rot = null) {
		//Undo.RecordObject(t, undo);
		t.RecordUndo(undo);
		if (scl != null) t.localScale = scl.Value;
		if (rot != null) t.localEulerAngles = rot.Value;
		if (pos != null) t.localPosition = pos.Value;
	}
	internal static void SetLocalPosition(this Transform t, Vector3 pos, string undo) {
		t.SetLocalTransform(undo, pos);
	}
	internal static void SetLocalScale(this Transform t, Vector3 scl, string undo) {
		t.SetLocalTransform(undo, null, scl);
	}
	internal static void SetLocalRotation(this Transform t, Vector3 rot, string undo) {
		t.SetLocalTransform(undo, null, null, rot);
	}

	internal static void ResetLocalTransform(this Transform t, string undo) {
		SetLocalTransform(t, undo, Vector3.zero, Vector3.one, Vector3.zero);
	}
	internal static void ResetLocalPosition(this Transform t, string undo) {
		t.SetLocalTransform(undo, Vector3.zero);
	}
	internal static void ResetLocalScale(this Transform t, string undo) {
		t.SetLocalTransform(undo, null, Vector3.one);
	}
	internal static void ResetLocalRotation(this Transform t, string undo) {
		t.SetLocalTransform(undo, null, null, Vector3.zero);
	}

	internal static bool IsRenaming() {

		var oFocus = EditorWindow.focusedWindow;
		var hWindow = HierarchyWindow;
		var type = "UnityEditor.BaseProjectWindow".GetTypeByName();
		var result = (int)hWindow.GetField("m_RealEditNameMode", null, type) == 2;
	    if (oFocus != null) {
	        oFocus.Focus();
	    }

		return result;
	}
	internal static void Rename(this GameObject go) {

		var hWindow = HierarchyWindow;

		if (Event.current != null && Event.current.keyCode == KeyCode.Escape) {
			vlbGUI.renameGO = null;
			vlbGUI.renameStep = 0;
			hWindow.Repaint();
			return;
		}

		//if (Event.current == null || Event.current.type != EventType.repaint) Debug.Log("Rename : " + Event.current);

		if (vlbGUI.renameGO != go) {
			vlbGUI.renameGO = go;
			vlbGUI.renameStep = 2;
		}

		//else {
		var type = "UnityEditor.BaseProjectWindow".GetTypeByName();

		if ((int)hWindow.GetField("m_RealEditNameMode", null, type) != 2) {
			//not yet in edit name mode, try to do it now
			Selection.activeGameObject = go;
			var property = new HierarchyProperty(HierarchyType.GameObjects);
			property.Find(go.GetInstanceID(), null);

			hWindow.Invoke(type, "BeginNameEditing", null, go.GetInstanceID());
			hWindow.SetField("m_NameEditString", go.name, null, type); //name will be missing without this line
			hWindow.Repaint();
		} else {
			if (Event.current == null) {
				vlbGUI.renameStep = 2;
				//Debug.Log("How can Event.current be null ?");
				return;
			}

			if (Event.current.type == EventType.repaint && vlbGUI.renameStep > 0) {
				vlbGUI.renameStep--;
				//hWindow.Repaint();
			}

			if (Event.current.type != EventType.repaint && vlbGUI.renameStep == 0) {
				vlbGUI.renameGO = null;
			}
		}
		//}
	}
	static internal bool IsAsset(this Object obj) {
		return obj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
	}
	//static internal string MonoScriptName(this MonoBehaviour b) {
	//	if (b != null) return MonoScript.FromMonoBehaviour(b).name;
	//	Debug.LogWarning("MonoScript is missing");
	//	return "Missing MonoBehaviour";
	//}

	internal static T GetEditorWindowAsDropdown<T>(this Rect rect) where T : EditorWindow {
		var edw = ScriptableObject.CreateInstance<T>();
		var r2 = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
		rect.x = r2.x;
		rect.y = r2.y;

		edw.ShowAsDropDown(rect.SetHeight(18f), new Vector2(rect.width, rect.height));
		edw.Focus();
		edw.GetField("m_Parent")
			.Invoke("UnityEditor.GUIView".GetTypeByName(), "AddToAuxWindowList");
		edw.wantsMouseMove = true;
		return edw;
	}
	static internal string GetTitle(this Object obj, bool nicify = true) {
		if (obj == null) return "Null";

		var name = obj is MonoBehaviour
			? MonoScript.FromMonoBehaviour((MonoBehaviour)obj).name
			: ObjectNames.GetClassName(obj);

		return nicify ? name : ObjectNames.NicifyVariableName(name);
	}
	static internal Type GetComponentTypeByName(this string cName) {
		var _tempGO = new GameObject();
		_tempGO.SetFlag(HideFlags.HideAndDontSave, true);
		_tempGO.AddComponent(cName);
		var c = _tempGO.GetComponent(cName);
		var t = c.GetType();
		Object.DestroyImmediate(_tempGO);
		return t;
	}
	internal static Transform NewTransform(string name, string undo, Transform p, Vector3? pos = null, Vector3? scl = null, Vector3? rot = null) {
		var t = new GameObject { name = name }.transform;
		//if (!string.IsNullOrEmpty(undo)){
		Undo.RegisterCreatedObjectUndo(t.gameObject, undo);
		t.Reparent(undo, p);
		t.SetLocalTransform(undo, pos ?? Vector3.zero, scl ?? Vector3.one, rot ?? Vector3.zero);
		t.gameObject.Rename();
		//}
		return t;
	}
	internal static GameObject NewPrimity(PrimitiveType type, string name, string undo, Transform p, Vector3? pos = null, Vector3? scl = null, Vector3? rot = null) {
		var primity = GameObject.CreatePrimitive(type);
		Undo.RegisterCreatedObjectUndo(primity, undo);
		primity.transform.Reparent(undo, p);
		primity.name = name;
		primity.transform.SetLocalTransform(undo, pos ?? Vector3.zero, scl ?? Vector3.one, rot ?? Vector3.zero);
		primity.Rename();
		return primity;
	}


	private static Dictionary<string, EditorWindow> WindowDict;
	static internal void ClearDefinitionCache() {
		//TypeDict = new Dictionary<string, Type>();
		WindowDict = new Dictionary<string, EditorWindow>();
	}
	internal static Type WindZoneT { get { return "UnityEngine.WindZone".GetTypeByName("UnityEngine"); } }
	internal static Type WindZoneModeT { get { return "UnityEngine.WindZoneMode".GetTypeByName("UnityEngine"); } }
	static internal EditorWindow GetEditorWindowByName(this string className, string pck = "UnityEditor") {
		if (WindowDict == null) WindowDict = new Dictionary<string, EditorWindow>();
		var hasCache = WindowDict.ContainsKey(className);
		var window = hasCache ? WindowDict[className] : null;

		if (hasCache) {
			if (window != null) return window;
			WindowDict.Remove(className);
		}

		window = EditorWindow.GetWindow(className.GetTypeByName());
		if (window != null) WindowDict.Add(className, window);
		return window;
	}
	internal static EditorWindow InspectorWindow {
		get { return "UnityEditor.InspectorWindow".GetEditorWindowByName(); }
	}
	internal static EditorWindow HierarchyWindow {
		get { return "UnityEditor.HierarchyWindow".GetEditorWindowByName(); }
	}
}

internal static class vlbGenericMenu {
	internal static GenericMenu Add(this GenericMenu menu, string text, Action func, bool selected = false) {
		if (func == null) {
			menu.AddDisabledItem(new GUIContent(text));
		} else {
			menu.AddItem(new GUIContent(text), selected, () => func());
		}

		return menu;
	}

    internal static GenericMenu AddSep(this GenericMenu menu, string text)
    {

#if UNITY_EDITOR_WIN
        menu.AddSeparator(text);
#else
        if (string.IsNullOrEmpty(text) || (text.IndexOf("/") == -1)){
			menu.AddSeparator(text);
		} else {
            menu.AddSeparator((text ?? "") + "--------------------");
		}
#endif
        return menu;
    }

	internal static GenericMenu AddIf(this GenericMenu menu, bool expression, string text, Action func) {
		if (expression) menu.Add(text, func);
		return menu;
	}
	internal static GenericMenu Add(this GenericMenu menu, bool has, string text1, string text2, Action<bool> func) {
		menu.AddItem(new GUIContent(has ? text1 : text2), false, () => func(has));
		return menu;
	}
	internal static GenericMenu AddIf(this GenericMenu menu, bool expression, bool has, string text1, string text2, Action<bool> func) {
		if (expression) Add(menu, has, text1, text2, func);
		return menu;
	}
}

static internal class vlbGUI {
	internal static bool _willRepaint;
	internal static bool willRepaint {
		get { return _willRepaint; }
		set { _willRepaint = value; /* if (value) Debug.Log("set to " + value + " : " + Event.current); */ }
	}

	internal static GameObject renameGO;
	public static int renameStep;

	internal static bool HasRightClick(this Rect r, bool useEvent = false) {
		var evt = Event.current;
		var result = evt.type == EventType.mouseDown && evt.button == 1 && r.Contains(evt.mousePosition);
		if (useEvent && result) evt.Use();
		return result;
	}
	internal static bool HasLeftClick(this Rect r, bool useEvent = false) {
		var evt = Event.current;
		var result = evt.type == EventType.mouseDown && evt.button == 0 && r.Contains(evt.mousePosition);
		if (useEvent && result) evt.Use();
		return result;
	}

	internal static int DrawTexture(this Rect r, Texture2D tex) {
		GUI.DrawTexture(r, tex);
		return r.GetLeftMouseDown();
	}
	internal static int DrawButton(this Rect r, string text) {
		return GUI.Button(r, text) ? (int)vlbEvent.CurrentEventModifier : -1;
	}
	internal static int DrawLabel(this Rect r, string text) {
		GUI.Label(r, text);
		return r.GetLeftMouseDown();
	}

	static internal GUIContent ToGUIContent(this string value) { return new GUIContent(value); }
	static internal GUIContent[] ToGUIContent(this string[] list) {
		var result = new GUIContent[list.Length];
		for (var i = 0; i < list.Length; i++) {
			result[i] = new GUIContent(list[i]);
		}
		return result;
	}
	static internal bool isNotLayout {
		get { return Event.current.type != EventType.layout && Event.current.type != EventType.used; }
	}
}

internal class vlbGUISkin {
	static Dictionary<string, Texture2D> Map;
	static Dictionary<Color, Texture2D> ColorMap;

	static int GetIndex(bool active) {
        // 0 - active pro
        // 1 - inactive pro
        // 2 - active indie
        // 3 - inactive indie
		return (active ? 0 : 1) + ((EditorGUIUtility.isProSkin ? 0 : 1) << 1);
	}
	internal static Texture2D GetColor(Color c, float alpha, float adjust) {
		var isDark = EditorGUIUtility.isProSkin;
		if (ColorMap == null) ColorMap = new Dictionary<Color, Texture2D>();
		c.a = alpha;

		var darkAdjust = adjust;
		var lightAdjust = 1f - adjust;

		//add 30% light for dark theme, add 30% dark for light theme
		c.r = isDark ? c.r + darkAdjust : lightAdjust * c.r;
		c.g = isDark ? c.g + darkAdjust : lightAdjust * c.g;
		c.b = isDark ? c.b + darkAdjust : lightAdjust * c.b;

		var tex = ColorMap.ContainsKey(c) ? ColorMap[c] : null;
		if (tex != null) return tex;

		tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
		tex.SetPixel(0, 0, c);
		tex.Apply();
		ColorMap.Add(c, tex);

		return tex;
	}
	internal static Texture2D Get(string id, bool active, params string[] list) {
		var index = GetIndex(active);

		id += index;
		if (Map == null) Map = new Dictionary<string, Texture2D>();

		if (Map.ContainsKey(id)) {
			if (Map[id] != null) return Map[id];
			Map.Remove(id); //script got reload 
		}

		var tex = new Texture2D(16, 16);
		tex.SetFlag(HideFlags.HideAndDontSave, true);
		tex.LoadImage(Convert.FromBase64String(list[index % list.Length]));
		Map.Add(id, tex);

		return tex;
	}

	internal static Texture2D icoLock(bool active) {
		return Get("iconLock", active
            // 0 - active pro   
            // 1 - inactive pro
            , "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"
            , "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAC8klEQVRYCbWWO27bQBCG9aAAAw4QqhFSUqrcRUcw4Av4CDqaylwigI4gAynSecvYFQ1EnR75foJDrJZLi6vYA/yYfcz889hdSsPFYjFIkdPpNBBiMhwOB0KKZCnG2H4FBchBTEoWHXiLbcbWeidQV12gVxAtjcwqHo1Gqn57OBzW7D3Zutl16b4JVJUT/B4ioUmAcSVKEOQEdhojDlzsxHg6ncq4U2qyO/QKo0dQgBtwJrXdzXg8LuhGfjwe3WQyeUGf2YWTSx3oqtzOWlqSg0KaRJZqf5ZljrnEgc5OjPNcvp1yB9mK3UdQAKv8N+M1+AE24A92BfhmnaALBes5HXGMX3RHYsiUrS9By5TdsobMSuDABr8N+glISiWg4KBgbp0o4RNHIwH/YNTs1IMwIduv1x3zNWPBaa2GLt5aYF82UeGFtNYv3YEzB4KVBNmC6plZNbT2TWsYq1p1SZ2QuiitDoQeRqRKfbHgWvPHvo2N9/u9DVs67ED4pVviofM0R1WotS5p7OUDQvsSRweaVzGcz+c+2XcqXeFYBakJCgxEJNFlc9IKEBHZFaCy15Ex1iuQH8NB86XURJIFRDnGesf3HW0V8VKOIgx8tXwmKkB8/iLPUhyN8L0IT6HZiw4UtE9wc/aTrLtgW5VuRe9D7pOesV2YyC98iq1XQMuqCi9w9d5WUEPMKXoE6sJ7nYDwL2Sv2OxEyvwWNWP+RfMUaR1BH2cCvRL0Jxf1WR1D5qw9oD8/AQIpoCp/Jolfei31se2Yay9JWnfgPW8FN7xnl7KXlEAKcV/bD0vgmvYryasuIcFuOYq5zl53gLm+51rrW3hjl5SAVUmgGeMH9E5JKCEYZw1rwiApAeMloJ5b8+Suqdy4khOwLhhBH20+pn2faAKqyOAbp45jAUOOaAIyqr9woX3SXAlYEqZDgiz43S/5vd6a0f+crXEEgbdwlrYnnQX/1xxra6r3/4bJ7ioJf3oVHDifLDwC/Vez//q+3aeN/wHFKE+dFZNXhQAAAABJRU5ErkJggg=="
            
            // 2 - active indie
            // 3 - inactive indie
            , "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"
			
		);
	}
	internal static Texture2D icoEye(bool active) {
		return Get("iconEye", active,
            // 0 - active pro   
            // 1 - inactive pro
			"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"
            // 2 - active indie
            // 3 - inactive indie
            , "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"
            , "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"
		);
	}
}
}