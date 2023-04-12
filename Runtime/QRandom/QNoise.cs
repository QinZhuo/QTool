using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QNoise
	{
		const int Size = 1024;
		const int Range = Size - 1;
		float[] Table = new float[Size];
		public float Scale=10;
		public QNoise(int seed = 0)
		{
			var Random = new System.Random(seed);
			for (int i = 0; i < Size; i++)
			{
				Table[i] = (Random.Next()&Range) / (1f * Size);
			}
		}
		private int OffsetIndex(int i)
		{
			return (int)(GetValue(i) * Size);
		}
		private float GetValue(int x)
		{
			return Table[x & Range];
		}
		private float GetValue(int x,int y)
		{
			return GetValue(y + OffsetIndex(x));
		}
		private float GetValue(int x, int y,int z)
		{
			return GetValue(z + OffsetIndex(y + OffsetIndex(x)));
		}
		private int FixIndex(ref float value)
		{
			value *= Scale;
			return (int)value;
		}
		public float this[float x]
		{
			get
			{
				var intx = FixIndex(ref x);
				return GetValue(intx).Lerp(GetValue(intx + 1), x - intx);
			}
		}
		public float this[float x, float y]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var xt = x - intx;
				var y1 = GetValue(intx,inty).Lerp(GetValue(intx+1, inty), xt);
				var y2 = GetValue(intx, inty+1).Lerp(GetValue(intx+1, inty + 1), xt);
				return y1.Lerp(y2, y - inty);
			}
		}
		public float this[float x, float y, float z]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var intz = FixIndex(ref z);
				var xt = x - intx;
				var y1 = GetValue(intx, inty,intz).Lerp(GetValue(intx+1, inty, intz), xt);
				var y2 = GetValue(intx, inty+1, intz).Lerp(GetValue(intx+1, inty+1, intz), xt);
				var y3 = GetValue(intx, inty, intz+1).Lerp(GetValue(intx+1, inty, intz+1), xt);
				var y4 = GetValue(intx, inty+1, intz+1).Lerp(GetValue(intx+1, inty+1, intz+1), xt);
				var yt = y - inty;
				var z1 = y1.Lerp(y2, yt);
				var z2 = y3.Lerp(y4, yt);
				return z1.Lerp(z2, z - intz);
			}
		}
	}

	public class QValueNoise:QNoise
	{

	}
}
