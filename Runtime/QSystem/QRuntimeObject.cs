using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System;
namespace QTool
{
	public abstract class QRuntime<RuntimeT, DataT> : IQPoolObject where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
	{
		public string Key { get; protected set; }
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
	

		public virtual void OnDestroy()
		{
			Key = "";
			Data = null;
		}
	}
	
	public abstract class QRuntimeObject<RuntimeT, DataT> : MonoBehaviour,IQPoolObject where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
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
					gameObject.UnRegisterEvent(_Runtime);
					_Runtime = value;
					gameObject.RegisterEvent(_Runtime);
				}
			}
		}
		public QDictionary<string, QRuntimeValue> RuntimeValues { get; private set; } = new QDictionary<string, QRuntimeValue>();
		public DataT Data => Runtime?.Data;
		public event Action<string> OnValueChange = null;
		public virtual void Awake()
		{
			gameObject.RegisterEvent(this);
			InitRuntimeValues();
		}
		public void InitRuntimeValues()
		{
			var runtime = Runtime;
			if (runtime != null)
			{
				var typeInfo = QSerializeType.Get(typeof(RuntimeT));
				foreach (var member in typeInfo.Members)
				{
					if (member.Type.Is(typeof(QRuntimeValue)))
					{
						var runtimeValue = member.Get(runtime).As<QRuntimeValue>();
						runtimeValue.Name = member.QName;
						RuntimeValues[member.QName] = runtimeValue;
						runtimeValue.OnValueChange += (key, value) =>
						{
							OnValueChange?.Invoke(key);
						};
					}
				}
			}
		}
		public virtual void OnDestroy()
		{
			gameObject.UnRegisterEvent(this);
			if (_Runtime != null)
			{
				Runtime = null;
				RuntimeValues.Clear();
			}
		}

	}
}

