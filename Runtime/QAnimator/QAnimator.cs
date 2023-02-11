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
#if UNITY_EDITOR
		[QToggle("预览动画")]
		public bool PreviewClip;
		[SerializeField]
		[QToolbar(nameof(Animations), pageSize = 5, visibleControl = nameof(PreviewClip))]
		[QOnChange(nameof(UpdateAll))]
		private int clipIndex;
		private AnimationClip[] Animations => Animator.runtimeAnimatorController.animationClips;
		[SerializeField]
		[QName("动画进度", nameof(PreviewClip))]
		[Range(0, 1)]
		[QOnChange(nameof(UpdateClip))]
		private float animationStep;
		[SerializeField]
		[QReadonly]
		[QName("时间", nameof(PreviewClip))]
		private float time;
		private List<ClipEventData> Events { get; set; }= new List<ClipEventData>();
		[QToolbar(nameof(Events),visibleControl = nameof(PreviewClip),name ="事件")]
		public int eventIndex;
		struct ClipEventData
		{
			public string name;
			public float time;
			public override string ToString()
			{
				return name + " " + time;
			}
		}
		private void UpdateClip()
		{
			if (clipIndex < Animations.Length)
			{
				var clip = Animations[clipIndex];
				time = clip.length * animationStep;
				clip.SampleAnimation(gameObject, time);
			}
		}
		private void UpdateAll()
		{
			UpdateClip();
			if (clipIndex < Animations.Length)
			{
				var clip = Animations[clipIndex];
				Events.Clear();
				foreach (var eventData in clip.events)
				{
					Events.Add(new ClipEventData { name = eventData.stringParameter, time = eventData.time });
				}
			}
		}
		[QName("定位动画文件", nameof(PreviewClip))]
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

}

