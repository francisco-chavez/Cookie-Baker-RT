
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


namespace FCT.CookieBakerP02
{
	public struct LightRay
	{
		public Vector3	Origin;
		public Color	Color;
		public Vector3 Direction
		{
			get { return _direction; }
			set
			{
				_direction		= value;
				InvDirection	= new Vector3(1.0f / value.x, 
											  1.0f / value.y, 
											  1.0f / value.z);
			}
		}
		private Vector3 _direction;
		public Vector3 InvDirection { get; private set; }

	}
}
