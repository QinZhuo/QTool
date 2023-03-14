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
		public QAudioType Type => AudioType;
		public void Play(AudioClip clip)
		{
			switch (AudioType)
			{
				case QAudioType.BGM:
				case QAudioType.BGS:
				case QAudioType.ME:
					Audio.clip = clip;
					if (Audio.clip != null)
					{
						Audio.Play();
					}
					break;
				default:
					if (clip != null)
					{
						Audio.PlayOneShot(clip);
					}
					break;
			}
		}
		internal QAudioSource StartAudio { get; private set; }
		internal QAudioSource TransitionAudio { get; private set; }
		public float Length
		{
			get
			{
				var length = 0f;
				if (StartAudio != null)
				{
					length += StartAudio.Length;
				}
				if (Audio.clip != null)
				{
					length += Audio.clip.length;
				}
				return length;
			}
		}
		public float Time
		{
			get
			{
				var time = 0f;
				if (StartAudio != null)
				{
					if (StartAudio.IsPlaying)
					{
						time += StartAudio.Time;
					}
					else
					{
						time += StartAudio.Length;
					}
				}
				if (Audio.clip != null)
				{
					time += Audio.time;
				}
				return time;
			}
		}
		public bool IsPlaying
		{
			get
			{
				if (StartAudio != null&&StartAudio.IsPlaying)
				{
					return true;
				}
				return Audio.isPlaying;
			}
		}
		public void Play(QMusicSetting bgm,float transition=0)
		{
			switch (AudioType)
			{
				case QAudioType.BGM:
				case QAudioType.BGS:
				case QAudioType.ME:
					if(StartAudio?.Audio.clip == bgm.start)
					{
						if (StartAudio?.IsPlaying==true)
						{
							Audio.clip = bgm.music;
							break;
						}
						else if (Audio.isPlaying &&Audio.clip != null && bgm.music != null && Audio.clip.length == bgm.music.length&&(Audio.clip.name.Contains(bgm.music.name)|| bgm.music.name.Contains(Audio.clip.name)))
						{
							var time = Audio.time;
							Audio.clip = bgm.music;
							Audio.time = time;
							Audio.Play();
							break;
						}
					}
					Stop(transition);
					PlayStart(bgm.start);
					Audio.clip = bgm.music;
					StartCoroutine(StartVolume(transition));
					break;
				default:
					throw new System.Exception(AudioType+" 不支持播放"+nameof(QMusicSetting)+" "+bgm);
			}
		}
		private void PlayStart(AudioClip startClip)
		{
			if (StartAudio == null)
			{
				StartAudio = transform.GetChild(nameof(StartAudio), true).GetComponent<QAudioSource>(true);
				StartAudio.Audio.outputAudioMixerGroup = Audio.outputAudioMixerGroup;
				StartAudio.Audio.loop = false;
			}
			StartAudio.Play(startClip);
		}
		private IEnumerator StartVolume(float transition)
		{
			var startTime = 0f;
			while (startTime < transition)
			{
				SetVolume(Mathf.Lerp(0, 1, startTime / transition));
				yield return null;
				startTime +=UnityEngine.Time.unscaledDeltaTime;
			}
			SetVolume(1);
		}
		private IEnumerator DelayStop(float transition)
		{
			if (TransitionAudio == null)
			{
				TransitionAudio = transform.GetChild(nameof(TransitionAudio), true).GetComponent<QAudioSource>(true);
				TransitionAudio.Audio.outputAudioMixerGroup = Audio.outputAudioMixerGroup;
			}
			TransitionAudio.Copy(this);
			Stop();
			var stopTime = 0f;
			while (stopTime<transition)
			{
				TransitionAudio.SetVolume(Mathf.Lerp(1, 0, stopTime / transition));
				yield return null;
				stopTime +=UnityEngine.Time.unscaledDeltaTime;
			}
			TransitionAudio.SetVolume(0);
			TransitionAudio.Stop();
		}
		private void Copy(QAudioSource other)
		{
			SetType(other.AudioType);
			if (other.StartAudio)
			{
				PlayStart(other.StartAudio.Audio.clip);
				if (!other.StartAudio.IsPlaying)
				{
					other.StartAudio.Stop();
				}
				StartAudio.Audio.time = other.StartAudio.Audio.time;
			}
			if (other.Audio.isPlaying)
			{
				Play(other.Audio.clip);
			}
			else
			{
				Audio.clip = other.Audio.clip;
			}
			Audio.time = other.Audio.time;
		}
		public void Stop(float transition=0)
		{
			if (transition > 0)
			{
				StartCoroutine(DelayStop(transition));
			}
			else
			{
				Audio.time = 0;
				Audio.clip = null;
				Audio.Stop();
				StartAudio?.Stop();
			}
		}
		public void Pause()
		{
			StartAudio?.Pause();
			Audio.Pause();
		}
		public void UnPause()
		{
			StartAudio?.UnPause();
			Audio.UnPause();
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
			if (StartAudio != null&&!Audio.isPlaying&& Audio.clip!=null)
			{
				if (!StartAudio.Audio.isPlaying)
				{
					Audio.Play();
				}
			}
		}
		internal void SetVolume(float vlaue)
		{
			Audio.volume = vlaue;
			StartAudio?.SetVolume(vlaue);
		}
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

	}
}
