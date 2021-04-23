using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public static class TimeManager
    {
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
        }
        public static float RealDeltaTime
        {
            get
            {
                return Time.unscaledDeltaTime;
            }
        }
        public static float DeltaTime
        {
            get
            {
                return Time.deltaTime;
            }
        }

        static QDcitionary<object, float> timeScaleList = new QDcitionary<object, float>();
        public static void ChangeScale(object obj, float timeScale)
        {
            if (timeScale >= 0)
            {
                timeScaleList[obj] = timeScale;
                UpdateTimeScale();
            }
        }
        public static void RevertScale(object obj)
        {
            timeScaleList.RemoveKey(obj);
            UpdateTimeScale();
        }
    }
}