
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace FCT.CookieBakerP01
{
	public class CookieBakerEditorWindow
		: EditorWindow
	{
		[MenuItem("Cookie Baker RT/Editor Window")]
		public static void Init()
		{
			var window = EditorWindow.GetWindow<CookieBakerEditorWindow>("Cookie Baker RT",
																		 new Type[] { Type.GetType("UnityEditor.InspectorWindow, UnityEditor.dll") });
		}
	}
}
