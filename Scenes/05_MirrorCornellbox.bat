..\GraphicTool.exe CreateImage ..\Data\05_MirrorCornellbox_json.txt -output 05_MirrorCornellbox.jpg -renderMod MediaVCM -sampleCount 10000 -width 1073 -height 940 -saveFolder AutoSave

rem ..\GraphicTool.exe CreateImage ..\Data\05_MirrorCornellbox_json.txt -output 05_MirrorCornellbox.raw -renderMod MediaVCM -sampleCount 10000 -width 1073 -height 940 -saveFolder AutoSave
rem ..\GraphicTool.exe RemoveFireFlys 05_MirrorCornellbox.raw -output 05_MirrorCornellboxNoFire.raw
rem ..\GraphicTool.exe Tonemapping 05_MirrorCornellbox.raw -output 05_MirrorCornellbox.jpg -method None
rem ..\GraphicTool.exe Tonemapping 05_MirrorCornellboxNoFire.raw -output 05_MirrorCornellboxNoFire.jpg -method None
rem del 05_MirrorCornellbox.raw
rem del 05_MirrorCornellboxNoFire.raw
