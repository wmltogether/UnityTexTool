using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityTexTool.UnityEngine;
using System.IO;
using ImageMagick;
using System.Runtime.InteropServices;

namespace UnityTexTool
{
    public class AppMsg
    {
        public static string msg =
            "======Support Format====\n" +
            "Alpha8, ARGB4444, RGB565, RGBA8888, DXT1, DXT3, ETC1, ETC2_RGB, ETC2_RGBA8,\n"+
            "========================\n" +
            " Usage:\n" +
            "-I -info :show texture info only\n" +
            "-d -dump :dump texture\n" +
            "-c -compress :compress texture\n" +
            "-i -input <path> :input name\n" +
            "-o -output <path> :output png name\n" +
            "-r -resS <path> :use specific *.resS file path\n" +
            "\n";
    }
    class Program
    {
        

        [DllImport("user32.dll", EntryPoint = "MessageBox")]
        public static extern int MsgBox(IntPtr hwnd, string text, string caption, uint type);
        public static void ShowMsgBox(string msg)
        {
            MsgBox(IntPtr.Zero, msg, "UnityTexTool", 1);
        }

        static void AddEnvironmentPaths()
        {
            System.Environment.SetEnvironmentVariable("PATH", System.IO.Path.Combine(Environment.CurrentDirectory, @"Library\64bit") + ";"
                                                            , EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("PATH", System.IO.Path.Combine(Environment.CurrentDirectory, @"Library\tool") + ";"
                                                            , EnvironmentVariableTarget.Process);
        }

        private static void ShowArgsMsg()
        {
            
            Console.WriteLine(AppMsg.msg);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_name">png name</param>
        /// <param name="output_name">tex2D name</param>
        private static void PNG2Texture(string input_name, string output_name, string resSFilePath = "./")
        {
            
            byte[] dstTex2D = File.ReadAllBytes(output_name);
            Texture2D texture;
            try
            {
                texture = new Texture2D(dstTex2D);
                if (texture.isTexture2D == false)
                {
                    return;
                }
                if (texture.format == TextureFormat.Alpha8 ||
                texture.format == TextureFormat.ARGB4444 ||
                texture.format == TextureFormat.RGBA4444 ||
                texture.format == TextureFormat.RGBA32 ||
                texture.format == TextureFormat.ARGB32 ||
                texture.format == TextureFormat.RGB24 ||
                texture.format == TextureFormat.RGB565 ||
                texture.format == TextureFormat.ETC_RGB4 ||
                texture.format == TextureFormat.ETC2_RGB ||
                texture.format == TextureFormat.ETC2_RGBA8 ||
                texture.format == TextureFormat.DXT5 ||
                texture.format == TextureFormat.DXT1 ||
                texture.format == TextureFormat.ASTC_RGBA_4x4 ||
                texture.format == TextureFormat.ASTC_RGB_4x4)
                {
                    ImageMagick.MagickImage im = new MagickImage(input_name);
                    if ((im.Width != texture.width) || im.Height != texture.height)
                    {
                        Console.WriteLine("Error: texture is {0} x {1} ,but png bitmap is {2} x {3}.Exit.",
                                            texture.width, texture.height,
                                            im.Width, im.Height);
                        return;
                    }
                    im.Flip();
                    byte[] sourceData = im.GetPixels().ToByteArray(0, 0, im.Width, im.Height, "RGBA");
                    byte[] outputData;
                    Console.WriteLine("Reading:{0}\n Width:{1}\n Height:{2}\n Format:{3}\n", input_name, im.Width, im.Height, texture.format.ToString());
                    Console.WriteLine("Converting...");
                    TextureConverter.CompressTexture(texture.format, im.Width, im.Height, sourceData, out outputData);
                    if (texture.bMipmap && texture.mipmapCount >= 3)
                    {
                        Console.WriteLine("Generating Mipmap...");
                        for (var m = 0; m <= 3; m++)
                        {

                            im.AdaptiveResize(im.Width / 2, im.Height / 2);
                            Console.WriteLine("Generating ...{0}x{1}", im.Width, im.Height);
                            sourceData = im.GetPixels().ToByteArray(0, 0, im.Width, im.Height, "RGBA");
                            byte[] dst;
                            TextureConverter.CompressTexture(texture.format, im.Width, im.Height, sourceData, out dst);
                            outputData = outputData.Concat(dst).ToArray();
                        }
                    }
                    if (outputData != null)
                    {
                        if ((outputData.Length >= texture.textureSize))
                        {
                            Console.WriteLine("Error: generated data size {0}> original texture size {1}. Exit.", outputData.Length, texture.textureSize);
                        }
                        if (texture.bHasResSData == true)
                        {
                            output_name = string.Format("{0}/{1}", resSFilePath, texture.resSName);

                        }
                        if (File.Exists(output_name))
                        {
                            Console.WriteLine("Writing...{0}", output_name);
                            using (FileStream fs = File.Open(output_name, FileMode.Open, FileAccess.ReadWrite))
                            {
                                fs.Seek(texture.dataPos, SeekOrigin.Begin);
                                fs.Write(outputData, 0, outputData.Length);
                                fs.Flush();
                            }
                            Console.WriteLine("File Created...");
                        }
                        else
                        {
                            Console.WriteLine("Error: file {0} not found", output_name);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: generated data size {0}> original texture size {1}. Exit.", outputData.Length, texture.textureSize);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:Not A valid Texture: {0}", ex.ToString());
            }
            
            




        }

        private static void Texture2PNG(string input_name, string output_name, string resSFilePath = "")
        {
            byte[] input = File.ReadAllBytes(input_name);
            Texture2D texture = new Texture2D(input, resSFilePath);
            if (texture.isTexture2D == false || texture.GetPixels() == null)
            {
                return;
            }

            Console.WriteLine("Reading: {0}\n Width: {1}\n Height: {2}\n Format: {3}\n Dimension: {4}\n Filter Mode: {5}\n Wrap Mode: {6}\n Mipmap: {7}",
                                input_name,
                                texture.width,
                                texture.height,
                                texture.format.ToString(),
                                texture.dimension.ToString(),
                                texture.filterMode.ToString(),
                                texture.wrapMode.ToString(),
                                texture.bMipmap);

            if (texture.format == TextureFormat.Alpha8 ||
                texture.format == TextureFormat.ARGB4444 ||
                texture.format == TextureFormat.RGBA4444 ||
                texture.format == TextureFormat.RGBA32 ||
                texture.format == TextureFormat.ARGB32 ||
                texture.format == TextureFormat.RGB24||
                texture.format == TextureFormat.RGB565 ||
                texture.format == TextureFormat.ETC_RGB4 ||
                texture.format == TextureFormat.ETC2_RGB ||
                texture.format == TextureFormat.ETC2_RGBA8 ||
                texture.format == TextureFormat.DXT5 ||
                texture.format == TextureFormat.DXT1 ||
                texture.format == TextureFormat.ASTC_RGBA_4x4 ||
                texture.format == TextureFormat.ASTC_RGB_4x4
                )
            {
                MagickReadSettings settings = new MagickReadSettings();
                settings.Format = MagickFormat.Rgba;
                settings.Width = texture.width;
                settings.Height = texture.height;
                
                ImageMagick.MagickImage im = new MagickImage(texture.GetPixels(), settings);
                im.Flip();//unity纹理是颠倒放置，要flip
                im.ToBitmap().Save(output_name);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Unity Texture2D Dump tool. \nCreated by wmltogether --20161225");
            
            AddEnvironmentPaths();
            //TextureConverter.Test();
            
            string filename = null;
            string outputName = null;
            string resSFilePath = null; //resS数据包存储路径
            bool bDecompress = false;
            bool bCompress = false;
            bool bShowInfo = false;
            bool bShowHelp = false;

            if (args.Length == 0)
            {
                ShowArgsMsg();
                Program.ShowMsgBox(string.Format("Error: no args \n  Please use this program in console!\n"));

                return;
            }
            #region check args
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-"))
                    {
                        switch (args[i].TrimStart('-'))
                        {
                            case "h":
                            case "help":
                                bShowHelp = true;
                                break;
                            case "I":
                            case "info":
                                bShowInfo = true;
                                break;
                            case "d":
                            case "dump":
                                bDecompress = true;
                                break;
                            case "o":
                            case "output":
                                outputName = args[++i];
                                break;
                            case "c":
                            case "compress":
                                bCompress = true;
                                break;
                            case "i":
                            case "input":
                                filename = args[++i];
                                break;
                            case "r":
                            case "resS":
                                resSFilePath = args[++i];

                                break;

                        }
                    }
                }

            }
            #endregion
            if (bShowHelp)
            {
                ShowArgsMsg();
                return;
            }
            if (bShowInfo && (filename != null))
            {
                try
                {
                    Texture2D texture = new Texture2D(File.ReadAllBytes(filename), resSFilePath);
                    if (texture.isTexture2D == false)
                    {
                        return;
                    }
                    Console.WriteLine("Reading: {0}\n Width: {1}\n Height: {2}\n Format: {3}\n Dimension: {4}\n Filter Mode: {5}\n " +
                                        "Wrap Mode: {6}\n Mipmap: {7}\n ResS Type : {8}\n Data Offset: {9:X8}",
                                        filename,
                                        texture.width,
                                        texture.height,
                                        texture.format.ToString(),
                                        texture.dimension.ToString(),
                                        texture.filterMode.ToString(),
                                        texture.wrapMode.ToString(),
                                        texture.bMipmap,
                                        texture.bHasResSData,
                                        texture.dataPos);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return;
            }
            if (filename == outputName)
            {
                Console.WriteLine("Error: can't overwrite input file");
                return;
            }
            if ((filename != null) && (outputName != null))
            {
                if (bDecompress) Texture2PNG(filename, outputName, resSFilePath);
                if (bCompress) PNG2Texture(filename, outputName, resSFilePath);
            }
            

        }
    }
}
