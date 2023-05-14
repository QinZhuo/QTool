using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	public abstract class QRuntime<RuntimeT,DataT> where RuntimeT:QRuntime<RuntimeT,DataT>,new() where DataT : QDataList<DataT>, new()
	{
		public string Key { get; protected set; }
		public DataT Data { get; protected set; }
		protected QRuntime() { }
		public static RuntimeT Get(string key)
		{
			var t= new RuntimeT();
			t.Init(key);
			return t;
		}
		public virtual void Init(string key)
		{
			Key = key;
			Data = QDataList<DataT>.Get(key);
			
		}
	}
	public abstract class QRuntimeObject<RuntimeT, DataT> : MonoBehaviour,IQPoolObject where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
	{
		private RuntimeT _Runtime = null;
		public RuntimeT Runtime
		{
			get
			{
				if (_Runtime == null)
				{
					Runtime = QRuntime<RuntimeT, DataT>.Get(gameObject.QName());
				}
				return _Runtime;
			}
			set
			{
				if (value != _Runtime)
				{
					gameObject.UnRegister(_Runtime);
					_Runtime = value;
					gameObject.Register(_Runtime);
				}
			}
		}
		public DataT Data => Runtime?.Data;
		public virtual void Start()
		{
			var runtime = Runtime;
		}
		public virtual void OnDestroy()
		{
			if (Runtime != null)
			{
				Runtime = null;
			}
		}

	}
}

