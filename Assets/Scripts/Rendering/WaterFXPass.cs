﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

public class WaterFXPass : ScriptableRenderPass
{
    const string k_RenderWaterFXTag = "Render Water FX";
    private RenderTargetHandle m_WaterFX = RenderTargetHandle.CameraTarget;

    public WaterFXPass(LightweightForwardRenderer renderer) : base(renderer)
    {
        RegisterShaderPassName("WaterFX");
        m_WaterFX.Init("_WaterFXMap");
	}

    public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_RenderWaterFXTag);

        RenderTextureDescriptor descriptor = renderer.CreateRTDesc(ref renderingData.cameraData);
        descriptor.width = (int)(descriptor.width * 0.5f);
        descriptor.height = (int)(descriptor.height * 0.5f);

        using (new ProfilingSample(cmd, k_RenderWaterFXTag))
        {
            cmd.GetTemporaryRT(m_WaterFX.id, descriptor, FilterMode.Bilinear);
            
            //m_WaterFXTexture.wrapMode = TextureWrapMode.Clamp;

            SetRenderTarget(
                cmd,
                m_WaterFX.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store,
                ClearFlag.Color,
                new Color(0.0f, 0.5f, 0.5f, 0.5f),
                descriptor.dimension);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = CreateDrawRendererSettings(renderingData.cameraData.camera, SortFlags.CommonTransparent, RendererConfiguration.None, renderingData.supportsDynamicBatching);
            if (renderingData.cameraData.isStereoEnabled)
            {
                Camera camera = renderingData.cameraData.camera;
                context.StartMultiEye(camera);
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.transparentFilterSettings);
                context.StopMultiEye(camera);
            }
            else
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.transparentFilterSettings);

        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void Dispose(CommandBuffer cmd)
    {
        if (m_WaterFX != RenderTargetHandle.CameraTarget)
        {
            cmd.ReleaseTemporaryRT(m_WaterFX.id);
        }
    }
}