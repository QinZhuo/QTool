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
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Property)]
	public class QNameAttribute : PropertyAttribute
	{
		public string name;
		public string visibleControl;
		public string tooltip;
		public QNameAttribute()
		{
			order = 0;
		}
		public QNameAttribute(string name, string visibleControl = "") : this()
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
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
	public class QIgnoreAttribute : Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class| AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class QOldNameAttribute : Attribute {
		public string name;
		public QOldNameAttribute(string name) {
			this.name = name;
		}
	}

	/// <summary>
	/// 使数据在inspector视窗不可更改
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
	public class QReadonlyAttribute : PropertyAttribute
	{
		public QReadonlyAttribute()
		{
		}
	}
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
	public class QObjectAttribute : PropertyAttribute {
		public Type type;
		public QObjectAttribute(Type type = null) {
			if (type == null) {
				type = typeof(UnityEngine.Object);
			}
			this.type = type;
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
	}/// <summary>
	 /// 将int显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
	 /// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
	public class QMaskAttribute : PropertyAttribute {
		public string[] getListFuncs = new string[0];
		public QMaskAttribute(params string[] getListFunc) {
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
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class QNavMeshAreaAttribute : PropertyAttribute
	{
		public QNavMeshAreaAttribute()
		{
		}
	}
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class QNavMeshAgentAttribute : PropertyAttribute
	{
		public bool mask = false;
		public QNavMeshAgentAttribute(bool mask=false)
		{
			this.mask = mask;
		}
	}

#if UNITY_EDITOR
	#region 自定义显示效果

#if Navigation

	
	[CustomPropertyDrawer(typeof(QNavMeshAgentAttribute))]
	public class QNavMeshAgentDrawer : PropertyDrawer
	{

		public QNavMeshAgentDrawer()
		{

		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if ((attribute as QNavMeshAgentAttribute).mask)
			{
				Unity.AI.Navigation.Editor.NavMeshComponentsGUIUtility.AgentMaskPopup(label.text, property);
			}
			else
			{
				Unity.AI.Navigation.Editor.NavMeshComponentsGUIUtility.AgentTypePopup(label.text, property);
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
	[CustomPropertyDrawer(typeof(QNavMeshAreaAttribute))]
	public class QNavMeshAreaDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			AreaPopupMask(label.text, property);
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}

		public static void AreaPopupMask(string labelName, SerializedProperty areaProperty)
		{
			var areaNames = UnityEngine.AI.NavMesh.GetAreaNames();
			var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginProperty(rect, GUIContent.none, areaProperty);
			areaProperty.intValue = EditorGUI.MaskField(rect, labelName, areaProperty.intValue, areaNames);
			EditorGUI.EndProperty();
		}
	}
#endif
	[CustomPropertyDrawer(typeof(QNameAttribute))]
	public class QNameDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			return new PropertyField(property, QReflection.QName(property));
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.PropertyField(position, property, new GUIContent(QReflection.QName(property)));
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
	[CustomPropertyDrawer(typeof(QObjectAttribute))]
	public class QIdObjectReferenceDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var qIdObject = attribute as QObjectAttribute;
			var root = new VisualElement();
			root.style.flexDirection = FlexDirection.Row;
			switch (property.propertyType) {
				case SerializedPropertyType.String: {
					var visual = root.AddObject(QReflection.QName(property), qIdObject.type, QObjectTool.GetObject(property.stringValue, qIdObject.type));
					visual.style.width = new Length(50, LengthUnit.Percent);
					visual.RegisterCallback<ChangeEvent<UnityEngine.Object>>(data =>
					{
						property.stringValue = QObjectTool.GetPath(data.newValue);
						property.serializedObject.ApplyModifiedProperties();
					});
				}
				break;
				default:
					break;
			}
			var propertyField = root.Add(property);
			propertyField.style.width = new Length(50, LengthUnit.Percent);
			return root;
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
			var propertyField = root.Add(property);
			propertyField.label = "";
			visual.style.width = new Length(50, LengthUnit.Percent);
			propertyField.style.width = new Length(50, LengthUnit.Percent);
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
						}
					}
				}
				property.serializedObject.ApplyModifiedProperties();
			});
			return root;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.PropertyField(position, property, label);
		}
	}
	[CustomPropertyDrawer(typeof(QMaskAttribute))]
	public class QMaskDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var root = new VisualElement();
			root.style.flexDirection = FlexDirection.Row;
			var popupData = QPopupData.Get(property, (attribute as QMaskAttribute).getListFuncs);
			popupData.List.RemoveAt(0);
			var value = property.intValue ;
			var visual = new MaskField(QReflection.QName(property), popupData.List, value);
			visual.style.width = new Length(100, LengthUnit.Percent);
			root.Add(visual);
			var propertyField = root.Add(property);
			propertyField.label = "";
			visual.style.width = new Length(50, LengthUnit.Percent);
			propertyField.style.width = new Length(50, LengthUnit.Percent);
			visual.RegisterCallback<ChangeEvent<int>>(data => {
				property.intValue = data.newValue;
				property.serializedObject.ApplyModifiedProperties();
			});
			return root;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.PropertyField(position, property, label);
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
	[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
	[CanEditMultipleObjects]
	public class QInspectorEditor : Editor {
		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement();
			root.Add(serializedObject);
			if (target != null) {
				foreach (var func in QSerializeType.Get(target?.GetType()).Functions) {
					if (func.MethodInfo.GetCustomAttribute<QNameAttribute>() != null) {
						root.AddButton(func.QName, () => func.Invoke(target));
					}
				}
			}
			return root;
		}
	}
#endregion
#endif
}
