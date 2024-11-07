using QTool.Inspector;
using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QTool.Graph {
	public abstract class QGraphAgent : MonoBehaviour, ISerializationCallbackReceiver {

		public bool HasGraph => Graphs.Count > 0;
		public List<QGraphRuntime> Graphs { get; private set; } = new List<QGraphRuntime>();
		[HideInInspector, SerializeField]
		private string blackBoardData;
		[QName]
		public Blackboard blackBoard { get; set; } = new Blackboard();
		public virtual float Time => UnityEngine.Time.time;
		public event Action onFresh;
		public QGraphRuntime StartGraph(QGraph graph, string key = null) {
			return StartGraph(graph?.GetRuntime(this));
		}
		public QGraphRuntime StartGraph(QGraphRuntime runtime, string key = null) {
			if (runtime != null) {
				Graphs.Add(runtime);
				runtime.blackboard.SetParent(blackBoard);
				runtime.Start(key);
				runtime.OnStop(flag => {
					Graphs.Remove(runtime);
					onFresh?.Invoke();
				});
				onFresh?.Invoke();
				return runtime;
			}
			return runtime;
		}
		public void OnBeforeSerialize() {
			blackBoardData = blackBoard.ToQData();
		}

		public void OnAfterDeserialize() {
			blackBoardData.ParseQData(blackBoard);
		}
#if UNITY_EDITOR
		public void SaveTest() {
			PlayerPrefs.SetString("SaveTest", Graphs[0].Save());
			GUIUtility.systemCopyBuffer = PlayerPrefs.GetString("SaveTest");
			Debug.LogError(PlayerPrefs.GetString("SaveTest"));
		}
		public void LoadTest() {
			Graphs[0].Load(PlayerPrefs.GetString("SaveTest"));
		}
		public void OpenGraph() {
			if(Graphs.Count > 0) {
				QGraphWindow.Open(Graphs[Graphs.Count - 1].Graph.Name);
			}
		}
#endif
		protected virtual void Update() {
			for (int i = 0; i < Graphs.Count; i++) {
				Graphs[i]?.Update();
			}
		}
		protected virtual void OnDestroy() {
			for (int i = Graphs.Count - 1; i >= 0; i--) {
				Graphs[i].Stop(false);
			}
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(QGraphAgent), true)]
	public class GraphAgentEditor : QInspectorEditor {
		public override VisualElement CreateInspectorGUI() {
			var visual = base.CreateInspectorGUI();
			if (visual == null) {
				visual = new VisualElement();
				visual.Add(serializedObject);
			}
			if (target is QGraphAgent agent) {
				var label = visual.AddLabel($"Graphs    {agent.Graphs.ToOneString("\n", graph => graph.Graph.Name)}");
				agent.onFresh += () => {
					label.text = $"Graphs    {agent.Graphs.ToOneString("\n", graph => graph.Graph.Name)}";
					agent.OpenGraph();
				};
				var back = visual.Add(nameof(QGraphAgent.blackBoard), agent.blackBoard, typeof(Blackboard), newValue => {
					target.SetDirty();
				});
				back.style.backgroundColor = Color.gray.Lerp(Color.black, 0.5f);
				back.style.SetBorder(Color.black, 1, 2);
			}
			return visual;
		}
	}
#endif
}