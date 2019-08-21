
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

	}
}
