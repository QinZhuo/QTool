using QTool.Inspector;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System;
using System.Reflection;

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
		[QAnimatorTrack]
		public void Attack() {
			Debug.LogError("start");
		}
		[QAnimatorTrack]
		public void AttackEnd()
		{
			Debug.LogError("end");
		}
		[QAnimatorTrack]
		public void QEventTrigger(string eventName)
		{
			gameObject.InvokeEvent(eventName);
		}
		public Transform GetHumanBone(HumanBodyBones name)
		{
			return Animator.GetBoneTransform(name);
		}
		private AnimationClip[] Animations => Animator.runtimeAnimatorController.animationClips;
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
		private AnimationEvent CurEvent { get; set; }
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
		public void AddEvent(string name, float time = 0)
		{
			EventList.Add(new AnimationEvent { functionName = name, time = time });
			UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
			if (!name.EndsWith("End"))
			{
				var func = EventFunctions.Get(name + "End", (obj) => obj.Name);
				if (func != null)
				{
					AddEvent(func.Name, time + 0.5f);
				}
			}
		}
		List<QFunctionInfo> events = null;
		List<QFunctionInfo> EventFunctions => events ??= GetAnimationEvents();
		QDictionary<string, QEventTrack> EventTracks = new QDictionary<string, QEventTrack>((key) => new QEventTrack());
		class QEventTrack
		{
			public Rect Rect;
			public QAnimatorTrackType Type = QAnimatorTrackType.普通;
		}
		enum QAnimatorTrackType
		{
			普通,
			范围,
		}
		private Rect GetTrack(string name, QFunctionInfo qFunctionInfo = null)
		{
			name = name.SplitStartString("End");
			if (!EventTracks.ContainsKey(name))
			{
				if (qFunctionInfo!=null&&qFunctionInfo.ParamInfos.Length > 0)
				{
					EventTracks[name].Type = QAnimatorTrackType.范围;
				}
				var rect = QGUI.Box(Color.Lerp(Color.white, Color.clear, 0.6f));
				EventTracks[name].Rect = rect;
				GUI.Label(rect, name, QGUI.CenterLable);
				var mousePos = Event.current.mousePosition;
				var curValue = (mousePos.x - rect.xMin) / rect.width;
				rect.RightMenu((menu) =>
				{
					menu.AddItem("添加事件".ToGUIContent(), false, () =>
					{
						AddEvent(name, (int)(curValue * Clip.length * Clip.frameRate) / Clip.frameRate);
					});
					CurEvent = EventList.Find((obj) => Mathf.Abs(obj.time / Clip.length - curValue) < 0.01f);
					if (CurEvent != null)
					{
						menu.AddItem("删除事件".ToGUIContent(), false, () =>
						{
							EventList.Remove(CurEvent);
							UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
						});
					}
				},
				() =>
				{
					CurEvent = EventList.Find((obj) => Mathf.Abs(obj.time / Clip.length - curValue) < 0.01f);
				});
			}
			return EventTracks[name].Rect;
		}
		private void TimeTarck()
		{
			var timeRange = QGUI.Box(Color.Lerp(Color.white, Color.clear, 0.7f));
			var size = QGUI.Size / timeRange.width;
			var value = ClipTim1e / Clip.length;
			if (Color.white.DragBar(nameof(ClipTim1e), timeRange, ref value))
			{
				ClipTim1e = (int)(value * Clip.length * Clip.frameRate) / Clip.frameRate;
				UpdateClip();
			}
			var max = Clip.length * Clip.frameRate;
			var timeStep = GetTimeStep();
			for (int i = 1; i <= max; i++)
			{
				var pos = (i / Clip.length) / Clip.frameRate;
				if (i % timeStep != 0)
				{
					continue;
				}
				var rect = timeRange.HorizontalRect(pos, pos + 0.1f);
				GUI.Label(rect, i.ToString());
				UnityEditor.Handles.color = Color.grey;
				UnityEditor.Handles.DrawLine(new Vector3(rect.x, rect.yMin + 4), new Vector3(rect.x, rect.yMax - 1));
			}
		}
		private AnimationEvent GetStart(AnimationEvent end)
		{
			var key = end.functionName.SplitStartString("End");
			AnimationEvent start = null;
			foreach (var eventData in EventList)
			{
				if (eventData.functionName == key && eventData.time <= end.time)
				{
					if (start == null)
					{
						start = eventData;
					}
					else if (eventData.time > start.time)
					{
						start = eventData;
					}
				}
			}
			return start;
		}
		private void SetTime(AnimationEvent cur,float value)
		{
			var index= EventList.IndexOf(cur);
			var start = EventList.Get(index - 1);
			var left =  start != null && start.functionName == cur.functionName ? start.time/Clip.length+0.01f : 0;
			var end = EventList.Get(index + 1);
			var right = end != null && end.functionName == cur.functionName ? end.time/Clip.length-0.01f : 1;
			cur.time = Mathf.Clamp(value, left, right) *Clip.length ;
		}
		public void OnQGUIEditor()
		{
			EventTracks.Clear();
			EventList.Clear();
			if (EditClip&&Clip != null)
			{
				EventList.RemoveAll((data) => data.time > Clip.length);
				EventList.AddRange(Clip.events);
				EventList.Sort((a, b) => string.Compare(a.functionName, b.functionName));
				TimeTarck();
				foreach (var eventFunc in EventFunctions)
				{
					GetTrack(eventFunc.Name,eventFunc);
				}
				int i = 0;
				foreach (var eventData in EventList)
				{
					var trackRect = GetTrack(eventData.functionName);
					var color = eventData.functionName.ToColor();
					var value = eventData.time / Clip.length;
					//trackRect.HorizontalRect(range.Item1, range.Item2)
					if (color.DragBar(i++ + "_" + eventData.functionName, trackRect, ref value))
					{
						SetTime(eventData, value);
						ClipTim1e = eventData.time;
						UpdateClip();
						UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
					}
					if (eventData.functionName.EndsWith("End"))
					{
						var start= GetStart(eventData);
						if (start!=null)
						{
							var box= color.Box(trackRect, start.time / Clip.length, value);
							var newRect=box.Drag( trackRect, i++ + "_" + eventData.functionName + "Range");
							if (newRect != trackRect&&Event.current.type!= EventType.Layout)
							{
								SetTime(start, (newRect.xMin - trackRect.xMin) / trackRect.width);
								SetTime(eventData, (newRect.xMax - trackRect.xMin) / trackRect.width);
								UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
							}
						}
					}
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
			return animationEvent.functionName + animationEvent.time;
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

