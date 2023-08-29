using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
    public class QObjectList : MonoBehaviour
    {
		[QName("预制体")]
        public GameObject prefab;
        public List<GameObject> objList{ get; private set; }= new List<GameObject>();

        public virtual GameObject this[string name]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(name))
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
			view.gameObject.PoolRelease();
            objList.Remove(view);
            OnPush?.Invoke(view);
        }
        public event System.Action<GameObject> OnPush;
        public event System.Action<GameObject> OnCreate;
        public event System.Action OnClear;
    }

}
