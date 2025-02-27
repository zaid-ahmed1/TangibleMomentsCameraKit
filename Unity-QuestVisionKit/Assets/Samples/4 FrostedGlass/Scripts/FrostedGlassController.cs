using System.Collections;
using UnityEngine;
using PassthroughCameraSamples;

public class FrostedGlassController : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager passthroughCameraManager;
    [SerializeField] private Material mappingBlurMaterial;
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private int kernelSize = 3;
    [SerializeField] private int kernelStep = 1;

    private WebCamTexture _webcamTexture;
    // Flag to disable update if camera ID is not found.
    private bool cameraFound = true;

    // Cache shader property IDs for performance.
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int KernelSizeId = Shader.PropertyToID("_KernelSize");
    private static readonly int KernelStepId = Shader.PropertyToID("_KernelStep");
    private static readonly int TextureSizeId = Shader.PropertyToID("_TextureSize");
    private static readonly int CameraPosId = Shader.PropertyToID("_CameraPos");
    private static readonly int CameraRotationMatrixId = Shader.PropertyToID("_CameraRotationMatrix");
    private static readonly int FocalLengthId = Shader.PropertyToID("_FocalLength");
    private static readonly int PrincipalPointId = Shader.PropertyToID("_PrincipalPoint");

    private void Start()
    {
        Debug.Log("FrostedGlassController: Starting, waiting for webcam texture.");
        StartCoroutine(WaitForWebcamTexture());
    }

    private IEnumerator WaitForWebcamTexture()
    {
        while (passthroughCameraManager.WebCamTexture == null || !passthroughCameraManager.WebCamTexture.isPlaying)
        {
            Debug.Log("FrostedGlassController: Waiting for webcam texture to start...");
            yield return null;
        }
        
        _webcamTexture = passthroughCameraManager.WebCamTexture;
        mappingBlurMaterial.SetTexture(MainTexId, _webcamTexture);
        mappingBlurMaterial.SetColor(TintColorId, tintColor);
        mappingBlurMaterial.SetInt(KernelSizeId, kernelSize);
        mappingBlurMaterial.SetInt(KernelStepId, kernelStep);
        Debug.LogFormat("FrostedGlassController: Webcam texture assigned. Resolution: {0}x{1}, Device: {2}",
                        _webcamTexture.width, _webcamTexture.height, _webcamTexture.deviceName);
    }

    private void Update()
    {
        if (_webcamTexture == null || !cameraFound)
        {
            return;
        }

        // Update texture size (used to normalize UVs in the shader).
        Vector2 texSize = new Vector2(_webcamTexture.width, _webcamTexture.height);
        mappingBlurMaterial.SetVector(TextureSizeId, texSize);

        try
        {
            // Retrieve the current camera pose.
            Pose cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(passthroughCameraManager.eye);
            mappingBlurMaterial.SetVector(CameraPosId, cameraPose.position);

            // Compute inverse rotation matrix (world → camera space).
            Matrix4x4 invRotMatrix = Matrix4x4.Rotate(Quaternion.Inverse(cameraPose.rotation));
            mappingBlurMaterial.SetMatrix(CameraRotationMatrixId, invRotMatrix);

            // Retrieve camera intrinsics.
            PassthroughCameraIntrinsics intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(passthroughCameraManager.eye);
            mappingBlurMaterial.SetVector(FocalLengthId, intrinsics.FocalLength);
            mappingBlurMaterial.SetVector(PrincipalPointId, intrinsics.PrincipalPoint);

            // Optional: log values for debugging.
            Debug.LogFormat("FrostedGlassController Update: TextureSize: {0}", texSize);
            Debug.LogFormat("FrostedGlassController Update: CameraPos: {0}", cameraPose.position);
            Debug.LogFormat("FrostedGlassController Update: InvRotMatrix: {0}", invRotMatrix);
            Debug.LogFormat("FrostedGlassController Update: FocalLength: {0}, PrincipalPoint: {1}", 
                              intrinsics.FocalLength, intrinsics.PrincipalPoint);
        }
        catch (System.ApplicationException ex)
        {
            Debug.LogError("FrostedGlassController: " + ex.Message);
            // Disable further updates if the camera ID (and thus the camera pose/intrinsics) cannot be found.
            cameraFound = false;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_webcamTexture == null)
        {
            Debug.LogWarning("FrostedGlassController: OnRenderImage - Webcam texture is null; passing through source.");
            Graphics.Blit(source, destination);
            return;
        }
        
        Graphics.Blit(_webcamTexture, destination, mappingBlurMaterial);
    }
}
