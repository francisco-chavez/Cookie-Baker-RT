
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Unity.EditorCoroutines;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

using FCT.CookieBakerRT.IPC_DataFormat;
using FlatBuffers;


namespace FCT.CookieBakerP03
{
	[DisallowMultipleComponent]
	public class CookieBakerEditorWindow
		: EditorWindow
	{

		#region Attributes

		/// <summary>
		/// The current overall state of the bake process.
		/// </summary>
		private static			BakeState			s_currentBakeState			= BakeState.SettingSelection;

		/// <summary>
		/// If a valid Light component is currently selected while inserting our settings, it will show up here.
		/// If we are not in setting insertion, this will hold the Light component we are currently baking for.
		/// </summary>
		private static			Light				s_currentLightComponent		= null;

		/// <summary>
		/// This is a collection of the selectable texture resolutions when it comes to importing a texture into 
		/// a Unity project. Because we are creating textures that will be imported and we need to display this 
		/// selection, so we should probably have this information.
		/// </summary>
		private static readonly int[]				s_resolutionOptions			= new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

		/// <summary>
		/// This is the indix of the resolution option that will be used to create the final texture.
		/// </summary>
		private static			int					s_selectedCookieResolution	= 3;

		/// <summary>
		/// This is a reference to the coroutine that will manage the bake processes.
		/// </summary>
		private static			EditorCoroutine		s_bakingCoroutine			= null;

		/// <summary>
		/// Anthing that's closer to our light source than this inner radius will not be used in our shadow 
		/// generation.
		/// </summary>
		private static			float				s_innerRadius				= 0.01f;

		/// <summary>
		/// Anything that's further away than this distance from our light source will not be used in our shadow 
		/// generation.
		/// </summary>
		private static			float				s_outerRadius				= 0.20f;

		/// <summary>
		/// The max number of times we will bounce a light ray off of objects.
		/// </summary>
		private static			int					s_maxBounceCount			= 3;

		/// <summary>
		/// This is the focal distance from which we do all of our math for calculating if a light ray adds to 
		/// the cookie-texture.
		/// </summary>
		private static			float				s_shadowFocusDistance		= 50.0f;

		/// <summary>
		/// This is the number of times to sample from each pixel. More samples will result in a more realistic 
		/// result, but increasing the sample count will also increase the run time of the bake processes.
		/// </summary>
		private static			int					s_sampleCount				= 10;

		private static			int					s_bakeProgress				= 0;

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
					// So, it turns out that the cookie for Cone and Pyramid are used in the exact same way. The only real
					// difference is that the cone will do less lighting based on the cone shape, but the cookie (which 
					// we're creating) is used in the exact same way.
					// -FCT
					case SpotLightShape.Cone:
					case SpotLightShape.Pyramid:
						break;

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

			EditorGUILayout.LabelField("Quality Options:");
			EditorGUI.indentLevel++;

			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Max Segment Count:", "This controls the maximum number of times a sample light ray may bounce off of items.");
			s_maxBounceCount = EditorGUILayout.IntSlider(guiContent, s_maxBounceCount, 1, 16);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Sample Count:", "This controls the number of samples we take. Higher sample counts lead to better quality and longer bake times.");
			s_sampleCount = EditorGUILayout.IntSlider(guiContent, s_sampleCount, 1, 500);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Shadow Focus Distance:", "This is the distance at which we measure if a light sample will add to the cookie-texture.");
			s_shadowFocusDistance = EditorGUILayout.Slider(guiContent, s_shadowFocusDistance, 1.01f, 2000.0f);
			EditorGUILayout.EndHorizontal();

			///
			/// Get the resolution for the generated cookie.
			/// 
			EditorGUILayout.BeginHorizontal();
			guiContent = new GUIContent("Cookie Resolution:");
			var resOptions = s_resolutionOptions.Select(res => { return new GUIContent(res.ToString()); });
			s_selectedCookieResolution = EditorGUILayout.Popup(guiContent, s_selectedCookieResolution, resOptions.ToArray());
			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel--;

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

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("The cookie is in the oven.");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUI.ProgressBar(new Rect(3, 45, position.width - 6, 20), ((float)s_bakeProgress) / s_sampleCount, "Bake Progress");
			EditorGUILayout.EndHorizontal();
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
			Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.3f);
			Handles.SphereHandleCap(0, worldLightPos, Quaternion.identity, 2.0f * s_innerRadius, EventType.Repaint);

			Handles.color = new Color(0.0f, 1.0f, 0.0f, 0.1f);
			Handles.SphereHandleCap(0, worldLightPos, Quaternion.identity, 2.0f * s_outerRadius, EventType.Repaint);
		}


		private IEnumerator CookieBaking()
		{
			yield return null;

			var resolution = s_resolutionOptions[s_selectedCookieResolution];


			// We only want to use GameObjects that are within the outer radius that we have set. So, a quick test
			// to see if something is within the ball park would be to check if their bounding box insersects with
			// a bounding box that contains the outer radius. Once that has been done, we can take a closer look at
			// what's left.
			var meshRenders			= FindObjectsOfType<MeshRenderer>();
			var lightCenter			= s_currentLightComponent.transform.position;
			var lightBoundingBox	= new UnityEngine.Bounds(lightCenter, 2.0f * s_outerRadius * Vector3.one);
			var intersectingBounds	= new List<MeshRenderer>();
			foreach (var meshRenderer in meshRenders)
			{
				var otherBounds = meshRenderer.bounds;
				if (otherBounds.Intersects(lightBoundingBox))
					intersectingBounds.Add(meshRenderer);
			}

			yield return null;

			// Now, we can limit things a bit more by finding the point within our mesh bounds that is nearset to
			// the light's center. We then check this point to see if it's within the distance of our outer radius.
			var intersectingOuterRadius = new List<MeshRenderer>();
			foreach (var meshRenderer in intersectingBounds)
			{
				var otherBounds = meshRenderer.bounds;
				var nearsetPointToLight = otherBounds.ClosestPoint(lightCenter);
				if ((nearsetPointToLight - lightCenter).sqrMagnitude <= (s_outerRadius * s_outerRadius))
					intersectingOuterRadius.Add(meshRenderer);
			}

			yield return null;

			var processMeshRenderer = intersectingBounds;
			var processMeshFilter = new List<MeshFilter>();

			// We're reusing the List from intersectingBounds and placing it under a more appropriate name. 
			// Basically, we're reusing a piece of memory that's no longer needed instead of allocating a new List
			// with a new array. Also, this array shouldn't need to be resized to something larger because it 
			// already a capacity that is greater than or equal to the number of items we will be processing.
			processMeshRenderer.Clear();
			intersectingBounds = null;


			// Also, while we're at it, lets make sure that we are only baking items that are static. After all, 
			// what's the point of baking the shadow casting item that might not be there?
			foreach (var meshRender in intersectingOuterRadius)
			{
				var gameObject = meshRender.gameObject;

				// If the gameObject isn't static, than it can be moved. Since we are not updating the cookie at 
				// runtime, having a moving object would be bad.
				if (!gameObject.isStatic)
					continue;

				// The mesh for our object is inside the MeshFilter, so we need that to get the mesh. Also, since we're
				// dealing with static meshes, then we really shouldn't be dealing with any Skinned Mesh Renderers.
				var meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter == null)
					continue;

				// If there's no mesh, then what are we even bothering with this?
				if (meshFilter.sharedMesh == null)
					continue;

				processMeshRenderer.Add(meshRender);
				processMeshFilter.Add(meshFilter);
			}

			yield return null;


			// Todo: With this design, items that use the same mesh will replicate the mesh verts and triangle index 
			//		 arrays. I need to add a way to check if we have already added a mesh and if so, pull up the triangle
			//		 index array information to pass along to the meshRefDatum. 
			var meshObjecRefData			= new List<MeshObjectData>(processMeshRenderer.Count);
			var indexList					= new List<int>();
			var vertexList					= new List<Vector3>(processMeshRenderer.Count * 50);

			for (int i = 0; i < processMeshRenderer.Count; i++)
			{
				var mesh				= processMeshFilter[i].sharedMesh;
				var meshVerts			= mesh.vertices;
				var meshTriangleIndices = mesh.triangles;

				var meshRefDatum = new MeshObjectData()
				{
					LocalToWorldMatrix	= processMeshRenderer[i].localToWorldMatrix,
					IndicesOffset		= indexList.Count,				// The starting index of the vert-index for this object's mesh.
					IndicesCount		= meshTriangleIndices.Length,	// The number of indices used to form all of the mesh triangles.
					VerticesOffset		= vertexList.Count,
					Bounds				= new Bounds()
										  {
												Center = processMeshRenderer[i].bounds.center,
												Extent = processMeshRenderer[i].bounds.extents
										  }
				};

				meshObjecRefData.Add(meshRefDatum);
				vertexList.AddRange(meshVerts);
				indexList.AddRange(meshTriangleIndices);
			}

			yield return null;

			///
			/// Try adding tracing code here.
			///
			var backgroundWorkerArgs = new BackgroundWorkerArgs()
			{
				Indices			= indexList,
				ObjectData		= meshObjecRefData,
				Vertices		= vertexList,

				LightPosition	= lightCenter,
				LightForward	= s_currentLightComponent.transform.forward,
				LightUpward		= s_currentLightComponent.transform.up
			};
			yield return null;

			using (var backgroundWorker = new BackgroundWorker())
			{
				backgroundWorker.DoWork += BackgroundWorker_DoWork;
				backgroundWorker.RunWorkerAsync(backgroundWorkerArgs);

				while (!backgroundWorkerArgs.Complete)
					yield return new EditorWaitForSeconds(0.15f);
			}

			s_currentBakeState = BakeState.Finalize;

			yield return null;


			///
			/// Code for saving our results into the project and applying them to the light component.
			/// 

			// Create a Texture2D to place our results into. This Texture2D will be added to the project's assets 
			// and it will be applied to the light source we were raytracing.
			Texture2D finalResults = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true)
			{
				alphaIsTransparency = true,
				filterMode			= FilterMode.Point,
				wrapMode			= TextureWrapMode.Clamp,
				name				= s_currentLightComponent.name + "_ShadowCookie_" + resolution.ToString()
			};

			yield return null;

			//finalResults.SetPixels(threadArgs.Result);
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
			EditorUtility.SetDirty(s_currentLightComponent.gameObject);
			s_bakingCoroutine	= null;		// The coroutines for MonoBehaviors have the option to manipulate them from the outside, which I have 
											// made use of. With a bit of luck, those functions will be added to the EditorCoroutines at some point 
											// in time.
											// -FCT
			yield return null;
			s_currentBakeState = BakeState.SettingSelection;
		}

		private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			var args = e.Argument as BackgroundWorkerArgs;

			var messageData = SerialzeWorkloadRequest(args);

			args.Complete = true;
		}

		private byte[] SerialzeWorkloadRequest(BackgroundWorkerArgs args)
		{

			int bufferSize = 10;									// A bit of slack

			bufferSize += (4 * 3 * 3);								// Light-Position, Forward-Dir, UpwardDir
			bufferSize += args.Indices.Count * 4;
			bufferSize += args.Vertices.Count * 4 * 3;
			bufferSize += args.ObjectData.Count * ((4 * 4 * 4)		// Matrix 4x4
												+ (4 * 3)			// Offsets (and count)
												+ (2 * 3 * 4));     // Bounds

			bufferSize += 13 * 4;									// A quick quess at the size of the rest of what goes into the WorkRequest
			bufferSize += 2 * 4;									// A quick guess at the size of the root message.


			// With the way that flatbuffers (that's a singular proper-noun that uses a lowercase starting letter, 
			// don't ask me why, I didn't name the thing) works, it encodes the data as a byte-array. Only, it does 
			// this piece by piece, return offsets objects that tells you the location with-in the byte-array. 
			// Then, when you create the larger pieces (tables), you feed it the offsets of the child data that has 
			// already been encoded. For more complicated data where you know that a value or child object gets 
			// shared, you can use the same offset to cutdown on the overall size of the byte-array. This also lets
			// you do things like decreasing the size of the byte-array by leaving out values.
			var builder = new FlatBufferBuilder(bufferSize);

			// It's been a while since I last used flatbuffers, so I don't remember which items are supposed to be 
			// created inside of which other-items. I'll learn as I get error codes telling me what to do.
			// -FCT
			var lightForwardOffset	= Vec3.CreateVec3(builder, args.LightForward.x, args.LightForward.y, args.LightForward.z);
			var lightUpwardOffset	= Vec3.CreateVec3(builder, args.LightUpward.x, args.LightUpward.y, args.LightUpward.z);
			var lightPositionOffset = Vec3.CreateVec3(builder, args.LightPosition.x, args.LightPosition.y, args.LightPosition.z);


			var indicesVectorOffset = WorkloadRequest.CreateIndicesVector(builder, args.Indices.ToArray());

			WorkloadRequest.StartVerticesVector(builder, args.Vertices.Count);
			foreach (var vert in args.Vertices)
				Vec3.CreateVec3(builder, vert.x, vert.y, vert.z);
			var vertsVectorOffset = builder.EndVector();

			WorkloadRequest.StartObjectDataVector(builder, args.ObjectData.Count);
			foreach (var objectDatum in args.ObjectData)
			{
				// I wasn't expecting the constructor of a struct with embedded structs to rollout the embedded 
				// structs, but it does make sense. How else would we created the embedded structs without adding them
				// to the builder first and then setting their offset into the parent struct? Doing it this way cuts 
				// down on the size of the byte-arra, but it really is a bit of a pain to type this out without any 
				// mistakes.
				// -FCT
				MeshObject.CreateMeshObject(builder,
					objectDatum.LocalToWorldMatrix.m00, objectDatum.LocalToWorldMatrix.m01, objectDatum.LocalToWorldMatrix.m02, objectDatum.LocalToWorldMatrix.m03,
					objectDatum.LocalToWorldMatrix.m10, objectDatum.LocalToWorldMatrix.m11, objectDatum.LocalToWorldMatrix.m12, objectDatum.LocalToWorldMatrix.m13,
					objectDatum.LocalToWorldMatrix.m20, objectDatum.LocalToWorldMatrix.m21, objectDatum.LocalToWorldMatrix.m22, objectDatum.LocalToWorldMatrix.m23,
					objectDatum.LocalToWorldMatrix.m30, objectDatum.LocalToWorldMatrix.m31, objectDatum.LocalToWorldMatrix.m32, objectDatum.LocalToWorldMatrix.m33,
					objectDatum.IndicesOffset, objectDatum.IndicesCount, objectDatum.VerticesOffset,
					objectDatum.Bounds.Center.x, objectDatum.Bounds.Center.y, objectDatum.Bounds.Center.z,
					objectDatum.Bounds.Extent.x, objectDatum.Bounds.Extent.y, objectDatum.Bounds.Extent.z);
			}
			var objectDataVectorOffset = builder.EndVector();


			WorkloadRequest.StartWorkloadRequest(builder);

			WorkloadRequest.AddLightSourceForwardDir(builder, lightForwardOffset);
			WorkloadRequest.AddLightSourceUpwardDir(builder, lightUpwardOffset);
			WorkloadRequest.AddLightSourcePosition(builder, lightPositionOffset);

			WorkloadRequest.AddBounceCount(builder, s_maxBounceCount);
			WorkloadRequest.AddMaxRange(builder, s_outerRadius);
			WorkloadRequest.AddMinRange(builder, s_innerRadius);
			WorkloadRequest.AddSampleCount(builder, s_sampleCount);
			WorkloadRequest.AddShadowFocusPlane(builder, s_shadowFocusDistance);
			WorkloadRequest.AddWorklodID(builder, 25);
			WorkloadRequest.AddResolution(builder, s_resolutionOptions[s_selectedCookieResolution]);

			WorkloadRequest.AddIndices(builder, indicesVectorOffset);
			WorkloadRequest.AddVertices(builder, vertsVectorOffset);
			WorkloadRequest.AddObjectData(builder, objectDataVectorOffset);

			var workloadRequestOffset = WorkloadRequest.EndWorkloadRequest(builder);


			Message.StartMessage(builder);

			Message.AddData(builder, workloadRequestOffset.Value);
			Message.AddDataType(builder, MessageDatum.WorkloadRequest);

			var messageOffset = Message.EndMessage(builder);

			builder.Finish(messageOffset.Value);
			return builder.SizedByteArray();
		}


		#region Internal Struct Definitions

		private class BackgroundWorkerArgs
		{
			public bool					Complete		= false;

			public List<MeshObjectData> ObjectData;
			public List<int>			Indices;
			public List<Vector3>		Vertices;

			public Vector3				LightForward;
			public Vector3				LightUpward;
			public Vector3				LightPosition;
		}

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

		private struct MeshObjectData
		{
			public UnityEngine.Matrix4x4	LocalToWorldMatrix;
			public Int32					IndicesOffset;
			public Int32					IndicesCount;
			public Int32					VerticesOffset;	
			public CookieBakerP03.Bounds	Bounds;	
		}

		#endregion

	}
}
