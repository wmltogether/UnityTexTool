# UnityTexTool


Compress, decompress and convert Unity3D Texture2D files (unpacked from raw *.assets packs)
support formats such as the DXT1 & DXT5 ,ETC1/2,RGBA8888,ARGB4444,Alpha8.

## Usage:

1.  commands:
            "-I -info :show texture info only" 
            "-d -dump :dump texture" 
            "-c -compress :compress texture" 
            "-i -input <path> :input name" 
            "-o -output <path> :output png name" 
            "-r -resS <path> :use specific *.resS file path" 
            "-h :show help" 


2.  Unpack assets files using my  [unpacker-scripts](https://github.com/wmltogether/U3Unpacker) 
3.  dump texture from texture2D binary.

    for example

    UnityTexTool-x64.exe -d -i 00000000_test.tex -o test.png -r ./

4.  compress texture to texture2D binary.

    UnityTexTool-x64.exe -c -i test.png -o 00000000_test.tex.png -r ./

## Thanks to these softwares

TextureConverter.dll from Unity Editor 

Magick.NET-Q8-AnyCPU.dll from Magick.NET

PVRTexLibWrapper.dll £¬ ATI.TextureConverter.dll and Nvidia.TextureTools from mono game framework.

astcenc.exe from ARM-software/astc-encoder








