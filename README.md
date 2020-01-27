# AR Image Tracking using 6D.ai and OpenCV

## Tested Environment
- Unity 2018.4.15f1
- XCode 11.3
- macOS Mojave (Version 10.14.6)
- iPad Pro (10.5 and 11 inch)

## License
このプロジェクトは、サードパーティのアセットを除き、MIT Licenseでライセンスされています。
This project is licensed under the MIT License excluding third party assets.

## Third party assets
以下のアセットをインポートする必要があります。  
You need to import the following assets.

- [6D.ai SDK v0.22.1](https://developer.6d.ai/user/dashboard/?view=release_notes&version=0221)
- [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088) 2.3.8+
- [MarkerLess AR Example](https://assetstore.unity.com/packages/templates/tutorials/markerless-ar-example-77560)

## How to setup 6D.ai SDK
1. unitypackageをインポートする
2. unitypackageには「GetAPI.mm」が含まれていないので、[Unity Sample App](https://developer.6d.ai/user/dashboard/?view=downloads_ios)から持ってくる
3. Assets/Plugins/iOS/SixDegreesSDK.plistを自分のAPIキーで更新する（API Key, Private Keyを置き換える）

## Build settings
- Allow unsafe code: True
- Target minimum iOS version: 11.4
- Camera Usage Description: Use Camera for Augmented Reality
