using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System.IO;
using System;

namespace QTool
{
	public struct QValue
	{

		private float a;
		private float b;

		public QValue(float value)
		{
			a = value * 0.5f;
			b = value * 0.5f;
		}
		public float Value
		{
			get
			{
				return a + b;
			}
			set
			{
				if (value == Value) return;
				a = value * UnityEngine.Random.Range(0.2f, 0.8f);
				b = value - a;
			}
		}


		public static implicit operator QValue(float value)
		{
			return new QValue(value);
		}
		public static implicit operator float(QValue value)
		{
			return value.Value;
		}
		public override string ToString()
		{
			return Value.ToString();
		}
	}

	public class QRuntimeValue
	{
		public QRuntimeValue()
		{
		}
		public QRuntimeValue(float value)
		{
			OriginValue = value;
		}
		public void Reset(float value)
		{
			OriginValue = value;
			OffsetValue = 0;
			PercentValue = 1;
		}
		public QValue OriginValue { get; private set; } = 0f;
		private QValue _OffsetValue = 0;
		public QValue OffsetValue {
			get => _OffsetValue;
			set
			{
				if (_OffsetValue != value)
				{
					_OffsetValue = value;
					FreshValue();
				}
			}
		}
		private QValue _PercentValue = 1;
		public QValue PercentValue
		{
			get => _PercentValue;
			set
			{
				if (_PercentValue != value)
				{
					_PercentValue = value;
					FreshValue();
				}
			}
		}
		public QValue Value { get; private set; }
		public Action OnValueChange = null;
		public void FreshValue()
		{
			Value = (OriginValue + OffsetValue) * PercentValue;
			OnValueChange?.Invoke();
		}
		public override string ToString()
		{
			return Value.ToString();
		}
	}
	public class QRuntimeRangeValue : QRuntimeValue
	{
		public QRuntimeRangeValue() { }
		public QRuntimeRangeValue(float value) : base(value)
		{
			CurrentValue = value;
		}

		public QValue MinValue { get; set; } = 0;
		public float MaxValue => Value;
		private QValue _CurrentValue = 0;
		public Action OnCurrentValueChange = null;
		public float CurrentValue
		{
			get => _CurrentValue;
			set
			{
				if (_CurrentValue != value)
				{
					_CurrentValue = Mathf.Clamp(value, MinValue, MaxValue);
					OnCurrentValueChange?.Invoke();
				}
			}
		}
		public override string ToString()
		{
			return CurrentValue + "/" + Value;
		}
	}
	public class QRuntimeData
	{
		public QDictionary<string, QRuntimeValue> Values = new QDictionary<string, QRuntimeValue>((key)=>new QRuntimeValue());
		public float this[string key]
		{
			get
			{
				if (Values.ContainsKey(key))
				{
					return Values[key].Value;
				}
				else
				{
					return 0;
				}
			}
		}
		public void Clear()
		{
			Values.Clear();
		}
	}
	public class QAverageValue
	{
		public float Value
		{
			get; private set;
		}
		QDictionary<double, float> list = new QDictionary<double, float>();
		public float AllSum { private set; get; }

		double _lastSumTime = -1;
		float _secondeSum = 0;
		public double StartTime { private set; get; } = -1;
		public double EndTime { private set; get; }
		public float SecondeSum
		{
			get
			{
				if (EndTime == 0) return 0;
				if (EndTime == _lastSumTime)
				{
					return _secondeSum;
				}
				_lastSumTime = EndTime;
				_secondeSum = 0f;
				foreach (var kv in list)
				{
					_secondeSum += kv.Value;
				}
				return _secondeSum;
			}
		}
		static double CurTime
		{
			get
			{
				return (DateTime.Now - new DateTime()).TotalSeconds;
			}
		}
		static List<double> buffer = new List<double>();
		public void Push(float value)
		{
			if (StartTime < 0)
			{
				StartTime = CurTime;
			}
			AllSum += value;
			list.RemoveAll((kv) => (CurTime - kv.Key) > 1, buffer);
			list[CurTime] = value;
			EndTime = CurTime;
			Value = SecondeSum / list.Count;
		}
		public void Clear()
		{
			list.Clear();
			StartTime = -1;
			_lastSumTime = -1;
			_secondeSum = 0;
		}
		public override string ToString()
		{
			return "总记[" + AllSum + "]平均[" + Value + "/s]";
		}
		public string ToString(Func<float, string> toString)
		{
			return "总记[" + toString(AllSum) + "]平均[" + toString(Value) + "/s]";
		}
	}
}

