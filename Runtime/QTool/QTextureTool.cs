using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QTextureTool 
	{
		static QTextureTool()
		{
			foreach (var kv in QToolSetting.Instance.qKeyColorList)
			{
				KeyColors[kv.key] = kv.color;
			}
		}
		#region 颜色映射
		private static QDictionary<string, Color> KeyColors= new QDictionary<string, Color>();
		public static Color ParseHtmlColor(this string key)
		{
			if (ColorUtility.TryParseHtmlString(key, out var newColor))
			{
				return newColor;
			}
			return default;
		}
		public static Color ToColor(this object obj, float s = -1, float v = -1)
		{
			var key = obj?.ToString();
			if (key.IsNull()) return Color.clear;
			if (!KeyColors.ContainsKey(key))
			{
				if (s == -1)
				{
					s = QToolSetting.Instance.DefualtColorSaturation;
				}
				if (v == -1)
				{
					v = QToolSetting.Instance.DefualtColorValue;
				}
				if (key.SplitTowString("/", out var start, out var end))
				{
					var colorValue = Mathf.Abs(start.GetHashCode() % 700f) + Mathf.Abs(end.GetHashCode() % 300f);
					KeyColors[key] = (colorValue / 1000f).ToColor(s, v);
				}
				else
				{
					KeyColors[key] = (Mathf.Abs(key.GetHashCode() % 700f) / 1000f).ToColor(s, v);
				}
			}
			return KeyColors[key];
		}
		#endregion
		#region 主色调提取
		public static Color ToColor(this float h, float s = -1, float v = -1, float a = 1)
		{
			if (s == -1)
			{
				s = QToolSetting.Instance.DefualtColorSaturation;
			}
			if (v == -1)
			{
				v = QToolSetting.Instance.DefualtColorValue;
			}
			var color = Color.HSVToRGB(h, s, v);
			color.a = a;
			return color;
		}
		public static void SetColorH(this UnityEngine.UI.Graphic graphic, float h)
		{
			graphic.color = graphic.color.SetH(h);
		}
		public static Color SetH(this Color color, float newH)
		{
			Color.RGBToHSV(color, out var h, out var s, out var v);
			return newH.ToColor(s, v, color.a);
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
		private static Color Average(this IList<Color> values)
		{
			var rSum = 0f;
			var gSum = 0f;
			var bSum = 0f;
			foreach (var value in values)
			{
				rSum += value.r;
				gSum += value.g;
				bSum += value.b;
			}
			return new Color(rSum / values.Count, gSum / values.Count, bSum / values.Count);
		}
		public static float Distance(this Color color,Color other)
		{
			return Vector3.Distance(new Vector3(color.r, color.g, color.b), new Vector3(other.r, other.g, other.b));
		}
		private static int NearIndex(this Color[] values, Color value)
		{
			var index = 0;
			var distance = float.MaxValue;
			for (int i = 0; i < values.Length; i++)
			{
				var checkDis = values[i].Distance(value);
				if (checkDis > distance)
				{
					distance = checkDis;
					index = i;
				}
			}
			return index;
		}
		private static QDictionary<Texture2D, Color> ColorCache = new QDictionary<Texture2D, Color>();
		public static Color GetMainColor(this Texture2D texture, int colorCount = 2)
		{
			if (!ColorCache.ContainsKey(texture))
			{
				var colors = new Color[texture.width * texture.height];
				for (int x = 0; x < texture.width; x++)
				{
					for (int y = 0; y < texture.height; y++)
					{
						colors[x * texture.height + y] = texture.GetPixel(x, y);
					}
				}
				var mainIndex = 0;
				var mainColors = new Color[colorCount];
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
				var mainColorLists = new List<Color>[colorCount];
				for (int i = 0; i < mainColorLists.Length; i++)
				{
					mainColorLists[i] = new List<Color>();
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
				ColorCache[texture] = mainColors.Get(0);
			}
			return ColorCache[texture];
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
		public static void SetTexture(this UnityEngine.UI.RawImage rawImage, Texture2D texture, bool autoColor = true)
		{
			rawImage.texture = texture;
			if (autoColor)
			{
				rawImage.color = texture == null ? Color.clear : Color.white;
			}
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
