using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool
{
	
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(QEventTrigger))]
	public class QAnimator : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField]
		[QToolbar(nameof(Animations),pageSize =5)]
		[QOnChange(nameof(SampleAnimation))]
		private int clipIndex;
		private AnimationClip[] Animations => Animator.runtimeAnimatorController.animationClips;
		[SerializeField]
		[QName("动画进度")]
		[Range(0,1)]
		[QOnChange(nameof(SampleAnimation))]
		private float animationStep;
		[SerializeField]
		[QReadonly]
		private float time;
		List<string> ActionNames = new List<string>();

		private void SampleAnimation()
		{
			if (clipIndex < Animations.Length)
			{
				var clip = Animations[clipIndex];
				time = clip.length * animationStep;
				clip.SampleAnimation(gameObject, time);
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
		public List<StateGroup> StateGroups = new List<StateGroup>();
		public StateGroup GetCurrentStateGroup()
		{
			var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
			var nextState = Animator.GetNextAnimatorStateInfo(0);
			if (StateGroups == null) return null;
			foreach (var group in StateGroups)
			{
				if (group.StateNameHash.Contains(stateInfo.shortNameHash) || group.StateNameHash.Contains(stateInfo.shortNameHash))
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
}

