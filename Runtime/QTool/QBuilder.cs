using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Xml.Serialization;
using System.Threading.Tasks;
using QTool.Reflection;
using System;

namespace QTool.Builder
{
	[DisallowMultipleComponent]
	public class QBuilder : MonoBehaviour
	{
		public static Type[] PrefabTypes => typeof(QBuilderBrefab<>).GetAllTypes();
		public static string[] BurshMode { get; private set; } = new string[] { "普通", "框选", };
		[QToggle("打开编辑模式")]
		public bool EditorMode;
		[QToolbar(nameof(PrefabTypes), nameof(EditorMode))]
		public int BrushTypeIndex;
		[QToolbar(nameof(CurrentBrushList), nameof(EditorMode),8)]
		public int BrushIndex;
		[QToolbar(nameof(BurshMode), nameof(EditorMode))]
		public int BrushModeIndex;

		private List<GameObject> CurrentBrushList
		{
			get
			{
				return AllBrushList[BrushTypeIndex];
			}
		}
		private QList<List<GameObject>> AllBrushList = new QList<List<GameObject>>(() => new List<GameObject>());

		#region 基础属性
        public QList<Vector3Int, QBuilderData> CurrentLayer { get => MapData.Layers[BrushTypeIndex]; }
        public List<Transform> CanvasTransform { get; private set; } = new List<Transform>();
		public QTileMapData MapData { get; private set; } = new QTileMapData();
        public Bounds Bounds { get; private set; } = new Bounds();
      
        private GameObject CurrentBrush
        {
            get
            {
                if (BrushIndex < 0 || CurrentBrushList.Count <= BrushIndex)
                {
                    return null;
                }
                return CurrentBrushList[BrushIndex];
            }
		}
		private Bounds CurrentBrushBounds => Preview.GetBounds();
		public Transform Preview => transform.GetChild(nameof(Preview), true);
		public bool Contains(Vector3Int pos)
		{
			return CurrentLayer.ContainsKey(pos) && CurrentLayer[pos].gameObject != null;
		}
		public void Save(string path)
		{
			for (int i = 0; i < MapData.Layers.Count; i++)
			{
				foreach (var kv in MapData.Layers[i])
				{
					if (kv.brushKey.IsNull())
					{
						continue;
					}
					var brush = PrefabTypes[i].InvokeFunction("Load", kv.brushKey) as GameObject;
					if (brush == null)
					{
						Debug.LogError("找不到笔刷[" + PrefabTypes[i].Name + "/" + kv.brushKey + "]");
						continue;
					}
					kv.qPrefab = kv.gameObject.SaveQPrefab(brush,typeof(Transform));
				}
			}
			MapData.SaveQData(path);
		}
		public void Load(string path)
        {
            ClearAll();
			MapData.LoadQData(path);
			for (int i = 0; i < MapData.Layers.Count; i++)
            {
				BrushTypeIndex = i;
				foreach (var tileData in CurrentLayer)
                {
					if (tileData.brushKey.IsNull())
					{
						continue;
					}
					var brush = PrefabTypes[i].InvokeFunction("Load", tileData.brushKey) as GameObject;
					if (brush == null)
					{
						Debug.LogError("找不到笔刷["+ PrefabTypes[i].Name+"/"+ tileData.brushKey + "]");
						continue;
					}
					SetPrefab(CurrentLayer, tileData.Key, brush);
				}
				foreach (var tileData in CurrentLayer)
				{
					if (tileData.qPrefab !=null)
					{
						tileData.qPrefab.Load(tileData.gameObject);
					}
				}
            }
        }

		public void ClearAll()
		{
			foreach (var list in MapData.Layers.ToArray())
			{
				foreach (var kv in list.ToArray())
				{
					if (kv.gameObject != null)
					{
						SetPrefab(list, kv.Key, null);
					}
				}
			}
			MapData.Layers.Clear();
		}
	
        #endregion
      
    
        public void InitBrush()
        {
			for (int i = 0; i < PrefabTypes.Length; i++)
			{
				var brushType = PrefabTypes[i];
				if (AllBrushList[i].Count==0)
				{
					AllBrushList[i].AddRange(brushType.InvokeStaticFunction("LoadAll") as IList<GameObject>);
				}
			}
		}
	
        void InitMap()
        {
			EditorMode = false;
			Bounds = new Bounds();
			MapData.Layers.Clear();
			AllBrushList.Clear();
			CanvasTransform.Clear();
			foreach (var brushType in PrefabTypes)
			{
				CanvasTransform.Add(transform.GetChild(QReflection.QName(brushType), true));
			}
			if (!Application.IsPlaying(this))
			{
				Preview.gameObject.hideFlags = HideFlags.HideAndDontSave;
				InitBrush();
			}
			for (int type = 0; type < CanvasTransform.Count; type++)
			{
				var canvas = CanvasTransform[type];
				QDebug.Log(nameof(QBuilder) + " 初始化 " + canvas.name + " " + canvas.childCount);
				for (int i = canvas.childCount - 1; i >= 0; i--)
				{
					var child = canvas.GetChild(i);
					if (child == null) continue;
					var gridPos= child.ToGrid();
					var tileData = MapData.Layers[type][gridPos];
					if (tileData == null)
					{
						tileData = new QBuilderData();
						MapData.Layers[type][gridPos] = tileData;
					}
					tileData.SetValue(child.gameObject);
					Bounds.Encapsulate(child.position);
				}
			}
        }


		[QOnInspector(QInspectorState.OnEnable)]
		public void Awake()
        {
			InitMap();
		}
  
        public void SetPrefab(QList<Vector3Int, QBuilderData> canvas,IList<Vector3Int> posList,GameObject brush,bool checkMerge=true)
        {
            if (brush!=null)
            {

                var mergetList = new QList<Vector3Int, QBuilderData>();
                foreach (var pos in posList)
                {
					mergetList[pos] = new QBuilderData();
					mergetList[pos].SetValue(SetPrefab(canvas, pos, brush));
                }
            }
            else
            {
                foreach (var pos in posList)
                {
                    SetPrefab(canvas, pos, null);
                }
            }
        }
        public GameObject SetPrefab(QList<Vector3Int, QBuilderData> layer, Vector3Int pos, GameObject prefab)
		{
			var obj = prefab == null ? null : prefab.GridInstantiate(pos, CanvasTransform[BrushTypeIndex]);
			if (layer[pos]?.gameObject != null)
			{
				layer[pos].gameObject.CheckDestory();
			}
			if (obj == null)
			{
				layer[pos]?.SetValue(null);
			}
            else
			{
				if (layer[pos] == null)
				{
					layer[pos] = new QBuilderData();
				}
				layer[pos].SetValue(obj);
				obj.transform.RotateAround(pos.GridToVector3(), Vector3.up, Preview.rotation.eulerAngles.y);
				Bounds.Encapsulate(obj.transform.position);
            }
            return obj;
        }
    
     
      
        public void Draw(List<Vector3Int> listPos,bool clear=false)
        {
            SetPrefab(CurrentLayer, listPos, clear?null:CurrentBrush);
        }
        public GameObject Draw(Vector3Int pos,bool clear=false)
        {
            return SetPrefab(CurrentLayer, pos, clear?null:CurrentBrush);
        }
		[QOnSceneInput(EventType.KeyDown)]
        public bool KeyDown()
        {
			if(Event.current.keyCode== KeyCode.R)
			{
				Preview.Rotate(new Vector3(0, 90));
				return true;
			}
            return false;
        }
		bool selectBox = false;
		Vector3Int SelectStart;
		[QOnSceneInput(EventType.MouseDown)]
		public bool MouseDown(Ray ray)
		{
			if (!EditorMode) return false;
			selectBox = BrushModeIndex == 1;
			if (!selectBox)
			{
				Brush(ray);
			}
			else
			{
				MouseMove(ray);
				SelectStart=Preview.ToGrid();
			}
			return EditorMode;
		}
		[QOnInspector(QInspectorState.OnDisable)]
		public void OnClose()
		{
			Preview.gameObject.CheckDestory();
		}
		private void UpdatePreview()
		{
			Preview.ToGrid();
			if (Preview.childCount > 0)
			{
				var previewObject = Preview.GetChild(0).gameObject;
				if (previewObject.name == CurrentBrush?.name)
				{
					return;
				}
				Preview.rotation = Quaternion.identity;
				previewObject.CheckDestory();
			}
			if (CurrentBrush != null)
			{
				var view=Instantiate(CurrentBrush,Preview);
				view.name = CurrentBrush.QName();
				view.transform.localPosition =Vector3.zero;
				foreach (var r in Preview.GetComponentsInChildren<Renderer>())
				{
					if (r.sharedMaterial == null) continue;
					r.sharedMaterial = new Material(r.sharedMaterial);
					r.sharedMaterial.color = Color.Lerp(r.sharedMaterial.color, Color.black, 0.4f);
				}
			}
		}
		[QOnSceneInput(EventType.MouseMove)]
        public bool MouseMove(Ray ray)
        {
            if (EditorMode) {
				var bounds = CurrentBrushBounds;
				Preview.position = ray.RayCastPlane(Vector3.up, Vector3.zero) - bounds.size / 2 + Preview.position - bounds.center+bounds.size/2;
				UpdatePreview();
				return EditorMode;
			}
			else
			{
				return false;
			}
        }
        List<Vector3Int> SelectList = new List<Vector3Int>();
        [QOnSceneInput(EventType.MouseDrag)]
        public bool Brush(Ray ray)
        {
            if (!EditorMode) return false;
            SelectList.Clear();
			MouseMove(ray);
			if (selectBox)
            {
                SelectList = Preview.GetGridList(SelectStart, Preview.ToGrid());
            }
            else
            {
                Draw(Preview.ToGrid(), Event.current.shift);
            }
            return EditorMode;
        }		
		[QOnSceneInput(EventType.MouseUp)]
		public bool MouseUp(Ray ray)
		{
			if (!EditorMode) return false;
			if (selectBox)
			{
				selectBox = false;
				MouseMove(ray);
				SelectList = Preview.GetGridList(SelectStart, Preview.ToGrid());
				Draw(SelectList, Event.current.shift);
				SelectList.Clear();
			}
			return EditorMode;
		}
#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
        {
            if (CurrentBrush == null)
            {
                return;
            }
            Gizmos.color = Color.HSVToRGB(((CurrentBrush.GetHashCode()%100)*1f/100) ,0.5f,0.8f);
            if (Preview != null)
            {
				var bounds = CurrentBrushBounds;
				var size = bounds.size.GridFixed();
				Gizmos.DrawWireCube(bounds.center, size);
				if (selectBox)
                {
                    foreach (var pos in SelectList)
                    {
                        Gizmos.DrawWireCube(pos.GridToVector3()+bounds.center-Preview.position, size);
                    }
                }
            }
           
        }
#endif
	}
	public class QTileMapData
	{
		public QList<QList<Vector3Int, QBuilderData>> Layers = new QList<QList<Vector3Int, QBuilderData>>(() => new QList<Vector3Int, QBuilderData>());
	}
	public static class QBuilderTool
	{
		public const float GridSize = 0.1f;
		public static Vector3Int ToGrid(this Transform transform)
		{
			return transform.ToGrid(GridSize * Vector3.one);
		}
		public static Vector3Int ToGrid(this Vector3 vector3)
		{
			return vector3.ToGrid(GridSize * Vector3.one); 
		}
		public static Vector3 GridToVector3(this Vector3Int vector3)
		{
			return (Vector3)vector3 * GridSize;
		}
		public static Vector3 GridFixed(this Vector3 vector3)
		{
			return vector3.GridFixed(GridSize * Vector3.one);
		}
		public static GameObject GridInstantiate(this GameObject prefab, Vector3Int gridPosition, Transform parent)
		{
			var obj = prefab.CheckInstantiate(parent);
			obj.transform.position = gridPosition.GridToVector3();
			obj.name = obj.QName();
			return obj;
		}
		public static List<Vector3Int> GetGridList(this Transform brush, Vector3Int start, Vector3Int end)
		{
			var size = brush.GetBounds().size.ToGrid();
			var posList = new List<Vector3Int>();
			var minX = Mathf.Min(start.x, end.x);
			var maxX = Mathf.Max(start.x, end.x);
			var minZ = Mathf.Min(start.z, end.z);
			var maxZ = Mathf.Max(start.z, end.z);
			for (int x = minX; x <= maxX; x+= size.x)
			{
				for (int z = minZ; z <= maxZ;z+=size.z)
				{
					posList.Add(new Vector3Int(x, start.y, z));
				}
			}
			return posList;
		}

	}
	[System.Serializable]
	public class QBuilderData : IKey<Vector3Int>
	{
		public Vector3Int Key { get; set; }
		public string brushKey { get; set; }
		public QPrefab qPrefab { get; set; }
		[QIgnore]
		public GameObject gameObject;
		public void SetValue(GameObject gameObject)
		{
			this.gameObject = gameObject;
			this.brushKey = gameObject?.name;
		}
	}
	public abstract class QBuilderBrefab<T> : QPrefabLoader<T> where T : QBuilderBrefab<T>
	{

	}
}
