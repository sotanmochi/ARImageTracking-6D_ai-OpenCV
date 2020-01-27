// Copyright (c) Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

namespace OpenCVMarkerLessAR_Extension
{
    public class WebCamImageDetector : MonoBehaviour
    {
        [SerializeField] ImageDetector _ImageDetector;
        [SerializeField] RawImage _UI_RawImage;
        [SerializeField] WebCamTextureToMatHelper _WebCamTextureToMatHelper;

        Mat _GrayMat;
        Texture2D _WebCamTexture;

        void Start()
        {
            _WebCamTextureToMatHelper.onInitialized.AddListener(OnWebCamTextureToMatHelperInitialized);
            _WebCamTextureToMatHelper.onDisposed.AddListener(OnWebCamTextureToMatHelperDisposed);
            _WebCamTextureToMatHelper.onErrorOccurred.AddListener(OnWebCamTextureToMatHelperErrorOccurred);

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            _WebCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
            #endif
            _WebCamTextureToMatHelper.Initialize();
        }

        void Update()
        {
            if (_WebCamTextureToMatHelper.IsPlaying () && _WebCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _WebCamTextureToMatHelper.GetMat ();
                Imgproc.cvtColor (rgbaMat, _GrayMat, Imgproc.COLOR_RGBA2GRAY);
                _ImageDetector.FindARMarker(_GrayMat);

                Utils.fastMatToTexture2D(rgbaMat, _WebCamTexture);
            }
        }

        public void OnWebCamTextureToMatHelperInitialized()
        {
            if (_WebCamTextureToMatHelper.GetWebCamDevice().isFrontFacing)
            {
                _WebCamTextureToMatHelper.flipHorizontal = true;
            }

            Mat webCamTextureMat = _WebCamTextureToMatHelper.GetMat();
            _GrayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

            _WebCamTexture = new Texture2D(webCamTextureMat.width(), webCamTextureMat.height(), TextureFormat.RGBA32, false);
            _UI_RawImage.texture = _WebCamTexture;

            _ImageDetector.Initialize(webCamTextureMat);
        }

        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (_GrayMat != null)
            {
                _GrayMat.Dispose();
            }
        }

        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }
    }
}
