using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Reflection;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
    /// <summary>
    /// 更改显示的名字
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method|AttributeTargets.Interface | AttributeTargets.Parameter|AttributeTargets.Property)]
    public class QNameAttribute : PropertyAttribute
    {
        public string name; 
        public string visibleControl = "";
        public QNameAttribute()
        {
            order = 0;
        }
        public QNameAttribute(string name, string visibleControl = "") :this()
        {
            this.name = name;
            this.visibleControl = visibleControl;
        }
    }
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Property)]
	public class QOldNameAttribute : Attribute
	{
		public string name;
		public QOldNameAttribute(string name)
		{
			this.name = name;
		}
	}
	/// <summary>
	/// 使数据在inspector视窗不可更改
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
    public class QReadonlyAttribute : PropertyAttribute
	{
        public QReadonlyAttribute() 
        {
        }
    }

	/// <summary>
	/// 将字符传显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
	public class QEnumAttribute : PropertyAttribute
	{
		public string funcKey;
		public QEnumAttribute(string GetKeyListFunc = "")
		{
			order = 1;
			this.funcKey = GetKeyListFunc;
		}
	}

}
namespace QTool.Inspector
{ 
	/// <summary>
	/// 将bool显示为按钮Toogle
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class QToggleAttribute : QNameAttribute
	{

		public QToggleAttribute(string name, string showControl = "") : base(name, showControl)
		{
			order = 1;
		}

	}

	/// <summary>
	/// 数值更改时调用changeCallBack函数
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
    public class QOnChangeAttribute : PropertyAttribute
    {
        public bool change;
        public string changeCallBack;
        public QOnChangeAttribute(string changeCallBack)
        {
            this.changeCallBack = changeCallBack;
        }
    }


	/// <summary>
	/// 将int索引显示为toolbar工具栏 数据来源 listMember
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
    public class QToolbarAttribute : QNameAttribute
    {
        public string getList;
        public int pageSize= 0;
        public QToolbarAttribute( string getList="", string showControl = "",int pageSize =0) : base("", showControl)
        {
            this.getList = getList;
			this.pageSize = pageSize;
            if (name.IsNull())
            {
                name = getList;
            }
        }
    }

  
    /// <summary>
    /// 当在scene视窗鼠标事件调用 传入参数Ray为当前鼠标射线
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple = true)]
    public class QOnSceneInputAttribute : Attribute
    {
        public EventType eventType = EventType.MouseDown;
        public QOnSceneInputAttribute(EventType eventType)
        {
            this.eventType = eventType;
        }
    }
    /// <summary>
    /// inspector状态更改时调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnInspectorAttribute : Attribute
    {
		public QInspectorState state;
		public QOnInspectorAttribute(QInspectorState state)
        {
			this.state = state;
		}
    }
	public enum QInspectorState
	{
		OnEnable,
		OnDisable,
	}
    public enum PlayModeState
    {
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in edit mode
        //     and was previously in play mode.
        EnteredEditMode = 0,
        //
        // 摘要:
        //     Occurs when exiting edit mode, before the Editor is in play mode.
        ExitingEditMode = 1,
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in play mode
        //     and was previously in edit mode.
        EnteredPlayMode = 2,
        //
        // 摘要:
        //     Occurs when exiting play mode, before the Editor is in edit mode.
        ExitingPlayMode = 3
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnPlayModeAttribute : Attribute
    {
		public static PlayModeState CurrentrState { set; get; } = PlayModeState.EnteredPlayMode;
		public PlayModeState state;
        public QOnPlayModeAttribute(PlayModeState state)
        {
            this.state = state;
        }

    }
  
    /// <summary>
    /// 选取对象按钮显示 会调用函数CallFunc传入GameObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class QSelectObjectButtonAttribute : QNameAttribute
    {
        public QSelectObjectButtonAttribute(string name,  string showControl = "") : base(name, showControl)
        {
        }
    }


	public class QInspectorType : QTypeInfo<QInspectorType>
	{
		public QDictionary<QOnInspectorAttribute, QFunctionInfo> inspectorState = new QDictionary<QOnInspectorAttribute, QFunctionInfo>();
		public QDictionary<QOnSceneInputAttribute, QFunctionInfo> mouseEventFunc = new QDictionary<QOnSceneInputAttribute, QFunctionInfo>();
		public QDictionary<QFunctionInfo, QNameAttribute> buttonFunc = new QDictionary<QFunctionInfo, QNameAttribute>();
		public QDictionary<QOnPlayModeAttribute, QFunctionInfo> playMode = new QDictionary<QOnPlayModeAttribute, QFunctionInfo>();
		protected override void Init(Type type)
		{
			base.Init(type);
			if (!TypeMembers.ContainsKey(type))
			{
				Members.RemoveAll(memebr => (!memebr.IsPublic && memebr.MemeberInfo.GetCustomAttribute<SerializeField>() == null) || !(memebr.MemeberInfo is FieldInfo));
			}
			foreach (var funcInfo in Functions)
			{
				foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnSceneInputAttribute>())
				{
					mouseEventFunc[att] = funcInfo;
				}
				foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QNameAttribute>())
				{
					buttonFunc[funcInfo] = att;
				}
				foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<ContextMenu>())
				{
					buttonFunc[funcInfo] = new QNameAttribute(att.menuItem);
				}
				foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnInspectorAttribute>())
				{
					inspectorState[att] = funcInfo;
				}
				foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnPlayModeAttribute>())
				{
					playMode[att] = funcInfo;
				}
			}
		}
	}
#if UNITY_EDITOR
	#region 自定义显示效果
	[CustomPropertyDrawer(typeof(QNameAttribute))]
	public class QNameDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (label.text == property.displayName)
			{
				label.text = property.QName();
			}
			if (label.tooltip != property.tooltip)
			{
				label.tooltip = property.tooltip;
			}
			property.Draw(position, label);
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return property.GetHeight();
		}
	}
	[CustomPropertyDrawer(typeof(QReadonlyAttribute))]
	public class QReadonlyDrawer : QNameDrawer
	{
	}
	[CustomPropertyDrawer(typeof(QOnChangeAttribute))]
	public class QOnChangeDrawer : QNameDrawer
	{

	}
	[CustomPropertyDrawer(typeof(QIdObject))]
	public class QIdObjectReferenceDrawer : PropertyDrawer
	{
	
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var id = property.FindPropertyRelative(nameof(QIdObject.id));
			id.stringValue =QGUI.DrawQIdObject(label.text, id.stringValue, typeof(UnityEngine.Object), position);
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label);
		}
	}

	[CustomPropertyDrawer(typeof(QToggleAttribute))]
	public class QToggleDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Boolean)
			{
				position.height = 30;
				QGUI.PushColor(property.boolValue ? Color.black : Color.white);
				property.boolValue = EditorGUI.Toggle(position, property.boolValue, QGUI.AlphaBackStyle);
				QGUI.PopColor();
				var style = EditorStyles.largeLabel;
				style.alignment = TextAnchor.MiddleCenter;
				EditorGUI.LabelField(position, (attribute as QToggleAttribute).name, style);
			}
			else
			{
				property.Draw(position, label);
			}

		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Boolean)
			{
				return 30;
			}
			else
			{
				return property.GetHeight();
			}
		}
	}

	[CustomPropertyDrawer(typeof(QEnumAttribute))]
	public class QEnumDrawer : QNameDrawer
	{
	
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!property.IsShow()) return;
			var data = QEnumListData.Get(property, (attribute as QEnumAttribute).funcKey);
			var value = property.propertyType == SerializedPropertyType.String ? property.stringValue : property.objectReferenceValue?.GetType()?.Name;
			data.UpdateList(value);
			EditorGUI.LabelField(position.HorizontalRect(0f, 0.3f), property.QName());
			var newIndex = EditorGUI.Popup(position.HorizontalRect(0.7f, 1), data.SelectIndex, data.List.ToArray());
			if (newIndex != data.SelectIndex || value.IsNull())
			{
				data.SelectIndex = newIndex;
				if (property.propertyType == SerializedPropertyType.String)
				{
					property.stringValue = data.SelectValue;
				}
				else
				{
					var gameObject = (property.serializedObject.targetObject as MonoBehaviour)?.gameObject;
					if (gameObject != null)
					{
						if (!value.IsNull())
						{
							GameObject.DestroyImmediate(gameObject.GetComponent(QReflection.ParseType(value)));
						}
						value = data.SelectValue;
						if (!value.IsNull())
						{
							property.objectReferenceValue = gameObject.GetComponent(QReflection.ParseType(value));
							if (property.objectReferenceValue == null)
							{
								property.objectReferenceValue = gameObject.AddComponent(QReflection.ParseType(value));
							}
						}
					}

				}
			}


		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return property.GetHeight();
		}
	}

	[CustomPropertyDrawer(typeof(QToolbarAttribute))]
	public class QToolbarDrawer : QNameDrawer
	{
		static QDictionary<int, int> IndexCache = new QDictionary<int, int>((key) => -1);
		const int Height = 30;
		public static void Draw(QToolbarAttribute toolbar, Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = Height;
			var listData = QEnumListData.Get(property, toolbar.getList);
			if (listData.List.Count == 0)
			{
				GUI.Toolbar(position, 0, new string[] { "无" + toolbar.name });
			}
			else if (toolbar.pageSize <= 0)
			{
				property.intValue = GUI.Toolbar(position, property.intValue, listData.List.ToArray());
			}
			else
			{
				var pageList = new List<GUIContent>();
				for (int i = 0; i < listData.List.Count / toolbar.pageSize + 1; i++)
				{
					pageList.AddObject(i);
				}
				if (IndexCache[listData.List.GetHashCode()] < 0)
				{
					IndexCache[listData.List.GetHashCode()] = property.intValue / toolbar.pageSize;
				}
				var start = IndexCache[listData.List.GetHashCode()] * toolbar.pageSize;
				var tempList = new List<GUIContent>();
				for (int i = start; i < Mathf.Min(start + toolbar.pageSize, listData.List.Count); i++)
				{
					tempList.Add(listData.List[i]);
				}
				if (listData.List.Count > toolbar.pageSize)
				{
					position.height = 20;
					IndexCache[listData.List.GetHashCode()] = GUI.Toolbar(position, IndexCache[listData.List.GetHashCode()], pageList.ToArray());
					position.y += 20;
					position.height = Height;
				}
				if (IndexCache.Count == 0)
				{
					IndexCache[listData.List.GetHashCode()] = 0;
				}
				else
				{
					var intValue = start + GUI.Toolbar(position, property.intValue - start, tempList.ToArray());
					if (property.intValue != intValue)
					{
						property.intValue = intValue;
					}
				}

			}

		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var toolbar = (attribute as QToolbarAttribute);
			return property.IsShow() ? (toolbar.pageSize > 0 && QEnumListData.Get(property, toolbar.getList).List.Count > toolbar.pageSize ? Height + 20 : Height) : 0;
		}
	}
	#endregion
#endif
}
