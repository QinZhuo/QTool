using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public interface IQNoise
	{
		public float this[float x] { get; }
		public float this[float x, float y] { get; }
		public float this[float x,float y,float z] { get; }
	}
	/// <summary>
	/// 白噪声 线性插值过渡
	/// </summary>
	public class QWhiteNoise: IQNoise
	{
		const int Size = 1024;
		const int Range = Size - 1;
		private static QDictionary<int, float[]> Tables = new QDictionary<int, float[]>((seed)=> {
			var table = new float[Size];
			var Random = new System.Random(seed);
			for (int i = 0; i < Size; i++)
			{
				table[i] = (Random.Next() & Range) / (1f * Size);
			}
			return table;
		});
		private float[] Table = null;
		public float Frequency { get; set; } = 10;
		public QWhiteNoise(int seed = 0)
		{
			Table = Tables[seed];
		}
		private int OffsetIndex(int index)
		{
			return (int)(GetValue(index) * Size);
		}
		protected float GetValue(int x)
		{
			return Table[x & Range];
		}
		protected float GetValue(int x, int y)
		{
			return GetValue(y + OffsetIndex(x));
		}
		protected float GetValue(int x, int y, int z)
		{
			return GetValue(z + OffsetIndex(y + OffsetIndex(x)));
		}
		protected virtual int FixIndex(ref float value)
		{
			value *= Frequency;
			return Mathf.FloorToInt(value);
		}
		public virtual float this[float x]
		{
			get
			{
				var intx = FixIndex(ref x);
				return GetValue(intx).Lerp(GetValue(intx + 1), Curve(x - intx));
			}
		}
		public virtual float this[float x, float y]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var xt = Curve(x - intx);
				var y1 = GetValue(intx, inty).Lerp(GetValue(intx + 1, inty), xt);
				var y2 = GetValue(intx, inty + 1).Lerp(GetValue(intx + 1, inty + 1), xt);
				return y1.Lerp(y2, Curve(y - inty));
			}
		}
		public virtual float this[float x, float y, float z]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var intz = FixIndex(ref z);
				var xt = Curve(x - intx);
				var y1 = GetValue(intx, inty, intz).Lerp(GetValue(intx + 1, inty, intz), xt);
				var y2 = GetValue(intx, inty + 1, intz).Lerp(GetValue(intx + 1, inty + 1, intz), xt);
				var y3 = GetValue(intx, inty, intz + 1).Lerp(GetValue(intx + 1, inty, intz + 1), xt);
				var y4 = GetValue(intx, inty + 1, intz + 1).Lerp(GetValue(intx + 1, inty + 1, intz + 1), xt);
				var yt = Curve(y - inty);
				var z1 = y1.Lerp(y2, yt);
				var z2 = y3.Lerp(y4, yt);
				return z1.Lerp(z2, Curve(z - intz));
			}
		}
		public virtual float Curve(float x)
		{
			return x;
		}
	}
	/// <summary>
	/// 值噪声 平滑边界过渡
	/// </summary>
	public class QValueNoise : QWhiteNoise
	{
		public override float Curve(float x)
		{
			return x * x * x * (x * (x * 6.0f - 15.0f) + 10.0f);
		}
	}
	/// <summary>
	/// 柏林噪声 使用随机方向信息计算得出最终数值
	/// </summary>
	public class QPerlinNoise : QValueNoise
	{
		public override float this[float x]
		{
			get
			{
				var intx = FixIndex(ref x);
				return Grad(GetValue(intx),x-intx).Lerp(Grad(GetValue(intx + 1),x-intx-1), Curve(x - intx));
			}
		}
		public override float this[float x, float y]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var xt = x - intx;
				var cxt = Curve(xt);
				var yt = y - inty ;
				var cyt = Curve(yt);
				var y1 = Grad(GetValue(intx, inty), xt, yt).Lerp(Grad(GetValue(intx + 1, inty), xt - 1, yt), cxt);
				var y2 = Grad(GetValue(intx, inty + 1), xt, yt - 1).Lerp(Grad(GetValue(intx + 1, inty + 1), xt - 1, yt - 1), cxt);
				return (y1.Lerp(y2, cyt) + 1f) / 2f;
			}
		}
		public override float this[float x, float y, float z]
		{
			get
			{
				var intx = FixIndex(ref x);
				var inty = FixIndex(ref y);
				var intz = FixIndex(ref z);
				var xt = x - intx;
				var yt = y - inty;
				var zt = z - intz;
				var cxt = Curve(xt);
				var y1 = Grad(GetValue(intx, inty, intz),xt,yt,zt).Lerp(Grad(GetValue(intx + 1, inty, intz),xt-1,yt,zt), cxt);
				var y2 = Grad(GetValue(intx , inty + 1, intz), xt , yt - 1, zt).Lerp(Grad(GetValue(intx + 1, inty + 1, intz), xt - 1, yt - 1, zt), cxt);
				var y3 = Grad(GetValue(intx, inty, intz + 1), xt , yt, zt - 1).Lerp(Grad(GetValue(intx + 1, inty , intz + 1), xt - 1, yt , zt - 1), cxt);
				var y4 = Grad(GetValue(intx , inty + 1, intz + 1), xt, yt - 1, zt - 1).Lerp(Grad(GetValue(intx + 1, inty + 1, intz + 1), xt - 1, yt - 1, zt - 1), cxt);
				var cyt = Curve(yt);
				var z1 = y1.Lerp(y2, cyt);
				var z2 = y3.Lerp(y4, cyt);
				return (z1.Lerp(z2, Curve(zt))+1)/2;
			}
		}
		public static float Grad(float value, float x)
		{
			var hash = (int)(value * byte.MaxValue);
			int h = hash & 15;
			float grad = 1.0f + (h & 7);    // Gradient value 1.0, 2.0, ..., 8.0
			if ((h & 8) != 0) grad = -grad; // Set a random sign for the gradient
			return (grad * x);              // Multiply the gradient with the distance
		}

		public static float Grad(float value, float x, float y)
		{
			var hash = (int)(value * byte.MaxValue);
			int h = hash & 7;           // Convert low 3 bits of hash code
			float u = h < 4 ? x : y;  // into 8 simple gradient directions,
			float v = h < 4 ? y : x;  // and compute the dot product with (x,y).
			return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v :  v);
		}

		public static float Grad(float value, float x, float y, float z)
		{
			var hash = (int)(value * byte.MaxValue);
			int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
			float u = h < 8 ? x : y; // gradient directions, and compute dot product.
			float v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
			return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
		}
	}
	public class QSimplexNoise : QPerlinNoise
	{
		protected override int FixIndex(ref float value)
		{
			value *= Frequency*0.5f;
			return Mathf.FloorToInt(value);
		}
		private void FixIndex(ref float x,ref float y,out int intx,out int inty)
		{
			x *= Frequency * 0.5f;
			y *= Frequency * 0.5f;
			var sum = (x + y) * 0.366025403f;
			intx = Mathf.FloorToInt(x + sum);
			inty = Mathf.FloorToInt(y + sum);
		}
		private void FixIndex(ref float x, ref float y,ref float z, out int intx, out int inty,out int intz)
		{
			x *= Frequency * 0.5f;
			y *= Frequency * 0.5f;
			z *= Frequency * 0.5f;
			var sum = (x + y + z) * 0.333333333f;
			intx = Mathf.FloorToInt(x + sum);
			inty = Mathf.FloorToInt(y + sum);
			intz = Mathf.FloorToInt(z + sum);
		}
		public override float this[float x] {
			get
			{
				var intx = FixIndex(ref x);
				return (GetSimplexValue(intx, x - intx)+GetSimplexValue(intx + 1, x - intx - 1)*0.395f+1)/2;
			}
		}
		public override float this[float x, float y]
		{
			get
			{
				FixIndex(ref x,ref y, out var intx, out var inty);
				const float G = 0.211324865f;
				float t = (intx + inty) * G;
				float xt = x - intx + t; 
				float yt = y - inty + t;
				int xo1, yo1; 
				if (xt > yt) { xo1 = 1; yo1 = 0; }
				else { xo1 = 0; yo1 = 1; }
				var n0 = GetSimplexValue(intx, inty, xt, yt);
				var n1 = GetSimplexValue(intx+xo1, inty+yo1, xt - xo1 + G, yt - yo1 + G);
				var n2 = GetSimplexValue(intx+1, inty+1, xt - 1.0f + 2.0f * G, yt - 1.0f + 2.0f * G);
				return (n0 + n1 + n2) * 30 +0.5f;
			}
		}
		public override float this[float x, float y, float z] {
			get
			{
				FixIndex(ref x, ref y,ref z, out var intx, out var inty,out var intz);
				const float G = 0.166666667f;
				float t = (intx + inty + intz) * G;
				float xt = x - intx + t;
				float yt = y - inty + t;
				float zt = z - intz + t;
				int xo1, yo1, zo1;
				int xo2, yo2, zo2;
				if (xt >= yt)
				{
					if (yt >= zt)
					{ xo1 = 1; yo1 = 0; zo1 = 0; xo2 = 1; yo2 = 1; zo2 = 0; } 
					else if (xt >= zt) { xo1 = 1; yo1 = 0; zo1 = 0; xo2 = 1; yo2 = 0; zo2 = 1; }
					else { xo1 = 0; yo1 = 0; zo1 = 1; xo2 = 1; yo2 = 0; zo2 = 1; } 
				}
				else
				{ 
					if (yt < zt) { xo1 = 0; yo1 = 0; zo1 = 1; xo2 = 0; yo2 = 1; zo2 = 1; }
					else if (xt < zt) { xo1 = 0; yo1 = 1; zo1 = 0; xo2 = 0; yo2 = 1; zo2 = 1; } 
					else { xo1 = 0; yo1 = 1; zo1 = 0; xo2 = 1; yo2 = 1; zo2 = 0; } 
				}
				var n0 = GetSimplexValue(intx, inty,intz, xt, yt,zt);
				var n1 = GetSimplexValue(intx + xo1, inty + yo1, intz + zo1, xt - xo1 + G, yt - yo1 + G, zt - zo1 + G);
				var n2 = GetSimplexValue(intx + xo2, inty + yo2, intz + zo2, xt - xo2 + G * 2, yt - yo2 + G * 2, zt - zo2 + G * 2);
				var n3 = GetSimplexValue(intx + 1, inty + 1, intz + 1, xt - 1 + G * 3, yt - 1 + G * 3, zt - 1 + G * 3);
				return (n0 + n1 + n2 + n3) * 16 + 0.5f;
			}
		}
		private float GetSimplexValue(int x,float xt)
		{
			var temp= 1 - x * x;
			temp *= temp;
			return temp * temp* Grad(GetValue(x), xt);
		}
		private float GetSimplexValue(int x,int y, float xt,float yt)
		{
			var temp = 0.5f - xt * xt - yt * yt;
			if (temp < 0)
			{
				return 0;
			}
			else
			{
				temp *= temp;
				return temp * temp * Grad(GetValue(x, y), xt, yt);
			}
		}
		private float GetSimplexValue(int x, int y,int z, float xt, float yt,float zt)
		{
			var temp = 0.6f - xt * xt - yt * yt - zt * zt;
			if (temp < 0)
			{
				return 0;
			}
			else
			{
				temp *= temp;
				return temp * temp * Grad(GetValue(x, y, z), xt, yt, zt);
			}
		}
		public override float Curve(float x)
		{
			return x;
		}
	}
	/// <summary>
	/// 噪声层级 更改噪声的频率幅度
	/// </summary>
	public class QNoiseLayer : IQNoise
	{
		public IQNoise Noise { get; set; }
		/// <summary>
		/// 频率
		/// </summary>
		public float Frequency { get; set; } = 1;
		/// <summary>
		/// 幅度
		/// </summary>
		public float Amplitude { get; set; } = 1;
		public QNoiseLayer(IQNoise noise,float frequency=1,float amplitude = 1)
		{
			Noise = noise;
			Frequency = frequency;
			Amplitude = amplitude;
		}
		public float this[float x] => Noise[x*Frequency]*Amplitude;

		public float this[float x, float y] => Noise[x * Frequency,y*Frequency] * Amplitude;

		public float this[float x, float y, float z] => Noise[x * Frequency, y * Frequency,z*Frequency] * Amplitude;
		
	}	
	/// <summary>
	/// 分形噪声 将多个噪声取不同幅度频率叠加在一起
	/// </summary>
	public class QFractionNoise : IQNoise
	{
		
		private QNoiseLayer[] Noises = null;
		private float AmplitudeSum { get; set; } = 0;
		public QFractionNoise(params QNoiseLayer[] noiseLayers)
		{
			Noises = noiseLayers;
			AmplitudeSum = 0;
			foreach (var noise in Noises)
			{
				AmplitudeSum += noise.Amplitude;
			}
		}
		public QFractionNoise(IQNoise noise, int layerCount = 5, float frequencyScale = 2, float amplitudeScale = 0.5f)
		{
			Noises = new QNoiseLayer[layerCount];
			AmplitudeSum = 0;
			var frequency = 1f;
			var amplitude = 1f;
			for (int i = 0; i < layerCount; i++)
			{
				Noises[i] = new QNoiseLayer(noise, frequency, amplitude);
				AmplitudeSum += amplitude;
				frequency *= frequencyScale;
				amplitude *= amplitudeScale;
			}
		}
		public float this[float x]
		{
			get
			{
				var value = 0f;
				foreach (var noise in Noises)
				{
					value += noise[x];
				}
				return FixResult(value);
			}
		}

		public float this[float x,float y]
		{
			get
			{
				var value = 0f;
				foreach (var noise in Noises)
				{
					value += noise[x,y];
				}
				return FixResult(value);
			}
		}
		public float this[float x, float y, float z]
		{
			get
			{
				var value = 0f;
				foreach (var noise in Noises)
				{
					value += noise[x, y, z];
				}
				return FixResult(value);
			}
		}
		public virtual float FixResult(float value)
		{
			return value / AmplitudeSum;
		}
	}
	/// <summary>
	/// 湍流噪声 把分型噪声先调整到 [-1, 1]，然后取绝对值，这样生成的图片：
	/// </summary>
	public class QTurbulenceNoise: QFractionNoise
	{
		public QTurbulenceNoise() : base(new QValueNoise()) { }
		public override float FixResult(float value)
		{
			return System.Math.Abs(base.FixResult(value) * 2 - 1);
		}
	}
	
}
