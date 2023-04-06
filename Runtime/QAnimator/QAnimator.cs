using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
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
		[QOnChange(nameof(UpdateAll))]
		private int clipIndex;
		[QName("时间", nameof(EditClip))]
		[QOnChange(nameof(UpdateClip))]
		private float time;
		private void UpdateClip()
		{
			Clip?.SampleAnimation(gameObject, time);
		}
		private void UpdateAll()
		{
			UpdateClip();
		}

		public void OnQGUIEditor()
		{
			if (Clip != null)
			{
				for (int i = 0; i < Clip.events.Length; i++)
				{
					var eventData = Clip.events[i];
					var color = eventData.functionName.ToColor();
					var pos = eventData.time / Clip.length;
					var rect= QGUI.Box(Color.Lerp(Color.white,Color.black,0.2f));
					var selectRect= QGUI.Box(color, rect, pos, pos+5/ rect.width);
					GUI.Label(rect, eventData.functionName,QGUI.CenterLable);
					if (Event.current.type== EventType.MouseDrag)
					{
						if (selectRect.Contains(Event.current.mousePosition))
						{
							var newTime= (Event.current.mousePosition.x - rect.x) / rect.width * Clip.length; ;
							if (eventData.time!= newTime)
							{
								eventData.time = newTime;
								Debug.LogError(eventData.time);
								//UnityEditor.AnimationUtility.SetAnimationEvents
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
		public static void SetTrigger(this Animator animator,string name,Vector2 vector2)
		{
			animator.SetFloat(name + "X",vector2.x);
			animator.SetFloat(name + "Y", vector2.x);
			animator.SetTrigger(name);
		}
	}

	
}

