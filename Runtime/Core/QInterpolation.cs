using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	/// <summary>
	/// 线性插值 外插值
	/// </summary>
    public static class QLerp
	{
		public static double Lerp(this double a, double b, float t)
		{
			var dir = b - a;
			return a + dir * t;
		}
		public static float Lerp(this float a, float b, float t)
        {
            var dir = b - a;
            return a + dir * t;
        }
        public static Vector2 Lerp(this Vector2 star, Vector2 end, float t)
        {
            return new Vector2(Lerp(star.x, end.x, t), Lerp(star.y, end.y, t));
        }
        public static Vector3 Lerp(this Vector3 star, Vector3 end, float t)
        {
            return new Vector3(Lerp(star.x, end.x, t), Lerp(star.y, end.y, t), Lerp(star.z, end.z, t));
        }
        public static Quaternion Lerp(this Quaternion star, Quaternion end, float t)
        {
            return Quaternion.Lerp(star, end, t);
        }
        public static Color Lerp(this Color star, Color end, float t)
        {
            return Color.Lerp(star, end, t);
        }
        public static string Lerp(this string a, string b, float t)
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
	/// <summary>
	/// 贝塞尔曲线
	/// </summary>
	public static class QBezier
	{
		static List<Vector3> PointList = new List<Vector3>();
		public static Vector3 Bezier(this Vector3 a, Vector3 b, float t, params Vector3[] point)
		{
			if (point.Length > 0)
			{
				PointList.Clear();
				var ta = Vector3.Lerp(a, point[0], t);
				for (int i = 0; i + 1 < point.Length; i++)
				{
					PointList.Add(Vector3.Lerp(point[0], point[1], t));
				}

				var tb = Vector3.Lerp(point[point.Length - 1], b, t);
				return Bezier(ta, tb, t, PointList.ToArray());
			}
			else
			{
				return Vector3.Lerp(a, b, t);
			}
		}
	}
	/// <summary>
	/// Catmull-Rom 曲线插值 与贝赛尔最大的区别是 曲线经过所有的点
	/// </summary>
	public static class QCatmullRom
	{
		public static Vector3 CatmullRom(Vector3 C0, Vector3 P0, Vector3 P1, Vector3 C1, float t)
		{
			const float m00 = 0f, m01 = -0.5f, m02 = 1f, m03 = -0.5f,
				m10 = 1f, m11 = 0f, m12 = -2.5f, m13 = 1.5f,
				m20 = 0f, m21 = 0.5f, m22 = 2f, m23 = -1.5f,
				m30 = 0f, m31 = 0f, m32 = -0.5f, m33 = 0.5f;

			float X0 = C0.x * m00 + P0.x * m10 + P1.x * m20 + C1.x * m30;
			float X1 = C0.x * m01 + P0.x * m11 + P1.x * m21 + C1.x * m31;
			float X2 = C0.x * m02 + P0.x * m12 + P1.x * m22 + C1.x * m32;
			float X3 = C0.x * m03 + P0.x * m13 + P1.x * m23 + C1.x * m33;
			float Y0 = C0.y * m00 + P0.y * m10 + P1.y * m20 + C1.y * m30;
			float Y1 = C0.y * m01 + P0.y * m11 + P1.y * m21 + C1.y * m31;
			float Y2 = C0.y * m02 + P0.y * m12 + P1.y * m22 + C1.y * m32;
			float Y3 = C0.y * m03 + P0.y * m13 + P1.y * m23 + C1.y * m33;
			float Z0 = C0.z * m00 + P0.z * m10 + P1.z * m20 + C1.z * m30;
			float Z1 = C0.z * m01 + P0.z * m11 + P1.z * m21 + C1.z * m31;
			float Z2 = C0.z * m02 + P0.z * m12 + P1.z * m22 + C1.z * m32;
			float Z3 = C0.z * m03 + P0.z * m13 + P1.z * m23 + C1.z * m33;

			return new Vector3(
				X0 + t * (X1 + t * (X2 + t * X3)),
				Y0 + t * (Y1 + t * (Y2 + t * Y3)),
				Z0 + t * (Z1 + t * (Z2 + t * Z3))
			);
		}
	}
}
