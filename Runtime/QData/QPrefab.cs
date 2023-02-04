using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using QTool.Inspector;
namespace QTool
{
	public class QPrefab
	{
		public QList<string, QPrefabComponent> Components = new QList<string, QPrefabComponent>();
	 	public QPrefab()
		{
		}
		public QPrefab(GameObject gameObject,GameObject prefab)
		{
			var components = gameObject.GetComponents<Component>();
			foreach (var component in components)
			{
				var prefabComponent = new QPrefabComponent(component, prefab?.GetComponent(component.GetType()));
				if (prefabComponent.Members.Count > 0)
				{
					Components.Add(prefabComponent);
				}
			}
		}	
		public void Load(GameObject gameObject,GameObject prefab)
		{
			
		}
	}
	public class QPrefabComponent:IKey<string>
	{
		public string Key { get; set; }
		public QDictionary<string, string> Members = new QDictionary<string, string>();
		public QPrefabComponent()
		{
		}
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
	}
	public partial class Tool
	{
		public static QPrefab SaveQPrefab(this GameObject gameObject,GameObject prefab=null)
		{
			return new QPrefab(gameObject,prefab);
		}
		public static GameObject Instantiate(this QPrefab qprefab,GameObject prefab,Transform parent=null)
		{
			var gameObject = prefab.CheckInstantiate(parent);
			qprefab.Load(gameObject, prefab);
			return gameObject;
		}
	}
}
