using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool {
	public interface IView<T> {
		void Fresh(T data);
	}
	public static class QViewTool {
		public static void SetData<T>(this GameObject view,T data) {
			view.GetComponent<IView<T>>().Fresh(data);
		}
		public static void SetDatas<T>(QObjectList objectList, IList<T> data) {
			foreach (var item in data) {
				var view = objectList[item.GetHashCode()];
				view.SetData(item);
			}
		}
	}

}

