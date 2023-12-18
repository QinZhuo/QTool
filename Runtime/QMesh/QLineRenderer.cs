using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
namespace QTool
{
	[RequireComponent(typeof(LineRenderer))]
	[ExecuteInEditMode]
	public class QLineRenderer : MonoBehaviour
	{
		[SerializeField]
		private Transform _EndTarget = null;
		public Transform EndTarget
		{
			get => _EndTarget;
			set => _EndTarget = value;
		}
		public LineRenderer lineRenderer;

		private void Reset()
		{
			lineRenderer = GetComponent<LineRenderer>();
		}

		private void LateUpdate()
		{
			if (EndTarget == null)
			{
				lineRenderer.SetPositions(new Vector3[] { transform.position, EndTarget.position });
			}
		}
	}
}
