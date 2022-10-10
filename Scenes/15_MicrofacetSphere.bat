..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\15_MicrofacetSphereBox_json.txt -output 15_MicrofacetSphere.jpg -renderMod FullBidirectionalPathTracing -sampleCount 1000 -width 1082 -height 944 -saveFolder AutoSave

rem ..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\15_MicrofacetSphereBox_json.txt -output 15_MicrofacetSphere.raw -renderMod FullBidirectionalPathTracing -sampleCount 1000 -width 1082 -height 944 -saveFolder AutoSave
rem ..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 15_MicrofacetSphere.raw -output 15_MicrofacetSphereNoFire.raw
rem ..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 15_MicrofacetSphereNoFire.raw -output 15_MicrofacetSphere.jpg -method None
rem del 15_MicrofacetSphere.raw
rem del 15_MicrofacetSphereNoFire.raw