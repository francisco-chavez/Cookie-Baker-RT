
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


namespace FCT.CookieBakerP02
{
	public class Bounds
	{

		private Vector3 Min;
		private Vector3 Max;
		private Vector3 Center;


		public Bounds(UnityEngine.Bounds unityBounds)
		{
			Center	= unityBounds.center;
			Min		= unityBounds.min - (0.005f * Vector3.one);
			Max		= unityBounds.max + (0.005f * Vector3.one);
		}


		public bool IntersectsLightRay(LightRay lightRay)
		{
			var		invDir	= lightRay.InvDirection;
			var		sign0	= invDir.x < 0;
			var		sign1	= invDir.y < 0;


			float	tMin	= ((sign0 ? Max : Min).x - lightRay.Origin.x) * invDir.x;
			float	tMax	= ((sign0 ? Min : Max).x - lightRay.Origin.x) * invDir.x;

			float	tyMin	= ((sign1 ? Max : Min).y - lightRay.Origin.y) * invDir.y;
			float	tyMax	= ((sign1 ? Min : Max).y - lightRay.Origin.y) * invDir.y;

			if ((tMin > tyMax) || (tyMin > tMax))
				return false;

			var sign2 = invDir.z < 0;

			tMin = (tyMin > tMin) ? tyMin : tMin;
			tMax = (tyMax < tMax) ? tyMax : tMax;

			float tzMin = ((sign2 ? Max : Min).z - lightRay.Origin.z) * invDir.z;
			float tzMax = ((sign2 ? Min : Max).z - lightRay.Origin.z) * invDir.z;

			return !((tMin > tzMax) || (tzMin > tMax));
		}

	}
}
