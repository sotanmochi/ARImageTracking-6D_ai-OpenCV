// Copyright (c) Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using SixDegrees;

namespace OpenCVMarkerLessAR_Extension
{
    public class SixDegreesImageDetector : MonoBehaviour
    {
        [SerializeField] ImageDetector _ImageDetector;

        [SerializeField] Material _DebugMaterial;
        Texture2D _DebugTexture;

        Texture2D _ARBackgroundTexture;
        byte[] _NativeTextureData;
        Mat _GrayMat;
        Mat _RgbaMat;

        bool _Initialized = false;

        void Update()
        {
            if (SDPlugin.IsSDKReady)
            {
                if (!_Initialized)
                {
                    Initialize();
                    _Initialized = true;
                    Debug.Log("*** SixDegreesImageDetector is initialized ***");
                }
                else
                {
                    iOSPlugins.TextureExtension.GetNativePixels(_ARBackgroundTexture, ref _NativeTextureData);

                    ARBackgroundTextureToMat();

                    Imgproc.cvtColor(_RgbaMat, _GrayMat, Imgproc.COLOR_RGBA2GRAY);
                    _ImageDetector.FindARMarker(_GrayMat);

                    Utils.matToTexture2D(_RgbaMat, _DebugTexture, true);
                    _DebugMaterial.mainTexture = _DebugTexture;
                }
            }
        }

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif
        [DllImport(LIBNAME)]
        private static extern void OpenCVForUnity_ByteArrayToMatData(IntPtr byteArray, IntPtr Mat);

        void ARBackgroundTextureToMat()
        {
            GCHandle arrayHandle = GCHandle.Alloc(_NativeTextureData, GCHandleType.Pinned);
            OpenCVForUnity_ByteArrayToMatData(arrayHandle.AddrOfPinnedObject(), _RgbaMat.nativeObj);
            arrayHandle.Free();
        }

        void Initialize()
        {
            SetupARBackgroundTexture();

            _NativeTextureData = new byte[4 * _ARBackgroundTexture.width * _ARBackgroundTexture.height]; // RGBA32

            _GrayMat = new Mat(_ARBackgroundTexture.height, _ARBackgroundTexture.width, CvType.CV_8UC1);
            _RgbaMat = new Mat(_ARBackgroundTexture.height, _ARBackgroundTexture.width, CvType.CV_8UC4);

            _ImageDetector.Initialize(_RgbaMat);

            _DebugTexture = new Texture2D(_ARBackgroundTexture.width, _ARBackgroundTexture.height, TextureFormat.RGBA32, false);
        }

        void SetupARBackgroundTexture()
        {
            IntPtr texturePtr = IntPtr.Zero;

#if UNITY_IOS && !UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3) 
            {
                int textureId = SDPlugin.SixDegreesSDK_GetEAGLBackgroundTexture();
                texturePtr = new IntPtr(textureId);
            }
            else
#endif
            {
                texturePtr = SDPlugin.SixDegreesSDK_GetBackgroundTexture();
            }

            if (texturePtr != IntPtr.Zero)
            {
                int width = 1920;
                int height = 1080;
                unsafe
                {
                    int* widthPtr = &width, heightPtr = &height;
                    SDPlugin.SixDegreesSDK_GetBackgroundTextureSize(widthPtr, heightPtr);
                }

                Debug.Log("Create External Texture:" + texturePtr + "(" + width + "x" + height + ")");
                _ARBackgroundTexture = Texture2D.CreateExternalTexture(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false,
                    false,
                    texturePtr);
                _ARBackgroundTexture.filterMode = FilterMode.Point;
                _ARBackgroundTexture.name = "AR_Background_Texture";
            }
        }
    }
}
