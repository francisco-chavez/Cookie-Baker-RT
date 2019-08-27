
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public class BakeJob
	{

		#region Job Settings

		public int					JobID;

		public int					SampleCount;
		public float				MinRange;
		public float				MaxRange;
		public float				ShadowfocusPlane;
		public int					Resolution;
		public int					BounceCount;

		public Vector4				LightSourcePosition;
		public Vector4				LightSourceForward;
		public Vector4				LightSourceUpward;
		public Vector4				LightSourceRightward;

		public ObjectMeshDatum[]	ObjectData;
		public Vector3[]			Vertices;
		public int[]				Indices;

		#endregion


		public bool JobComplete { get; private set; }


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

		}

		public void Update()
		{

		}

	}
}
