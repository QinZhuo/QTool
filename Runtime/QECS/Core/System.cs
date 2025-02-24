using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace QTool.ECS {
	public interface ISystem {
		public World World { get; set; }
		public void Start();
		public void Update();
	}
	public abstract class QuerySystem : ISystem {
		public string name { get; set; }
		public QuerySystem() {
			name=GetType().Name;
		}
		public override string ToString() {
			return name;
		}
		[QName]
		public IEnumerable<EntityType> EntityTypes { get; protected set; }
		public World World { get; set; }
		public abstract void Start();
		public abstract void Update();
	}
	public abstract class QuerySystem<T1> : QuerySystem where T1 : IComponent {
		public override void Start() {
			EntityTypes = World.GetEntityTypes(new Type[] { typeof(T1) });
		}
		public sealed override void Update() {
			foreach (var type in EntityTypes) {
				foreach (var entity in type.Entities) {
					Query(ref type.GetComponent<T1>(entity));
				}
			}
		}
		public abstract void Query(ref T1 comp1);
	}
	public abstract class QuerySystem<T1, T2> : QuerySystem where T1 : IComponent where T2 : IComponent {
		public override void Start() {
			EntityTypes = World.GetEntityTypes(new Type[] { typeof(T1), typeof(T2) });
		}
		public sealed override void Update() {
			foreach (var type in EntityTypes) {
				foreach (var entity in type.Entities) {
					Query(ref type.GetComponent<T1>(entity), ref type.GetComponent<T2>(entity));
				}
			}
		}
		public abstract void Query(ref T1 comp1, ref T2 comp2);
	}
	public abstract class QuerySystem<T1, T2, T3> : QuerySystem
	where T1 : IComponent
	where T2 : IComponent
	where T3 : IComponent {

		public override void Start() {
			EntityTypes = World.GetEntityTypes(new Type[] {
			typeof(T1), typeof(T2), typeof(T3)
		});
		}

		public sealed override void Update() {
			foreach (var type in EntityTypes) {
				foreach (var entity in type.Entities) {
					Query(
						ref type.GetComponent<T1>(entity),
						ref type.GetComponent<T2>(entity),
						ref type.GetComponent<T3>(entity)
					);
				}
			}
		}

		public abstract void Query(ref T1 comp1, ref T2 comp2, ref T3 comp3);
	}

	public abstract class QuerySystem<T1, T2, T3, T4> : QuerySystem
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent {

		public override void Start() {
			EntityTypes = World.GetEntityTypes(new Type[] {
			typeof(T1), typeof(T2), typeof(T3), typeof(T4)
		});
		}

		public sealed override void Update() {
			foreach (var type in EntityTypes) {
				foreach (var entity in type.Entities) {
					Query(
						ref type.GetComponent<T1>(entity),
						ref type.GetComponent<T2>(entity),
						ref type.GetComponent<T3>(entity),
						ref type.GetComponent<T4>(entity)
					);
				}
			}
		}

		public abstract void Query(ref T1 comp1, ref T2 comp2, ref T3 comp3, ref T4 comp4);
	}
	public abstract class QueryEntitySystem<T1> : QuerySystem where T1 : struct, ITuple {
		public override void Start() {
			EntityTypes = World.GetEntityTypes<T1>();
		}
		public override void Update() {
			foreach (var type in EntityTypes) {
				foreach (var entity in type.Entities) {
					var comps = type.GetComponents<T1>(entity);
					Query(ref comps);
					type.SetComponents(entity, comps);
				}
			}
		}
		public abstract void Query(ref T1 entity);
	}
	public abstract class QueryEntitySystem<T1, T2> : QueryEntitySystem<T1> where T1 : struct, ITuple where T2 : struct, ITuple {
		[QName]
		public IEnumerable<EntityType> EntityTypes2 { get; protected set; }
		public override void Start() {
			EntityTypes = World.GetEntityTypes<T1>();
			EntityTypes2 = World.GetEntityTypes<T2>();
		}
		public override void Update() {
			foreach (var type1 in EntityTypes) {
				foreach (var entity1 in type1.Entities) {
					var comps1 = type1.GetComponents<T1>(entity1);
					Query(ref comps1);
					foreach (var type2 in EntityTypes2) {
						foreach (var entity2 in type2.Entities) {
							var comps2 = type2.GetComponents<T2>(entity2);
							Query(ref comps1, ref comps2);
							type2.SetComponents(entity2, comps2);
						}
					}
					type1.SetComponents(entity1, comps1);
				}
			}
		}
		public abstract void Query(ref T1 entity1, ref T2 entity2);
	}

}

