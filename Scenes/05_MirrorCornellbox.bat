rem ..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\05_MirrorCornellbox_json.txt -output 05_MirrorCornellbox.jpg -renderMod MediaFullBidirectionalPathTracing -sampleCount 1000 -width 1073 -height 940 -saveFolder AutoSave

..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\05_MirrorCornellbox_json.txt -output 05_MirrorCornellbox.raw -renderMod MediaFullBidirectionalPathTracing -sampleCount 1000 -width 1073 -height 940 -saveFolder AutoSave
..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 05_MirrorCornellbox.raw -output 05_MirrorCornellboxNoFire.raw
..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 05_MirrorCornellboxNoFire.raw -output 05_MirrorCornellbox.jpg -method None
del 05_MirrorCornellbox.raw
del 05_MirrorCornellboxNoFire.raw