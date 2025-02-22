using QTool.Inspector;
using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace QTool.ECS {
	public class QWorld : QSingletonManager<QWorld> {
		public static World Active => Instance.World;
		public World World { get;private set; }
		protected override void Awake() {
			base.Awake();
			World = new World();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (var type in assembly.GetTypes()) {
					if (type.Is(typeof(QuerySystem)) && !type.IsAbstract) {
						Active.RegisterSystem(Activator.CreateInstance(type) as QuerySystem);
					}
				}
			}
		}
		private void Update() {
			Active.Update();
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(QWorld))]
	public class QWorldEditor : QInspectorEditor {
		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			var qWorld = target as QWorld;
			root.Add("Systems", qWorld.World.Systems, typeof(IEnumerable<ISystem>), null);
			return root;
		}
	}
#endif
}
