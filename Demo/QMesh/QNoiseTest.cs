using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Mesh;
namespace QTool.Mesh
{

	public class QNoiseTest : MonoBehaviour
	{
		public Material material;
		public int size = 50;
		private Texture2D texture;
		public void NoiseTest()
		{
			var noise = new QValueNoise();
			texture = new Texture2D(512, 512);
			for (int y = 0; y < texture.height; y++)
			{
				for (int x = 0; x < texture.height; x++)
				{
					float n = noise[x*1f/ texture.width, y*1f/ texture.height];
					texture.SetPixel(x, y, new Color(n, n, n, 1));
				}
			}
			texture.Apply();
			gameObject.GenerateMesh(new QNoiseVoxel(noise), material);
		}
		void Start()
		{
			NoiseTest();
		}


		void OnGUI()
		{
			GUILayout.Box(texture);
		}
	}
}
