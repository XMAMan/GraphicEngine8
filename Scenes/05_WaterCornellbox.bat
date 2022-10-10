..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\05_WaterCornellbox_json.txt -output 05_WaterCornellbox.jpg -renderMod MediaFullBidirectionalPathTracing -sampleCount 1000 -width 971 -height 952 -saveFolder AutoSave

rem ..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\05_WaterCornellbox_json.txt -output 05_WaterCornellbox.raw -renderMod MediaFullBidirectionalPathTracing -sampleCount 1000 -width 971 -height 952 -saveFolder AutoSave
rem ..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 05_WaterCornellbox.raw -output 05_WaterCornellboxNoFire.raw
rem ..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 05_WaterCornellboxNoFire.raw -output 05_WaterCornellbox.jpg -method None
rem del 05_WaterCornellbox.raw
rem del 05_WaterCornellboxNoFire.raw