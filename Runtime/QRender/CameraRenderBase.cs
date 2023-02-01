using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace QTool
{
    public struct VertexInfo
    {
        public Vector3 position;
        public Vector3 uv;
        public float x => position.x;
        public float y => position.y;
        public float z => position.z;
	}
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
			var meshs = gameObject.GetComponentsInChildren<MeshFilter>(true);
			StartMatrix(transform);
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
        public static void Start(Material mat, int pass = 0, bool is2D = true)
        {
            if (!mat)
            {
                Debug.LogError("Please Assign a material on the inspector");
                return;
            }
            mat.SetPass(pass);
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
        public static void DrawTriangle(VertexInfo a, VertexInfo b, VertexInfo c)
        {
         
            GL.Begin(GL.TRIANGLES);
            GL.Vertex(a.position);
            GL.TexCoord(a.uv);
            GL.Vertex(b.position);
            GL.TexCoord(a.uv);
            GL.Vertex(c.position);
            GL.TexCoord(a.uv);
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
