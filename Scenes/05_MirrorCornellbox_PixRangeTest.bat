rem Es wird hier aus dem MirrorCornellbox-Bild ein einzelner Bildausschnitt genommen und geprüft, wie effektiv dieser durch BidirectionalPathTracing und Multiplexed Metropolis Light Transport gerendert werden kann

rem Schritt 1: Referenzbild erzeugen
..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\05_MirrorCornellbox_json.txt  -output 05_MirrorCornellboxPixRange_Reference.bmp -renderMod MediaBidirectionalPathTracing -sampleCount 1000000 -width 1073 -height 940 -pixelRange [730;819;746;835]

rem Schritt 2: Messdaten für BPT erheben
..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile 05_MirrorCornellboxPixRange_Reference.bmp -outputFolder 05_MirrorCornellBoxPixRangeData_BPT -sampleCount 1000000 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod MediaBidirectionalPathTracing -width 1073 -height 940 -pixelRange [730;819;746;835]

rem Schritt 3: Messdaten für SingleBPT erheben
..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile 05_MirrorCornellboxPixRange_Reference.bmp -outputFolder 05_MirrorCornellBoxPixRangeData_SingleBPT -sampleCount 100000 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod SinglePathBPT_WithMedia -width 1073 -height 940 -pixelRange [730;819;746;835]

rem Schritt 4: Messdaten für Multiplexed Metropolis Light Transport erheben
..\Source\Tools\bin\Debug\GraphicTool.exe CollectImageConvergenceData ..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile 05_MirrorCornellboxPixRange_Reference.bmp -outputFolder 05_MirrorCornellBoxPixRangeData_MMLT -sampleCount 100000 -collectionTimerTick 5 -progressImageSaveRate 2 -renderMod MMLT_WithMedia -width 1073 -height 940 -pixelRange [730;819;746;835]

rem Schritt 5: Messdaten aus den Csv-Dateien nehmen und visualisieren
..\Source\Tools\bin\Debug\GraphicTool.exe PrintImageConvergenceData -referenceImageInputFile 05_MirrorCornellboxPixRange_Reference.bmp -dataFolder1 05_MirrorCornellBoxPixRangeData_BPT -label1 "BPT" -dataFolder2 05_MirrorCornellBoxPixRangeData_MMLT -label2 "MMLT" -dataFolder3 05_MirrorCornellBoxPixRangeData_SingleBPT -label3 "Single BPT" -width 783 -height 400 -scaleUpFactor 12 -layout AllInColum -maxShownError 40 -maxTime Min -outputImageFile 05_MirrorCornellboxPixRange_BPT_MMLT_Compare.jpg