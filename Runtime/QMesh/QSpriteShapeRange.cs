using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if SpriteShap
using UnityEngine.U2D;
namespace QTool
{
	[RequireComponent(typeof(SpriteShapeController))]
	public class QSpriteShapeRange : MonoBehaviour
	{
		public SpriteShapeController Controller => _Controller ??= GetComponent<SpriteShapeController>();
		private SpriteShapeController _Controller;
		[QName("形状")]
		public Shape shape = Shape.矩形;
		[QName("长度")]
		public float length = 2;
		[QName("宽度")]
		public float width = 1;
		public enum Shape
		{
			矩形,
			扇形,
			圆形,
		}

		private void OnValidate()
		{
			var left = Vector2.left * width;
			var right = Vector2.right * width;
			var start = left + Vector2.up * length;
			var end = right + Vector2.up * length;
			var index = 0;
			Controller.spline.Clear();
			try
			{
				switch (shape)
				{
					case Shape.矩形:
						{
							Controller.spline.InsertPointAt(index++, left);
							Controller.spline.InsertPointAt(index++, start);
							Controller.spline.InsertPointAt(index++, end);
							Controller.spline.InsertPointAt(index++, right);
						}
						break;
					case Shape.扇形:
						{
							Controller.spline.InsertPointAt(index++, Vector2.zero);
							Controller.spline.InsertPointAt(index++, start);
							var endAngle = Vector2.Angle(start, end);
							for (float angle = 5; angle < endAngle; angle += 5)
							{
								Controller.spline.InsertPointAt(index++, start.Rotate(angle));
								Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Continuous);
							}
							Controller.spline.InsertPointAt(index++, end);
							Controller.spline.InsertPointAt(index++, Vector2.zero);
						}
						break;
					case Shape.圆形:
						{
							var dir = Vector2.up * Mathf.Max(length, width) / 2;
							for (float angle = 0; angle < 360; angle += 5)
							{
								Controller.spline.InsertPointAt(index++, dir.Rotate(angle));
								Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Continuous);
							}
						}
						break;
					default:
						break;
				}
			}
			catch (System.Exception e)
			{
				QDebug.LogWarning(e);
			}
		}
	}
}
#endif
