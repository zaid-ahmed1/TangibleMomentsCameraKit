using UnityEngine;
using System.Collections;
using PassthroughCameraSamples;

public class FrostedGlassController : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager passthroughCameraManager;
    [SerializeField] private Material mappingBlurMaterial;
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private int kernelSize = 3;
    [SerializeField] private int kernelStep = 1;

    private WebCamTexture _webcamTexture;
    private bool _cameraFound = true;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int CameraPosId = Shader.PropertyToID("_CameraPos");
    private static readonly int KernelSizeId = Shader.PropertyToID("_KernelSize");
    private static readonly int KernelStepId = Shader.PropertyToID("_KernelStep");
    private static readonly int TextureSizeId = Shader.PropertyToID("_TextureSize");
    private static readonly int FocalLengthId = Shader.PropertyToID("_FocalLength");
    private static readonly int PrincipalPointId = Shader.PropertyToID("_PrincipalPoint");
    private static readonly int IntrinsicResolutionId = Shader.PropertyToID("_IntrinsicResolution");
    private static readonly int CameraRotationMatrixId = Shader.PropertyToID("_CameraRotationMatrix");

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => passthroughCameraManager.WebCamTexture != null && passthroughCameraManager.WebCamTexture.isPlaying);

        _webcamTexture = passthroughCameraManager.WebCamTexture;
        mappingBlurMaterial.SetTexture(MainTexId, _webcamTexture);
        mappingBlurMaterial.SetColor(TintColorId, tintColor);
        mappingBlurMaterial.SetInt(KernelSizeId, kernelSize);
        mappingBlurMaterial.SetInt(KernelStepId, kernelStep);

        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(passthroughCameraManager.eye);
        mappingBlurMaterial.SetVector(IntrinsicResolutionId, new Vector4(intrinsics.Resolution.x, intrinsics.Resolution.y, 0, 0));
    }

    private void Update()
    {
        if (!_webcamTexture || !_cameraFound)
        {
            return;
        }

        var texSize = new Vector2(_webcamTexture.width, _webcamTexture.height);
        mappingBlurMaterial.SetVector(TextureSizeId, texSize);

        try
        {
            var camPose = PassthroughCameraUtils.GetCameraPoseInWorld(passthroughCameraManager.eye);
            mappingBlurMaterial.SetVector(CameraPosId, camPose.position);
            mappingBlurMaterial.SetMatrix(CameraRotationMatrixId, Matrix4x4.Rotate(Quaternion.Inverse(camPose.rotation)));

            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(passthroughCameraManager.eye);
            mappingBlurMaterial.SetVector(FocalLengthId, intrinsics.FocalLength);
            mappingBlurMaterial.SetVector(PrincipalPointId, intrinsics.PrincipalPoint);
            mappingBlurMaterial.SetVector(IntrinsicResolutionId, new Vector4(intrinsics.Resolution.x, intrinsics.Resolution.y, 0, 0));
        }
        catch (System.ApplicationException)
        {
            _cameraFound = false;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_webcamTexture == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        Graphics.Blit(_webcamTexture, destination, mappingBlurMaterial);
    }
}
