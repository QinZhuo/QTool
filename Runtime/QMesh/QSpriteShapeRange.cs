using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if SpriteShap
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace QTool
{
	[RequireComponent(typeof(SpriteShapeController))]
	public class QSpriteShapeRange : MonoBehaviour
	{
		public SpriteShapeController Controller => _Controller ??= GetComponent<SpriteShapeController>();
		private SpriteShapeController _Controller;
		public SpriteShapeRenderer Renderer => Controller.spriteShapeRenderer;
		[QName("形状")]
		public Shape shape = Shape.矩形;
		[QName("距离")]
		public float distance = 2;
		[QName("宽度")]
		public float width = 1;
		public enum Shape
		{
			矩形,
			扇形,
			圆形,
		}
		public void FreshShape()
		{
			var left = Vector2.left * width / 2;
			var right = Vector2.right * width / 2;
			Controller.spline.Clear();
			switch (shape)
			{
				case Shape.矩形:
					{
						var start = left + Vector2.up * distance;
						var end = right + Vector2.up * distance;
						Controller.AddPoint(left);
						Controller.AddPoint(start);
						Controller.AddPoint(end);
						Controller.AddPoint(right);
					}
					break;
				case Shape.扇形:
					{
						Controller.AddPoint(Vector2.zero);
						var endAngle = Mathf.Atan2(width / 2, distance) / Mathf.PI * 360 * 2;
						var dir = (Vector2.up * distance).Rotate(-endAngle / 2);
						Controller.AddPoint(dir);
						float angle = 5;
						for (; angle <= endAngle; angle += 5)
						{
							Controller.AddPoint(dir.Rotate(angle), ShapeTangentMode.Continuous);
						}
						var end = dir.Rotate(endAngle);
						if (Vector2.Distance(dir.Rotate(angle), end) > 1f)
						{
							Controller.AddPoint(end);
						}
						else
						{
							Controller.spline.SetTangentMode(Controller.spline.GetPointCount() - 1, ShapeTangentMode.Linear);
						}
					}
					break;
				case Shape.圆形:
					{
						var dir = Vector2.up * Mathf.Max(distance, width / 2);
						for (float angle = 0; angle < 360; angle += 5)
						{
							Controller.AddPoint(dir.Rotate(angle), ShapeTangentMode.Continuous);
						}
					}
					break;
				default:
					break;
			}
		}
		private void OnValidate()
		{
			FreshShape();
		}
	}
	public static class QSpriteShapeTool
	{
		public static void AddPoint(this SpriteShapeController controller, Vector2 point, ShapeTangentMode tangentMode = ShapeTangentMode.Linear)
		{
			var spline = controller.spline;
			var index = spline.GetPointCount();
			spline.InsertPointAt(index, point);
			spline.SetTangentMode(index, tangentMode);
			spline.SetLeftTangent(index, point.Rotate(-90).normalized * 0.1f);
			spline.SetRightTangent(index, point.Rotate(90).normalized * 0.1f);
		}
	}
}
#endif
