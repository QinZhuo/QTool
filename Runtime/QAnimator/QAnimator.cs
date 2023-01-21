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
#if UNITY_EDITOR
		public UnityEditor.Animations.AnimatorController AnimatorController => (Animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController);
		
		private string[] _states = null;
		public string[] States
		{
			get
			{
				if (_states == null || _states.Length == 0)
				{
					var stateInfos = AnimatorController.layers[0].stateMachine.states;
					_states = new string[stateInfos.Length];
					for (int i = 0; i < stateInfos.Length; i++)
					{
						_states[i] = stateInfos[i].state.name;
					}
				}
				return _states;
			}
		}
#endif
		public List<StateGroup> StateGroups = new List<StateGroup>();
		public StateGroup CurrentStateGroup { get; private set; }
		public string CurrentState => CurrentStateGroup == null ? "null" : CurrentStateGroup.Key;
		private void FixedUpdate()
		{
			if(Animator.updateMode== AnimatorUpdateMode.Normal)
			{
				UpdateStateGroup();
			}
		}
		public void UpdateStateGroup()
		{
			var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
			foreach (var group in StateGroups)
			{
				if (group.StateNameHash.Contains(stateInfo.shortNameHash))
				{
					CurrentStateGroup = group;
					return;
				}
			}
			CurrentStateGroup = null;
		}
		private void SampleAnimation()
		{
			if (clipIndex < Animations.Length)
			{
				var clip = Animations[clipIndex];
				time = clip.length * animationStep;
				clip.SampleAnimation(gameObject, time);
			}
		}
#endif
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
			List<int> _StateNameHash = null;
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

