﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImageMagick;
using PVRTexLibNET;
using Nvidia.TextureTools;
using ATI.TextureConverter;
using System.Runtime.InteropServices;

namespace UnityTexTool.UnityEngine
{
    
    public class TextureConverter
    {
        
        
        /// <summary>
        /// Compress Texture from RGBA32 to dest byte arrays
        /// </summary>
        /// <param name="format">Texture2D TextureFormat</param>
        /// <param name="width">Texture2D width</param>
        /// <param name="height">Texture2D height</param>
        /// <param name="sourceData">RGBA32 source data 8bit per channel {R8:G8:B8:A8}</param>
        /// <param name="output">dest byte arrays</param>
        /// <returns></returns>
        public static bool CompressTexture(TextureFormat format, int width, int height, byte[] sourceData ,out byte[] output, bool bMipmapout = false)
        {
            bool result = false;
            int pos;
            int outPos;
            output = new byte[] { };
            switch (format)
            {
                case TextureFormat.Alpha8:
                    output = new byte[width * height];
                    pos = 0;
                    outPos = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var fR = sourceData[pos];
                            var fG = sourceData[pos + 1];
                            var fB = sourceData[pos + 2];
                            var fA = sourceData[pos + 3];
                            output[outPos] = (byte)(((fR + fB + fG) / 3) & 0XFF);
                            pos += 4;
                            outPos += 1;

                        }
                    }
                    break;
                case TextureFormat.ARGB4444:
                    output = new byte[width * height * 2];
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(sourceData, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.RGBA8888, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        bool bDoDither = true;//降色抖动算法
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA4444, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, bDoDither);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize); // texData 是 {A ,B , G, R}结构的像素字节数组，和传入的相反

                        output = new byte[texDataSize];
                        // Unity 的ARGB4444需要的是｛B , G, R, A｝的数组，所以需要交换通道
                        pos = 0;
                        outPos = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                var v0 = texData[pos];
                                var v1 = texData[pos + 1];
                                // 4bit little endian {A, B},{G, R}
                                var sA = v0 & 0xF0 >> 4;
                                var sB = (v0 & 0xF0) >> 4;
                                var sG = v1 & 0xF0 >> 4;
                                var sR = (v1 & 0xF0) >> 4;
                                // swap to little endian {B, G, R, A }
                                var fB = sB;
                                var fG = sG;
                                var fR = sR;
                                var fA = sA;
                                output[outPos] = (byte)((fB << 4) + fG);
                                output[outPos + 1] = (byte)((fR << 4) + fA);
                                pos += 2;
                                outPos += 2;

                            }
                        }
                        result = true;
                    }
                    break;
                case TextureFormat.RGB565:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(sourceData, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.RGBA8888, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        bool bDoDither = true;
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGB565, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.ETCMedium, bDoDither);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = new byte[texDataSize];
                    }
                    break;
                case TextureFormat.RGBA32:
                    output = sourceData;
                    break;

                case TextureFormat.ATC_RGBA8:
                    output = ATICompressor.Compress(sourceData, width, height, ATICompressor.CompressionFormat.AtcRgbaExplicitAlpha);
                    break;
                case TextureFormat.ATC_RGB4:
                    output = ATICompressor.Compress(sourceData, width, height, ATICompressor.CompressionFormat.AtcRgb);
                    break;

                case TextureFormat.ETC2_RGBA8:
                    output = ATICompressor.Compress(sourceData, width, height, ATICompressor.CompressionFormat.Etc2Rgba);
                    break;
                case TextureFormat.ETC_RGB4:
                    output = ATICompressor.Compress(sourceData, width, height, ATICompressor.CompressionFormat.Etc1);
                    break;
                case TextureFormat.DXT5:
                    using (DxtCompressor compressor = new DxtCompressor())
                    {
                        result = compressor.Compress(sourceData, width, height, Nvidia.TextureTools.Format.DXT5, out output);
                    }
                    
                    break;
                case TextureFormat.DXT1:
                    using (DxtCompressor compressor = new DxtCompressor())
                    {
                        result = compressor.Compress(sourceData, width, height, Nvidia.TextureTools.Format.DXT1, out output);
                    }
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
        }


        public static bool DecompressTexture(TextureFormat format , int width, int height, byte[] input, out byte[] output)
        {
            bool result = false;
            output = new byte[width * height * 4];
            int pos;
            int outPos;
            switch (format)
            {
                case TextureFormat.Alpha8:
                    pos = 0;
                    outPos = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var fA = input[pos];
                            output[outPos] = (byte)fA;
                            output[outPos + 1] = (byte)fA;
                            output[outPos + 2] = (byte)fA;
                            output[outPos + 3] = (byte)0xff;
                            pos += 1;
                            outPos += 4;
                        }
                    }
                    break;
                case TextureFormat.ARGB4444:
                    pos = 0;
                    outPos = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var v0 = input[pos];
                            var v1 = input[pos + 1];
                            // 4bit little endian {B, G}, {R, A }
                            // 16BIT位图 每个颜色占4bit 比如 一个 hex F1F2 > 0xF2F1
                            // B, G, R, A = 01, 0F, 02 ,0F
                            // 则 A , R, ,G, B = 0F, 02, 0F ,01
                            // 转换回RGBA8888 则是 02 0F 01 0F
                            var fB = v0 & 0xF0 >> 4; //低四位
                            var fG = (v0 & 0xF0) >> 4;//高四位
                            var fR = v1 & 0xF0 >> 4;//低四位
                            var fA = (v1 & 0xF0) >> 4;//高四位
                            /*var fA = v0 & 0xF0 >> 4;
                            var fB = (v0 & 0xF0) >> 4;
                            var fG = v1 & 0xF0 >> 4;
                            var fR = (v1 & 0xF0) >> 4;*/


                            fA = (fA * 255 + 7) / 15;
                            fR = (fR * 255 + 7) / 15;
                            fG = (fG * 255 + 7) / 15;
                            fB = (fB * 255 + 7) / 15;

                            output[outPos] = (byte)fR;
                            output[outPos + 1] = (byte)fG;
                            output[outPos + 2] = (byte)fB;
                            output[outPos + 3] = (byte)fA;

                            pos += (2);
                            outPos += 4;

                        }
                    }
                    
                    break;
                case TextureFormat.RGBA32:
                    pos = 0;
                    outPos = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var fR = input[pos];
                            var fG = input[pos + 1];
                            var fB = input[pos + 2];
                            var fA = input[pos + 3];

                            output[outPos] = fR;
                            output[outPos + 1] = fG;
                            output[outPos + 2] = fB;
                            output[outPos + 3] = fA;

                            pos += 4;
                            outPos += 4;

                        }
                    }

                    break;
                case TextureFormat.RGB565:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.RGB565, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;

                    }
                    break;
                case TextureFormat.ETC2_RGBA8:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.ETC2_RGBA, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;

                    }
                    break;
                case TextureFormat.ETC2_RGBA1:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.ETC2_RGB_A1, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;
                    }
                    break;

                case TextureFormat.ETC_RGB4:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.ETC1, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;
                    }
                    break;

                case TextureFormat.DXT1:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.DXT1, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;
                    }
                    break;

                case TextureFormat.DXT5:
                    using (var pvrTexture = PVRTexLibNET.PVRTexture.CreateTexture(input, (uint)width, (uint)height, 1, PVRTexLibNET.PixelFormat.DXT5, true, VariableType.UnsignedByte, ColourSpace.sRGB))
                    {
                        pvrTexture.Transcode(PVRTexLibNET.PixelFormat.RGBA8888, VariableType.UnsignedByte, ColourSpace.sRGB, CompressorQuality.PVRTCNormal, false);
                        var texDataSize = pvrTexture.GetTextureDataSize(0);
                        var texData = new byte[texDataSize];
                        pvrTexture.GetTextureData(texData, texDataSize);
                        output = texData;
                    }
                    break;

                default:
                    result = false;
                    break;
            }
            return result;
        }
    }

    
}