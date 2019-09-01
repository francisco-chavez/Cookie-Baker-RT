
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public struct ObjectMeshDatum
	{
		// 3 + 16 + 6 = 25
		// 32 / 8 = 4
		// 25 * 4 = 100
		public int			IndicesOffset;			// 32
		public int			IndicesCount;			// 32
		public int			VerticesOffset;			// 32
		public Matrix4x4	LocalToWorldMatrix;		// 32 * 4 * 4
		public AABB_Bounds	BoundingBox;			// 32 * 3 * 2
	}
}
