using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace QTool
{
 
	public static class QGizmos
	{
		static Stack<Matrix4x4> MatrixStack = new Stack<Matrix4x4>();
		public static void StartMatrix(Transform transform)
		{
			StartMatrix(transform.localToWorldMatrix);
		}
		public static void StartMatrix(Matrix4x4 transform)
		{
			MatrixStack.Push(Gizmos.matrix);
			Gizmos.matrix = transform;
		}
		public static void EndMatrix()
		{
			if (MatrixStack.Count > 0)
			{
				Gizmos.matrix = MatrixStack.Pop();
			}
		}
		public static void Draw(GameObject gameObject, Transform transform)
		{
			StartMatrix(transform);
			var meshs = gameObject.GetComponentsInChildren<MeshFilter>();
			foreach (var mesh in meshs)
			{
				Gizmos.color =Color.Lerp(mesh.GetComponent<Renderer>().sharedMaterial.color,Color.clear,0.5f);
				Gizmos.DrawMesh(mesh.sharedMesh,mesh.transform.position-gameObject.transform.position, mesh.transform.rotation,mesh.transform.lossyScale);
			}
			EndMatrix();
		}
	}
    public static class QGL
    {
		public readonly static Material DefaultMaterial = new Material(Shader.Find("Unlit/Color"));
        public static void Start(Material mat = null,bool is2D = true)
        {
			if (mat != null)
			{
				mat.SetPass(0);
			}
			else
			{
				DefaultMaterial.SetColor("_Color", Color.white);
				DefaultMaterial.SetPass(0);
			}
            GL.PushMatrix();
            if (is2D)
            {
                GL.LoadOrtho();
            }
        }
        public static void End()
        {
            GL.PopMatrix();
        }
        /// <summary>
        /// 顺时针三点
        /// </summary>
        public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            GL.Begin(GL.TRIANGLES);
            GL.Vertex(a);
            GL.Vertex(b);
            GL.Vertex(c);
            GL.End();
        }
		public static void DrawQUADS(Rect rect)
		{
			GL.Begin(GL.QUADS);
			GL.Vertex(new Vector3(rect.xMin, rect.yMin));
			GL.Vertex(new Vector3(rect.xMin, rect.yMax));
			GL.Vertex(new Vector3(rect.xMax, rect.yMax));
			GL.Vertex(new Vector3(rect.xMax, rect.yMin));
			GL.End();
		}
	}

    public abstract class OnPostRenderBase : MonoBehaviour
    {
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }
        public void OnEndCameraRendering(ScriptableRenderContext context,Camera camera)
        {
            OnPostRender();
        }
        protected abstract void OnPostRender();
    }

}
