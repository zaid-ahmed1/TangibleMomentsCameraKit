using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class GaussianBlurFeature : ScriptableRendererFeature
{
    [Range(1, 1000)]
    public int kernelSize = 1;

    [Range(1, 10)]
    public int kernelStep = 1;

    public Color tintColor = Color.white;

    public Material blurMaterial;

    class GaussianBlurPass : ScriptableRenderPass
    {
        int kernelSize;
        int kernelStep;
        Color tintColor;
        Material blurMaterial;

        public GaussianBlurPass(int kernelSize, int kernelStep, Color tintColor, Material blurMaterial)
        {
            this.kernelSize = kernelSize;
            this.kernelStep = kernelStep;
            this.tintColor = tintColor;
            this.blurMaterial = blurMaterial;
        }

        private class CopyPassData
        {
            public TextureHandle sourceTexture;
        }

        private class BlurPassData
        {
            public TextureHandle sourceTexture;
            public Material blurMaterial;
            public RendererListHandle rendererList;
            public string name;
        }

        private static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;

            return descriptor;
        }

        static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source)
        {
            //Blitter.BlitCameraTexture(cmd, source, destination)
            Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void DrawBlurRenderers(BlurPassData data, RasterGraphContext context)
        {
            data.blurMaterial.SetTexture(data.name, data.sourceTexture);
            context.cmd.DrawRendererList(data.rendererList);
        }

        public RendererListHandle GetRendererList(RenderGraph renderGraph, ContextContainer frameData, string lightMode, Material material, int passIndex)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var cullingResults = renderingData.cullResults;

            RenderQueueRange renderQueueRange = RenderQueueRange.all;
            FilteringSettings filteringSettings =
                new FilteringSettings(renderQueueRange);

            ShaderTagId shaderTagId = new ShaderTagId(lightMode);

            var sortFlags = cameraData.defaultOpaqueSortFlags;

            var drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, renderingData, cameraData, lightData, sortFlags);
            drawingSettings.overrideMaterial = material;
            drawingSettings.overrideMaterialPassIndex = passIndex;

            RendererListParams rendererParams = new RendererListParams(cullingResults, drawingSettings, filteringSettings);
            return renderGraph.CreateRendererList(in rendererParams);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            profilingSampler = new ProfilingSampler("Gaussian Blur");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (cameraData.isPreviewCamera)
            {
                return;
            }

            if (blurMaterial == null)
            {
                return;
            }

            var descriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle horizontalHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_GaussianBlur_Horizontal", false);
            TextureHandle verticleHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_GaussianBlur_Vertical", false);

            blurMaterial.SetInteger("_KernelSize", kernelSize);
            blurMaterial.SetInteger("_KernelStep", kernelStep);
            blurMaterial.SetColor("_TintColor", tintColor);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("GaussianBlur_Copy", out var passData, profilingSampler))
            {
                passData.sourceTexture = resourceData.activeColorTexture;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(horizontalHandle, 0, AccessFlags.Write);

                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, passData.sourceTexture));
            }

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>("GaussianBlur_Horizontal", out var passData, profilingSampler))
            {
                passData.sourceTexture = horizontalHandle;
                passData.name = "_GaussianBlur_Horizontal";
                passData.blurMaterial = blurMaterial;
                passData.rendererList = GetRendererList(renderGraph, frameData, "GaussianBlur", blurMaterial, 0);

                builder.UseRendererList(passData.rendererList);
                builder.UseTexture(horizontalHandle, AccessFlags.Read);
                builder.SetRenderAttachment(verticleHandle, 0, AccessFlags.Write);

                builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => DrawBlurRenderers(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>("GaussianBlur_Vertical", out var passData, profilingSampler))
            {
                passData.sourceTexture = verticleHandle;
                passData.name = "_GaussianBlur_Vertical";
                passData.blurMaterial = blurMaterial;
                passData.rendererList = GetRendererList(renderGraph, frameData, "GaussianBlur", blurMaterial, 1);

                builder.UseRendererList(passData.rendererList);
                builder.UseTexture(verticleHandle, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => DrawBlurRenderers(data, context));
            }
        }
    }

    GaussianBlurPass m_GaussianBlurPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_GaussianBlurPass = new GaussianBlurPass(kernelSize, kernelStep, tintColor, blurMaterial);

        // Configures where the render pass should be injected.
        m_GaussianBlurPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    { 
        renderer.EnqueuePass(m_GaussianBlurPass);
    }
}
