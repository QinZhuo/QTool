using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(AudioSource))]
	public class QAudioSource : MonoBehaviour
	{
		public AudioSource Audio { get; private set; }
		[SerializeField]
		private QAudioType AudioType = QAudioType.BGS;
		internal void SetType(QAudioType type)
		{
			AudioType = type;
			switch (AudioType)
			{
				case QAudioType.BGM:
					Audio.loop = true;
					break;
				case QAudioType.BGS:
					Audio.loop = true;
					break;
				case QAudioType.ME:
					Audio.loop = false;
					break;
				default:
					Audio.loop = false;
					break;
			}
		}
		public void Play(AudioClip clip)
		{
			switch (AudioType)
			{
				case QAudioType.BGM:
				case QAudioType.BGS:
				case QAudioType.ME:
					Audio.clip = clip;
					Audio.Play();
					break;
				default:
					Audio.PlayOneShot(clip);
					break;
			}
		}
		public void Play(AudioClip loopClip,bool loop=true)
		{
			Audio.loop = loop;
			Audio.Play();
		}
		public void Awake()
		{
			name = this.QName();
			Audio =this.GetComponent<AudioSource>(true);
			Audio.playOnAwake = false;
			SetType(AudioType);
		}
		private void Update()
		{

		}

		
	}
}
