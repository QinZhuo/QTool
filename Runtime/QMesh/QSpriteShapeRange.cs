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
			var index = 0;
			Controller.spline.Clear();
			switch (shape)
			{
				case Shape.矩形:
					{
						var start = left + Vector2.up * distance;
						var end = right + Vector2.up * distance;
						Controller.spline.InsertPointAt(index++, left);
						Controller.spline.InsertPointAt(index++, start);
						Controller.spline.InsertPointAt(index++, end);
						Controller.spline.InsertPointAt(index++, right);
						
					}
					break;
				case Shape.扇形:
					{
						Controller.spline.InsertPointAt(index++, Vector2.zero);
						var endAngle = Mathf.Atan2(width / 2, distance) / Mathf.PI * 360 * 2;
						var dir = (Vector2.up * distance).Rotate(-endAngle / 2);
						Controller.spline.InsertPointAt(index++, dir);
						float angle = 5;
						for (; angle <= endAngle; angle += 5)
						{
							Controller.spline.InsertPointAt(index++, dir.Rotate(angle));
						}
						var end = dir.Rotate(endAngle);
						if (Vector2.Distance(dir.Rotate(angle), end) > 1f)
						{
							Controller.spline.InsertPointAt(index++, end);
						}
						for (int i = 0; i < index; i++)
						{
							switch (i)
							{
								case 0:
								case 1:
									Controller.spline.SetTangentMode(i, ShapeTangentMode.Linear);
									break;
								default:
									Controller.spline.SetTangentMode(i, i + 1 == index ? ShapeTangentMode.Linear : ShapeTangentMode.Continuous);
									break;
							}
						}
					}
					break;
				case Shape.圆形:
					{
						var dir = Vector2.up * Mathf.Max(distance, width / 2);
						for (float angle = 0; angle < 360; angle += 5)
						{
							Controller.spline.InsertPointAt(index++, dir.Rotate(angle));
						}
						for (int i = 0; i < index; i++)
						{
							Controller.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
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
}
#endif
