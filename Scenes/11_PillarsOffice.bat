..\Source\Tools\bin\Debug\GraphicTool.exe CreateImage ..\Data\11_PillarsOfficeGodRay_json.txt -output 11_PillarsOffice.raw -renderMod MediaBeamTracer -sampleCount 10000 -width 1920 -height 1016 -saveFolder AutoSave

rem die Maske habe ich erstellt indem ich alles außer Säule10 entfernt habe und dann mit RaytracerSimple; Die Gamma/Brightness-Werte habe ich mit dem Imageeditor ermittelt
..\Source\Tools\bin\Debug\GraphicTool.exe TonemappingTwoAreas 11_PillarsOffice.raw -output 11_PillarsOffice.jpg -brigthness1 1 -gamma1 1,2 -brigthness2 0,31 -gamma2 1,637 -mask ..\Data\11_PillarsOfficeMask.png
if exist 11_PillarsOffice.raw del 11_PillarsOffice.raw