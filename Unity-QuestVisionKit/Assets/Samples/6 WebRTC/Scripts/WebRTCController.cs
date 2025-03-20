using PassthroughCameraSamples;
using SimpleWebRTC;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QuestCameraKit.WebRTC {
    public class WebRTCController : MonoBehaviour {

        [SerializeField] private WebCamTextureManager passthroughCameraManager;
        [SerializeField] private RawImage canvasRawImage;
        [SerializeField] private WebRTCConnection webRTCConnection;

        private WebCamTexture _webcamTexture;

        private IEnumerator Start() {
            yield return new WaitUntil(() => passthroughCameraManager.WebCamTexture != null && passthroughCameraManager.WebCamTexture.isPlaying);

            _webcamTexture = passthroughCameraManager.WebCamTexture;
            canvasRawImage.texture = _webcamTexture;
        }

        private void Update() {
            if (OVRInput.Get(OVRInput.Button.Start)) {
                webRTCConnection.StartVideoTransmission();
            }
        }
    }
}