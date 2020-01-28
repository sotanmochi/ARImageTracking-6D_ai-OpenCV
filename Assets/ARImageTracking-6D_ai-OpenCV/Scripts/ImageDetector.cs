// Copyright (c) Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using OpenCVMarkerLessAR;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVMarkerLessAR_Extension
{
    [Serializable]
    public class ReferenceImage
    {
        public Texture2D Texture;
        public float ImageSizeScale;
    }

    public class ImageDetector : MonoBehaviour
    {
        public List<ReferenceImage> ReferenceImageList = new List<ReferenceImage>();
        public Camera ARCamera;
        public GameObject ARObjectPrefab;
        public Vector3 ARObjLocalRotEuler;

        private Dictionary<string, Texture2D> _TextureImages = new Dictionary<string, Texture2D>();
        private Dictionary<string, Pattern> _Patterns = new Dictionary<string, Pattern>();
        private Dictionary<string, PatternDetector> _PatternDetectors = new Dictionary<string, PatternDetector>();
        private Dictionary<string, GameObject> _ARObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, Matrix4x4> _ARObjectScaleMatrix = new Dictionary<string, Matrix4x4>();

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat _CamMatrix;

        /// <summary>
        /// The dist coeffs.
        /// </summary>
        MatOfDouble _DistCoeffs;

        /// <summary>
        /// The matrix that inverts the Y axis.
        /// </summary>
        Matrix4x4 _InvertYMat;

        /// <summary>
        /// The matrix that inverts the Z axis.
        /// </summary>
        Matrix4x4 _InvertZMat;

        public void Initialize(Mat inputImageMat)
        {
            InitializePatternDetector();
            InitializeMatrix();
            InitializeCameraMatrix(inputImageMat);
        }

        void InitializePatternDetector()
        {
            // Learning the feature points of the pattern image.
            foreach(ReferenceImage image in ReferenceImageList)
            {
                Texture2D patternTexture = image.Texture;

                Mat patternMat = new Mat(patternTexture.height, patternTexture.width, CvType.CV_8UC4);
                Utils.texture2DToMat(patternTexture, patternMat);

                Pattern pattern = new Pattern();
                PatternDetector patternDetector = new PatternDetector(null, null, null, true);

                patternDetector.buildPatternFromImage(patternMat, pattern);
                patternDetector.train(pattern);

                _Patterns[patternTexture.name] = pattern;
                _PatternDetectors[patternTexture.name] = patternDetector;

                _TextureImages[patternTexture.name] = patternTexture;
            }

            Debug.Log("**** _Patterns.Count @Initialize(): " + _Patterns.Count);
            Debug.Log("**** _PatternDetectors.Count @Initialize(): " + _PatternDetectors.Count);
        }

        void InitializeMatrix()
        {
            _InvertZMat = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
            Debug.Log ("_InvertZMat " + _InvertZMat.ToString ());

            _InvertYMat = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, -1, 1));
            Debug.Log ("_InvertYMat " + _InvertYMat.ToString ());

            foreach(ReferenceImage image in ReferenceImageList)
            {
                float scale = image.ImageSizeScale;
                Matrix4x4 scaleMat = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3 (scale, scale, scale));
                _ARObjectScaleMatrix[image.Texture.name] = scaleMat;
            }
        }

        void InitializeCameraMatrix(Mat inputImageMat)
        {
            Debug.Log ("******************************");

            float width = inputImageMat.width();
            float height = inputImageMat.height();

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }

            // Set camera param
            int max_d = (int)Mathf.Max (width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            _CamMatrix = new Mat (3, 3, CvType.CV_64FC1);
            _CamMatrix.put (0, 0, fx);
            _CamMatrix.put (0, 1, 0);
            _CamMatrix.put (0, 2, cx);
            _CamMatrix.put (1, 0, 0);
            _CamMatrix.put (1, 1, fy);
            _CamMatrix.put (1, 2, cy);
            _CamMatrix.put (2, 0, 0);
            _CamMatrix.put (2, 1, 0);
            _CamMatrix.put (2, 2, 1.0f);
            Debug.Log ("CamMatrix " + _CamMatrix.dump ());

            _DistCoeffs = new MatOfDouble (0, 0, 0, 0);
            Debug.Log ("DistCoeffs " + _DistCoeffs.dump ());

            // Calibration camera
            Size imageSize = new Size (width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point (0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(_CamMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log ("ImageSize " + imageSize.ToString ());
            Debug.Log ("ApertureWidth " + apertureWidth);
            Debug.Log ("ApertureHeight " + apertureHeight);
            Debug.Log ("Fovx " + fovx [0]);
            Debug.Log ("Fovy " + fovy [0]);
            Debug.Log ("FocalLength " + focalLength [0]);
            Debug.Log ("PrincipalPoint " + principalPoint.ToString ());
            Debug.Log ("Aspectratio " + aspectratio [0]);

            // To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan ((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2 ((float)cx, (float)fx) + Mathf.Atan2 ((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan ((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2 ((float)cy, (float)fy) + Mathf.Atan2 ((float)(imageSize.height - cy), (float)fy));

            Debug.Log ("FovXScale " + fovXScale);
            Debug.Log ("FovYScale " + fovYScale);

            // Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                ARCamera.fieldOfView = (float)(fovx [0] * fovXScale);
            }
            else
            {
                ARCamera.fieldOfView = (float)(fovy [0] * fovYScale);
            }

            Debug.Log ("******************************");
        }

        public void FindARMarker(Mat imgMat)
        {
            PatternTrackingInfo patternTrackingInfo = new PatternTrackingInfo();
            foreach(string patternName in _Patterns.Keys)
            {
                bool patternFound = _PatternDetectors[patternName].findPattern(imgMat, patternTrackingInfo);
                // Debug.Log ("PatternFound " + patternFound);

                if(patternFound)
                {
                    patternTrackingInfo.computePose(_Patterns[patternName], _CamMatrix, _DistCoeffs);

                    Matrix4x4 transformationM = patternTrackingInfo.pose3d; // Marker to Camera Coordinate System Convert Matrix

                    Matrix4x4 scaleMat = _ARObjectScaleMatrix[patternName];
                    Matrix4x4 ARM = ARCamera.transform.localToWorldMatrix * scaleMat * _InvertYMat * transformationM * _InvertZMat;

                    GameObject ARGameObject;
                    if (!_ARObjects.TryGetValue(patternName, out ARGameObject))
                    {
                        ARGameObject = GameObject.Instantiate(ARObjectPrefab, Vector3.zero, Quaternion.identity);
                        ARGameObject.name = ARObjectPrefab.name + "_" + patternName;
                        _ARObjects[patternName] = ARGameObject;

                        Material material = ARGameObject.GetComponentInChildren<MeshRenderer>().material;
                        material.mainTexture = _TextureImages[patternName];
                    }

                    ARUtils.SetTransformFromMatrix(ARGameObject.transform, ref ARM);
                    ARGameObject.transform.Rotate(ARObjLocalRotEuler);
                }
            }
        }
    }
}