
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public class BakeJob
	{

		#region Attributes and Properties

		#region Job Settings

		public	int					JobID;

		public	int					SampleCount;
		public	float				MinRange;
		public	float				MaxRange;
		public	float				ShadowFocusPlane;
		public	int					Resolution;
		public	int					BounceCount;

		public	Vector4				LightSourcePosition;
		public	Vector4				LightSourceForward;
		public	Vector4				LightSourceUpward;
		public	Vector4				LightSourceRightward;
		public	float				LightSourceTheta;

		public	ObjectMeshDatum[]	ObjectData;
		public	Vector3[]			Vertices;
		public	int[]				Indices;

		#endregion

		public	ComputeShader		ComputeShader;

		private RenderTexture		_renderTexture;

		private int					_kernalID;
		private int					_uvOffsetID;
		private int					_sampleCountID;
		private int					_maxSegmentsID;

		private int					_shadowFocusDistanceID;
		private int					_imageResolutionID;
		private int					_lightPositionID;
		private int					_lightForwardID;

		private int					_lightUpwardID;
		private int					_lightRightwardID;
		private int					_spotlightThetaID;
		private int					_innerRangeID;

		private int					_outerRangeID;
		private int					_meshDataID;
		private int					_verticesID;
		private int					_indicesID;

		private int					_renderTextureID;

		private ComputeBuffer		_objectDataBuffer;
		private ComputeBuffer		_vertexDataBuffer;
		private ComputeBuffer		_indexDataBuffer;


		public	ProcessorScript		Processor		{ get; set; }
		public	bool				JobComplete		{ get; private set; }
		public	int					BakeProgress	{ get; private set; }

		#endregion


		public BakeJob()
		{
			SampleCount				= -1;
			MinRange				=  0.0f;
			MaxRange				=  0.0f;
			ShadowFocusPlane		=  0.0f;
			Resolution				= -1;
			BounceCount				= -1;

			LightSourceForward		= Vector4.zero;
			LightSourcePosition		= Vector4.zero;
			LightSourceRightward	= Vector4.zero;
			LightSourceUpward		= Vector4.zero;

			ObjectData				= null;
			Vertices				= null;
			Indices					= null;

			JobComplete				= false;
		}


		public void CancelJob()
		{
			Cleanup();
		}

		public void StartJob()
		{
			BakeProgress = 0;
			Processor.SendUpdate();

			_renderTexture = new RenderTexture(Resolution,						// Width
											   Resolution,						// Height
											   0,								// Depth/Stencel Buffer
											   RenderTextureFormat.ARGBFloat,	// Pixel Type
											   RenderTextureReadWrite.Linear)	// Gama/Linear Choice
			{
				anisoLevel			= 1,
				antiAliasing		= 1,
				autoGenerateMips	= false,
				enableRandomWrite	= true,
				filterMode			= FilterMode.Point,
				useMipMap			= false,
				wrapMode			= TextureWrapMode.Clamp,
				hideFlags			= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave | HideFlags.HideInHierarchy
			};
			_renderTexture.Create();


			_kernalID				= ComputeShader.FindKernel("CSMain");

			_imageResolutionID		= Shader.PropertyToID("_ImageResolution");
			_indicesID				= Shader.PropertyToID("_Indices");
			_innerRangeID			= Shader.PropertyToID("_InnerRange");
			_lightForwardID			= Shader.PropertyToID("_LightForwardDir");

			_lightPositionID		= Shader.PropertyToID("_LightPosition");
			_lightRightwardID		= Shader.PropertyToID("_LightRightwardDir");
			_lightUpwardID			= Shader.PropertyToID("_LightUpwardDir");
			_maxSegmentsID			= Shader.PropertyToID("_MaxSegments");

			_meshDataID				= Shader.PropertyToID("_MeshObjectData");
			_outerRangeID			= Shader.PropertyToID("_OuterRange");
			_renderTextureID		= Shader.PropertyToID("_Result");
			_sampleCountID			= Shader.PropertyToID("_SampleCount");

			_shadowFocusDistanceID	= Shader.PropertyToID("_ShadowFocusDistance");
			_spotlightThetaID		= Shader.PropertyToID("_SpotLightAngleRad");
			_uvOffsetID				= Shader.PropertyToID("_UV_Offset");
			_verticesID				= Shader.PropertyToID("_Vertices");

			_objectDataBuffer		= new ComputeBuffer(ObjectData.Length, 100, ComputeBufferType.Default);
			_vertexDataBuffer		= new ComputeBuffer(Vertices.Length, 12, ComputeBufferType.Default);
			_indexDataBuffer		= new ComputeBuffer(Indices.Length, 4, ComputeBufferType.Default);

			_objectDataBuffer.SetData(ObjectData);
			_vertexDataBuffer.SetData(Vertices);
			_indexDataBuffer.SetData(Indices);
		}

		public void Update()
		{
			if (JobComplete)
				return;

			///
			/// Passing data to the Compute shader for ray-tracing
			/// 
			ComputeShader.SetBuffer(_kernalID, _meshDataID, _objectDataBuffer);
			ComputeShader.SetBuffer(_kernalID, _verticesID, _vertexDataBuffer);
			ComputeShader.SetBuffer(_kernalID, _indicesID, _indexDataBuffer);

			ComputeShader.SetFloat(_innerRangeID, MinRange);
			ComputeShader.SetFloat(_outerRangeID, MaxRange);
			ComputeShader.SetFloat(_shadowFocusDistanceID, ShadowFocusPlane);
			ComputeShader.SetFloat(_spotlightThetaID, LightSourceTheta);

			ComputeShader.SetInt(_imageResolutionID, Resolution);
			ComputeShader.SetInt(_maxSegmentsID, BounceCount);
			ComputeShader.SetInt(_sampleCountID, SampleCount);

			ComputeShader.SetTexture(_kernalID, _renderTextureID, _renderTexture);

			ComputeShader.SetVector(_lightForwardID, LightSourceForward);
			ComputeShader.SetVector(_lightRightwardID, LightSourceRightward);
			ComputeShader.SetVector(_lightUpwardID, LightSourceUpward);
			ComputeShader.SetVector(_lightPositionID, LightSourcePosition);

			// Unity's Random gives us a value in the range of [0.0, 1.0] and what we want is a value in the range 
			// of [0.0, 1.0), so for the few cases where we get a value of 1.0, we'll just ask for a new value.
			// -FCT
			var uvOffset = new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
			do
			{
				uvOffset.x = Random.value;
			} while (uvOffset.x < 1.0f);
			do
			{
				uvOffset.y = Random.value;
			} while (uvOffset.y < 1.0f);

			ComputeShader.SetVector(_uvOffsetID, uvOffset);


			///
			/// Running the ray-tracing code.
			/// 
			ComputeShader.Dispatch(_kernalID, 8, 8, 1);

			///
			/// Tracking/Updating Progress
			/// 
			BakeProgress++;
			JobComplete = BakeProgress == SampleCount;
			Processor.SendUpdate();
		}

		public Color[] Finish()
		{
			Texture2D systemTexture = new Texture2D(Resolution, Resolution, TextureFormat.RGBAFloat, false, true);
			systemTexture.filterMode = FilterMode.Point;
			systemTexture.wrapMode = TextureWrapMode.Clamp;

			RenderTexture.active = _renderTexture;
			systemTexture.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0, false);
			systemTexture.Apply();

			RenderTexture.active = null;

			var pixels = systemTexture.GetPixels();

			Cleanup();

			return pixels;
		}

		private void Cleanup()
		{
			if (_renderTexture != null)
			{
				_renderTexture.Release();
				_renderTexture = null;
			}

			if (_objectDataBuffer != null)
			{
				_objectDataBuffer.Release();
				_objectDataBuffer.Dispose();
				_objectDataBuffer = null;
			}

			if (_vertexDataBuffer != null)
			{
				_vertexDataBuffer.Release();
				_vertexDataBuffer.Dispose();
				_vertexDataBuffer = null;
			}

			if (_indexDataBuffer != null)
			{
				_indexDataBuffer.Release();
				_indexDataBuffer.Dispose();
				_indexDataBuffer = null;
			}

			ObjectData	= null;
			Vertices	= null;
			Indices		= null;

			Processor	= null;
		}
	}
}
