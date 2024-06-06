using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System.IO;
using System;
using static UnityEngine.Rendering.DebugUI;

namespace QTool
{
	public class QRuntimeValue<T> : IQSerializeCallback {
		private T _Value = default;
		public virtual T Value {
			get => _Value;
			set {
				if (!Equals(value, _Value)) {
					_Value = value;
					InvokeOnChange();
				}
			}
		}
		public event Action<T> OnValueChange = null;
		public virtual void InvokeOnChange() {
			OnValueChange?.Invoke(Value);
		}
		public override string ToString() {
			return Value?.ToString();
		}

		public virtual void OnLoad() {
			InvokeOnChange();
		}

		public virtual void OnBeforeQSerialize() {

		}
	}
	public class QRuntimeValue : QRuntimeValue<float>
	{
		public QRuntimeValue()
		{
			OffsetValues = new QDictionary<string, float>()
			{
				OnChange = key => FreshValue()
			};
			ScaleValues = new QDictionary<string, float>()
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

		private float _BaseValue { get; set; } = 0f;
		[QName]
		public float BaseValue
		{
			get => BaseRuntimeValue == null ? _BaseValue : BaseRuntimeValue.Value;
			set
			{
				if (BaseRuntimeValue != null) { throw new Exception("无法更改" + nameof(BaseRuntimeValue)); }
				if (_BaseValue != value) { _BaseValue = value; FreshValue(); }
			}
		}
		[QName]
		public QDictionary<string, float> OffsetValues = null;
		private List<string> buffer = new List<string>();
		/// <summary>
		/// 不会受Scale影响的数值
		/// </summary>
		public float OffsetValue
		{
			get
			{
				var value = 0f;
				foreach (var item in OffsetValues)
				{
					value += item.Value;
				}
				return value;
			}
		}
		[QName]
		public QDictionary<string, float> ScaleValues = null;
		public float ScaleValue
		{
			get
			{
				var value = 1f;
				foreach (var item in ScaleValues)
				{
					value += item.Value;
				}
				return value;
			}
		}
		private float m_Value { get; set; } = 0;
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
			m_Value = BaseValue * ScaleValue + OffsetValue;
			InvokeOnChange();
		}
		public override void OnLoad()
		{
			FreshValue();
		}
		public override string ToString()
		{
			var info = BaseRuntimeValue == null ? (BaseValue == 0 ? "" : BaseValue.ToString()) : BaseRuntimeValue.ToString();
			if (ScaleValues.Count > 0)
			{
				info = "(" + info + ") * (";
				info += ScaleValues.ToOneString(" ", kv => (kv.Value > 0 ? "+ " + kv.Value : "- " + -kv.Value) + " (" + kv.Key.SplitStartString("_").ToLozalizationColorKey() + ") ");
				info += ")";
			}
			if (OffsetValues.Count > 0)
			{
				info += " " + OffsetValues.ToOneString(" ", kv => (kv.Value > 0 ? "+ " + kv.Value : "- " + -kv.Value) + " (" + kv.Key.SplitStartString("_").ToLozalizationColorKey() + ") ");
			}
			return info;
		}
	}

	public class QRuntimeRangeValue : QRuntimeValue {
		public QRuntimeRangeValue() { }
		public QRuntimeRangeValue(float value) : base(value) {
			CurrentValue = value;
		}

		public float MinValue { get; set; } = 0;
		public float MaxValue => Value;
		private float _CurrentValue = 0;
		public float CurrentValue {
			get => _CurrentValue;
			set {
				if (_CurrentValue != value) {
					_CurrentValue = Mathf.Clamp(value, MinValue, MaxValue);
					InvokeOnChange();
				}
			}
		}
		public event Action<float> OnCurrentValueChange = null;
		public event Action<float> OnMinValueChange = null;
		public override string ToString() {
			return CurrentValue + "/" + Value;
		}
		public override void InvokeOnChange() {
			base.InvokeOnChange();
			OnCurrentValueChange?.Invoke(CurrentValue);
			OnMinValueChange?.Invoke(MinValue);
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

