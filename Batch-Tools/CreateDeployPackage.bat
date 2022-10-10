rem Versuch1: So wollte ich die Dlls im Deploy-Packe loswerden -> Da sich ILMerge und Newtonsoft.Json nicht vertragen, darf ich das leider nicht machen
rem if exist ..\Source\Tools\bin\Debug\BitmapHelper.dll (
rem 	CALL CreateSingleExe.bat
rem 	
rem )

if exist ExeFolder rmdir /Q /S ExeFolder
mkdir ExeFolder
copy ..\Source\Tools\bin\Debug\*.dll ExeFolder
copy ..\Source\Tools\bin\Debug\GraphicTool.exe ExeFolder

del ExeFolder\*TestPlatform*dll

rem Kopiert alle Daten aus dem Data-Ordner
rem xcopy /E /I ..\Data ExeFolder\Data

rem Kopiert nur die Daten aus dem Data-Ordner, die in den Scene.Bat-Dateien/Scene-Json-Dateien referenziert werden
mkdir ExeFolder\Data\
..\Source\Tools\bin\Debug\GraphicTool.exe CopyOnlyUsedData -scenesFolder ..\Scenes\ -dataSourceFolder ..\Data\ -dataDestinationFolder ExeFolder\Data\

rem mkdir ExeFolder\PixelRangeResult

mkdir ExeFolder\Scenes
mkdir ExeFolder\Scenes\AutoSave
copy ..\Scenes\*.bat ExeFolder\Scenes

rem https://stackoverflow.com/questions/138497/iterate-all-files-in-a-directory-using-a-for-loop
setlocal enabledelayedexpansion
for %%f in (ExeFolder\Scenes\*.bat) do (
    rem echo "fullname: %%f"
  rem echo "name: %%~nf"
  rem echo "contents: !val!"

  rem For /f "tokens=* delims= " %%a in (%%f) do (
  rem	Set str=%%a
  rem	set str=!str:"..\Tools\bin\Debug\GraphicTool.exe"="GraphicEngine8.exe"!
  rem	echo !str!>>%%f
  rem )

  rem https://www.dostips.com/DtCodeBatchFiles.php#Batch.FindAndReplace
  rem Replace "..\Source\Tools\bin\Debug\GraphicTool.exe" with "..\GraphicEngine8.exe"
  CALL BatchSubstitute.bat "..\Source\Tools\bin\Debug\GraphicTool.exe" ..\GraphicTool.exe %%f > ExeFolder\Scenes\%%~nf.bat1
  copy /y ExeFolder\Scenes\%%~nf.bat1 ExeFolder\Scenes\%%~nf.bat
  del ExeFolder\Scenes\%%~nf.bat1
  
)

mkdir ExeFolder\Batch-Tools
echo ..\GraphicTool.exe Test_2D -dataFolder ..\Data\ > ExeFolder\Batch-Tools\2DTest.bat
echo ..\GraphicTool.exe Test_3D -dataFolder ..\Data\ > ExeFolder\Batch-Tools\3DTest.bat
echo ..\GraphicTool.exe MasterTest High -size 5 -dataFolder ..\Data\ > ExeFolder\Batch-Tools\CreateMasterImage_High.bat
echo ..\GraphicTool.exe MasterTest Normal -size 4 -dataFolder ..\Data\ > ExeFolder\Batch-Tools\CreateMasterImage_Normal.bat
echo ..\GraphicTool.exe MasterTest High -size 1  -dataFolder ..\Data\ > ExeFolder\Batch-Tools\CreateMasterImage_Quick.bat
echo ..\GraphicTool.exe ImageEditor > ExeFolder\Batch-Tools\ImageEditor.bat

rem Versuch 2: Packe alle Dlls in die Exe mit rein (Brauche ich nicht innerhalb des ExeFolder machen wenn ich im Debug-Ordner bereits gepackt habe)
rem copy ..\Source\Tools\bin\Debug\*.xml ExeFolder
rem copy ..\Source\Tools\bin\Debug\GraphicTool.exe.config ExeFolder\GraphicTool.exe.config
rem ..\Source\Tools\bin\Debug\GraphicTool.exe CreateILMergeBatFile -exeFolder ExeFolder -ilMergeFilePath ILMerge.exe -outputFileName ExeFolder\Batch-Tools\CreateSingleExe.bat
rem CALL ExeFolder\Batch-Tools\CreateSingleExe.bat
rem del ExeFolder\Batch-Tools\CreateSingleExe.bat

ENDLOCAL

rem So kann man alle Dlls ausgeben: for %a in (*) do @echo %a   https://stackoverflow.com/questions/23228983/batch-file-list-files-in-directory-only-filenames
rem So kann man echo ohne Zeilenumbruch nutzen: echo|set /p="Hello World"  https://stackoverflow.com/questions/7105433/windows-batch-echo-without-new-line
rem Ausgabe aller Dlls mit Leerzeichen als Trenner: for %a in (ExeFolder\*) do @echo|set /p="%a "
rem Ausgabe aller Dlls mit Leerzeichen als Trenner in der CMD-Datei: for %%a in (ExeFolder\*) do @echo|set /p="%%a "

rem Vorgehen: Erst unter Batch-Tools auf der Konsole for %a in (ExeFolder\*) do @echo|set /p="%a " eingeben
rem Dann aus der Ausgabe ExeFolder\GraphicTool.exe und ExeFolder\SlimDX.dll entfernen

rem Das klappt leider nicht da ilmerge ein Fehler wirft. Deswegen nutze ich stattdessen Costura.Foddy im Tools-Projekt als NuGet-Packet
rem ilmerge /targetplatform:v4,"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client" ExeFolder\GraphicTool.exe /out:ExeFolder\Graphic8All.exe ExeFolder\BitmapHelper.dll ExeFolder\FullPathGenerator.dll ExeFolder\GraphicGlobal.dll ExeFolder\GraphicMinimal.dll ExeFolder\GraphicPanels.dll ExeFolder\GraphicPanelsTest.dll ExeFolder\GraphicPipelineCPU.dll ExeFolder\GraphicPipelineDirect3D11.dll ExeFolder\GraphicPipelineOpenGLv1_0.dll ExeFolder\GraphicPipelineOpenGLv3_0.dll ExeFolder\ImageCreator.dll ExeFolder\IntersectionTests.dll ExeFolder\Microsoft.VisualStudio.TestPlatform.TestFramework.dll ExeFolder\Microsoft.Win32.Primitives.dll ExeFolder\netstandard.dll ExeFolder\Newtonsoft.Json.dll ExeFolder\ObjectDivider.dll ExeFolder\OpenTK.dll ExeFolder\OpenTK.GLControl.dll ExeFolder\ParticipatingMedia.dll ExeFolder\Photonusmap.dll ExeFolder\PointSearch.dll ExeFolder\PowerArgs.dll ExeFolder\Radiosity.dll ExeFolder\Rasterizer.dll ExeFolder\RayCamera.dll ExeFolder\RayObjects.dll ExeFolder\RayTracerGlobal.dll ExeFolder\RaytracerMain.dll ExeFolder\RaytracingBrdf.dll ExeFolder\RaytracingColorEstimator.dll ExeFolder\RaytracingLightSource.dll ExeFolder\RaytracingMethods.dll ExeFolder\RaytracingRandom.dll ExeFolder\SubpathGenerator.dll ExeFolder\System.AppContext.dll ExeFolder\System.Collections.Concurrent.dll ExeFolder\System.Collections.dll ExeFolder\System.Collections.NonGeneric.dll ExeFolder\System.Collections.Specialized.dll ExeFolder\System.ComponentModel.dll ExeFolder\System.ComponentModel.EventBasedAsync.dll ExeFolder\System.ComponentModel.Primitives.dll ExeFolder\System.ComponentModel.TypeConverter.dll ExeFolder\System.Console.dll ExeFolder\System.Data.Common.dll ExeFolder\System.Diagnostics.Contracts.dll ExeFolder\System.Diagnostics.Debug.dll ExeFolder\System.Diagnostics.FileVersionInfo.dll ExeFolder\System.Diagnostics.Process.dll ExeFolder\System.Diagnostics.StackTrace.dll ExeFolder\System.Diagnostics.TextWriterTraceListener.dll ExeFolder\System.Diagnostics.Tools.dll ExeFolder\System.Diagnostics.TraceSource.dll ExeFolder\System.Diagnostics.Tracing.dll ExeFolder\System.Drawing.Primitives.dll ExeFolder\System.Dynamic.Runtime.dll ExeFolder\System.Globalization.Calendars.dll ExeFolder\System.Globalization.dll ExeFolder\System.Globalization.Extensions.dll ExeFolder\System.IO.Compression.dll ExeFolder\System.IO.Compression.ZipFile.dll ExeFolder\System.IO.dll ExeFolder\System.IO.FileSystem.dll ExeFolder\System.IO.FileSystem.DriveInfo.dll ExeFolder\System.IO.FileSystem.Primitives.dll ExeFolder\System.IO.FileSystem.Watcher.dll ExeFolder\System.IO.IsolatedStorage.dll ExeFolder\System.IO.MemoryMappedFiles.dll ExeFolder\System.IO.Pipes.dll ExeFolder\System.IO.UnmanagedMemoryStream.dll ExeFolder\System.Linq.dll ExeFolder\System.Linq.Expressions.dll ExeFolder\System.Linq.Parallel.dll ExeFolder\System.Linq.Queryable.dll ExeFolder\System.Net.Http.dll ExeFolder\System.Net.NameResolution.dll ExeFolder\System.Net.NetworkInformation.dll ExeFolder\System.Net.Ping.dll ExeFolder\System.Net.Primitives.dll ExeFolder\System.Net.Requests.dll ExeFolder\System.Net.Security.dll ExeFolder\System.Net.Sockets.dll ExeFolder\System.Net.WebHeaderCollection.dll ExeFolder\System.Net.WebSockets.Client.dll ExeFolder\System.Net.WebSockets.dll ExeFolder\System.ObjectModel.dll ExeFolder\System.Reflection.dll ExeFolder\System.Reflection.Extensions.dll ExeFolder\System.Reflection.Primitives.dll ExeFolder\System.Resources.Reader.dll ExeFolder\System.Resources.ResourceManager.dll ExeFolder\System.Resources.Writer.dll ExeFolder\System.Runtime.CompilerServices.VisualC.dll ExeFolder\System.Runtime.dll ExeFolder\System.Runtime.Extensions.dll ExeFolder\System.Runtime.Handles.dll ExeFolder\System.Runtime.InteropServices.dll ExeFolder\System.Runtime.InteropServices.RuntimeInformation.dll ExeFolder\System.Runtime.Numerics.dll ExeFolder\System.Runtime.Serialization.Formatters.dll ExeFolder\System.Runtime.Serialization.Json.dll ExeFolder\System.Runtime.Serialization.Primitives.dll ExeFolder\System.Runtime.Serialization.Xml.dll ExeFolder\System.Security.Claims.dll ExeFolder\System.Security.Cryptography.Algorithms.dll ExeFolder\System.Security.Cryptography.Csp.dll ExeFolder\System.Security.Cryptography.Encoding.dll ExeFolder\System.Security.Cryptography.Primitives.dll ExeFolder\System.Security.Cryptography.X509Certificates.dll ExeFolder\System.Security.Principal.dll ExeFolder\System.Security.SecureString.dll ExeFolder\System.Text.Encoding.dll ExeFolder\System.Text.Encoding.Extensions.dll ExeFolder\System.Text.RegularExpressions.dll ExeFolder\System.Threading.dll ExeFolder\System.Threading.Overlapped.dll ExeFolder\System.Threading.Tasks.dll ExeFolder\System.Threading.Tasks.Parallel.dll ExeFolder\System.Threading.Thread.dll ExeFolder\System.Threading.ThreadPool.dll ExeFolder\System.Threading.Timer.dll ExeFolder\System.ValueTuple.dll ExeFolder\System.Xml.ReaderWriter.dll ExeFolder\System.Xml.XDocument.dll ExeFolder\System.Xml.XmlDocument.dll ExeFolder\System.Xml.XmlSerializer.dll ExeFolder\System.Xml.XPath.dll ExeFolder\System.Xml.XPath.XDocument.dll ExeFolder\Tao.OpenGl.dll ExeFolder\Tao.Platform.Windows.dll ExeFolder\TextureHelper.dll ExeFolder\TriangleObjectGeneration.dll ExeFolder\UnitTestHelper.dll


if NOT "%~1" == "NoZip" (
	rem https://superuser.com/questions/201371/create-zip-folder-from-the-command-line-windows
	tar.exe -a -c -f ExeFolder.zip ExeFolder
	rem tar.exe -c -C ExeFolder.zip ExeFolder
	if exist ExeFolder.zip rmdir /Q /S ExeFolder
)