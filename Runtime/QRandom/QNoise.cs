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
		public float Scale = 10;
		public QWhiteNoise(int seed = 0)
		{
			Table = Tables[seed];
		}
		private int OffsetIndex(int index)
		{
			return (int)(GetValue(index) * Size);
		}
		private float GetValue(int x)
		{
			return Table[x & Range];
		}
		private float GetValue(int x, int y)
		{
			return GetValue(y + OffsetIndex(x));
		}
		private float GetValue(int x, int y, int z)
		{
			return GetValue(z + OffsetIndex(y + OffsetIndex(x)));
		}
		private int FixIndex(ref float value)
		{
			value *= Scale;
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
				return ChangeResult(value);
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
				return ChangeResult(value);
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
				return ChangeResult(value);
			}
		}
		public virtual float ChangeResult(float value)
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
		public override float ChangeResult(float value)
		{
			return System.Math.Abs(base.ChangeResult(value) * 2 - 1);
		}
	}
}
