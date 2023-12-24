using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	[ExecuteInEditMode]
	public class QObjectList : MonoBehaviour
	{
		[QName("预制体")]
		public GameObject prefab;
		public List<GameObject> objList { get; private set; } = new List<GameObject>();

		public virtual GameObject this[string name,GameObject prefab]
		{
			get
			{
				this.prefab = prefab;
				return this[name];
			}
		}
		public virtual GameObject this[string name]
		{
			get
			{
				if (name.IsNull())
				{
					Debug.LogError(this + "索引为空[" + name + "]");
					return null;
				}
				var trans = transform.Find(name);
				var view = (trans != null && trans.gameObject.activeSelf) ? trans.gameObject : null;
				if (view == null)
				{
					view = QPoolManager.Get(prefab);
					view.transform.SetParent(transform, false);
					view.transform.localScale = Vector3.one;
					view.transform.localRotation = Quaternion.identity;
					view.transform.SetAsLastSibling();
					objList.Add(view);
					view.name = name;
					_count++;
					OnCreate?.Invoke(view);
				}
				return view;
			}
		}
		private void OnTransformChildrenChanged()
		{
			objList.RemoveAll(obj => obj.transform.parent != transform);
		}
		public virtual void Clear()
		{
			for (int i = objList.Count - 1; i >= 0; i--)
			{
				var view = objList[i];
				Release(view);
			}
			OnClear?.Invoke();
		}
		private int _count = 0;
		public int Count
		{
			get
			{
				return _count;
			}
		}
		public void Release(GameObject view)
		{
			_count--;
			objList.Remove(view);
			OnPush?.Invoke(view);
			view.gameObject.PoolRelease();
		}
		public event System.Action<GameObject> OnPush;
		public event System.Action<GameObject> OnCreate;
		public event System.Action OnClear;
#if UNITY_EDITOR
		private void OnEnable()
		{
			if (!Application.isPlaying)
			{
				UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
				if (prefab != null)
				{
					QEditorPath.Insert(UnityEditor.AssetDatabase.GetAssetPath(prefab), 1);
				}
			}
			
		}
		private void OnDisable()
		{
			if (!Application.isPlaying)
			{
				UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
			}
		}
		private void OnDrawGizmos()
		{
			if (transform is RectTransform rectTransform)
			{
			
				if (UnityEditor.Selection.activeGameObject != gameObject)
				{
					Gizmos.color = name.ToColor().Lerp(Color.clear, 0.7f);
					Gizmos.DrawCube(rectTransform.Center(), rectTransform.Size());
				}
				else
				{
					Gizmos.color = name.ToColor().Lerp(Color.clear, 0.8f);
					Gizmos.DrawCube(rectTransform.Center(), rectTransform.Size());
				}
			}
		}
		[QName("测试数目")]
		public int TestCount = 4;
		private int lastCount = 0;
		private void OnPrefabStageClosing(UnityEditor.SceneManagement.PrefabStage prefabStage)
		{
			lastCount = 0;
		}

		private void Update()
		{
			if (!Application.isPlaying && prefab != null && TestCount != lastCount)
			{
				lastCount = TestCount;
				OnDestroy();
				for (int i = 0; i < TestCount; i++)
				{
					var obj = prefab.CheckInstantiate(transform);
					obj.hideFlags = HideFlags.HideAndDontSave;
					obj.name = prefab.name + "测试";
				}
			}
		}
		private void OnDestroy()
		{
			transform.ClearChild(child => child.name == prefab.name + "测试");
		}
#endif
	}

}
