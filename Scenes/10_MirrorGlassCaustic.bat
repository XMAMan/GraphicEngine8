rem ..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\10_MirrorGlassCaustic_json.txt -output 10_MirrorGlassCaustic.jpg -renderMod UPBP -sampleCount 1000 -width 1218 -height 957 -pixelRange [0;30;1217;630] -saveFolder AutoSave

..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\10_MirrorGlassCaustic_json.txt -output 10_MirrorGlassCaustic.raw -renderMod UPBP -sampleCount 1000 -width 1218 -height 957 -pixelRange [0;30;1217;630] -saveFolder AutoSave
..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 10_MirrorGlassCaustic.raw -output 10_MirrorGlassCausticNoFire.raw
..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 10_MirrorGlassCausticNoFire.raw -output 10_MirrorGlassCaustic.jpg -method None
del 10_MirrorGlassCaustic.raw
del 10_MirrorGlassCausticNoFire.raw