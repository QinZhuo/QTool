using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System;
namespace QTool
{
	public abstract class QRuntime<RuntimeT, DataT> : IKey<string>, IQPoolObject where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
	{
		[QName]
		public virtual string Key { get; set; }
		public override string ToString()
		{
			return Key;
		}
		public DataT Data { get; protected set; }
		protected QRuntime() { }
		public static RuntimeT Get(string key)
		{
			var t = QPoolManager.Get(typeof(RuntimeT).FullName, () => new RuntimeT());
			t.Init(key);
			return t;
		}
		protected virtual void Init(string key)
		{
			Key = key;
			Data = QDataList<DataT>.Get(key);
		}

		public virtual void OnPoolGet()
		{

		}
		public virtual void OnPoolRelease()
		{
			Key = "";
			Data = null;
		}
	}
	
	public abstract class QRuntimeObject<RuntimeT, DataT> : MonoBehaviour,IKey<string>,IQPoolObject where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
	{
		private RuntimeT _Runtime = null;
		public RuntimeT Runtime
		{
			get
			{
				if (this == null) return null;
				var name = gameObject.QName();
				if (_Runtime == null && QDataList<DataT>.ContainsKey(name))
				{
					Runtime = QRuntime<RuntimeT, DataT>.Get(name);
				}
				return _Runtime;
			}
			set
			{
				if (value != _Runtime)
				{
					if (_Runtime != null)
					{
						OnUnsetRuntime();
					}
					_Runtime = value;
				
					if (_Runtime != null)
					{
						OnSetRuntime();
					}
				}
			}
		}
		public QDictionary<string, QRuntimeValue> RuntimeValues { get; private set; } = new QDictionary<string, QRuntimeValue>();
		public DataT Data => Runtime?.Data;
		public virtual string Key { get => Runtime.Key; set => Runtime.Key = value; }
		public Action<string> OnValueChange = null;
		public virtual void Awake()
		{
			OnPoolGet();
		}
		public virtual void OnDestroy()
		{
			OnPoolRelease();
		}
		public virtual void OnPoolGet()
		{
		//	gameObject.RegisterEvent(this);
		}
		public virtual void OnPoolRelease()
		{
			//gameObject.UnRegisterEvent(this);
			if (_Runtime != null)
			{
				Runtime = null;
			}
		}
		protected virtual void OnSetRuntime()
		{
			//gameObject.RegisterEvent(_Runtime?.Data);
			//gameObject.RegisterEvent(_Runtime);
			var typeInfo = QSerializeType.Get(typeof(RuntimeT));
			foreach (var member in typeInfo.Members)
			{
				if (member.Type.Is(typeof(QRuntimeValue)))
				{
					var runtimeValue = member.Get(_Runtime).As<QRuntimeValue>();
					runtimeValue.Name = member.QName;
					RuntimeValues[member.QName] = runtimeValue;
					runtimeValue.OnValueChange += (key, value) =>
					{
						OnValueChange?.Invoke(key);
					};
				}
			}
		}
		protected virtual void OnUnsetRuntime()
		{
			//gameObject.UnRegisterEvent(_Runtime);
			RuntimeValues.Clear();
		}
	}
}

