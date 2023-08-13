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
	public class QRuntimeValue<T>:IQSerializeCallback
	{
		public string Name { get; set; }
		private T _Value = default;
		public virtual T Value
		{
			get => _Value;
			set
			{
				if (!Equals(value,_Value))
				{
					_Value = value;
					InvokeOnChange();
				}
			}
		}
		public event Action<string, T> OnValue = null;
		public virtual void InvokeOnChange()
		{
			InvokeOnChange(Name, Value);
		}
		protected void InvokeOnChange(string key,T value)
		{
			OnValue?.Invoke(key,value);
		}
		public override string ToString()
		{
			return Value?.ToString();
		}

		public virtual void OnDeserializeOver()
		{
			InvokeOnChange();
		}
	}
	public class QRuntimeValue: QRuntimeValue<float>
	{
		public QRuntimeValue()
		{
		}
		public QRuntimeValue(float value)
		{
			OriginValue = value;
			FreshValue();
		}
		public void Reset(float value)
		{
			OriginValue = value;
			OffsetValue = 0;
			ScaleValue = 1;
			FreshValue();
		}
		[QName]
		public QValue OriginValue { get; private set; } = 0f;
		private QValue _OffsetValue = 0;
		[QName]
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
		private QValue _ScaleValue = 1;
		[QName,QOldName("PercentValue")]
		public QValue ScaleValue
		{
			get => _ScaleValue;
			set
			{
				if (_ScaleValue != value)
				{
					_ScaleValue = value;
					FreshValue();
				}
			}
		}
		private QValue _Value { get; set; } = 0;
		[QIgnore]
		public override float Value
		{
			get => _Value; set
			{
				if (ScaleValue == 1)
				{
					OffsetValue = value - OriginValue;
				}
				else
				{
					throw new Exception(nameof(QRuntimeValue) + "." + nameof(ScaleValue) + "不为1 无法直接更改 " + nameof(Value) + " 可尝试更改 " + nameof(OffsetValue));
				}
			}
		}
		public int IntValue => Mathf.RoundToInt(Value);
		private void FreshValue()
		{
			_Value = (OriginValue + OffsetValue) * ScaleValue;
			InvokeOnChange();
		}
		public override void OnDeserializeOver()
		{
			FreshValue();
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
		public float CurrentValue
		{
			get => _CurrentValue;
			set
			{
				if (_CurrentValue != value)
				{
					_CurrentValue = Mathf.Clamp(value, MinValue, MaxValue);
					InvokeOnChange();
				}
			}
		}
		public override string ToString()
		{
			return CurrentValue + "/" + Value;
		}
		public override void InvokeOnChange()
		{
			base.InvokeOnChange();
			InvokeOnChange("当前" + Name, CurrentValue);
			InvokeOnChange(Name+"比例", (CurrentValue-MinValue)/MaxValue);
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

