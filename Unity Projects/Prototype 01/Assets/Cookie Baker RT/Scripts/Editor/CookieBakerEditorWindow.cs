﻿
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

		/// <summary>
		/// Anthing that's closer to our light source than this inner radius will not be used in our shadow generation.
		/// </summary>
		private static			float			s_innerRadius				= 0.01f;

		/// <summary>
		/// Anything that's further away than this distance from our light source will not be used in our shadow generation.
		/// </summary>
		private static			float			s_outerRadius				= 0.20f;

		private static			ComputeShader	s_computeShader;

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

			SceneView.RepaintAll();
		}

		private void OnEnable()
		{
			SceneView.onSceneGUIDelegate += MyOnGizmo;

			if (s_computeShader == null)
				s_computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Cookie Baker RT/Shaders/CookieBakerComputeShader.compute");
		}

		private void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= MyOnGizmo;
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

			GUIContent guiContent = null;

			EditorGUILayout.LabelField("Object Selection:");
			EditorGUI.indentLevel++;
			///
			/// Get the range over which we'll be raytracing.
			/// 
			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Bake Item Range:", "Only items within the selected range can affect the results of the raytracing.");
			EditorGUILayout.MinMaxSlider(guiContent, ref s_innerRadius, ref s_outerRadius, 0.001f, 1.0f);
			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			///
			/// Get the resolution for the generated cookie.
			/// 
			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Cookie Resolution:");
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


		private void MyOnGizmo(SceneView sceneView)
		{
			if (s_currentLightComponent == null)
				return;

			var worldLightPos = s_currentLightComponent.transform.position;

			/// Note: The SphereHandleCap method takes in the diameter of the sphere for the sphere's size. Normally, when 
			///		  I think of spheres and circles sizes in terms of their radius because it's easier to apply that to
			///		  the various measurements you might do. Yet, when you stop to think about it, when you put a bounding
			///		  box around the sphere, the size of the bounding box will be the diameter length, making it a better
			///		  parameter than radius to an API User.
			///		  -FCT
			Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.2f);
			Handles.SphereHandleCap(0, worldLightPos, Quaternion.identity, 2.0f * s_innerRadius, EventType.Repaint);

			Handles.color = new Color(0.0f, 1.0f, 0.0f, 0.1f);
			Handles.SphereHandleCap(0, worldLightPos, Quaternion.identity, 2.0f * s_outerRadius, EventType.Repaint);
		}

		private IEnumerator CookieBaking()
		{
			yield return null;

			yield return new EditorWaitForSeconds(1.5f);

			s_currentBakeState = BakeState.Bake;

			yield return new EditorWaitForSeconds(1.5f);

			///
			/// Code for saving our results into the project and applying them to the light component.
			/// 
			s_currentBakeState = BakeState.Finalize;
			yield return null;

			// Create a Texture2D to place our results into. This Texture2D will be added to the project's assets 
			// and it will be applied to the light source we were raytracing.
			var resolution = s_resolutionOptions[s_selectedCookieResolution];
			Texture2D finalResults = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true)
			{
				alphaIsTransparency = true,
				filterMode			= FilterMode.Point,
				wrapMode			= TextureWrapMode.Clamp,
				name				= s_currentLightComponent.name + "_ShadowCookie_" + resolution.ToString()
			};

			yield return null;
			

			// Create an array of pixels to use as a result while we work on the real code.
			Color[] colors = new Color[resolution * resolution];
			for (int y = 0; y < resolution; y++)
				for (int x = 0; x < resolution; x++)
				{
					colors[resolution * y + x] = Color.gray;
				}

			yield return null;


			// Apply the results to the Texture2D
			finalResults.SetPixels(colors);
			finalResults.Apply();

			yield return null;


			// Check for the location where we will be saving the Texture2D. If that location doesn't exist, then 
			// create it.
			var assetFolderPath = "Assets/Light_Cookies/" + s_currentLightComponent.gameObject.scene.name;

			if (!Directory.Exists(assetFolderPath))
			{
				if (!Directory.Exists("Assets/Light_Cookies"))
				{
					AssetDatabase.CreateFolder("Assets", "Light_Cookies");
					yield return null;
				}
				AssetDatabase.CreateFolder("Assets/Light_Cookies", s_currentLightComponent.gameObject.scene.name);
			}
			yield return null;


			// Save the Texture2D as a project asset.
			var assetPath = assetFolderPath + "/" + finalResults.name + ".asset";
			AssetDatabase.CreateAsset(finalResults, assetPath);

			yield return null;

			// Apply the Texture2D to the light as a cooke, then finish cleaning up.
			s_currentLightComponent.cookie = finalResults;
			s_bakingCoroutine = null;   // The coroutines for MonoBehaviors have the option to manipulate them from the outside, which I have 
										// made use of. With a bit of luck, those functions will be added to the EditorCoroutines at some point 
										// in time.
										// -FCT
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
