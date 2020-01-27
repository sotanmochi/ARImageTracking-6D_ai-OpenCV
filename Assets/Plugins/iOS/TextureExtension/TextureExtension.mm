// The orignal code is written by @HiiroHitoyo
// https://qiita.com/HiiroHitoyo/items/4e9b678f645220863f22

#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

extern "C" {
    void iOS_GetNativePixels(id<MTLTexture> texture, int width, int height, int sizePerRow, char *buffer)
    {
        [texture getBytes:buffer bytesPerRow:sizePerRow fromRegion:MTLRegionMake2D(0, 0, width, height) mipmapLevel:0];
    }
}
