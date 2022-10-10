..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\06_ChinaRoom_json.txt -output 06_ChinaRoom.raw -renderMod VertexConnectionMerging -tonemapping ACESFilmicToneMappingCurve -sampleCount 50000 -width 1536 -height 801 -saveFolder AutoSave
if exist 06_ChinaRoom.raw ..\Source\Tools\bin\Debug\GraphicTool.exe RemoveFireFlys 06_ChinaRoom.raw -output 06_ChinaRoomNoFire.raw -searchMask ..\Data\06_ChinaFireMask.png
if exist 06_ChinaRoomNoFire.raw ..\Source\Tools\bin\Debug\GraphicTool.exe Tonemapping 06_ChinaRoomNoFire.raw -output 06_ChinaRoom.jpg -method ACESFilmicToneMappingCurve
if exist 06_ChinaRoom.raw del 06_ChinaRoom.raw
if exist 06_ChinaRoomNoFire.raw del 06_ChinaRoomNoFire.raw