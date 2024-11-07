using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTool
{
	public class QConnectElement : VisualElement
	{
		public enum ConnectType {
			Hor,
			Ver,
		}
		public QConnectElement()
		{
			generateVisualContent += data =>
			{
#if UNITY_2022_1_OR_NEWER
				var painter = data.painter2D;
				painter.lineJoin = LineJoin.Round;
				painter.lineCap = LineCap.Round;
				painter.lineWidth = LineWidth;
				painter.BeginPath();
				var gradient = new Gradient();
				gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(StartColor, 0), new GradientColorKey(EndColor, 1) },
					new GradientAlphaKey[] { new GradientAlphaKey(1, 1) });
				painter.strokeGradient = gradient;

				painter.MoveTo(Start);
				var size = (End - Start);
				var center = (Start + End) / 2;
				switch (Type) {
					case ConnectType.Hor: {
						painter.LineTo(new Vector2(Start.x + 15, Start.y));
						painter.LineTo(new Vector2(End.x - 15, End.y));
					}
					break;
					case ConnectType.Ver: {
						painter.LineTo(new Vector2(Start.x, Start.y + 15));
						painter.LineTo(new Vector2(End.x, End.y - 15));
					}
					break;
					default:
						break;
				}
				//if (End.x > Start.x || Mathf.Abs(size.x) < Mathf.Abs(size.y))
				//{
					
				//}
				//else
				//{
				//	var top = Mathf.Min(Start.y, End.y) - 30;
				//	painter.LineTo(new Vector2(Start.x + 15, Start.y));
				//	painter.LineTo(new Vector2(Start.x + 15, top));
				//	painter.LineTo(new Vector2(End.x - 15, top));
				//	painter.LineTo(new Vector2(End.x - 15, End.y));
				//}
				painter.LineTo(End);
				painter.Stroke();
#endif
			};
		}
		public Color StartColor
		{
			get { return _StartColor; }
			set
			{
				_StartColor = value;
				MarkDirtyRepaint();
			}
		}
		Color _StartColor;

		public ConnectType Type {
			get { return _Type; }
			set {
				_Type = value;
				MarkDirtyRepaint();
			}
		}
		ConnectType _Type = ConnectType.Hor;
		public Color EndColor
		{
			get { return _EndColor; }
			set
			{
				_EndColor = value;
				MarkDirtyRepaint();
			}
		}
		Color _EndColor;
		public float LineWidth
		{
			get { return _LineWidth; }
			set
			{
				_LineWidth = value;
				MarkDirtyRepaint();
			}
		}
		float _LineWidth;
		public Vector2 Start
		{
			private get
			{
				if (StartElement != null)
				{
					_Start = StartElement.worldBound.center;
				}
				return parent == null ? _End : _Start - parent.worldBound.position;
			}
			set
			{
				_Start = value;
				_StartElement = null;
				MarkDirtyRepaint();
			}
		}
		Vector2 _Start;
		public Vector2 End
		{
			private get
			{

				if (EndElement != null)
				{
					_End = EndElement.worldBound.center;
				}
				return parent == null ? _End : _End - parent.worldBound.position;
			}
			set
			{
				_End = value;
				_EndElement = null; ;
				MarkDirtyRepaint();
			}
		}
		Vector2 _End;
		public VisualElement StartElement
		{
			get => _StartElement;
			set
			{
				_StartElement = value;
				MarkDirtyRepaint();
			}
		}
		public VisualElement _EndElement;
		public VisualElement EndElement
		{
			get => _EndElement;
			set
			{
				_EndElement = value;
				MarkDirtyRepaint();
			}
		}
		public VisualElement _StartElement;
	}
}
