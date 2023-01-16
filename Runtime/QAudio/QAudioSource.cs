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
		internal QBackgroundMusic? CurBGM { get; private set; }
		internal QAudioSource StartAudio { get; private set; }
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
			CurBGM = null;
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
		public void Play(QBackgroundMusic bgm)
		{
			switch (AudioType)
			{
				case QAudioType.BGM:
				case QAudioType.BGS:
				case QAudioType.ME:
					if (CurBGM == null || CurBGM.Value.start != bgm.start)
					{
						PlayStart(bgm.start);
					}
					Audio.clip = bgm.music;
					break;
				default:
					throw new System.Exception(AudioType+" 不支持播放 "+bgm);
			}
			CurBGM = bgm;
		}
		private void PlayStart(AudioClip startClip)
		{
			if (startClip == null) return;
			if (StartAudio == null)
			{
				StartAudio = transform.GetChild(nameof(StartAudio), true).GetComponent<QAudioSource>(true);
				StartAudio.Audio.playOnAwake = false;
				StartAudio.Audio.loop = false;
			}
			StartAudio.Play(startClip);
		}
		public void Awake()
		{
			name = this.QName();
			Audio =this.GetComponent<AudioSource>(true);
			Audio.playOnAwake = false;
			SetType(AudioType);
		}
		[ExecuteInEditMode]
		private void Update()
		{
			if (StartAudio != null&&!Audio.isPlaying&&Audio.clip!=null)
			{
				if (!StartAudio.Audio.isPlaying)
				{
					Audio.Play();
				}
			}
		}

		
	}
}
