using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using QTool.Inspector;
using System.Linq;
using System;

namespace QTool
{
	public class QPrefab
	{
		public QDictionary<string, string> Members = new QDictionary<string, string>();
		public QDictionary<int, QPrefab> Childs = new QDictionary<int, QPrefab>();
		public QList<string, QPrefabComponent> Components = new QList<string, QPrefabComponent>();
		public bool HasData => Members.Count > 0 || Childs.Count > 0 || Components.Count > 0;
		public QPrefab() { }
		public QPrefab(GameObject gameObject,GameObject prefab,params Type[] ignoreComponent)
		{
			if (gameObject.activeSelf != prefab?.activeSelf)
			{
				Members[nameof(gameObject.activeSelf)] = gameObject.activeSelf.ToQData();
			}
			var start = prefab == null ? gameObject.transform.childCount - 1 : prefab.transform.childCount - 1;
			for (int i = start; i >= 0; i--)
			{
				var child = gameObject.transform.GetChild(i);
				if (child != null)
				{
					var childPrefab = new QPrefab(child.gameObject, prefab?.transform?.Find(child.name)?.gameObject);
					if (childPrefab.HasData)
					{
						Childs[i]=childPrefab;
					}
				}
			}
			var components = gameObject.GetComponents<Component>();
			foreach (var component in components)
			{
				var type = component.GetType();
				if (ignoreComponent.Contains(type)) continue;
				QPrefabComponent prefabComponent = null;
				if (component is IQPrefabComponent iprefab)
				{
					prefabComponent= iprefab.SaveQPrefab(prefab?.GetComponent(component.GetType()));
					prefabComponent.Key = type.QTypeName();
				}
				else
				{
					prefabComponent = new QPrefabComponent(component, prefab?.GetComponent(component.GetType()));
				}
				if (prefabComponent.HasData)
				{
					Components.Add(prefabComponent);
				}
			}
		}	
		public void Load(GameObject gameObject)
		{
			foreach (var member in Members)
			{
				switch (member.Key)
				{
					case nameof(gameObject.activeSelf):
						gameObject.SetActive(member.Value.ParseQData<bool>());
						break;
					default:
						break;
				}
			}
			foreach (var childData in Childs)
			{
				var child = gameObject.transform.GetChild(childData.Key);
				if (child != null)
				{
					childData.Value.Load(child.gameObject);
				}
			}
			foreach (var componentData in Components)
			{
				var type= QReflection.ParseType(componentData.Key);
				if (type == null)
				{
					Debug.LogError("找不到脚本[" + componentData.Key + "]");
				}
				var component = gameObject.GetComponent(type);
				if (component == null)
				{
					component=gameObject.AddComponent(type);
				}
				if (component is IQPrefabComponent iprefab)
				{
					iprefab.LoadQPrefab(componentData);
				}
				else if (component != null)
				{
					componentData.Load(component);
				}
			}
		}
	}
	public interface IQPrefabComponent
	{
		QPrefabComponent SaveQPrefab(Component prefab);
		void LoadQPrefab(QPrefabComponent data);
	}
	public class QPrefabComponent:IKey<string>
	{
		public string Key { get; set; }
		public QDictionary<string, string> Members = new QDictionary<string, string>();
		public bool HasData => Members.Count > 0;
		public QPrefabComponent() { }
		public QPrefabComponent(Component component,Component prefab)
		{
			var typeInfo = QInspectorType.Get(component.GetType());
			Key = typeInfo.Type.QTypeName();
			foreach (var member in typeInfo.Members)
			{
				var value = member.Get(component);
				if (prefab == null)
				{
					Members[member.QName] = value.ToQDataType(member.Type);
				}
				else
				{
					if (!Equals(value,member.Get(prefab)))
					{
						Members[member.QName] = value.ToQDataType(member.Type);
					}
				}
			}
		}
		public void Load(Component component)
		{
			var typeInfo = QInspectorType.Get(component.GetType());
			foreach (var member in Members)
			{
				try
				{
					var memberInfo = typeInfo.GetMemberInfo(member.Key);
					memberInfo.Set(component, member.Value.ParseQDataType(memberInfo.Type));
				}
				catch (Exception e)
				{
					Debug.LogError("[" + member.Key + "]" + member.Value + " :" + e);
				}
			}
		}
	}
	public partial class Tool
	{
		public static QPrefab SaveQPrefab(this GameObject gameObject,GameObject prefab=null,params Type[] ignoreComponent)
		{
			return new QPrefab(gameObject,prefab, ignoreComponent);
		}
		public static GameObject Instantiate(this QPrefab qprefab,GameObject prefab,Transform parent=null)
		{
			var gameObject = prefab.CheckInstantiate(parent);
			qprefab.Load(gameObject);
			return gameObject;
		}
	}
}
