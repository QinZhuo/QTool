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
		private AnimationClip[] Animations => GetComponent<Animator>().runtimeAnimatorController.animationClips;
		[SerializeField]
		[QName("动画进度")]
		[Range(0,1)]
		[QOnChange(nameof(SampleAnimation))]
		private float animationStep;
		[SerializeField]
		[QReadonly]
		private float time;
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
	}
}

