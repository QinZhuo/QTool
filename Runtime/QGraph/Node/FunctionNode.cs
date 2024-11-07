using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using QTool.Graph;
using System.Reflection;
#if NODECANVAS
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
#endif
namespace QTool {
	[Category("函数")]
	abstract public class FunctionNode : QNodeRuntime {
	}

	abstract public class FunctionNode<TResult> : FunctionNode {
		[QOutputPort]
		public TResult result;
		abstract public TResult Invoke();
		protected override void OnStart() {
			result = Invoke();
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		abstract public TResult Invoke(T1 a);
		protected override void OnStart() {
			result = Invoke(p1);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		abstract public TResult Invoke(T1 a, T2 b);
		protected override void OnStart() {
			result = Invoke(p1,p2);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		abstract public TResult Invoke(T1 a, T2 b, T3 c);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4, T5> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		[QInputPort] public T5 p5;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d, T5 e);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4, p5);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4, T5, T6> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		[QInputPort] public T5 p5;
		[QInputPort] public T6 p6;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4, p5, p6);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4, T5, T6, T7> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		[QInputPort] public T5 p5;
		[QInputPort] public T6 p6;
		[QInputPort] public T7 p7;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4, p5, p6, p7);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4, T5, T6, T7, T8> : FunctionNode {
		[QOutputPort]
		public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		[QInputPort] public T5 p5;
		[QInputPort] public T6 p6;
		[QInputPort] public T7 p7;
		[QInputPort] public T8 p8;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4, p5, p6, p7, p8);
			End();
		}
	}

	abstract public class FunctionNode<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> : FunctionNode {
		[QOutputPort] public TResult result;
		[QInputPort] public T1 p1;
		[QInputPort] public T2 p2;
		[QInputPort] public T3 p3;
		[QInputPort] public T4 p4;
		[QInputPort] public T5 p5;
		[QInputPort] public T6 p6;
		[QInputPort] public T7 p7;
		[QInputPort] public T8 p8;
		[QInputPort] public T9 p9;
		abstract public TResult Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h, T9 i);
		protected override void OnStart() {
			result = Invoke(p1, p2, p3, p4, p5, p6, p7, p8, p9);
			End();
		}
	}



	[Category("运算")]
	[Description("Returns if the object is not null")]
	[QName("Is Valid")]
	public class IsNotNull : FunctionNode<bool, object> {
		public override bool Invoke(object OBJECT) {
			return OBJECT != null && !OBJECT.Equals(null);
		}
	}

	//[Category("运算")]
	//[Description("Returns whether the input object is of type T as well as the object itself for convenience.")]
	//public class IsOfType : FunctionNode<bool, object, System.Type> {
	//	public object OBJECT { get; private set; }
	//	public override bool Invoke(object OBJECT, System.Type type) {
	//		this.OBJECT = OBJECT;
	//		return OBJECT != null && type.IsAssignableFrom(OBJECT.GetType());
	//	}
	//}

	////--ANY COMPARABLE

	//[Category("运算/Any")]
	//[QName(">")]
	//[Description("Any Greater Than")]
	//public class AnyGreaterThan : FunctionNode<bool, IComparable, IComparable> {
	//	public override bool Invoke(IComparable a, IComparable b) {
	//		return a.CompareTo(b) == 1;
	//	}
	//}

	//[Category("运算/Any")]
	//[QName("≥")]
	//[Description("Any Greater Or Equal Than")]
	//public class AnyGreaterEqualThan : FunctionNode<bool, IComparable, IComparable> {
	//	public override bool Invoke(IComparable a, IComparable b) {
	//		return a.CompareTo(b) == 1 || object.Equals(a, b);
	//	}
	//}

	//[Category("运算/Any")]
	//[QName("<")]
	//[Description("Any Less Than")]
	//public class AnyLessThan : FunctionNode<bool, IComparable, IComparable> {
	//	public override bool Invoke(IComparable a, IComparable b) {
	//		return a.CompareTo(b) == -1;
	//	}
	//}

	//[Category("运算/Any")]
	//[QName("≤")]
	//[Description("Any Less Or Equal Than")]
	//public class AnyLessEqualThan : FunctionNode<bool, IComparable, IComparable> {
	//	public override bool Invoke(IComparable a, IComparable b) {
	//		return a.CompareTo(b) == -1 || object.Equals(a, b);
	//	}
	//}

	//[Category("运算/Any")]
	//[QName("=")]
	//[Description("Any Equal To")]
	//public class AnyEqual : FunctionNode<bool, object, object> {
	//	public override bool Invoke(object a, object b) {
	//		return Equals(a, b);
	//	}
	//}

	//[Category("运算/Any")]
	//[QName("≠")]
	//[Description("Any Not Equal To")]
	//public class AnyNotEqual : FunctionNode<bool, object, object> {
	//	public override bool Invoke(object a, object b) {
	//		return !Equals(a, b);
	//	}
	//}


	////--FLOATS

	[Category("运算/Floats")]
	[QName("+")]
	[Description("Float Add")]
	public class FloatAdd : FunctionNode<float, float, float> {
		public override float Invoke(float a, float b) {
			return a + b;
		}
	}

	[Category("运算/Floats")]
	[QName("-")]
	[Description("Float Subtract")]
	public class FloatSubtract : FunctionNode<float, float, float> {
		public override float Invoke(float a, float b) {
			return a - b;
		}
	}

	[Category("运算/Floats")]
	[QName("×")]
	[Description("Float Mutliply")]
	public class FloatMultiply : FunctionNode<float, float, float> {
		public override float Invoke(float a, float b) {
			return a * b;
		}
	}

	[Category("运算/Floats")]
	[QName("÷")]
	[Description("Float Divide")]
	public class FloatDivide : FunctionNode<float, float, float> {
		public override float Invoke(float a, float b) {
			return a / b;
		}
	}

	[Category("运算/Floats")]
	[QName("%")]
	[Description("Float Modulo")]
	public class FloatModulo : FunctionNode<float, float, float> {
		public override float Invoke(float value, float mod) {
			return value % mod;
		}
	}

	[Category("运算/Floats")]
	[QName(">")]
	[Description("Float Greater Than")]
	public class FloatGreaterThan : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a > b;
		}
	}

	[Category("运算/Floats")]
	[QName("≥")]
	[Description("Float Greater Or Equal Than")]
	public class FloatGreaterEqualThan : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a >= b;
		}
	}

	[Category("运算/Floats")]
	[QName("<")]
	[Description("Float Less Than")]
	public class FloatLessThan : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a < b;
		}
	}

	[Category("运算/Floats")]
	[QName("≤")]
	[Description("Float Less Or Equal Than")]
	public class FloatLessEqualThan : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a <= b;
		}
	}

	[Category("运算/Floats")]
	[QName("=")]
	[Description("Float Equal To")]
	public class FloatEqual : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a == b;
		}
	}

	[Category("运算/Floats")]
	[QName("≠")]
	[Description("Float Not Equal To")]
	public class FloatNotEqual : FunctionNode<bool, float, float> {
		public override bool Invoke(float a, float b) {
			return a != b;
		}
	}

	[Category("运算/Floats")]
	[QName("Invert")]
	[Description("Float Invert the input ( value = value * -1 )")]
	public class FloatInvert : FunctionNode<float, float> {
		public override float Invoke(float value) {
			return value * -1;
		}
	}

	[Category("运算/Floats")]
	[Description("Float Round value to closest of interval ( round(value / interval) * interval )")]
	public class FloatSnap : FunctionNode<int, float, int> {
		public override int Invoke(float value, int interval) {
			return (int)Mathf.Round(value / interval) * interval;
		}
	}


	//--INTEGER

	[Category("运算/Integers")]
	[QName("+")]
	[Description("Integer Add")]
	public class IntegerAdd : FunctionNode<int, int, int> {
		public override int Invoke(int a, int b) {
			return a + b;
		}
	}

	[Category("运算/Integers")]
	[QName("-")]
	[Description("Integer Subtract")]
	public class IntegerSubtract : FunctionNode<int, int, int> {
		public override int Invoke(int a, int b) {
			return a - b;
		}
	}

	[Category("运算/Integers")]
	[QName("×")]
	[Description("Integer Multiply")]
	public class IntegerMultiply : FunctionNode<int, int, int> {
		public override int Invoke(int a, int b) {
			return a * b;
		}
	}

	[Category("运算/Integers")]
	[QName("÷")]
	[Description("Integer Divide")]
	public class IntegerDivide : FunctionNode<int, int, int> {
		public override int Invoke(int a, int b) {
			return b == 0 ? 0 : a / b;
		}
	}

	[Category("运算/Integers")]
	[QName("%")]
	[Description("Integer Modulo")]
	public class IntegerModulo : FunctionNode<int, int, int> {
		public override int Invoke(int value, int mod) {
			return value % mod;
		}
	}


	[Category("运算/Integers")]
	[QName(">")]
	[Description("Integer Greater Than")]
	public class IntegerGreaterThan : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a > b;
		}
	}

	[Category("运算/Integers")]
	[QName("≥")]
	[Description("Integer Greater Or Equal Than")]
	public class IntegerGreaterEqualThan : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a >= b;
		}
	}

	[Category("运算/Integers")]
	[QName("<")]
	[Description("Integer Less Than")]
	public class IntegerLessThan : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a < b;
		}
	}

	[Category("运算/Integers")]
	[QName("≤")]
	[Description("Integer Less Or Equal Than")]
	public class IntegerLessEqualThan : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a <= b;
		}
	}

	[Category("运算/Integers")]
	[QName("=")]
	[Description("Integer Equal To")]
	public class IntegerEqual : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a == b;
		}
	}

	[Category("运算/Integers")]
	[QName("≠")]
	[Description("Integer Not Equal To")]
	public class IntegerNotEqual : FunctionNode<bool, int, int> {
		public override bool Invoke(int a, int b) {
			return a != b;
		}
	}

	[Category("运算/Integers")]
	[QName("Invert")]
	[Description("Integer Invert the input ( value = value * -1 )")]
	public class IntegerInvert : FunctionNode<int, int> {
		public override int Invoke(int value) {
			return value * -1;
		}
	}

	[Category("运算/Integers")]
	[Description("Integer Round value to closest of interval ( round(value / interval) * interval )")]
	public class IntegerSnap : FunctionNode<int, int, int> {
		public override int Invoke(int value, int interval) {
			return (int)Mathf.Round(value / interval) * interval;
		}
	}

	//--BOOLEAN

	[Category("运算/Boolean")]
	[QName("=")]
	[Description("Boolean Equal To")]
	public class BooleanEqual : FunctionNode<bool, bool, bool> {
		public override bool Invoke(bool a, bool b) {
			return a == b;
		}
	}

	[Category("运算/Boolean")]
	[QName("≠")]
	[Description("Boolean Not Equal To")]
	public class BooleanNotEqual : FunctionNode<bool, bool, bool> {
		public override bool Invoke(bool a, bool b) {
			return a != b;
		}
	}

	[Category("运算/Boolean")]
	[Description("True if A and B are both true")]
	public class AND : FunctionNode<bool, bool, bool> {
		public override bool Invoke(bool a, bool b) {
			return a && b;
		}
	}

	[Category("运算/Boolean")]
	[Description("True if A or B is true")]
	public class OR : FunctionNode<bool,bool,bool> {
		public override bool Invoke(bool a, bool b) {
			return a || b;
		}
	}

	[Category("运算/Boolean")]
	[Description("True if A or B is true, but not both")]
	public class XOR : FunctionNode<bool, bool, bool> {
		public override bool Invoke(bool a, bool b) {
			return (a || b) && (a != b);
		}
	}

	[Category("运算/Boolean")]
	[Description("Inverts the input")]
	public class NOT : FunctionNode<bool, bool> {
		public override bool Invoke(bool value) {
			return !value;
		}
	}

	//--VECTORS

	[Category("运算/Vector3")]
	[QName("=")]
	[Description("Vector3 Equal To")]
	public class Vector3Equal : FunctionNode<bool, Vector3, Vector3> {
		public override bool Invoke(Vector3 a, Vector3 b) {
			return a == b;
		}
	}

	[Category("运算/Vector3")]
	[QName("≠")]
	[Description("Vector3 Not Equal To")]
	public class Vector3NotEqual : FunctionNode<bool, Vector3, Vector3> {
		public override bool Invoke(Vector3 a, Vector3 b) {
			return a != b;
		}
	}

	[Category("运算/Vector3")]
	[QName("+")]
	[Description("Vector3 Add")]
	public class Vector3Add : FunctionNode<Vector3, Vector3, Vector3> {
		public override Vector3 Invoke(Vector3 a, Vector3 b) {
			return a + b;
		}
	}

	[Category("运算/Vector3")]
	[QName("-")]
	[Description("Vector3 Subtract")]
	public class Vector3Subtract : FunctionNode<Vector3, Vector3, Vector3> {
		public override Vector3 Invoke(Vector3 a, Vector3 b) {
			return a - b;
		}
	}

	[Category("运算/Vector3")]
	[QName("×")]
	[Description("Vector3 Multiply")]
	public class Vector3Multiply : FunctionNode<Vector3, Vector3, float> {
		public override Vector3 Invoke(Vector3 a, float b) {
			return a * b;
		}
	}

	[Category("运算/Vector3")]
	[QName("÷")]
	[Description("Vectro3 Divide")]
	public class Vector3Divide : FunctionNode<Vector3, Vector3, float> {
		public override Vector3 Invoke(Vector3 a, float b) {
			return a / b;
		}
	}

	[Category("运算/Vector3")]
	[QName("Invert")]
	[Description("Vector3 Invert the input ( value = value * -1 )")]
	public class Vector3Invert : FunctionNode<Vector3, Vector3> {
		public override Vector3 Invoke(Vector3 value) {
			return value * -1;
		}
	}

	////--FLAGS

	//[Category("运算/Flags")]
	//[QName("AND")]
	//[Description("Bitwise logical AND & operator")]
	//public class BitwiseAnd<T> : FunctionNode<T, T, T> where T : Enum {
	//	public override T Invoke(T a, T b) {
	//		var result = ((int)(object)a) & ((int)(object)b);
	//		return (T)(object)result;
	//	}
	//}

	//[Category("运算/Flags")]
	//[QName("OR")]
	//[Description("Bitwise logical OR | operator")]
	//public class BitwiseOr<T> : FunctionNode<T, T, T> where T : Enum {
	//	public override T Invoke(T a, T b) {
	//		var result = ((int)(object)a) | ((int)(object)b);
	//		return (T)(object)result;
	//	}
	//}

	//[Category("运算/Flags")]
	//[QName("Invert")]
	//[Description("Bitwise complement ~ operator")]
	//public class BitwiseInvert<T> : FunctionNode<T, T> where T : Enum {
	//	public override T Invoke(T a) {
	//		var result = ~((int)(object)a);
	//		return (T)(object)result;
	//	}
	//}
}
