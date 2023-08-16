using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTool
{
	public class QConnectElement : VisualElement
	{
		public QConnectElement()
		{
			generateVisualContent += data =>
			{
				var painter = data.painter2D;
				painter.strokeColor = Color;
				painter.lineJoin = LineJoin.Round;
				painter.lineCap = LineCap.Round;
				painter.lineWidth = LineWidth;
				painter.BeginPath();
				var center = (Start + End) / 2;
				painter.MoveTo(Start);
				painter.LineTo(new Vector2(center.x,Start.y));
				painter.LineTo(new Vector2(center.x,End.y));
				painter.LineTo(End);
				painter.Stroke();
			};
		}
		public Color Color
		{
			get { return _Color; }
			set
			{
				_Color = value;
				MarkDirtyRepaint();
			}
		}
		Color _Color;
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
