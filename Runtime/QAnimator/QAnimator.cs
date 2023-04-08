using QTool.Inspector;
using QTool.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QTool
{

	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(QEventTrigger))]
	public class QAnimator : MonoBehaviour, IQGUIEditor
	{
		Animator _animator;
		public Animator Animator
		{
			get
			{
				if (_animator == null)
				{
					_animator = GetComponent<Animator>();
				}
				return _animator;
			}
		}
		[SerializeField]
		private List<StateGroup> StateGroupList = new List<StateGroup>();
		public StateGroup GetCurrentStateGroup()
		{
			var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
			var nextState = Animator.GetNextAnimatorStateInfo(0);
			foreach (var group in StateGroupList)
			{
				if (group.StateNameHash.Contains(stateInfo.shortNameHash) || group.StateNameHash.Contains(nextState.shortNameHash))
				{
					return group;
				}
			}
			return null;
		}
		public Transform GetHumanBone(HumanBodyBones name)
		{
			return Animator.GetBoneTransform(name);
		}
		private AnimationClip[] Animations =>
#if UNITY_EDITOR
			Animator.runtimeAnimatorController.animationClips;
#else
			null;
#endif
		private AnimationClip Clip => Animations.Get(clipIndex);
		[QToggle("编辑动画")]
		public bool EditClip = false;
		[SerializeField]
		[QToolbar(nameof(Animations), pageSize = 5, visibleControl = nameof(EditClip))]
		private int clipIndex;

		private float ClipTim1e = 0;
		private void UpdateClip()
		{
			Clip?.SampleAnimation(gameObject, ClipTim1e);
		}

		private bool IsEventDrag = false;
		private List<AnimationEvent> EventList = new List<AnimationEvent>();
		private int GetTimeStep()
		{
			var max = Clip.length * Clip.frameRate;
			var size = 0;
			if (max > 15)
			{
				while (max / size > 30)
				{
					if (size == 0)
					{
						size = 15;
					}
					else
					{
						size *= 2;
					}
				}
			}
			return size;
		}
		public List<QFunctionInfo> GetAnimationEvents()
		{
			var infos = new List<QFunctionInfo>();
			var components = gameObject.GetComponents<Component>();
			foreach (var component in components)
			{
				infos.AddRange(QReflectionType.Get(component.GetType()).Functions);
			}
			infos.RemoveAll((func) => func.MethodInfo.GetCustomAttribute<QAnimatorTrackAttribute>() == null);
			return infos;
		}
		private void SaveEvents(string lastKey=null, string dragKey=null)
		{
			if (QGUI.DragKey==lastKey&&!dragKey.IsNull())
			{
				QGUI.DragKey = dragKey;
			}
#if UNITY_EDITOR
			UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
#endif
		}
		private AnimationEvent AddEvent(string name, float time = 0)
		{
			var newEvent = new AnimationEvent { functionName = name, time = time };
			EventList.Add(newEvent);
			if (!name.EndsWith("End"))
			{
				var func = EventFunctions.Get(name + "End", (obj) => obj.Name);
				if (func != null)
				{
					var endEvent= AddEvent(func.Name, time);
					SetTime(endEvent, time + 0.1f * Clip.length);
				}
				SaveEvents();
			}
			return newEvent;
		}
		private void RemoveEvent(params AnimationEvent[] eventDatas)
		{
			foreach (var item in eventDatas)
			{
				EventList.Remove(item);
			}
			SaveEvents();
		}
		List<QFunctionInfo> events = null;
		List<QFunctionInfo> EventFunctions => events ??= GetAnimationEvents();
		QDictionary<string, Rect> EventTracks = new QDictionary<string, Rect>();
		
	
		private Rect GetTrack(string name, QFunctionInfo qFunctionInfo = null)
		{
			name = name.SplitStartString("End");
			if (!EventTracks.ContainsKey(name))
			{
				var rect = QGUI.Box(Color.Lerp(Color.white, Color.clear, 0.6f));
				EventTracks[name] = rect;
				GUI.Label(rect, name, QGUI.CenterLable);
			}
			return EventTracks[name];
		}
		private void TimeTarck()
		{
			var timeRange = QGUI.Box(Color.Lerp(Color.white, Color.clear, 0.7f));
			var size = QGUI.Size / timeRange.width;
			var value = ClipTim1e / Clip.length;
			if (Color.white.DragBar(nameof(ClipTim1e), timeRange, ref value))
			{
				ClipTim1e = value * Clip.length;
				UpdateClip();
			}
			var max = Clip.length * Clip.frameRate;
			var timeStep = GetTimeStep();

			for (int i = 1; i <= max; i++)
			{
				var pos = (i / Clip.length) / Clip.frameRate;
				if (timeStep!=0&& i % timeStep != 0)
				{
					continue;
				}
				var rect = timeRange.HorizontalRect(pos, pos + 0.1f);
				GUI.Label(rect, i.ToString());
#if UNITY_EDITOR
				UnityEditor.Handles.color = Color.grey;
				UnityEditor.Handles.DrawLine(new Vector3(rect.x, rect.yMin + 4), new Vector3(rect.x, rect.yMax - 1));
#endif
			}
		}
		private AnimationEvent GetLeft(AnimationEvent cur)
		{
			var key = cur.functionName.SplitStartString("End");
			AnimationEvent left = null;
			foreach (var eventData in EventList)
			{
				if (eventData == cur) continue;
				if ((eventData.functionName == key || eventData.functionName.SplitStartString("End") == key) && eventData.time < cur.time)
				{
					if (left == null)
					{
						left = eventData;
					}
					else if (eventData.time > left.time)
					{
						left = eventData;
					}
				}
			}
			return left;
		}
		private AnimationEvent GetRight(AnimationEvent cur)
		{
			var key = cur.functionName.SplitStartString("End");
			AnimationEvent right = null;
			foreach (var eventData in EventList)
			{
				if (eventData == cur) continue;
				if ((eventData.functionName == key||eventData.functionName.SplitStartString("End")==key) && eventData.time > cur.time)
				{
					if (right == null)
					{
						right = eventData;
					}
					else if (eventData.time < right.time)
					{
						right = eventData;
					}
				}
			}
			return right;
		}
		private void SetTime(AnimationEvent eventData,float value)
		{
			var width= GetTrack(eventData.functionName).width;
			var start = GetLeft(eventData);
			var left =  start != null ? start.time/Clip.length+ 6/width : 0;
			var end = GetRight(eventData);
			var right = end != null ? end.time/Clip.length- 6 / width : 1;
			eventData.time = Mathf.Clamp(value, left, right) *Clip.length ;
		}
		public void OnQGUIEditor()
		{
			EventTracks.Clear();
			EventList.Clear();
			if (EditClip&&Clip != null)
			{
				EventList.RemoveAll((data) => ! (data.time>=0&&data.time<= Clip.length));
				EventList.AddRange(Clip.events);
				EventList.Sort((a, b) => string.Compare(a.functionName, b.functionName));
				TimeTarck();
				foreach (var eventFunc in EventFunctions)
				{
					GetTrack(eventFunc.Name,eventFunc);
				}
				foreach (var eventData in EventList)
				{
					var trackRect = GetTrack(eventData.functionName);
					var color = eventData.functionName.ToColor();
					var value = eventData.time / Clip.length;
					var key = eventData.GetKey();
					if (color.DragBar(key, trackRect, ref value,(menu)=> {
						menu.AddItem("删除事件".ToGUIContent(), false, () => RemoveEvent( eventData));
					}))
					{
						SetTime(eventData, value);
						ClipTim1e = eventData.time;
						UpdateClip();
						SaveEvents(key, eventData.GetKey());
					}
					if (eventData.functionName.EndsWith("End"))
					{
						var start = GetLeft(eventData);
						if (start != null)
						{
							var box = color.Box(trackRect, start.time / Clip.length, value);
							var newRect = box.Drag(trackRect, key + "_Range", (menu) => {
								menu.AddItem("删除事件".ToGUIContent(), false, () =>RemoveEvent(start, eventData));
							});
							if (newRect != trackRect && Event.current.type != EventType.Layout)
							{
								SetTime(start, (newRect.xMin - trackRect.xMin) / trackRect.width);
								SetTime(eventData, (newRect.xMax - trackRect.xMin) / trackRect.width);
								SaveEvents(key + "_Range",eventData.GetKey()+"_Range");
							}
						}
					}
				}
				foreach (var track in EventTracks)
				{
					var rect = track.Value;
					var mousePos = Event.current.mousePosition;
					var curValue = (mousePos.x - rect.xMin) / rect.width;
					rect.MouseMenu((menu) =>
					{
						menu.AddItem("添加事件".ToGUIContent(), false, () =>
						{
							AddEvent(track.Key, curValue * Clip.length);
						});
					});
				}
			}
		}
#if UNITY_EDITOR

		[QName("定位动画文件", nameof(EditClip))]
		public void SelectClip()
		{
			if (clipIndex < Animations.Length)
			{
				UnityEditor.Selection.activeObject = Animations[clipIndex];
			}
		}


		public UnityEditor.Animations.AnimatorController AnimatorController => (Animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController);

		private List<string> _states = null;
		public List<string> States
		{
			get
			{
				if (_states == null)
				{
					var stateMachines = new List<UnityEditor.Animations.AnimatorStateMachine>();
					stateMachines.Add(AnimatorController.layers[0].stateMachine);
					foreach (var cs in AnimatorController.layers[0].stateMachine.stateMachines)
					{
						stateMachines.Add(cs.stateMachine);
					}
					_states = new List<string>();
					foreach (var stateMachine in stateMachines)
					{
						foreach (var state in stateMachine.states)
						{
							_states.Add(state.state.name);
						}
					}
				}
				return _states;
			}
		}
#endif
		[System.Serializable]
		public class StateGroup
		{
			public string Key;
#if UNITY_EDITOR
			[QEnum(nameof(States))]
#endif
			public List<string> StateName = new List<string>();
			private List<int> _StateNameHash = null;
			public List<int> StateNameHash
			{
				get
				{
					if (_StateNameHash == null)
					{
						_StateNameHash = new List<int>();
						for (int i = 0; i < StateName.Count; i++)
						{
							_StateNameHash.Add(Animator.StringToHash(StateName[i]));
						}
					}
					return _StateNameHash;
				}
			}
		}
	}
	public static class QAnimatorTool
	{
		public static string GetKey(this AnimationEvent animationEvent)
		{
			if (animationEvent == null) return "";
			return animationEvent.functionName + animationEvent.time.ToString("f3");
		}
		public static void SetTrigger(this Animator animator,string name,Vector2 vector2)
		{
			animator.SetFloat(name + "X",vector2.x);
			animator.SetFloat(name + "Y", vector2.x);
			animator.SetTrigger(name);
		}
	}
	/// <summary>
	/// 更改显示的名字
	/// </summary>
	[AttributeUsage( AttributeTargets.Method)]
	public class QAnimatorTrackAttribute:Attribute
	{ 
		public QAnimatorTrackAttribute()
		{
		}
	}

}

