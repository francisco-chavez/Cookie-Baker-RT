
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.EditorCoroutines;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;


namespace FCT.CookieBakerP01
{
	[DisallowMultipleComponent]
	public class CookieBakerEditorWindow
		: EditorWindow
	{

		#region Attributes

		/// <summary>
		/// The current overall state of the bake process.
		/// </summary>
		private static			BakeState		s_currentBakeState			= BakeState.SettingSelection;

		/// <summary>
		/// If a valid Light component is currently selected while inserting our settings, it will show up here.
		/// If we are not in setting insertion, this will hold the Light component we are currently baking for.
		/// </summary>
		private static			Light			s_currentLightComponent		= null;

		/// <summary>
		/// This is a collection of the selectable texture resolutions when it comes to importing a texture into 
		/// a Unity project. Because we are creating textures that will be imported and we need to display this 
		/// selection, so we should probably have this information.
		/// </summary>
		private static readonly int[]			s_resolutionOptions			= new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

		/// <summary>
		/// This is the indix of the resolution option that will be used to create the final texture.
		/// </summary>
		private static			int				s_selectedCookieResolution	= 4;

		/// <summary>
		/// This is a reference to the coroutine that will manage the bake processes.
		/// </summary>
		private static			EditorCoroutine s_bakingCoroutine			= null;

		#endregion


		/// <summary>
		/// Open the Editor Window.
		/// </summary>
		[MenuItem("Cookie Baker RT/Editor Window")]
		public static void Init()
		{
			var window = EditorWindow.GetWindow<CookieBakerEditorWindow>("Cookie Baker RT",
																		 new Type[] { Type.GetType("UnityEditor.InspectorWindow, UnityEditor.dll") });
			window.Show();
		}

		/// <summary>
		/// Draw this Window (or the contents of this window).
		/// </summary>
		private void OnGUI()
		{
			switch (CookieBakerEditorWindow.s_currentBakeState)
			{
				case BakeState.SettingSelection:
					DrawSettingSelection();
					break;

				case BakeState.Prep:
					DrawPrepStage();
					break;

				case BakeState.Bake:
					DrawBakeStage();
					break;

				case BakeState.Finalize:
					DrawFinalizeStage();
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// Things that need to be called in a loop and are not automatically called should be here. This is for
		/// items that only need to be called a few times a second and not as fast a possible. According to the
		/// docs that I read, this gets called 10 times a second.
		/// </summary>
		private void OnInspectorUpdate()
		{
			// Normally, OnGUI() is called when a value that we can set in this Window is changed through this 
			// Window. So, if a value is changed elsewhere, or a piece of data we pull from elsewhere is changed, 
			// then we don't repaint the window. Placing the repaint call in here will keep the window up to date 
			// at about 10 fps.
			// -FCT
			Repaint();
		}

		private void DrawSettingSelection()
		{

			#region State Checking

			//
			// Making sure we have all the valid inputs needed for drawing User Inputs.
			//

			if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating)
			{
				GUILayout.Label("Unity Editor is currently busy.");
				return;
			}

			var activeGameObject = Selection.activeGameObject;

			if (activeGameObject == null)
			{
				GUILayout.Label("Please select a valid light source in the scene.");
				return;
			}

			var selectedLightComponent = activeGameObject.GetComponent<Light>();
			if (selectedLightComponent == null)
			{
				GUILayout.Label("Please select a valid light source in the scene.");
				return;
			}

			switch (selectedLightComponent.type)
			{
				case LightType.Spot:
					break;

				// I'm planning to support Point lights in the future, so I'm separating this from default.
				// -FCT
				case LightType.Point:
				default:
					GUILayout.Label("Please select a valid light source in the scene.");
					return;
			}

			// Check some HDRP relectaed things.
			HDAdditionalLightData additionalLightData = activeGameObject.GetComponent<HDAdditionalLightData>();
			if (additionalLightData == null)
			{
				// Not an light in HDRP (I think).
				GUILayout.Label("Please select a valid light source in the scene.");
				return;
			}
			switch (additionalLightData.lightTypeExtent)
			{
				case LightTypeExtent.Rectangle:
				case LightTypeExtent.Tube:
					GUILayout.Label("Please select a valid light source in the scene.");
					return;
			}

			// I know that at the moment, Spot light is the only light that will make it this far, but I'm planning
			// to have this plugin work with Point lights too. Doing this now, is one less change I'll need to make
			// later on.
			// -FCT
			if (selectedLightComponent.type == LightType.Spot)
			{

				switch (additionalLightData.spotLightShape)
				{
					case SpotLightShape.Cone:
						break;

					// I need to check if each of the different shapes requires different math, so for now, I'll just pick 
					// one shape to work on. I'm picking Cone because it looks like it'll have the most realistic light 
					// spread.
					// -FCT
					default:
						GUILayout.Label("Please select a valid light source in the scene.");
						return;
				}
			}

			#endregion

			CookieBakerEditorWindow.s_currentLightComponent = selectedLightComponent;

			///
			/// Get the resolution for the generated cookie.
			/// 
			EditorGUILayout.BeginHorizontal();
			var guiContent = new GUIContent("Cookie Resolution:");
			var resOptions = s_resolutionOptions.Select(res => { return new GUIContent(res.ToString()); });
			s_selectedCookieResolution = EditorGUILayout.Popup(guiContent, s_selectedCookieResolution, resOptions.ToArray());
			EditorGUILayout.EndHorizontal();

			if (GUILayout.Button("Start Baking."))
			{
				s_currentBakeState = BakeState.Prep;
				s_bakingCoroutine = this.StartCoroutine(CookieBaking());
			}
		}

		private void DrawPrepStage()
		{
			GUILayout.Label("Preparing for cookie bake.");
		}

		private void DrawBakeStage()
		{
			GUILayout.Label("The cookie is in the oven.");
		}

		private void DrawFinalizeStage()
		{
			GUILayout.Label("Just finishing up.");
		}

		private IEnumerator CookieBaking()
		{
			yield return null;

			yield return new EditorWaitForSeconds(3.5f);

			s_currentBakeState = BakeState.Bake;

			yield return new EditorWaitForSeconds(3.5f);

			s_currentBakeState = BakeState.Finalize;
			yield return null;

			var resolution = s_resolutionOptions[s_selectedCookieResolution];
			Texture2D finalResults = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true);
			finalResults.alphaIsTransparency = true;
			finalResults.anisoLevel = 0;
			finalResults.filterMode = FilterMode.Point;
			finalResults.name = s_currentLightComponent.name + "_ShadowCookie_" + resolution.ToString();
			finalResults.wrapMode = TextureWrapMode.Clamp;

			yield return new EditorWaitForSeconds(0.5f);
			
			Color[] colors = new Color[resolution * resolution];
			for (int y = 0; y < resolution; y++)
				for (int x = 0; x < resolution; x++)
				{
					colors[resolution * y + x] = Color.gray;
				}

			yield return new EditorWaitForSeconds(0.5f);

			finalResults.SetPixels(colors);

			finalResults.Apply();

			yield return new EditorWaitForSeconds(0.5f);

			var assetFoldrPath = "Assets/Light_Cookies/" + s_currentLightComponent.gameObject.scene.name;

			if (!Directory.Exists(assetFoldrPath))
			{
				if (!Directory.Exists("Assets/Light_Cookies"))
				{
					AssetDatabase.CreateFolder("Assets", "Light_Cookies");
					yield return null;
				}
				AssetDatabase.CreateFolder("Assets/Light_Cookies", s_currentLightComponent.gameObject.scene.name);
			}
			yield return null;

			// In my experience, encoding a Texture2D to a byte array is a CPU heavy step that can stall a 
			// powerfull machine. I've seen this step take more time on a new (late 2017, early 2018) Intel server
			// CPU than the processes of saving the resulting data to file. I've seen Windows Machines crash
			// due to the load of encoding a series of 4K images where each image took over 1 second to encode. In
			// our case, we're only encoding one image, so it should be fine. Also, I noticed a nice bump in speed
			// for the Unity Recorder when switching from Unity 2017.4 to Unity 2018.1+, and that thing was also
			// using the Texture2D encoding methods. Anyways, I'm adding a yield return null at the end of the 
			// encoding method because I don't want to stallout the editor to the point of crashing. I think that 
			// not having it will not be too much of an issue, but I'd rather play it safe on this one.
			// -FCT
			var exrEncodedResult = finalResults.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
			yield return null;

			File.WriteAllBytes(assetFoldrPath + "/" + finalResults.name + ".exr", exrEncodedResult);

			yield return null;


			//var assetPath = assetFoldrPath + "/" + finalResults.name + ".asset";
			//AssetDatabase.CreateAsset(finalResults, assetPath);

			yield return new EditorWaitForSeconds(0.15f);

			s_bakingCoroutine = null;
			s_currentBakeState = BakeState.SettingSelection;
		}


		#region Internal Struct Definitions

		/// <summary>
		/// One thing I've learned the hard way about creating tools in Unity is that a bool isn't always enough 
		/// when you are dealing with multiple threads or different loop stages.
		/// -FCT
		/// </summary>
		private enum BakeState
		{
			SettingSelection,
			Prep,
			Bake,
			Finalize
		}

		#endregion

	}
}
