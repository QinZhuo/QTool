using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace QTool.Mesh
{
	[System.Serializable]
	public class QVoxelData: QVoxel
	{ 
		[QName("Voxels")]
		public QDictionary<Vector3Int, float> Voxels { get; private set; } = new QDictionary<Vector3Int, float>();
		public QVoxelData() { }
	
		public void SetVoxel(Vector3Int pos, Color color)
		{
			if (color.a<=0)
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
			SetVoxel(pos, index  + Surface);
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
		public override float this[int x,int y,int z]=> Voxels[new Vector3Int(x,y,z)];
	}
	public abstract class QVoxel:QSerializeObject
	{
		[QName("Colors")]
		public List<Color32> Colors { get;protected set; } = new List<Color32>{ Color.white };
		public void ReplaceColor(int index,Color32 color)
		{
			if (index <= 0) return;
			Colors[index] = color;
			SetDirty();
		}
		public Color32 GetColor(float value)
		{
			if (value <= Surface)
			{
				return Color.clear;
			}
			var index =Mathf.RoundToInt(value - Surface);
			if (index < 0 || index >= Colors.Count)
			{
				Debug.LogError(index + "/" + Colors.Count);
				return Color.white;
			}
			return Colors[index];
		}
		public override void OnDeserializeOver()
		{
		}
		public override void SetDirty()
		{
			base.SetDirty();
			MeshData.SetDirty();
		}
		public float Surface { get; set; } = 0f;
		public Vector3Int Max { get; set; } = Vector3Int.one * int.MinValue;
		public Vector3Int Min { get; set; } = Vector3Int.one * int.MaxValue;
		public Vector3Int Size => Max - Min+Vector3Int.one;
		public virtual float this[int x] =>this[x,0,0];
		public virtual float this[int x, int y]=>this[x,y,0];
		public abstract float this[int x, int y, int z] { get; }
		public float this[float x]
		{
			get
			{
				var intx = (int)x;
				return this[intx].LerpTo(this[intx + 1], x - intx);
			}
		}
		public float this[float x, float y]
		{
			get
			{
				var intx = (int)x;
				var inty = (int)y;
				var xt = x - intx;
				var y1 = this[intx, inty].LerpTo(this[intx + 1, inty], xt);
				var y2 = this[intx, inty + 1].LerpTo(this[intx + 1, inty + 1], xt);
				return y1.LerpTo(y2, y - inty);
			}
		}
		public float this[float x, float y, float z]
		{
			get
			{
				var intx = (int)x;
				var inty = (int)y;
				var intz = (int)z;
				var xt = x - intx;
				var y1 = this[intx, inty, intz].LerpTo(this[intx + 1, inty, intz], xt);
				var y2 = this[intx, inty + 1, intz].LerpTo(this[intx + 1, inty + 1, intz], xt);
				var y3 = this[intx, inty, intz + 1].LerpTo(this[intx + 1, inty, intz + 1], xt);
				var y4 = this[intx, inty + 1, intz + 1].LerpTo(this[intx + 1, inty + 1, intz + 1], xt);
				var yt = y - inty;
				var z1 = y1.LerpTo(y2, yt);
				var z2 = y3.LerpTo(y4, yt);
				return z1.LerpTo(z2, z - intz);
			}
		}
		public float this[Vector3Int pos]=>this[pos.x,pos.y,pos.z];
		[QIgnore]
		public QMeshData MeshData { private set; get; } = new QMeshData();
		public QVoxel()
		{
			MeshData = new QMeshData();
		}
		public UnityEngine.Mesh GenerateMesh() {
			QMarchingCubes.GenerateMeshData(this);
			return MeshData.Mesh;
		} 
	}
}
