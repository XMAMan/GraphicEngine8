..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\27_MirrorsEdge_json.txt -output 27_MirrorsEdge.raw -renderMod ThinMediaMultipleScattering -tonemapping ACESFilmicToneMappingCurve -sampleCount 10000 -width 1920 -height 1017 -saveFolder AutoSave
if exist 27_MirrorsEdge.raw ..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 27_MirrorsEdge.raw -output 27_MirrorsEdgeNoFire.raw
if exist 27_MirrorsEdgeNoFire.raw ..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 27_MirrorsEdgeNoFire.raw -output 27_MirrorsEdge.jpg -method ACESFilmicToneMappingCurve
if exist 27_MirrorsEdge.raw del 27_MirrorsEdge.raw
if exist 27_MirrorsEdgeNoFire.raw  del 27_MirrorsEdgeNoFire.raw