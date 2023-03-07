using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class BillboardBaker : MonoBehaviour
{
#if UNITY_EDITOR
	public BillboardAsset m_outputFile;
	public Material m_material;

	[ContextMenu("Bake Billboard")]
	void BakeBillboard()
	{
		BillboardAsset billboard = new BillboardAsset();

		billboard.material = m_material;
		Vector4[] texCoords = new Vector4[8];
		ushort[] indices = new ushort[12];
		Vector2[] vertices = new Vector2[6];
		texCoords[0].Set(0.230981f, 0.33333302f, 0.230981f, -0.33333302f);
		texCoords[1].Set(0.230981f, 0.66666603f, 0.230981f, -0.33333302f);
		texCoords[2].Set(0.33333302f, 0.0f, 0.33333302f, 0.23098099f);
		texCoords[3].Set(0.564314f, 0.23098099f, 0.23098099f, -0.33333302f);
		texCoords[4].Set(0.564314f, 0.564314f, 0.23098099f, -0.33333403f);
		texCoords[5].Set(0.66666603f, 0.0f, 0.33333302f, 0.23098099f);
		texCoords[6].Set(0.89764804f, 0.23098099f, 0.230982f, -0.33333302f);
		texCoords[7].Set(0.89764804f, 0.564314f, 0.230982f, -0.33333403f);

		indices[0] = 4;
		indices[1] = 3;
		indices[2] = 0;
		indices[3] = 1;
		indices[4] = 4;
		indices[5] = 0;
		indices[6] = 5;
		indices[7] = 4;
		indices[8] = 1;
		indices[9] = 2;
		indices[10] = 5;
		indices[11] = 1;

		vertices[0].Set(0.47093f, 0.020348798f);
		vertices[1].Set(0.037790697f, 0.498547f);
		vertices[2].Set(0.037790697f, 0.976744f);
		vertices[3].Set(0.52906996f, 0.020348798f);
		vertices[4].Set(0.95930207f, 0.498547f);
		vertices[5].Set(0.95930207f, 0.976744f);

		billboard.SetImageTexCoords(texCoords);
		billboard.SetIndices(indices);
		billboard.SetVertices(vertices);

		billboard.width = 10.35058f;
		billboard.height = 7.172371f;
		billboard.bottom = -0.2622106f;

		if (m_outputFile != null)
		{
			EditorUtility.CopySerialized(billboard, m_outputFile);
		}
		else
		{
			string path;
			path = AssetDatabase.GetAssetPath(m_material) + ".asset";
			AssetDatabase.CreateAsset(billboard, path);
		}
	}
#endif
}
