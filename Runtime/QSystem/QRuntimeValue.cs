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
	public class QRuntimeValue<T> : IQSerializeCallback
	{
		public string Name { get; set; }
		private T _Value = default;
		public virtual T Value
		{
			get => _Value;
			set
			{
				if (!Equals(value, _Value))
				{
					_Value = value;
					InvokeOnChange();
				}
			}
		}
		public event Action<string, T> OnValueChange = null;
		public event Action<string, string> OnStringChange = null;
		public virtual void InvokeOnChange()
		{
			InvokeOnChange(Name, Value);
		}
		protected void InvokeOnChange(string key, T value)
		{
			OnValueChange?.Invoke(key, value);
			OnStringChange?.Invoke(key, ToString());
		}
		public override string ToString()
		{
			return Value?.ToString();
		}

		public virtual void OnQDeserializeOver()
		{
			InvokeOnChange();
		}

		public virtual void OnBeforeQSerialize()
		{

		}
	}
	public class QRuntimeValue : QRuntimeValue<float>
	{
		public QRuntimeValue()
		{
			OffsetValues = new QDictionary<string, QValue>()
			{
				OnChange = key => FreshValue()
			};
			ScaleValues = new QDictionary<string, QValue>()
			{
				OnChange = key => FreshValue()
			};
		}
		public QRuntimeValue(float value) : this()
		{
			BaseValue = value;
			FreshValue();
		}
		public QRuntimeValue(QRuntimeValue value) : this()
		{
			BaseRuntimeValue = value;
			FreshValue();
		}
		public QRuntimeValue BaseRuntimeValue { get; private set; }
		public void Reset(float value,params string[] ignoreKey)
		{
			BaseValue = value;
			Clear(ignoreKey);
		}
		public void Clear(params string[] ignoreKey)
		{
			OffsetValues.RemoveAll(kv => !ignoreKey.Contains(kv.Key), buffer);
			ScaleValues.RemoveAll(kv => !ignoreKey.Contains(kv.Key), buffer);
			FreshValue();
		}

		private QValue _BaseValue { get; set; } = 0f;
		[QName]
		public QValue BaseValue
		{
			get => BaseRuntimeValue == null ? _BaseValue : BaseRuntimeValue.Value;
			set
			{
				if (BaseRuntimeValue != null) { throw new Exception("无法更改" + nameof(BaseRuntimeValue)); }
				if (_BaseValue != value) { _BaseValue = value; FreshValue(); }
			}
		}
		[QName]
		public QDictionary<string, QValue> OffsetValues = null;
		private List<string> buffer = new List<string>();
		public float OffsetValue
		{
			get
			{
				var value = 0f;
				OffsetValues.RemoveAll(kv => kv.Value == 0, buffer);
				foreach (var item in OffsetValues)
				{
					value += item.Value;
				}
				return value;
			}
		}
		[QName]
		public QDictionary<string, QValue> ScaleValues = null;
		public float ScaleValue
		{
			get
			{
				var value = 1f;
				ScaleValues.RemoveAll(kv => kv.Value == 0, buffer);
				foreach (var item in ScaleValues)
				{
					value += item.Value;
				}
				return value;
			}
		}
		private QValue m_Value { get; set; } = 0;
		[QIgnore]
		public override float Value
		{
			get => m_Value; set
			{
				throw new Exception(nameof(QRuntimeValue) + " 不可直接更改 " + nameof(Value) + " 可尝试更改 " + nameof(BaseValue));
			}
		}
		public int IntValue => Mathf.RoundToInt(Value);
		private void FreshValue()
		{
			m_Value = (BaseValue + OffsetValue) * ScaleValue;
			InvokeOnChange();
		}
		public override void OnQDeserializeOver()
		{
			FreshValue();
		}
		public override string ToString()
		{
			var info = BaseRuntimeValue == null ? (BaseValue == 0 ? "" : BaseValue.ToString()) : BaseRuntimeValue.ToString();
			if (OffsetValues.Count > 0)
			{
				info += " " + OffsetValues.ToOneString(" ", kv => (kv.Value > 0 ? "+ " + kv.Value : "- " + -kv.Value) + " (" + kv.Key.ToColorString(kv.Key.ToColor()) + ") ");
			}
			if (ScaleValues.Count > 0)
			{
				info = "(" + info + ") * (";
				info += ScaleValues.ToOneString(" ", kv => (kv.Value > 0 ? "+ " + kv.Value : "- " + -kv.Value) + " (" + kv.Key.ToColorString(kv.Key.ToColor()) + ") ");
				info += ")";
			}
			return info;
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
			InvokeOnChange(Name + "比例", (CurrentValue - MinValue) / MaxValue);
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

