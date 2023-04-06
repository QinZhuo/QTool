using QTool.Inspector;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(QEventTrigger))]
	public class QAnimator : MonoBehaviour,IQGUIEditor
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
		//[QOnChange(nameof(UpdateClip))]
		private int clipIndex;
		
		private float ClipTim1e=0;
		private void UpdateClip()
		{
			Clip?.SampleAnimation(gameObject, ClipTim1e);
		}
		
		private bool IsEventDrag=false;
		private List<AnimationEvent> EventList = new List<AnimationEvent>();
		public void OnQGUIEditor()
		{
			EventList.Clear();
			if (EditClip&&Clip != null)
			{
				EventList.AddRange(Clip.events);
				var timeRange = QGUI.Box(Color.Lerp(Color.white, Color.clear, 0.6f));
				var max= Clip.length * Clip.frameRate;
				for (int i = 1; i <= max; i++)
				{
					var pos =( i / Clip.length)/ Clip.frameRate;
					if (max > 30 && i % 15 != 0)
					{
						continue;
					}
					var rect = timeRange.HorizontalRect(pos, pos + 0.1f);
					GUI.Label(rect, i.ToString());
					UnityEditor.Handles.color = Color.grey;
					UnityEditor.Handles.DrawLine(new Vector3(rect.x, rect.yMin+4), new Vector3(rect.x, rect.yMax-1));

				}
				var size = QGUI.Size / timeRange.width ;
				var left = ClipTim1e / Clip.length;
				var right = left + size;
				if (Color.white.DragBox(nameof(ClipTim1e), timeRange,ref left,ref right)){
					ClipTim1e = (left+size/2)*Clip.length;
					UpdateClip();
				}
				for (int i = 0; i < EventList.Count; i++)
				{
					var eventData = EventList[i];
					var color = eventData.functionName.ToColor();
					left = eventData.time / Clip.length;
					right = left + size;
					var rangeRect= QGUI.Box(Color.Lerp(Color.white,Color.clear,0.6f));
					GUI.Label(rangeRect, eventData.functionName, QGUI.CenterLable);
					if (color.DragBox(i + "_"+Clip.name +"_"+ eventData.functionName, rangeRect,ref left, ref right))
					{
						EventList[i].time = (left + size / 2) * Clip.length;
						UnityEditor.AnimationUtility.SetAnimationEvents(Clip, EventList.ToArray());
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
		public static void SetTrigger(this Animator animator,string name,Vector2 vector2)
		{
			animator.SetFloat(name + "X",vector2.x);
			animator.SetFloat(name + "Y", vector2.x);
			animator.SetTrigger(name);
		}
	}

	
}

