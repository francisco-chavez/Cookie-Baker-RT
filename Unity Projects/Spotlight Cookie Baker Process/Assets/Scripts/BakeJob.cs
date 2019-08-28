
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public class BakeJob
	{

		#region Attributes and Properties

		#region Job Settings

		public int					JobID;

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
		}

		public void Update()
		{

		}

	}
}
