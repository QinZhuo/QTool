using QTool.Reflection;
using System;
using UnityEngine;
namespace QTool.ECS {
	public class QWorld : QSingletonManager<QWorld> {
		public static World Active => Instance.World;
		public World World { get;private set; }
		protected override void Awake() {
			base.Awake();
			World = new World();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (var type in assembly.GetTypes()) {
					if (type.Is(typeof(QSystem)) && !type.IsAbstract) {
						Active.RegisterSystem(Activator.CreateInstance(type) as QSystem);
					}
				}
			}
		}
		private void Update() {
			Active.Update();
		}
	}
}
