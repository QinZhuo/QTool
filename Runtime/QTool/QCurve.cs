using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    public enum QEaseCurve
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InBack,
        OutBack,
        InOutBack,
        InElastic,
        OutElastic,
        InOutElastic,
        InBounce,
        OutBounce,
        InOutBounce,

    }
    public static class QCurve
    {
        public static Func<float,float> Get(QEaseCurve ease)
        {
            switch (ease)
            {
                case QEaseCurve.Linear:
                    return Linear;
                case QEaseCurve.InSine:
                    return Sine;
                case QEaseCurve.OutSine:
                    return Sine.Out();
                case QEaseCurve.InOutSine:
                    return Sine.InOut();
                case QEaseCurve.InQuad:
                    return Quad;
                case QEaseCurve.OutQuad:
                    return Quad.Out();
                case QEaseCurve.InOutQuad:
                    return Quad.InOut();
                case QEaseCurve.InCubic:
                    return Cubic;
                case QEaseCurve.OutCubic:
                    return Cubic.Out();
                case QEaseCurve.InOutCubic:
                    return Cubic.InOut();
                case QEaseCurve.InQuart:
                    return Quart;
                case QEaseCurve.OutQuart:
                    return Quart.Out();
                case QEaseCurve.InOutQuart:
                    return Quart.InOut();
                case QEaseCurve.InQuint:
                    return Quint;
                case QEaseCurve.OutQuint:
                    return Quint.Out();
                case QEaseCurve.InOutQuint:
                    return Quint.InOut();
                case QEaseCurve.InExpo:
                    return Expo;
                case QEaseCurve.OutExpo:
                    return Expo.Out();
                case QEaseCurve.InOutExpo:
                    return Expo.InOut();
                case QEaseCurve.InCirc:
                    return Circ;
                case QEaseCurve.OutCirc:
                    return Circ.Out();
                case QEaseCurve.InOutCirc:
                    return Circ.InOut();
                case QEaseCurve.InBack:
                    return Back;
                case QEaseCurve.OutBack:
                    return Back.Out();
                case QEaseCurve.InOutBack:
                    return Back.InOut();
                case QEaseCurve.InElastic:
                    return Elastic;
                case QEaseCurve.OutElastic:
                    return Elastic.Out();
                case QEaseCurve.InOutElastic:
                    return Elastic.InOut();
                case QEaseCurve.InBounce:
                    return Bounce;
                case QEaseCurve.OutBounce:
                    return Bounce.Out();
                case QEaseCurve.InOutBounce:
                    return Bounce.InOut();
                default:
                    return Linear;
            }
        }
        public static Func<float,float> Linear
        {
            get
            {
                return QCurveFunction.Linear;
            }
        }
        public static Func<float, float> Sine
        {
            get
            {
                return QCurveFunction.Sine;
            }
        }
       
        public static Func<float, float> Quad
        {
            get
            {
                return QCurveFunction.PowFunc(2);
            }
        }
        public static Func<float, float> Cubic
        {
            get
            {
                return QCurveFunction.PowFunc(3);
            }
        }
        public static Func<float, float> Quart
        {
            get
            {
                return QCurveFunction.PowFunc(4);
            }
        }
        public static Func<float, float> Quint
        {
            get
            {
                return QCurveFunction.PowFunc(5);
            }
        }
        public static Func<float, float> Expo
        {
            get
            {
                return QCurveFunction.Expo;
            }
        }
        public static Func<float, float> Circ
        {
            get
            {
                return QCurveFunction.Circ;
            }
        }
        public static Func<float, float> Back
        {
            get
            {
                return QCurveFunction.back;
            }
        }
        public static Func<float, float> Elastic
        {
            get
            {
                return QCurveFunction.Elastic;
            }
        }
        public static Func<float, float> Bounce
        {
            get
            {
                return QCurveFunction.Out( QCurveFunction.Bounce);
            }
        }
    }
 
    static class QCurveFunction
	{
		public static float Linear(float t)
		{
			return t;
		}
		public static Func<float, float> Out(this Func<float, float> InFunc)
        {
            return (t) =>
            {
                return 1 - InFunc(1 - t);
            };
        }
        
        static float temp1 = 1.70158f;
        static float d1 = 2.75f;
        static float temp2 = 7.5625f;
     
        public static float Sine(float t)
        {
            return 1-Mathf.Sin((1-t )* Mathf.PI/2);
        }
        public static Func<float,float> PowFunc(float p)
        {
            return (t) => Mathf.Pow(t, p);
        }
        public static float Expo(float t)
        {
            return t == 0 ? t : Mathf.Pow(2, 10 * (t - 1));
        }
        public static float Circ(float t)
        {
            return 1 - Mathf.Sqrt(1 - t * t);
        }
        public static float back(float t)
        {
            return  t * t * t * (temp1 + 1) - t * t * temp1;
        }
        public static float Elastic(float t)
        {
            if (t == 0 || t == 1)
            {
                return t;
            }
            else
            {
                return -Mathf.Pow(2, 10 * (t - 1)) * Mathf.Sin((t * 10*(1 - 0.75f)) * Mathf.PI*2/3);
            }
        }
        static float BonceTool(float t,float p)
        {
            return temp2 * Mathf.Pow(t - (p / d1), 2);
        }
        public static float Bounce(float t)
        {
            if (t < 1/ d1)
            {
                return temp2 * t * t;
            }
            else if(t<2/d1)
            {
                return BonceTool(t, 1.5f) + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return BonceTool(t, 2.25f) + 0.93375f;
            }
            else
            {
                return BonceTool(t, 2.625f) + 0.984375f;
            }
        }
		public static Func<float, float> InOut(this Func<float, float> InFunc)
		{
			return (t) =>
			{

				if (t < 0.5f)
				{
					return InFunc(t * 2) / 2;
				}
				else
				{
					return InFunc.Out()(t * 2 - 1) / 2 + 0.5f;
				}
			};
		}
	}
  
}

