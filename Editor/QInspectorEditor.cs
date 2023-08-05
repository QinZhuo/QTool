using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using QTool.Reflection;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace QTool.Inspector
{
  


	[CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
	[CanEditMultipleObjects]
	public class QInspectorEditor : Editor
	{
		public QInspectorType typeInfo { get; private set; }
		protected virtual void OnEnable()
		{
			typeInfo = QInspectorType.Get(target.GetType());
			typeInfo.InvokeQInspectorState(target,QInspectorState.OnEnable);
			EditorApplication.playModeStateChanged += OnPlayMode;
		}

		protected virtual void OnDestroy()
		{
			typeInfo.InvokeQInspectorState(target,QInspectorState.OnDisable);
			EditorApplication.playModeStateChanged -= OnPlayMode;
		}
		//public override void OnInspectorGUI()
		//{
		//	if (target == null)
		//	{
		//		GUILayout.Label("脚本丢失");
		//		return;
		//	}
		//	serializedObject.Draw();
		//	typeInfo.DrawGUI(target);
		//}
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			root.Add(serializedObject);
			root.Add(typeInfo, target);
			return root;
		}
		private void OnSceneGUI()
		{
			MouseCheck();
		}
		public void MouseCheck()
		{
			if (typeInfo.mouseEventFunc.Count <= 0) return;
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			Event input = Event.current;
			if (!input.alt)
			{
				if (input.button == 0)
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(input.mousePosition);
					foreach (var kv in typeInfo.mouseEventFunc)
					{
						if (input.type == kv.Key.eventType)
						{
							if (input.isMouse)
							{
								if ((bool)kv.Value.Invoke(target, mouseRay))
								{
									input.Use();
								}
							}
							else
							{
								if ((bool)kv.Value.Invoke(target))
								{
									input.Use();
								}
							}
						}
					}
				}
			}
		}
	
		void OnPlayMode(PlayModeStateChange state)
		{
			QOnPlayModeAttribute.CurrentrState = (PlayModeState)(byte)state;
			foreach (var kv in typeInfo.playMode)
			{
				if ((byte)kv.Key.state == (byte)state)
				{
					kv.Value.Invoke(target);
				}
			}
		}

	}

}
