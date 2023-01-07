rem Es wird das komplette MirrorCornellbox-Bild genommen und geprüft, wie effektiv dieses durch VCM und MarkovChain-VCM gerendert werden kann

rem Schritt 1: Referenzbild erzeugen
rem call 05_MirrorCornellbox.bat

rem Schritt 2: (Im Paint 05_MirrorCornellbox.jpg öffnen und auf 20% verkleinern und unter 05_MirrorCornellboxSmall.bmp speichern)

rem Schritt 3: Messdaten für VCM erheben
rem ..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile 05_MirrorCornellboxSmall.bmp -outputFolder 05_MirrorCornellBoxPixRangeData_VCM -sampleCount 100 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod MediaVCM -width 215 -height 188 -progressImageScaleUpFactor 1

rem Schritt 4: Messdaten für McVcm erheben
..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile 05_MirrorCornellboxSmall.bmp -outputFolder 05_MirrorCornellBoxPixRangeData_McVcm -sampleCount 100 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod McVcm_WithMedia -width 215 -height 188 -progressImageScaleUpFactor 1

rem Schritt 5: Messdaten aus den Csv-Dateien nehmen und visualisieren
..\Source\Tools\bin\Debug\GraphicTool.exe PrintImageConvergenceData -referenceImageInputFile 05_MirrorCornellboxSmall.bmp -dataFolder1 05_MirrorCornellBoxPixRangeData_VCM -label1 "VCM" -dataFolder2 05_MirrorCornellBoxPixRangeData_McVcm -label2 "McVcm" -width 783 -height 400 -scaleUpFactor 2 -layout AllInColum -maxShownError 40 -maxTime Min -outputImageFile 05_MirrorCornellboxPixRange_VCM_McVcm_Compare.jpg