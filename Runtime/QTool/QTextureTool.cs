using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QTextureTool 
	{
		const float DefaultS = 0.5f;
		const float DefaultV = 1f;
		static QTextureTool()
		{
			foreach (var kv in QToolSetting.Instance.qKeyColorList)
			{
				KeyColor[kv.key] = kv.color;
			}
		}
		#region 颜色映射
		public static QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
		public static Color ToColor(this string key, float s = DefaultS, float v = DefaultV)
		{
			if (key.IsNull()) return Color.white;
			if (ColorUtility.TryParseHtmlString(key, out var newColor))
			{
				return newColor;
			}
			else
			{
				if (!KeyColor.ContainsKey(key))
				{
					if (key.SplitTowString("/", out var start, out var end))
					{
						var colorValue = Mathf.Abs(start.GetHashCode() % 700f) + Mathf.Abs(end.GetHashCode() % 300f);
						KeyColor[key] = Color.HSVToRGB(colorValue / 1000f, s, v);
					}
					else
					{
						KeyColor[key] = Color.HSVToRGB(Mathf.Abs(key.GetHashCode() % 700f) / 1000f, s, v);
					}
				}
				return KeyColor[key];
			}
		}
		#endregion
		#region 主色调提取
		public static Color ToColor(this float h, float s = DefaultS, float v = DefaultV)
		{
			return Color.HSVToRGB(h, s, v);
		}
		public static float ToH(this Color color)
		{
			Color.RGBToHSV(color, out var h, out var s, out var v);
			return h;
		}
		public static Texture2D Downsample(this Texture2D texture, int count = 2)
		{
			var newTexture = new Texture2D(texture.width / count, texture.height / count, texture.format, false);
			for (int x = 0; x < texture.width / count; x++)
			{
				for (int y = 0; y < texture.height / count; y++)
				{
					newTexture.SetPixel(x, y, texture.GetPixel(x, y));
				}
			}
			newTexture.Apply();
			return newTexture;
		}
		private static float Average(this IList<float> values)
		{
			var sum = 0f;
			foreach (var value in values)
			{
				sum += value;
			}
			return sum / values.Count;
		}
		private static int NearIndex(this float[] values, float value)
		{
			var index = 0;
			var distance = float.MaxValue;
			for (int i = 0; i < values.Length; i++)
			{
				if (Mathf.Abs(values[i] - value) > distance)
				{
					index = i;
				}
			}
			return index;
		}
		private static QDictionary<Texture2D, float> ColorHCache = new QDictionary<Texture2D, float>();
		public static float GetMainColorH(this Texture2D texture, int colorCount = 2)
		{
			if (!ColorHCache.ContainsKey(texture))
			{
				var colors = new float[texture.width * texture.height];
				for (int x = 0; x < texture.width; x++)
				{
					for (int y = 0; y < texture.height; y++)
					{
						colors[x * texture.height + y] = texture.GetPixel(x, y).ToH();
					}
				}
				var mainIndex = 0;
				var mainColors = new float[colorCount];
				for (int i = 0; i < colors.Length; i++)
				{
					if (mainIndex == 0)
					{
						mainColors[mainIndex++] = colors[i];
					}
					else if (!mainColors.Contains(colors[i]))
					{
						mainColors[mainIndex++] = colors[i];
					}
					if (mainIndex >= mainColors.Length)
					{
						break;
					}
				}
				var mainColorLists = new List<float>[colorCount];
				for (int i = 0; i < mainColorLists.Length; i++)
				{
					mainColorLists[i] = new List<float>();
				}
				while (true)
				{
					for (int i = 0; i < colors.Length; i++)
					{
						mainIndex = mainColors.NearIndex(colors[i]);
						mainColorLists[mainIndex].Add(colors[i]);
					}
					bool isOver = true;
					for (mainIndex = 0; mainIndex < mainColors.Length; mainIndex++)
					{
						var newColor = mainColorLists[mainIndex].Average();
						mainColorLists[mainIndex].Clear();
						if (isOver && !newColor.Equals(mainColors[mainIndex]))
						{
							isOver = false;
						}
						mainColors[mainIndex] = newColor;
					}
					if (isOver)
					{
						break;
					}
				}
				ColorHCache[texture] = mainColors.Get(0);
			}
			return ColorHCache[texture];
		}
		#endregion

		#region 存储

		public static void SaveJPG(this Texture2D tex, string path, int quality = 50)
		{
			var bytes = tex.EncodeToJPG(quality);
			if (bytes != null)
			{
				QFileTool.Save(path, bytes);
			}
		}
		public static void SavePNG(this Texture2D tex, string path)
		{
			var bytes = tex.EncodeToPNG();
			if (bytes != null)
			{
				QFileTool.Save(path, bytes);
			}
		}
		public static Texture2D LoadTexture(this string path)
		{
			var bytes =QFileTool.LoadBytes(path);
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(bytes);
			return tex;
		}
#if UNITY_EDITOR
		public static Sprite SaveSprite(this Texture2D texture, string path, float spritePixelsPerUnit = 100)
		{
			texture.SavePNG(path);
			UnityEditor.AssetDatabase.Refresh();
			var textureImporter = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
			textureImporter.textureType = UnityEditor.TextureImporterType.Sprite;
			textureImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
			textureImporter.spritePixelsPerUnit = spritePixelsPerUnit;
			textureImporter.maxTextureSize = 16384;
			UnityEditor.AssetDatabase.ImportAsset(path);
			return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
		}
#endif

	}

	#endregion
}
