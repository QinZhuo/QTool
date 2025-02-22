using System;
using UnityEngine;

namespace QTool.ECS {
	public interface IComponent { }
	internal interface IComponentArray {
		public void Resize(int size);
		public void Remove(int index, int end);
		public void SetObject(int index, object obj);
		public object GetObject(int index);
		public void CopyTo(int index, IComponentArray targetArrary, int targetIndex);
	}
	internal sealed class ComponentArray<T> : IComponentArray where T : IComponent {
		public T[] components = new T[EntityType.INIT_SIZE];
		public void Resize(int size) {
			Array.Resize(ref components, size);
		}
		public void Remove(int index, int end) {
			components[index] = components[end];
		}
		public void SetObject(int index, object obj) {
			components[index] = (T)obj;
		}
		public object GetObject(int index) {
			return components[index];
		}
		public void Set(int index, in T comp) {
			components[index] = comp;
		}
		public ref T Get(int index) {
			return ref components[index];
		}

		public void CopyTo(int index, IComponentArray targetArrary, int targetIndex) {
			if (targetArrary is ComponentArray<T> target) {
				target.Set(targetIndex, in Get(index));
			}
			else {
				throw new ArgumentException("Invalid targetArrary");
			}
		}
	}
}
