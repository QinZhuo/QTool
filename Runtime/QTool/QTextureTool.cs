using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QTextureTool 
	{
		public static Texture2D Downsample(this Texture2D texture,int count=2)
		{
			var newTexture = new Texture2D(texture.width / count, texture.height / count, texture.format, false);
			for (int x = 0; x < texture.width/count; x++)
			{
				for (int y = 0; y < texture.height/count; y++)
				{
					newTexture.SetPixel(x, y, texture.GetPixel(x, y));
				}
			}
			newTexture.Apply();
			return newTexture;
		}
		public static Color[] GetMainColors(this Texture texture,int colorCount=2)
		{
			var colors = new Color[colorCount];
			for (int x = 0; x < texture.width; x++)
			{
				for (int y = 0; y < texture.height; y++)
				{

				}
			}
			return colors;
		}
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
