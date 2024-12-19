using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Mesh;
namespace QTool.Mesh
{

	public class QNoiseTest : MonoBehaviour
	{
		public Material material;
		private Texture2D texture;
		public void NoiseTest()
		{
			var noise = new QSimplexNoise() { Frequency = 5 };
			texture = new Texture2D(256, 256);
			for (int y = 0; y < texture.height; y++)
			{
				for (int x = 0; x < texture.height; x++)
				{
					float n = noise[x*1f/ texture.width, y*1f/ texture.height];
					if (n < 0 || n > 1)
					{
						Debug.LogError(n);
					}
					texture.SetPixel(x, y, new Color(n, n, n, 1));
				}
			}
			texture.Apply();
			var voxelData = new QVoxelData();
			voxelData.Surface = 0.5f;
			for (int x = -10; x <10; x++)
			{
				for (int y = -10; y <10; y++)
				{
					for (int z = -10; z < 10; z++)
					{
						var value= noise[x / 20f, y / 20f, z / 20f]; 
						voxelData[x, y, z] = value;
						
					}
				}
			}
			gameObject.GenerateMesh(voxelData, material);
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
