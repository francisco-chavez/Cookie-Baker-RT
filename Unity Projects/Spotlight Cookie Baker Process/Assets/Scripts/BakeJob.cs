
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
		public	float				ShadowfocusPlane;
		public	int					Resolution;
		public	int					BounceCount;

		public	Vector4				LightSourcePosition;
		public	Vector4				LightSourceForward;
		public	Vector4				LightSourceUpward;
		public	Vector4				LightSourceRightward;

		public	ObjectMeshDatum[]	ObjectData;
		public	Vector3[]			Vertices;
		public	int[]				Indices;

		public ComputeShader		ComputeShader;

		#endregion


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


		public	bool				JobComplete		{ get; private set; }
		public	int					BakeProgress	{ get; private set; }

		#endregion


		public BakeJob()
		{
			SampleCount				= -1;
			MinRange				=  0.0f;
			MaxRange				=  0.0f;
			ShadowfocusPlane		=  0.0f;
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

		}

		public void StartJob()
		{
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
			ComputeShader.SetBuffer(_kernalID, _meshDataID, _objectDataBuffer);
		}

	}
}
