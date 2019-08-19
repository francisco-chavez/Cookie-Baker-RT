
using UnityEngine;


namespace FCT.CookieBakerP03
{
	public class Bounds
	{

		private Vector3 Extent;
		private Vector3 Center;


		public Bounds(UnityEngine.Bounds unityBounds)
		{
			Center = unityBounds.center;
			Extent = unityBounds.extents;
		}

	}
}
