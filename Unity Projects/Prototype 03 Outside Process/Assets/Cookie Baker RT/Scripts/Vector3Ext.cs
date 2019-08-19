
using UnityEngine;


namespace FCT.CookieBakerP03
{
	public static class Vector3Ext
	{
		/// <summary>
		/// Returns the given Vector3 as a Vector4 with 1 for its 'w' value.
		/// </summary>
		public static Vector4 Position(this Vector3 pos)
		{
			return new Vector4(pos.x, pos.y, pos.z, 1.0f);
		}

		/// <summary>
		/// Returns the given Vector3 as a Vector4 with a 0 for its 'w' value.
		/// </summary>
		public static Vector4 Direction(this Vector3 dir)
		{
			return new Vector4(dir.x, dir.y, dir.z, 0.0f);
		}
	}
}
