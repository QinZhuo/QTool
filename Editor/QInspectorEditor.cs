using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using QTool.Reflection;
using System.Threading.Tasks;

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
			InvokeQInspectorState(QInspectorState.OnEnable);
			EditorApplication.playModeStateChanged += OnPlayMode;
		}

		protected virtual void OnDestroy()
		{
			InvokeQInspectorState(QInspectorState.OnDisable);
			EditorApplication.playModeStateChanged -= OnPlayMode;
		}
		public override void OnInspectorGUI()
		{
			if (target == null)
			{
				GUILayout.Label("脚本丢失");
				return;
			}
			serializedObject.Draw();
			DrawButton();
		}
		private void OnSceneGUI()
		{
			MouseCheck();
		}
	

		public void DrawButton()
		{
			foreach (var kv in typeInfo.buttonFunc)
			{
				var att = kv.Value;

				if (att.Active(target))
				{
					var name = att.name.IsNull() ? kv.Key.Name : att.name;

					if (att is QSelectObjectButtonAttribute)
					{
						if (GUILayout.Button(name))
						{
							EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "", name.GetHashCode());

						}
						if (Event.current.commandName == "ObjectSelectorClosed")
						{
							if (EditorGUIUtility.GetObjectPickerControlID() == name.GetHashCode())
							{
								var obj = EditorGUIUtility.GetObjectPickerObject();
								if (obj != null)
								{
									kv.Key.Invoke(target, obj);
								}

							}
						}
					}
					else
					{
						if (GUILayout.Button(name))
						{
							kv.Key.Invoke(target);
						}
					}

				}
			}
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
		void InvokeQInspectorState(QInspectorState state)
		{
			foreach (var kv in typeInfo.inspectorState)
			{
				if (kv.Key.state == state)
				{
					var result = kv.Value.Invoke(target);
					if (result is Task task)
					{
						_ = task.Run();
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
