using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;


#if NODECANVAS
using NodeCanvas.Framework;
using ParadoxNotion.Design;
#endif
using QTool.Graph;
namespace QTool {

	[Category("基础")]
	public abstract class BaseNode : QNodeRuntime {

	}
	public abstract class StartNode : BaseNode {

	}
	[QName("OnStart")]
	public class OnStartEvent : StartNode {
		public override string Description => "起点";
		protected override void OnStart() {
			End(true);
		}
	}
	[QName("OnUpdate")]
	public class OnUpdateEvent : StartNode,INodeUpdate {
		protected override void OnStart() {
		}
		public void OnUpdate() {
			RunPort(QFlowKey.NextPort);
		}
	}
	[QEndNode, QOldName("Finish")]
	public class End : BaseNode {
		[QInputPort]
		public bool success = true;
		protected override void OnStart() {
			Graph.Stop(success);
		}
	}
	//public class GetVariable : BaseNode {
	//	public string key;
	//	[QOutputPort(true)]
	//	public object value;
	//	public override string Description => $"获取变量 {key}";
	//	public override void OnStart() {
	//		value = Graph.GetVar<object>(key);
	//		End(true);
	//	}
	//}
	
	public class GetVariable<T> : BaseNode {
		public string key;
		[QOutputPort(true)]
		public T value;
		public override string Description => $"获取变量 {key}";
		protected override void OnStart() {
			value = Graph.GetVar<T>(key);
			End(true);
		}
	}
	//public class SetVariable : BaseNode {
	//	public string key;
	//	public object value;
	//	public override void OnStart() {
	//		Graph.SetVar(key, value);
	//		End(true);
	//	}
	//}
	public class SetVariable<T> : BaseNode {
		public string key; 
		[QInputPort]
		public T value;
		protected override void OnStart() {
			Graph.SetVar(key, value);
			End(true);
		}
	}
	public class SwitchInt : BaseNode {
		[QInputPort]
		public int value;
		[QOutputPort, QFlowPort]
		public int[] values;
		[QOutputPort]
		public QFlow Default;
		protected override void OnStart() {
			for (int i = 0; i < values.Length; i++) {
				var cur = values[i];
				if (Equals(value, cur)) {
					SetNetFlowPort(nameof(values), i);
					End(true);
					return;
				}
			}
			SetNetFlowPort(nameof(Default));
			End(true);
		}
	}
	public class Subgraph : BaseNode, INodeUpdate {
		[QObject]
		public string graphAsset;
		protected override void OnStart() {
			var runtime = Graph.SubgraphRuntimes[Node.Key];
			runtime.OnStop(End);
			runtime.Start();
		}
		public void OnUpdate() {
			Graph.SubgraphRuntimes[Node.Key].Update();
		}
	}
	public class SelectOnBool<T> : BaseNode {
		[QInputPort]
		public bool condition;
		public T True;
		public T False;
		[QOutputPort]
		public T value;
		protected override void OnStart() {
			value = condition ? True : False;
		}
	}
	public class MissingNode : BaseNode {
		public string data;
		protected override void OnStart() {
			End(true);
		}
	}
	public class LogText : BaseNode {
		[QInputPort]
		public object text;
		protected override void OnStart() {
			Debug.Log(text);
			End(true);
		}
	}
	public class ReflectedMethodNode : BaseNode {
		internal object obj;
		internal object result;
		internal object[] methodParams;
		protected override void OnStart() {
			if (Node.Info.method != null) {
				result = Node.Info.method.Invoke(obj, methodParams);
				End();
			}
		}
	}
#if !NODECANVAS
	///<summary>Use for categorization</summary>
	[AttributeUsage(AttributeTargets.All)]
	public class CategoryAttribute : Attribute {
		readonly public string category;
		public CategoryAttribute(string category) {
			this.category = category;
		}
	}

	///<summary>Use to give a description</summary>
	[AttributeUsage(AttributeTargets.All)]
	public class DescriptionAttribute : Attribute {
		readonly public string description;
		public DescriptionAttribute(string description) {
			this.description = description;
		}
	}
#endif
}
