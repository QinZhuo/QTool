using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public static class QLerp
	{
		public static double LerpTo(this double a, double b, float t)
		{
			var dir = b - a;
			return a + dir * t;
		}
		public static float LerpTo(this float a, float b, float t)
        {
            var dir = b - a;
            return a + dir * t;
        }
        public static Vector2 LerpTo(this Vector2 star, Vector2 end, float t)
        {
            return new Vector2(LerpTo(star.x, end.x, t), LerpTo(star.y, end.y, t));
        }
        public static Vector3 LerpTo(this Vector3 star, Vector3 end, float t)
        {
            return new Vector3(LerpTo(star.x, end.x, t), LerpTo(star.y, end.y, t), LerpTo(star.z, end.z, t));
        }
        public static Quaternion LerpTo(this Quaternion star, Quaternion end, float t)
        {
            return Quaternion.Lerp(star, end, t);
        }
        public static Color LerpTo(this Color star, Color end, float t)
        {
            return Color.Lerp(star, end, t);
        }
        public static string LerpTo(this string a, string b, float t)
        {
            var str = "";
            for (int i = 0; i < a.Length || i < b.Length; i++)
            {
                if (i < t * b.Length)
                {
                    str += b[i];
                }
                else if (i < a.Length)
                {
                    str += a[i];
                }
            }
            return str;
        }
    }
	
}
