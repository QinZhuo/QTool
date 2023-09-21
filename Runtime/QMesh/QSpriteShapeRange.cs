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
						var count = Mathf.CeilToInt(width * 2);
						for (int i = 0; i <= count; i++)
						{
							var angle = endAngle / count * i;
							Controller.AddPoint(dir.Rotate(angle), i == 0 || i == count ? ShapeTangentMode.Linear : ShapeTangentMode.Continuous);
						}
					}
					break;
				case Shape.圆形:
					{
						var max = Mathf.Max(distance, width / 2);
						var dir = Vector2.up * max;
						var count = Mathf.CeilToInt(Mathf.Max( max * 8,4));
						for (int i = 0; i < count; i++)
						{
							var angle = 360f / count * i;
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
			if (index > 0)
			{
				var left = (Vector2)spline.GetPosition(index - 1);
				var right = (Vector2)spline.GetPosition(0);
				var leftOffset = Vector2.Distance(left, point);
				spline.SetLeftTangent(index, point.Rotate(-90).normalized * leftOffset * 0.4f);
				spline.SetRightTangent(index - 1, left.Rotate(90).normalized * leftOffset * 0.4f);
				var rightOffset = Vector2.Distance(left, point);
				spline.SetRightTangent(index, point.Rotate(90).normalized * rightOffset * 0.4f);
				spline.SetLeftTangent(0, right.Rotate(-90).normalized * rightOffset * 0.35f);
			}
		}
	}
}
#endif
