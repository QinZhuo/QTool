using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System;

namespace QTool
{
    public static class QTime
    {
		public static DateTime ParseTimesTamp(long timestamp,int offset=7)
		{
			var baseValue = 1;
			for (int i = 0; i < offset; i++)
			{
				baseValue *= 10;
			}
			return new DateTime(1970, 1, 1,8,0,0).Add(new TimeSpan(timestamp* baseValue));
		}
        public static void Clear()
        {
            timeScaleList.Clear();
            UpdateTimeScale();
        }
        public static event System.Action<float> OnScaleChange;

        private static void UpdateTimeScale()
        {
            var value = 1f;
            foreach (var kv in timeScaleList)
            {
                value *= kv.Value;
            }
            Time.timeScale = value;
            OnScaleChange?.Invoke(value);
			QDebug.Log("更改时间速度 " + Time.timeScale + " :\n" + timeScaleList.ToOneString());
		}
		public static float GetTimeScale(object obj)
		{
			if (timeScaleList.ContainsKey(obj))
			{
				return timeScaleList[obj];
			}
			else
			{
				return 1;
			}
		}

        static QDictionary<object, float> timeScaleList = new QDictionary<object, float>();
        public static void ChangeScale(object obj, float timeScale)
        {
			if (timeScaleList.ContainsKey(obj))
			{
				if (timeScaleList[obj] == timeScale) return;
			}
			else
			{
				if (timeScale == 1) return;
			}
			if (timeScale==1)
            {
				timeScaleList.RemoveKey(obj);
			}
			else
			{
				timeScaleList[obj] = timeScale;
			}
			UpdateTimeScale();
		}
        public static void RevertScale(object obj)
        {
			if (timeScaleList.ContainsKey(obj))
			{
				timeScaleList.RemoveKey(obj);
				UpdateTimeScale();
			}
        }
    }
	public class QTimer
	{
        public float Time { get; protected set; }
        public float CurTime { get; protected set; }
		public bool IsOver => CurTime >= Time;
		public QTimer()
		{

		}
		public QTimer(float Time, bool startOver = false)
		{
			Reset(Time, startOver);
		}
		public void Clear()
		{
			CurTime = 0;
		}
        public void Over()
        {
            CurTime = Time;
        }
        public void SetCurTime(float curTime)
        {
            CurTime = curTime;
        }
        public void Reset(float time, bool startOver = false)
        {
            this.Time = time;
            Clear();
            if (startOver) Over();
        }
		/// <summary>
		/// 更新deltaTime并检测计时是否结束
		/// </summary>
		/// <param name="deltaTime">更新数值</param>
		/// <param name="autoClear">计时成功是否清空</param>
		/// <returns>计时时否成功</returns>
		public bool Check(float deltaTime = 0, bool autoClear = true)
		{
			CurTime = CurTime + deltaTime;
			var timeOffset = CurTime - Time;
			if (timeOffset >= 0)
			{
				if (autoClear)
				{
					CurTime = timeOffset;
				}
				else
				{
					CurTime = Time;
				}
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
