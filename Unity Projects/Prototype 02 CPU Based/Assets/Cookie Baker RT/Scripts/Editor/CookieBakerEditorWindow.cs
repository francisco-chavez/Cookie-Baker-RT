
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

using RandomS = System.Random;


namespace FCT.CookieBakerP02
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
			var meshObjecRefData			= new List<MeshObject>(processMeshRenderer.Count);
			var indexList					= new List<int>();
			var vertexList					= new List<Vector3>(processMeshRenderer.Count * 50);

			for (int i = 0; i < processMeshRenderer.Count; i++)
			{
				var mesh				= processMeshFilter[i].sharedMesh;
				var meshVerts			= mesh.vertices;
				var meshTriangleIndices = mesh.triangles;

				var meshRefDatum = new MeshObject()
				{
					LocalToWorldMatrix	= processMeshRenderer[i].localToWorldMatrix,
					IndicesOffset		= indexList.Count,				// The starting index of the vert-index for this object's mesh.
					IndicesCount		= meshTriangleIndices.Length,	// The number of indices used to form all of the mesh triangles.
					VerticesOffset		= vertexList.Count,
					Bounds				= new Bounds(processMeshRenderer[i].bounds)
				};

				meshObjecRefData.Add(meshRefDatum);
				vertexList.AddRange(meshVerts);
				indexList.AddRange(meshTriangleIndices);
			}

			yield return null;

			var threadArgs = new MainBakeArgs()
			{
				ObjectData				= meshObjecRefData,
				Vertices				= vertexList,
				Indices					= indexList,

				ImageResolution			= resolution,

				LightSourcePosition		= lightCenter,
				LightSourceForward		= s_currentLightComponent.transform.forward,
				LightSourceUpward		= s_currentLightComponent.transform.up,
				LightSourceRightward	= s_currentLightComponent.transform.right,
				LightSourceTheata		= Mathf.Deg2Rad * s_currentLightComponent.spotAngle / 2.0f
			};

			using (var mainBakeThread = new BackgroundWorker())
			{

				mainBakeThread.DoWork += MainBakeThread_DoWork;
				yield return null;
				mainBakeThread.RunWorkerAsync(threadArgs);
				s_currentBakeState = BakeState.Bake;
				yield return null;

				while (!threadArgs.Complete)
					yield return new EditorWaitForSeconds(0.2f);
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

			finalResults.SetPixels(threadArgs.Result);
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
			s_bakingCoroutine	= null;     // The coroutines for MonoBehaviors have the option to manipulate them from the outside, which I have 
											// made use of. With a bit of luck, those functions will be added to the EditorCoroutines at some point 
											// in time.
											// -FCT
			yield return null;
			s_currentBakeState = BakeState.SettingSelection;
		}

		private void MainBakeThread_DoWork(object sender, DoWorkEventArgs e)
		{
			var args = e.Argument as MainBakeArgs;

			Vector2		pixelOffset		= 0.5f * Vector2.one;
			float		halfSize		= s_shadowFocusDistance * Mathf.Tan(args.LightSourceTheata);
			float		colorAdjustment = 1.0f / s_sampleCount;

			RandomS		random			= new RandomS(0);

			// Create an N by N array of Colors;
			Vector3[][]	result			= new Vector3[args.ImageResolution][];
			for (int i = 0; i < args.ImageResolution; i++)
				result[i] = new Vector3[args.ImageResolution];


			ParallelOptions threadingOptionsOuterLoop = new ParallelOptions()
			{
				MaxDegreeOfParallelism = 3
			};
			ParallelOptions theadingOptionsInnerLoop = new ParallelOptions()
			{
				MaxDegreeOfParallelism = 3
			};

			// The way I leared it, you want the outer most loop to be the threaded loop in order to decrease the
			// performance loss from starting and stopping theads. Yet, using the middle loop will already provide
			// us with quite a bit of work per thread and splitting it up by sample isn't a bad way to tack our 
			// progress.
			// -FCT
			for (int i = 0; i < s_sampleCount; i++)
			{
				s_bakeProgress = i;


				// Note: Normally, you don't do something like replace "Vector2.one" with "new Vector2(1.0f, 1.0f)" 
				//		 until after you do performance testing and look at the metrics. Yet, I know for a fact that
				//		 "VectorN.one" has less performance than just doing "new VectorN(1.0f,..., 1.0f)" because it
				//		 it does an additional call stack alloction than calling the constructor directly. In the other
				//		 code that I've writen for this project, I have been using VectorN.one, but inside the loop
				//		 code that gets called hundreds of thousands of times and takes forever to run, I figured I 
				//		 might as well do that know.
				//		 -FCT
				Parallel.For(0, args.ImageResolution, threadingOptionsOuterLoop, pixY => 
				{
					Parallel.For(0, args.ImageResolution, theadingOptionsInnerLoop, pixX =>
					{
						///
						/// Convert pixel coordinates into UV coordinates.
						/// 
						Vector2 uv = new Vector2(pixX, pixY) + pixelOffset;
						uv /= args.ImageResolution;
						uv *= 2.0f;
						uv -= new Vector2(1.0f, 1.0f);

						///
						/// Create initial lightRay based on UV coordinates and shadow plane intersection.
						/// 
						LightRay lightRay = CreateInitialLightRay(uv, halfSize, args);

						///
						/// Tracing Code
						/// 
						for (int j = 0; j < s_maxBounceCount; j++)
						{
							RayHit hit = Trace(lightRay, args);

							if (hit.HasAHit)
							{
								lightRay.Color		*= 0.5f;
								lightRay.Direction	= hit.Normal;
								lightRay.Origin		= hit.Position + (0.0005f * hit.Normal);
							}
							else
							{
								j = s_maxBounceCount;
							}
						}

						///
						/// Convert our lightRay into a point on the shadow plane.
						/// 

						Vector3 N = -args.LightSourceForward;
						float dot_N_LightRayDir = Vector3.Dot(N, lightRay.Direction);
						if (dot_N_LightRayDir > -float.Epsilon)
							return;

						Vector3 v0			= (s_shadowFocusDistance * args.LightSourceForward) + args.LightSourcePosition;
						Vector3 p0			= args.LightSourcePosition;

						float	s_i			= Vector3.Dot(-N, p0 - v0) / dot_N_LightRayDir;
						Vector3 lightPoint	= (s_i * lightRay.Direction) + p0;

						///
						/// Convert out lightPoint from a World-Space coord into a uv-coord
						/// 
						Vector3 planeCoord	= lightPoint - v0;
						float	uOffset		= Vector3.Dot(planeCoord, args.LightSourceRightward);
						float	vOffset		= Vector3.Dot(planeCoord, args.LightSourceUpward);
						Vector2 uvPrime		= (new Vector2(uOffset, vOffset)) / halfSize;

						if (uvPrime.x < -1.0f || +1.0f <= uvPrime.x)
							return;
						if (uvPrime.y < -1.0f || +1.0f <= uvPrime.y)
							return;

						///
						/// Convert our new uv coord (uvPrime) into a pixel index to add to the correct pixel.
						///
						Vector2 pixPrime = uvPrime + new Vector2(1.0f, 1.0f);
						pixPrime *= (args.ImageResolution / 2.0f);

						result[(int) pixPrime.y][(int) pixPrime.x] += lightRay.Color;

					});	// End PixX Loop
				}); // End PixY Loop


				// I tried using Unity's Random, but it turns out that even Unity's Random is locked to the main 
				// thread. Maybe it would work if I were to use the job system to thread this, but after the failure of
				// dispatching a Compute Shader from a single job, I'm not sure it would be worth the effort.
				//
				// Select the next pixel sample offset and make sure that it ranges [0.0, 1.0). While System.Random's
				// method for generating random doubles claims to be in the range that I want, I am also down-casting
				// to a float. I don't know if it's possible for a double to round up to a one when down-casting, but
				// I'm not taking that chance. So, if we get a one in the pixel offset, we'll try again.
				// -FCT
				do
				{
					pixelOffset.x = (float) random.NextDouble();
				} while (!(pixelOffset.x < 1.0f));
				do
				{
					pixelOffset.y = (float) random.NextDouble();
				} while (!(pixelOffset.y < 1.0f));
			}


			var finalResult = new Color[args.ImageResolution * args.ImageResolution];
			Parallel.For(0, args.ImageResolution, y =>
			{
				int rowOffset = y * args.ImageResolution;
				for (int x = 0; x < args.ImageResolution; x++)
				{
					var colorValues = colorAdjustment * result[y][x];
					finalResult[rowOffset + x] = new Color(colorValues.x, colorValues.y, colorValues.z, 1.0f);
				}

				result[y] = null;
			});

			args.Result = finalResult;
			args.Complete = true;
		}

		private static RayHit Trace(LightRay lightRay, MainBakeArgs bakeArgs)
		{
			RayHit bestHit = BlankHit();

			for (int i = 0; i < bakeArgs.ObjectData.Count; i++)
			{
				var objectDatum = bakeArgs.ObjectData[i];

				if (!objectDatum.Bounds.IntersectsLightRay(lightRay))
					continue;

				for (int j = 0; j < objectDatum.IndicesCount; j += 3)
				{
					int subIndex0 = j + objectDatum.IndicesOffset;

					// Going from an object's Local-Space and into World-Space requires matrix-multiplication. For this to
					// work, we need to Vector4s with a 'w' component that contains a value of 1.0. This value of 1.0
					// allows the matrix multiplication to perfrom a transpose that will carry over into the result. It's
					// one of the reasons for using "w = 1.0" for positions and "w = 0.0" for directions.
					// -FCT
					Vector3 v0			= objectDatum.LocalToWorldMatrix * bakeArgs.Vertices[bakeArgs.Indices[subIndex0    ] + objectDatum.VerticesOffset].Position();
					Vector3 v1			= objectDatum.LocalToWorldMatrix * bakeArgs.Vertices[bakeArgs.Indices[subIndex0 + 1] + objectDatum.VerticesOffset].Position();
					Vector3 v2			= objectDatum.LocalToWorldMatrix * bakeArgs.Vertices[bakeArgs.Indices[subIndex0 + 2] + objectDatum.VerticesOffset].Position();

					float	s			= float.MaxValue;
					Vector3 n			= new Vector3(0.0f, 0.0f, 0.0f);

					bool	theresAHit	= TriangleIntersect(lightRay, v0, v1, v2, bakeArgs.LightSourcePosition, out s, out n);

					if (theresAHit)
					{
						if (s < bestHit.Distance)
						{
							bestHit.Distance	= s;
							bestHit.Position	= lightRay.Origin + (s * lightRay.Direction);
							bestHit.Normal		= n;
							bestHit.HasAHit		= true;
						}
					}
				}	// End loop that checks all triangles for current object
			}	// End loop that checks all objects

			if (bestHit.HasAHit)
				bestHit.Normal = bestHit.Normal.normalized;

			return bestHit;
		}

		private static bool TriangleIntersect(LightRay lightRay, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 lightSourcePos, out float s, out Vector3 n)
		{
			s = float.MaxValue;

			Vector3 edge0 = v1 - v0;
			Vector3 edge1 = v2 - v0;

			// Unity uses clockwise vert winding for a triangle's forward direction, and it uses a 
			// Left-Handed-Space which is something that must be kept in mind when doing cross products. 
			// * Now, place vert0 at the bottom-left, vert1 at the top-right, and vert2 at
			//   the bottom-right.
			// * Place your left-hand at vert0.
			// * Point your index-finger (of your left-hand) to vert1.
			// * Point your middle-finger (of your left-hand) to vert2.
			// * Stick our your thumb. It should be pointing at your face. 
			// You just did a rough estimate of the directions in a left-handed cross product. So, your 
			// index-finger was edge0, which goes from vert0's position to vert1's position. Your middle-finger
			// was edge1, which goes from vert0's position to vert2's position.

			// This is the normal vector of the triangle that's created by our verts.
			n = Vector3.Cross(edge0, edge1);
			//n = Vector3.Normalize(n);

			// We want the light ray to head towards the triangle surface, but we also want it to hit the front 
			// side of the serface. If the dot-product of the surface-normal 'n' and the light's-direction are 
			// close to zero, then the ray is traveling parallel to the surface and will never get closer or 
			// further away. This means, that unless the ray starts on the surface, it will never touch the 
			// surface. So, we'll just take out anything that ranges in [-EPSILON, +EPSILON]. 
			// 
			// Second, if the light-ray is heading to the surface from behind, then this dot product will be a 
			// positive value. For now, we will be using closed surfaces. And, when dealing with closed surfaces, 
			// the light-ray will never hit the back of a surface because. Because of this, we'll take out any dot
			// product in the range of [0, +1]. Please keep in mind that all dot-products will be in the range of
			// [-1.0, +1.0] because we are using normalized vectors.
			//
			// So, any dot-product that runs in the range of [-EPSILON, +1.0] will not result in a hit for our 
			// current settings.
			float nDotLightRayDir = Vector3.Dot(n, lightRay.Direction);
			if (nDotLightRayDir > -float.Epsilon)
				return false;

			// This is the LightRay's current starting point.
			Vector3 p0 = lightRay.Origin;

			// If the point of origin of our light-ray is one point in a second triangle, and the point at which it
			// hits the plane created by our verts is another point in the triangle. Then the point on the plane
			// that is nearest to the point of origin is the third point of our second triangle. 's' would be the 
			// length of the hypotenuse of our second triangle. The theta for our second triangle would be at the
			// corner marked by our point of origin. We can find theta with the dot product of our normal and the
			// light ray direction. We can find the length of the adjacent side with the dot product of the normal
			// and the difference between the point of origin and any point on the plain. By combining the length
			// of our adjacent side and theta, we can find the length of the hypotenuse.
			//
			// This is the scale factor by which we multiple our lightRay's direction in order to find the offset 
			// for the intersection point. This would be the size of the hypotenuse of that other triangle we were
			// talking about.
			s = Vector3.Dot(-n, p0 - v0) / nDotLightRayDir;

			// This is our intersection point with the plane. "s * lightRay.Direction" was the hypotenuse.
			Vector3 intersectionPoint = (s * lightRay.Direction) + p0;

			/// 
			/// Check to see if we are within the actionable range. If we are outside of the actionable range, then 
			/// it doesn't matter if we intersected a triangle, otherwise, what was the point of giving the user the
			/// option of selecting an actionable range.
			///
			Vector3 deltaFromLightSource = intersectionPoint - lightSourcePos;
			float distFromLight2 = Vector3.Dot(deltaFromLightSource, deltaFromLightSource);
			if (distFromLight2 < (s_innerRadius * s_innerRadius))
				return false;	// This intersection point was within the exclusion zone.
			if (distFromLight2 > (s_outerRadius * s_outerRadius))
				return false;	// This intersection point was outside of the inclusion zone.

			/// 
			/// We now know where the light will hit the triangle's plane, but we don't know if it will hit the 
			/// triangle itself. That's the next thing we need to check.
			/// -FCT
			///

			// The cross product of edge0 and n, gives me a vector that points away from the direction that's 
			// inside the triangle from edge0. Since this vector is in the wrong direction, than any point away
			// from a point on edge0 (like vert0), should create a non-positive value when we take its dot product
			// with vector 'c'. And, if that value isn't non-positive (0 is OK), then it's outside of the triangle.
			// -FCT
			Vector3 c = Vector3.Cross(edge0, n);
			Vector3 delta = intersectionPoint - v0;
			if (Vector3.Dot(delta, c) > 0)
				return false;

			// 
			// Before, the cross product gave us a vector that was pointing away from the insdie of the triangle.
			// But, before, we were using an edge that was following the clock-wise direction that creates the 
			// forward (front-facing) direction of our triangle. 'edge1' doesn't follow the clock-wise direction,
			// and because of this, it gave us a vector that points to the inside of the triangle. Since our 
			// directional vector now points to the inside of the triangle, now it's the negative values created by
			// our dot-product that indicate a point outside the triangle.
			// -FCT
			c = Vector3.Cross(edge1, n);
			if (Vector3.Dot(delta, c) < 0)
				return false;

			// This one follows a clock-wise direction, so it'll be like edge0.
			c = Vector3.Cross((v2 - v1), n);
			delta = intersectionPoint - v1;		// We want our reference point to be on the edge we used to create 'c'.
			if (Vector3.Dot(delta, c) > 0)
				return false;

			return true;
		}

		private static LightRay CreateInitialLightRay(Vector2 uv, float halfSize, MainBakeArgs bakeArgs)
		{
			var intersectionPointOffset = ((uv.x * halfSize) * bakeArgs.LightSourceRightward)
										+ ((uv.y * halfSize) * bakeArgs.LightSourceUpward)
										+ (s_shadowFocusDistance * bakeArgs.LightSourceForward);

			LightRay lightRay = new LightRay()
			{
				Color		= new Vector3(1.0f, 1.0f, 1.0f),
				Origin		= bakeArgs.LightSourcePosition,
				Direction	= intersectionPointOffset.normalized
			};

			return lightRay;
		}

		/// <summary>
		/// Because a certain company doesn't trust software-developers to be able to create a default 
		/// constructor for a struct.
		/// </summary>
		private static RayHit BlankHit()
		{
			var hit = new RayHit()
			{
				Distance	= float.MaxValue,
				HasAHit		= false,
				Normal		= new Vector3(),
				Position	= new Vector3()
			};
			return hit;
		}


		#region Internal Struct Definitions

		private struct RayHit
		{
			public Vector3	Position;
			public float	Distance;
			public Vector3	Normal;
			public bool		HasAHit;
		}

		private class MainBakeArgs
		{
			public bool				Complete				= false;
			public object			LockObject				= new object();
			public Color[]			Result;

			public List<MeshObject> ObjectData;
			public List<Vector3>	Vertices;
			public List<int>		Indices;

			public int				ImageResolution;

			public Vector3			LightSourcePosition;
			public Vector3			LightSourceForward;
			public Vector3			LightSourceUpward;
			public Vector3			LightSourceRightward;
			public float			LightSourceTheata;
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

		/// <summary>
		/// I'm using http://blog.three-eyed-games.com/2019/03/18/gpu-path-tracing-in-unity-part-3/ as my 
		/// starting point for the Ray Tracing, and will then add or alter things as needed. This struct will
		/// work as a object specific data holder. This way, if two or more object share a mesh, they can still
		/// be told apart by their Local to World Matrix. As time goes on, we'll be adding more information.
		/// </summary>
		/// <remarks>
		/// Size: 102 bytes
		/// </remarks>
		private struct MeshObject
		{
			public Matrix4x4				LocalToWorldMatrix; // 16 floats, so 64 bytes
			public Int32					IndicesOffset;		// 4 bytes
			public Int32					IndicesCount;       // 4 bytes
			public Int32					VerticesOffset;     // 4 bytes
			public CookieBakerP02.Bounds	Bounds;				// 9 floats, so 36 bytes
		}

		#endregion

	}
}
