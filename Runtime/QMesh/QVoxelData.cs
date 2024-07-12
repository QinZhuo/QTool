using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace QTool
{
	[System.Serializable]
	public class QVoxelData: QSerializeObject
	{
		[QName("Colors")]
		public List<Color32> Colors { get; protected set; } = new List<Color32> { Color.white };
		[QName("Voxels")]
		public QDictionary<Vector3Int, float> Voxels { get; private set; } = new QDictionary<Vector3Int, float>();
		public QVoxelData() { }
		public void SetVoxel(Texture2D color,Texture2D depth=null,int maxDepthCount=10)
		{
			for (int x = 0; x < color.width; x++)
			{
				for (int y = 0; y < color.height; y++)
				{
					if (depth == null)
					{
						SetVoxel(new Vector3Int(x, y, 0), color.GetPixel(x, y));
					}
					else
					{
						var depthCount = maxDepthCount * depth.GetPixel(x, y).r;
						for (int z = 0; z < depthCount; z++)
						{
							SetVoxel(new Vector3Int(x, y, -z), color.GetPixel(x, y));
						}
					}
				}
			}
		}
		public void SetVoxel(Vector3Int pos, Color color)
		{
			if (color.a<=Surface)
			{
				SetVoxel(pos, 0);
				return;
			}
			if (Colors.Count == 0)
			{
				Colors.Add(Color.clear);
			}
			var index=Colors.IndexOf(color);
			if (index < 0)
			{
				index = Colors.Count;
				Colors.Add(color);
			}
			SetVoxel(pos, index + color.a * 0.1f);
		}
		public void SetVoxel(Vector3Int pos,float value)
		{
			if (Voxels.ContainsKey(pos))
			{
				if (Voxels[pos] == value) return;
			}
			if (value == 0)
			{
				Voxels.Remove(pos);
				SetDirty();
			}
			else if(value!=Voxels[pos])
			{
				Max = Vector3Int.Max(Max, pos);
				Min = Vector3Int.Min(Min, pos);
				Voxels[pos] = value;
				SetDirty();
			}
		}
		
		public void FreshSize()
		{
			Max = Vector3Int.one * int.MinValue;
			Min = Vector3Int.one * int.MaxValue;
			foreach (var voxel in Voxels)
			{
				Max = Vector3Int.Max(Max, voxel.Key);
				Min = Vector3Int.Min(Min, voxel.Key);
			}
		}
		public void Clear()
		{
			Colors.Clear();
			Max = Vector3Int.one * int.MinValue;
			Min= Vector3Int.one * int.MaxValue;
			Voxels.Clear();
			MeshData.Clear();
			SetDirty();
		}
		public float this[int x, int y, int z] { 
			get => Voxels[new Vector3Int(x, y, z)];
			set
			{
				SetVoxel(new Vector3Int(x, y, z), value);
			}
		}
		public void SetDirty()
		{
			MeshData.SetDirty();
		}
		public void ReplaceColor(int index, Color32 color)
		{
			if (index <= 0) return;
			Colors[index] = color;
			SetDirty();
		}
		public Mesh GenerateMesh()
		{
			QMarchingCubes.GenerateMeshData(this);
			return MeshData.GetMesh();
		}
		public Color32 GetColor(float value)
		{
			if (value <= Surface)
			{
				return Color.clear;
			}
			var index = Mathf.RoundToInt(value);
			if (index < 0 || index >= Colors.Count)
			{
				return Color.white;
			}
			return Colors[index];
		}
		public float Surface { get; set; } = 0f;
		public Vector3Int Max { get; set; } = Vector3Int.one * int.MinValue;
		public Vector3Int Min { get; set; } = Vector3Int.one * int.MaxValue;
		public Vector3Int Size => Max - Min + Vector3Int.one;
		public float this[float x, float y, float z]
		{
			get
			{
				var intx = (int)x;
				var inty = (int)y;
				var intz = (int)z;
				var xt = x - intx;
				var y1 = this[intx, inty, intz].Lerp(this[intx + 1, inty, intz], xt);
				var y2 = this[intx, inty + 1, intz].Lerp(this[intx + 1, inty + 1, intz], xt);
				var y3 = this[intx, inty, intz + 1].Lerp(this[intx + 1, inty, intz + 1], xt);
				var y4 = this[intx, inty + 1, intz + 1].Lerp(this[intx + 1, inty + 1, intz + 1], xt);
				var yt = y - inty;
				var z1 = y1.Lerp(y2, yt);
				var z2 = y3.Lerp(y4, yt);
				return z1.Lerp(z2, z - intz);
			}
		}
		public float this[Vector3Int pos] => this[pos.x, pos.y, pos.z];
		[QIgnore]
		public QMeshData MeshData { private set; get; } = new QMeshData();
	}

}
