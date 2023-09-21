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
			try
			{
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
							Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Linear);
							var endAngle = Mathf.Atan2(width / 2, distance) / Mathf.PI * 360 * 2;
							var dir = (Vector2.up * distance).Rotate(-endAngle / 2);
							Controller.spline.InsertPointAt(index++, dir);
							Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Linear);
							for (float angle = 5; angle <= endAngle; angle += 5)
							{
								Controller.spline.InsertPointAt(index++, dir.Rotate(angle));
								Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Continuous);
							}
							Controller.spline.InsertPointAt(index++, dir.Rotate(endAngle));
							Controller.spline.SetTangentMode(index - 1, ShapeTangentMode.Linear);
						}
						break;
					case Shape.圆形:
						{
							var dir = Vector2.up * Mathf.Max(distance, width / 2);
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
		private void OnValidate()
		{
			FreshShape();
		}
	}
}
#endif
