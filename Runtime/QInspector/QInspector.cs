using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
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
		public string visibleControl;
		public string tooltip;
        public QNameAttribute()
        {
            order = 0;
        }
        public QNameAttribute(string name, string visibleControl = "") :this()
        {
            this.name = name;
            this.visibleControl = visibleControl;
        }
		public bool Active(object target)
		{
			if (visibleControl.IsNull())
			{
				return true;
			}
			else
			{
				return (bool)target.GetPathBool(visibleControl);
			}
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
	public class QPopupAttribute : PropertyAttribute
	{
		public string[] getListFuncs = new string[0];
		public QPopupAttribute(params string[] getListFunc)
		{
			order = 1;
			this.getListFuncs = getListFunc;
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
		public string[] getListFuncs;
		public int pageSize = 0;
		public Type type;
		public QToolbarAttribute(Type type, string showControl = "", int pageSize = 0):this("")
		{
			this.type = type;
		}

		public QToolbarAttribute( string getListFunc="", string showControl = "",int pageSize =0) : base("", showControl)
        {
			if (getListFunc.IsNull())
			{
				getListFuncs = new string[0];
			}
			else
			{
				getListFuncs = new string[] { getListFunc };
			}
			this.pageSize = pageSize;
            if (name.IsNull())
            {
                name = getListFunc;
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
 

	public class QInspectorType : QTypeInfo<QInspectorType>
	{
		public QDictionary<QOnInspectorAttribute, QFunctionInfo> inspectorState = new QDictionary<QOnInspectorAttribute, QFunctionInfo>();
		public QDictionary<QOnSceneInputAttribute, QFunctionInfo> mouseEventFunc = new QDictionary<QOnSceneInputAttribute, QFunctionInfo>();
		public QDictionary<QFunctionInfo, QNameAttribute> buttonFunc = new QDictionary<QFunctionInfo, QNameAttribute>();
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
			}
		}
		public void InvokeQInspectorState(object target,QInspectorState state)
		{
			foreach (var kv in inspectorState)
			{
				if (kv.Key.state == state)
				{
					var result = kv.Value.Invoke(target);
					if (result is Task task)
					{
						_ = task.Run();
					}
				}
			}
		}
		
	
	}
#if UNITY_EDITOR
	#region 自定义显示效果
	[CustomPropertyDrawer(typeof(QNameAttribute))]
	public class QNameDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			return new PropertyField(property, QReflection.QName(property));
		}
	}
	[CustomPropertyDrawer(typeof(QReadonlyAttribute))]
	public class QReadonlyDrawer : QNameDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var visual= base.CreatePropertyGUI(property);
			visual.SetEnabled(false);
			return visual;
		}
	}
	[CustomPropertyDrawer(typeof(QOnChangeAttribute))]
	public class QOnChangeDrawer : QNameDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var visual= base.CreatePropertyGUI(property);
			visual.RegisterCallback<SerializedPropertyChangeEvent>(data =>
			{
				property.InvokeFunction((attribute as QOnChangeAttribute).changeCallBack);
			});
			return visual;
		}
	}
	[CustomPropertyDrawer(typeof(QIdObject))]
	public class QIdObjectReferenceDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var id = property.FindPropertyRelative(nameof(QIdObject.id));
			var obj = property.FindPropertyRelative("_"+nameof(QIdObject.Object));
			var visual = new PropertyField(obj, QReflection.QName(property) + "[" + id.stringValue.ToShortString(5) + "]");
			visual.RegisterCallback<ChangeEvent<UnityEngine.Object>>(data =>
			{
				id.stringValue = QIdTool.GetQId(data.newValue)?.ToString();
				visual.Q<Label>().text = QReflection.QName(property) + "[" + id.stringValue.ToShortString(5) + "]";
			});
			return visual;
		}
	}

	[CustomPropertyDrawer(typeof(QToggleAttribute))]
	public class QToggleDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var button = new Button();
			button.text = QReflection.QName(property);
			button.style.height = 30;
			var color = button.style.backgroundColor;
			button.RegisterCallback<ClickEvent>(data =>
			{
				property.boolValue = !property.boolValue;
				property.serializedObject.ApplyModifiedProperties();
				button.style.backgroundColor = property.boolValue ?Color.black.Lerp(color.value,0.8f): color;
			});
			return button;
		}
	}

	[CustomPropertyDrawer(typeof(QPopupAttribute))]
	public class QPopupDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var root = new VisualElement();
			root.style.flexDirection = FlexDirection.Row;
			var popupData = QPopupData.Get(property, (attribute as QPopupAttribute).getListFuncs);
			var value = property.propertyType == SerializedPropertyType.String ? property.stringValue : property.objectReferenceValue?.GetType()?.Name;
			if (!popupData.List.Contains(value))
			{
				popupData.List.Add(value);
			}
			if (property.propertyType == SerializedPropertyType.ObjectReference && value == null)
			{
				value = popupData.List.Get(0);
			}
			var visual = new PopupField<string>(QReflection.QName(property), popupData.List, value);
			root.Add(visual);
			PropertyField propertyField = null;
			if (property.propertyType != SerializedPropertyType.String)
			{
				propertyField = root.Add(property);
				propertyField.label = "";
				propertyField.style.width = new Length(100, LengthUnit.Percent);
				if (property.objectReferenceValue != null)
				{
					propertyField.SetEnabled(false);
				}
			}
			visual.RegisterCallback<ChangeEvent<string>>(data =>
			{
				if (property.propertyType == SerializedPropertyType.String)
				{
					property.stringValue = data.newValue;
				}
				else
				{
					var gameObject = (property.serializedObject.targetObject as MonoBehaviour)?.gameObject;
					if (gameObject != null)
					{
						value = data.newValue;
						if (value.IsNull())
						{
							property.objectReferenceValue = null;
							propertyField.SetEnabled(true);
						}
						else
						{
							try
							{
								property.objectReferenceValue = gameObject.GetComponent(QReflection.ParseType(value), true);
							}
							catch (Exception)
							{
								throw;
							}
							finally
							{
								propertyField.SetEnabled(false);
							}
							
						}
					}
				}
				property.serializedObject.ApplyModifiedProperties();
			});
			return root;
		}
	}
	[CustomPropertyDrawer(typeof(QToolbarAttribute))]
	public class QToolbarDrawer : PropertyDrawer
	{
		static QDictionary<int, int> IndexCache = new QDictionary<int, int>((key) => -1);
		const int Height = 30;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Draw(attribute as QToolbarAttribute, position, property, label);
		}
		public static void Draw(QToolbarAttribute toolbar, Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = Height;
			var listData = toolbar.type != null ? QPopupData.Get(toolbar.type) : QPopupData.Get(property, toolbar.getListFuncs);
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
				var tempList = new List<string>();
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
			return property.IsShow() ? (toolbar.pageSize > 0 && QPopupData.Get(property, toolbar.getListFuncs).List.Count > toolbar.pageSize ? Height + 20 : Height) : 0;
		}
	}
#endregion
#endif
}
