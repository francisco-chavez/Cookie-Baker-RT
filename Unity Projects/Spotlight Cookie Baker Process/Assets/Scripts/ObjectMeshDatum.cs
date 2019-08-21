
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public struct ObjectMeshDatum
	{
		public int			IndicesOffset;
		public int			IndicesCount;
		public int			VerticesOffset;
		public Matrix4x4	LocalToWorldMatrix;
		public AABB_Bounds	BoundingBox;
	}
}
