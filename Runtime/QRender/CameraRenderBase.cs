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
		public static void Draw(GameObject gameObject,Transform transform)
		{
			var matrix = Gizmos.matrix;
			var meshs= gameObject.GetComponentsInChildren<MeshFilter>();
			foreach (var mesh in meshs)
			{
				Gizmos.matrix = transform.localToWorldMatrix ;
				Gizmos.DrawMesh(mesh.sharedMesh,mesh.transform.position);
			}
			Gizmos.matrix = matrix;
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
