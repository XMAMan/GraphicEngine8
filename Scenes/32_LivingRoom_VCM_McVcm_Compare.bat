rem Es wird das komplette LivingRoom-Bild genommen und geprüft, wie effektiv dieses durch VCM und MarkovChain-VCM gerendert werden kann

rem Schritt 1: Referenzbild erzeugen
rem call 32_LivingRoom.bat

rem Schritt 2: (Im Paint 32_LivingRoom.jpg öffnen und auf 20% verkleinern und unter 32_LivingRoomSmall.bmp speichern)

rem Schritt 3: Messdaten für VCM erheben
rem ..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\32_LivingRoom_json.txt -referenceImageInputFile 32_LivingRoomSmall.bmp -outputFolder 32_LivingRoomPixRangeData_VCM -sampleCount 879 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod VertexConnectionMerging -tonemapping ACESFilmicToneMappingCurve -width 336 -height 198 -progressImageScaleUpFactor 1

rem Schritt 4: Messdaten für McVcm erheben
rem ..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\32_LivingRoom_json.txt -referenceImageInputFile 32_LivingRoomSmall.bmp -outputFolder 32_LivingRoomPixRangeData_McVcm -sampleCount 100 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod McVcm -tonemapping ACESFilmicToneMappingCurve -width 336 -height 198 -progressImageScaleUpFactor 1

rem Schritt 5: Messdaten aus den Csv-Dateien nehmen und visualisieren
..\Source\Tools\bin\Debug\GraphicTool.exe PrintImageConvergenceData -referenceImageInputFile 32_LivingRoomSmall.bmp -dataFolder1 32_LivingRoomPixRangeData_VCM -label1 "VCM" -dataFolder2 32_LivingRoomPixRangeData_McVcm -label2 "McVcm" -width 674 -height 400 -scaleUpFactor 2 -layout AllInColum -maxShownError 80 -maxTime Min -outputImageFile 32_LivingRoomPixRange_VCM_McVcm_Compare.jpg