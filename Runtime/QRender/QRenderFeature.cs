using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if URP
using UnityEngine.Rendering.Universal;

namespace QTool
{
	
	public class QRenderFeature: ScriptableRendererFeature
    {
		[System.Serializable]
		public class QSetting
		{
			[QName("材质")]
			public Material material;
			[QName("渲染顺序")]
			public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			public LayerMask OverrideLayer;
		}
		public class QRenderPass : ScriptableRenderPass
		{
			private static int QRenderPassTempTexture = Shader.PropertyToID(nameof(QRenderPass) + "_Temp" + nameof(Texture));
			private static RenderTargetIdentifier CameraColorTexture = new RenderTargetIdentifier(nameof(QShaderKey._CameraColorTexture));
			public QSetting setting;
			FilteringSettings filter;
			public QRenderPass(QSetting setting)
			{
				this.setting = setting;
				filter = new FilteringSettings(new RenderQueueRange { lowerBound = 2000, upperBound = 3500 }, setting.OverrideLayer);
			}
			public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
			{
				cmd.GetTemporaryRT(QRenderPassTempTexture, renderingData.cameraData.cameraTargetDescriptor, FilterMode.Point);
			}
			public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
			{
				base.Configure(cmd, cameraTextureDescriptor);
				if (setting.material != null)
				{
					var shaderTexId = Shader.PropertyToID(setting.material.name);
					cmd.GetTemporaryRT(shaderTexId, cameraTextureDescriptor);
					ConfigureTarget(shaderTexId);
					if(setting.OverrideLayer.value != 0)
					{
						ConfigureClear(ClearFlag.All, Color.black);
					}
				}
			}
			ShaderTagId shaderTag = new ShaderTagId("DepthOnly");
			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				CommandBuffer cmd = CommandBufferPool.Get(nameof(QRenderFeature) + "_" + nameof(CommandBuffer));
				if (setting.OverrideLayer.value == 0)
				{
					//if (renderingData.cameraData.cameraType == CameraType.Game)
					{
						if (setting.material == null)
						{
							cmd.Blit(CameraColorTexture, QRenderPassTempTexture);
							cmd.Blit(QRenderPassTempTexture, CameraColorTexture);
						}
						else
						{
							cmd.Blit(CameraColorTexture, QRenderPassTempTexture);
							cmd.Blit(QRenderPassTempTexture, CameraColorTexture, setting.material, 0);
						}
					}
					context.ExecuteCommandBuffer(cmd);
				}
				else
				{
					context.ExecuteCommandBuffer(cmd);
					var draw = CreateDrawingSettings(shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
					draw.overrideMaterial = setting.material;
					draw.overrideMaterialPassIndex = 0;
					context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);
				}
				CommandBufferPool.Release(cmd);
			}

			public override void OnCameraCleanup(CommandBuffer cmd)
			{
				cmd.ReleaseTemporaryRT(QRenderPassTempTexture);
			}
		}

	
        private QRenderPass Pass = null;
		[SerializeField]
		public QSetting setting = new QSetting();
		private void OnValidate()
		{
			if (setting != null && setting.material != null)
			{
				name = setting.material.name;
			}
		}
		public override void Create()
        {
			Pass = new QRenderPass(setting);
            Pass.renderPassEvent = setting.renderPassEvent;
        }
       
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(Pass); 
        }

    }
	public enum QShaderKey
	{
		_CameraColorTexture,
	}

}
#endif
