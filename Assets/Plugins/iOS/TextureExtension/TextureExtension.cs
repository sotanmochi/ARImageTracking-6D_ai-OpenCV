// The orignal code is written by @HiiroHitoyo
// https://qiita.com/HiiroHitoyo/items/4e9b678f645220863f22

using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace iOSPlugins
{
    public class TextureExtension
    { 
        [DllImport("__Internal")]
        public static extern void iOS_GetNativePixels(IntPtr texturePtr, int width, int height, int sizePerRow, [Out] byte[] buffer);

        public static void GetNativePixels(Texture2D texture, ref byte[] buffer)
        {
            IntPtr texturePtr = texture.GetNativeTexturePtr();
            int width = texture.width;
            int height = texture.height;
            int sizeParRow = texture.width * 4; // Texture format is RGBA32
            iOS_GetNativePixels(texturePtr, width, height, sizeParRow, buffer);
        }
    }
}
