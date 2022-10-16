xcopy /Y AutoSave\*.dat AutoSave\progressImage.raw
..\GraphicTool.exe TonemappingTwoAreas AutoSave\progressImage.raw -output 11_PillarsOffice_Progress.jpg -brigthness1 1 -gamma1 1,2 -brigthness2 0,31 -gamma2 1,637 -mask ..\Data\11_PillarsOfficeMask.png
