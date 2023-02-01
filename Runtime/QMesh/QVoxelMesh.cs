using QTool.Inspector;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Mesh
{
	public class QVoxelMesh : MonoBehaviour
	{
		private void Reset()
		{
			voxelData.Clear(); 
			gameObject.GenerateMesh(voxelData);
		}
		public QVoxelData voxelData = new QVoxelData();
		[QToggle("编辑"),QOnChange(nameof(ModeChange))]
		public bool editorMode;
		[QToolbar("", nameof(editorMode))]
		public QVoxelBrush brush;
		[QToolbar(nameof(Color32s), nameof(editorMode))]
		[QOnChange(nameof(OnChangeColorIndex))]
		public int colorIndex =0;
		[QName("颜色", nameof(editorMode))]
		[QOnChange(nameof(OnChangeColor))]
		public Color32 brushColor = Color.white;
		public Color32[] Color32s => voxelData.Colors.ToArray();
		public enum QVoxelBrush
		{
			自由,
			平面,
		}
		void OnChangeColorIndex()
		{
			if (colorIndex == 0)
			{
				brushColor = Color.black;
			}
			else
			{
				brushColor = voxelData.Colors[colorIndex];
			}
		}
		int lastColorIndex = -1;
		void OnChangeColor()
		{
			if(lastColorIndex != colorIndex)
			{
				lastColorIndex = colorIndex;
			}
			else if (colorIndex > 0)
			{
				voxelData.ReplaceColor(colorIndex, brushColor);
				gameObject.GenerateMesh(voxelData);
			}
		}
	
		void ModeChange()
		{
			if (editorMode)
			{
				StartEditor();
			}
			else
			{
				EditorOver();
			}
		}
		void StartEditor()
		{
			editorMode = true;
		}
		[QOnInspector(QInspectorState.OnDisable)]
		void EditorOver()
		{
			CurPos = null;
			editorMode = false;
			if (this == null) return;
			voxelData.FreshSize();
			gameObject.GenerateMesh(voxelData);
		}
		Vector3Int? CurPos { get; set; } = null;
		Vector3Int? StartPos { get; set; }
		[QOnSceneInput(EventType.MouseMove)]
		public bool MouseMove(Ray ray)
		{
			if (editorMode)
			{
				var rayPos = (brush != QVoxelBrush.平面 || StartPos == null) ? ray.RayCast() : ray.RayCastPlane(StartPos.Value);
				var pos = transform.worldToLocalMatrix * (rayPos + ray.direction * 0.4f * transform.localScale.x - transform.position);
				CurPos = new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(pos.x), -20, 20)
					, Mathf.Clamp(Mathf.RoundToInt(pos.y), -20, 20)
					, Mathf.Clamp(Mathf.RoundToInt(pos.z), -20, 20));
				
				switch (brush)
				{
					case QVoxelBrush.平面:
						{
							switch (Event.current.type)
							{
								case EventType.MouseDown:
									{
										StartPos = CurPos.Value;
									}
									break;
								case EventType.MouseUp:
									{
										StartPos = null;
									}
									break;
								default:
									break;
							}
						}
						break;
					default:
						break;
				}
					
			}
			return editorMode;
		}
		[QOnSceneInput(EventType.MouseUp)]
		[QOnSceneInput(EventType.MouseDown)]
		[QOnSceneInput(EventType.MouseDrag)]
		public bool MouseDrag(Ray ray)
		{
			if (editorMode)
			{
				MouseMove(ray);
				voxelData.SetVoxel(CurPos.Value, Event.current.shift?(Color32)Color.clear:brushColor);
				this.SetDirty();
				return true;
			}
			return false;
		}

		private void OnDrawGizmosSelected()
		{
			if (!editorMode) return;
			QGizmos.StartMatrix(transform);
			if (voxelData.MeshData.Dirty)
			{
				gameObject.GenerateMesh(voxelData);
			}
			if (CurPos != null)
			{
				Gizmos.color = Color.green;
				if (voxelData.Voxels.ContainsKey(CurPos.Value))
				{
					Gizmos.DrawSphere(new Vector3(CurPos.Value.x, CurPos.Value.y, CurPos.Value.z), 0.5f);
				}
				Gizmos.DrawWireCube(new Vector3(CurPos.Value.x, CurPos.Value.y, CurPos.Value.z), Vector3.one);
			}
			QGizmos.EndMatrix();
		}
		
	}

}
