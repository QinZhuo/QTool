using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Mesh
{

	public abstract class QNoiseVoxel:QVoxel
	{
	
		public QNoiseVoxel()
		{
			Surface = 0.5f;
			Max = Vector3Int.one * 8;
			Min = Vector3Int.one;
		}
		
	}
	public class QValueNoise : QNoiseVoxel
	{

		private QNoiseTable Table { get; set; }
		public QValueNoise(int seed = 0) 
		{
			Table = QNoiseTable.Tables[seed];
		}
		public override float this[int x] => Table[x];

		public override float this[int x, int y] => Table[x, y];

		public override float this[int x, int y, int z] => Table[x, y, z];
	}

    internal class QNoiseTable
	{
		public static QDictionary<int, QNoiseTable> Tables = new QDictionary<int, QNoiseTable>((seed) => new QNoiseTable(seed));
		const int Size = 1024;
		System.Random Random = null;
		float[] Table = new float[Size];
		private QNoiseTable(int seed = 0)
		{
			Random = new System.Random(seed);
			for (int i = 0; i < Size; i++)
			{
				Table[i] = (Random.Next()%Size)/(1f*Size);
			}
		}
		public float this[int x]=>Table[x%Size];
		int GetIndex(int i)
		{
			return (int)(this[i] * Size) ;
		}
		public float this[int x, int y]
		{
			get
			{
				return this[y + GetIndex(x)];
			}
		}
		public float this[int x, int y, int z]
		{
			get
			{
				return this[z + GetIndex(y + GetIndex(x))];
			}
		}
	}
}
