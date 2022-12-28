using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
namespace QTool
{
    public class QMusicManager : QToolManagerBase<QMusicManager>
    {
        
        static AudioSource previewAudio;
        protected override void Awake()
        {
            base.Awake();
            previewAudio = gameObject.AddComponent<AudioSource>();
        }
        const float intervel = 0.1f;
        public static void ParseMusic(AudioClip clip)
        {
            var key = "QMusicData_" + clip.name;
            if (PlayerPrefs.HasKey(key))
            {
                try
                {
                    AllData = PlayerPrefs.GetString(key).ParseQData<float[][]>();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("读取出错：" + e);
                }
                if (AllData != null)
                {
                    Debug.LogError("读取【" + clip.name + "】");
                    return;
                }
                PlayerPrefs.DeleteKey(key);
                ParseMusic(clip);
            }
            else
            {
                Instance.StartCoroutine(CorParseMusic(clip));
            }
        }
        static float curTime;
        static IEnumerator CorParseMusic(AudioClip clip)
        {
            previewAudio.clip = clip;
            previewAudio.Play();
            AllData = new float[(int)(clip.length / intervel)][];
            while (previewAudio.isPlaying)
            {
                AllData[(int)(previewAudio.time/intervel)] = GetData();
                curTime = previewAudio.time;
                yield return null;
            }
            var key = "QMusicData_" + clip.name;
            PlayerPrefs.SetString(key, AllData.ToQData());
            Debug.LogError(key);

        }
        static float[][] AllData;
        public static float[] GetParseData(float time)
        {
            return AllData.Get((int)(time/ intervel));
        }
        static float[] tempData = new float[2048];
        static float[] GetData(int samples = 2048)
        {

            previewAudio.GetSpectrumData(tempData, 0, FFTWindow.Rectangular);
            var datas = new float[2048];
            var max = 0f;
            for (int i = 0; i < tempData.Length; i++)
            {
                if (tempData[i] > tempData.Get(i - 1) && tempData[i] > tempData.Get(i + 1))
                {
                    datas[i] = tempData[i];
                    max = Mathf.Max(datas[i], max);
                }
                else
                {
                    datas[i] = 0;
                }
            }
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] < max * 0.4f)
                {
                    datas[i] = 0;
                }
            }
            return datas;
        }
    }
}
