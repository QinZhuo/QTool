using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using QTool;
using System.Threading.Tasks;

public class QBGMSetting:InstanceBehaviour<QBGMSetting>
{
    public AudioClip BGM;
    public AudioClip BGMIntro;
    public float BGMFadeInTime = 1f;
    public float BGMDelayTime = 0f;
    public AudioClip FightIntroMusic1;
    public AudioClip FightIntroMusic2;
    public AudioClip FightBGM_1X;
    public AudioClip FightBGM_2X;
    private static bool isSpeed2X =false;
    public static int list = 0;
    float time = 0;
    public static bool isStart = false;
    public bool isIntro = false;
    public float volume= 1f;
    public static bool Continue = true;
    public static bool newBGM = false;
//	public static async Task Stop(float time = 1, bool isAwait = false)
//    {
//        if (curAudio == null) return;
//		ChangeBGMVolume(0, time);

//		//curAudio.Stop(time);
//        curAudio.loop = true;
//        list = 2;
//        isStart = false;
//		if (isAwait)
//		{
//			if (await QTask.Wait(time, true).IsCancel())
//			{
//				return;
//			}
//		}
//	}

//    public static void Pause()
//    {
//        curAudio?.Pause();
//    }
//    public static void Play()
//    {
//        if(curAudio!=null&&!curAudio.isPlaying)
//        {
//            curAudio.Play();
//        }
//    }
//    public static AudioSource curAudio;
//    public static string GetAudioName()
//    {
//		if (curAudio != null && curAudio.clip!=null)
//        {
//			return curAudio.clip.name;
//		}
//		return "";
//    }
//    public static void PlayBGM(AudioClip bgm, float time = 1, float startTime = 0, float volume = 1f,bool isLoopTwo = false)
//    {
//        if (curAudio == null)
//        {
//            curAudio = MusicManager.PlayBgmMusic(bgm, time, true, startTime);
//			ChangeBGMVolume(volume, time, true);
//			//sif (curAudio == null) return;
//			if (curAudio != null)
//            {
//                DontDestroyOnLoad(curAudio.gameObject);
//               //Debug.LogError("当前更换的BGM:" + bgm.name);
//            }
//        }
//        else
//        {
//			//Debug.LogError("当前更换的BGM:" + bgm.name + "   list" + list);
//			//curAudio.volume = MusicManager.BGMVolume;
//			try 
//			{
//				if (curAudio.isPlaying)
//				{
//					curAudio.Stop();
//				}
//				curAudio.clip = bgm;
//				curAudio.time = 0;
//				curAudio.Play();
//				if (!isLoopTwo)
//				{
//					ChangeBGMVolume(volume, time, true);
//				}
//				else
//				{
//					curAudio.volume = volume;
//				}
//				curAudio.loop = true;
//			}
//            catch (System.Exception e)
//            {
//                Debug.LogError("播放 " + bgm.name + " 出错：\n" + e);
//            }
			
//        }
        
//        MusicAppView.OnChangeMusic(bgm.name);
//    }
//	public static void ChangeBGMVolume(float volume, float delay,bool isFadeIn = false)
//	{
//		if (curAudio == null) return;
//		if (isFadeIn) curAudio.volume = 0;
//		QTweenManager.Tween(() => curAudio.volume, (value) => curAudio.volume = value, volume, delay).Play();
//	}



//	protected  void Start()
//    {
//        isStart = false;
//		MusicManager.MusicSetting.SetFloat("全局高频滤波", 22000);
//		if (curAudio&& isIntro) curAudio.time = 0;

//		if (isIntro)
//        {
//            list = 0;
//            PlayBGM(FightIntroMusic1, 0, 0, volume);
//            //curAudio = await MusicManager.PlayBgmMusic(IntroMusic1.clip, 1, true, 0);
//        }
// /*       else
//        {
//            PlayBGM(new MusicSetting { musicAssetkey = BGMIntro?.name }, BGMFadeInTime, 0, 1);
//        }*/
//		QEventManager.Register("BGM二倍速", ChangeBGMTo2X);
//		QEventManager.Register<bool>("战斗暂停", ChangeBGMModel);
//		QEventManager.Register<WorldSetting>("World", OnWorldChange);

//	}

//	public void OnWorldChange(WorldSetting world)
//	{
//		MusicManager.MusicSetting.SetFloat("全局高频滤波", world.key=="水下" ? 700 : 22000);
//	}
//	public void ChangeBGMModel(bool isPause)
//	{
//		//MusicManager.MusicSetting.SetFloat("音乐Pitch", isPause ? 0.8f : 1f);
//		MusicManager.MusicSetting.SetFloat("音乐高频滤波", isPause ? 1000 : 22000);
//		MusicManager.MusicSetting.SetFloat("MusicResonance", isPause ? 1.5f : 1f);
//		MusicManager.MusicSetting.SetFloat("MusicDryLevel", isPause ? -1000 : 0);
//		MusicManager.MusicSetting.SetFloat("音乐房间效果", isPause ? -600 : -10000);
//		MusicManager.MusicSetting.SetFloat("MusicReverb", isPause ? -4000 : 0);
//		if (isPause)
//		{
//			_=MusicManager.BtnPauseMusic.PlaySFXMusic();
//		}
//		else
//		{
//			_ = MusicManager.BtnRecoveryMusic.PlaySFXMusic();
//		}

//	}

//	private void OnDestroy()
//	{
//		QEventManager.UnRegister("BGM二倍速", ChangeBGMTo2X);
//		QEventManager.UnRegister<bool>("战斗暂停", ChangeBGMModel);
//		QEventManager.UnRegister<WorldSetting>("World", OnWorldChange);
//	}


///*	async void Play(MusicSetting music, float time = 0.1f)
//    {
//        if (music == null)
//        {
//            return;
//        }
//        await MusicManager.PlayBgmMusic(await music.GetClip(), time, true);
//    }*/
//    public static void ChangeBGMTo2X()
//    {
//        if (Instance == null|| Instance.FightBGM_1X==null|| Instance.FightBGM_2X==null) return;
//		float curTime = curAudio.time;
//        if(curAudio.clip.name== Instance.FightBGM_1X.name)
//        {
//            curAudio.clip = Instance.FightBGM_2X;
//            curAudio.time = curTime;
//            isSpeed2X = true ;
//            curAudio.Play();
//        }
//        else if(curAudio.clip.name == Instance.FightBGM_2X.name)
//        {
//            curAudio.clip = Instance.FightBGM_1X;
//            curAudio.time = curTime;
//            isSpeed2X = false;
//            curAudio.Play();
//        }
//        else
//        {
//            isSpeed2X = !isSpeed2X;
//        }
//		_ = MusicManager.BtnAccelerateMusic.PlaySFXMusic();
//	}
//    QTimer nullTime = new QTimer(1);

//	public void FightBgmContro()
//	{
//		if (list == 0)
//		{
//			curAudio.loop = false;
//		}
//		if (!curAudio.isPlaying && list == 0)
//		{
//			list = 1;
//			PlayBGM(FightIntroMusic2, 0, 0, volume);
//			curAudio.loop = false;
//			//curAudio = await MusicManager.PlayBgmMusic(IntroMusic2.clip,0, false, 0);
//		}
//		if ((curAudio.time > curAudio.clip.length - 0.01f || !curAudio.isPlaying) && list == 1)
//		{
//			list = 2;
//			if(isSpeed2X)
//			{
//				PlayBGM(FightBGM_2X, 0, 0, volume, true);
//			}
//			else
//			{
//				PlayBGM(FightBGM_1X, 0, 0, volume, true);
//			}
//			//curAudio = await MusicManager.PlayBgmMusic(FightBGM.clip,0f, true,0f);
//		}
//	}
//	float musicVolume;
//	public void BeforeScenceChangeBGMChange(bool newBgm,bool continueBgm)
//	{
//		if (newBgm)
//		{
//			QBGMSetting.Stop(0.5f, true);
//			continueBgm = false;
//		}
//		if (!continueBgm)
//		{
//			list = 2;
//		}
		
//		MusicManager.MusicSetting.GetFloat("音效", out musicVolume);
//		MusicManager.ChangeVolume("音效", 0f);
//	}

//	public async Task AfterScenceChangeBGMChange(bool newBgm, bool continueBgm,string lastScene)
//	{
//		MusicManager.MusicSetting.SetFloat("音效", musicVolume);
//        if (QBGMSetting.Instance!=null&&QBGMSetting.Instance.BGM != null && QBGMSetting.Instance.BGM.name == QBGMSetting.GetAudioName()&& lastScene != LoadingMaskView.CurScene)
//        {
//            continueBgm = true;
//            newBgm = false;
//        }
//        if (!continueBgm && !QBGMSetting.Instance.isIntro)
//        {
//            await QBGMSetting.Stop(0.5f, true);
//		}
//        if (newBgm)
//        {
//            QBGMSetting.newBGM = true;
//            QBGMSetting.list = 0;
//		}
//	}

//	bool canBgmIntro = true;
//	public void NormalBgmContro()
//	{
//		if (!newBGM)
//		{
//			if (curAudio == null && list == 0 && time >= BGMDelayTime && BGMIntro != null)
//			{
//				if (BGMIntro != null && canBgmIntro)
//				{
//					PlayBGM(BGMIntro, BGMFadeInTime, 0, volume);
//					canBgmIntro = false;
//				}
//			}
//		}
//		else
//		{
//			if (list == 0 && BGMIntro != null )
//			{
				
//				if (BGMIntro != null && canBgmIntro)
//				{
//					PlayBGM(BGMIntro, BGMFadeInTime, 0, volume);
//					canBgmIntro = false;
//				}
//			}
//			else if (list == 0 && BGMIntro == null && BGM != null)
//			{
//				list = 2;
//				PlayBGM(BGM, 1, 0, volume, true);
//				curAudio.loop = true;
//			}
//		}
//		if (curAudio != null && list == 0 && curAudio.isPlaying)
//		{
//			list = 1;
//			canBgmIntro = true;
//			curAudio.loop = false;
//		}
//		if (curAudio != null && !curAudio.isPlaying && list == 1)
//		{
//			curAudio.loop = true;
//			list = 2;
//			PlayBGM(BGM, 1, 0, volume, true);
//		}
//	}



//    protected void Update()
//    {
//        time += Time.deltaTime;
//        if (isStart && isIntro)
//        {
//			FightBgmContro();
//		}
//        if (!isIntro)
//        {
//			NormalBgmContro();
//		}
//    }

}
