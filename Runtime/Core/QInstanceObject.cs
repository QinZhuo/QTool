using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static System.Collections.Specialized.BitVector32;

namespace QTool {

	/// <summary>
	///会自动创建对象的单例 继承MonoBehaviour 
	/// </summary>
	public abstract class QInstanceManager<T> : MonoBehaviour where T : QInstanceManager<T> {

		private static T _instance;
		public static T Instance {
			get {
				if (_instance == null) {
					_instance = FindObjectOfType<T>(true);
					if (_instance == null && QTool.IsPlaying) {
						QDebug.Log("单例实例化 " + nameof(QInstanceManager<T>) + "<" + typeof(T).Name + ">");
						var obj = new GameObject(typeof(T).Name);
						_instance = obj.AddComponent<T>();
						_instance.SetDirty();
					}
				}
				return _instance;
			}
			protected set => _instance = value;
		}
		public static bool IsExist => _instance != null;
		protected virtual void Awake() {
			_instance = this as T;
			DontDestroyOnLoad(gameObject);
		}
	}
	/// <summary>
	/// 不会自动创建的单例 继承MonoBehaviour
	/// </summary>
	public abstract class QInstanceBehaviour<T> : MonoBehaviour where T : QInstanceBehaviour<T> {

		private static T _instance;
		public static T Instance {
			get {
				if (_instance == null) {
					_instance = FindObjectOfType<T>(true);
				}
				return _instance;
			}
		}
		public static bool IsExist => _instance != null;
		protected virtual void Awake() {
			_instance = this as T;
		}
	}
	/// <summary>
	/// SO文件单例 存储在Resouces文件夹下 继承ScriptableObject 在ProjectSetting中可直接设置
	/// </summary>
	public abstract class QInstanceScriptable<T> : ScriptableObject where T : QInstanceScriptable<T> {
		private static T _instance;
		public static T Instance {
			get {
				if (_instance == null) {
					_instance = QTool.LoadAndCreate<T>("Settings/" + typeof(T).Name);
				}
				return _instance;
			}
		}
		public static bool IsExist => _instance != null;
	}
}
