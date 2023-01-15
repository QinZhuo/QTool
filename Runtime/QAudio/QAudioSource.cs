using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(AudioSource))]
	public class QAudioSource : MonoBehaviour
	{
		private new AudioSource audio;
		public AudioClip loopClip { get; private set; }
		public void Play(AudioClip loopClip,bool loop=true)
		{
			audio.loop = loop;
			audio.Play();
		}
		public void Awake()
		{
			audio = GetComponent<AudioSource>();
		}
		private void Update()
		{
		}

	}
}
