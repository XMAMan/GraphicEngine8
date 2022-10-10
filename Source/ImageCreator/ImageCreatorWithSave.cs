using GraphicMinimal;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BitmapHelper;

namespace ImageCreator
{
    //Erweitert ein IImageCreatorPixel oder IImageCreatorFrame um die SaveToFolder-Methode
    //Speichert nach jeden erstellten SaveArea das Bild automatisch(wenn saveFinishedImagesAutomaticly==true). 
    //AutoSave-Mäßig speichert es aber keine Zwischenbilder der jeweileigen SaveArea. Die SaveToFolder-Methode 
    //muss von Außen gerufen werden um Zwischenbilder(Halbfertiges SaveArea-Bild) zu erhalten.
    public class ImageCreatorWithSave : IImageCreatorWithSave
    {
        private IImageCreator imageCreator;
        private string saveFolder;
        private ImageBuffer renderBuffer;
        private SaveArea currentArea;
        private long pixelFinishCounter = 0; //So viele Pixel sind bereits fertig
        private Size saveAreaSizeIfAutoSaveIsInAreaMode; //Diese Größe haben die SaveAreas, wenn autoSaveMode==RaytracerAutoSaveMode.SaveAreas ist; Ansonsten entspricht sie pixelRange.Width/Height
        private CancellationTokenSource stopTrigger;
        private ImagePixelRange pixelRange; //Dieser Parameter wurde bei GetImage übergeben 
        private RaytracerAutoSaveMode autoSaveMode; //Wenn jemand Raytracing-Stopp aufruft oder eine SaveArea fertig wird

        public RaytracerWorkingState State { get; private set; }

        //Wenn saveFolder ein Leerstring
        public ImageCreatorWithSave(IImageCreator imageCreator, string saveFolder, Size saveAreaSizeIfAutoSaveIsInAreaMode, RaytracerAutoSaveMode autoSaveMode, CancellationTokenSource stopTrigger)
        {
            this.imageCreator = imageCreator;
            this.saveFolder = saveFolder;
            this.saveAreaSizeIfAutoSaveIsInAreaMode = saveAreaSizeIfAutoSaveIsInAreaMode;
            this.autoSaveMode = autoSaveMode;
            this.stopTrigger = stopTrigger;
            this.State = RaytracerWorkingState.Created;
        }

        public ImageBuffer GetImage(ImagePixelRange range)
        {
            //Schritt 1: renderBuffer anlegen
            this.pixelRange = range;
            this.currentArea = null;
            this.pixelFinishCounter = 0;
            this.renderBuffer = new ImageBuffer(range.Width, range.Height);
            this.State = RaytracerWorkingState.Created;

            //Schritt 2: PixelRange in lauter SaveAreas unterteilen
            var saveFolderFiles = Directory.Exists(this.saveFolder) ? new DirectoryInfo(this.saveFolder).GetFiles().Select(x => x.Name).ToArray() : new string[0];
            var saveAreas = range
                .GetRandomPixelRangesInsideFromHere(this.autoSaveMode == RaytracerAutoSaveMode.SaveAreas ? this.saveAreaSizeIfAutoSaveIsInAreaMode : new Size(range.Width, range.Height), new Random(0))
                .Select(x => new SaveArea(x, saveFolderFiles))
                .ToList();

            //Schritt 3: Für jedes SaveArea schauen, ob es bereits Daten im Sicherungsordner gibt und diese einladen
            foreach (var area in saveAreas)
            {
                if (area.CurrentState == SaveArea.State.Finish)
                {
                    this.State = RaytracerWorkingState.LoadingAfterResume;
                    this.pixelFinishCounter += area.PixelRange.Width * area.PixelRange.Height;
                    LoadSaveAreaFromFolder(area);
                }
            }

            //Schritt 4: Render die eine InWork-Area weiter
            if (saveAreas.Any(x => x.CurrentState == SaveArea.State.InWork))
            {
                if (saveAreas.Count(x => x.CurrentState == SaveArea.State.InWork) != 1) throw new Exception("Es darf nur eine InWork-Datei im Sicherungsordner liegen");

                var inWorkArea = saveAreas.First(x => x.CurrentState == SaveArea.State.InWork);
                lock(this) //Locke das Schreiben auf die currentArea (Sie kann initial auch null sein)
                {
                    this.currentArea = inWorkArea;
                }                
                var resumedImage = GetResumedImage(this.currentArea);
                this.renderBuffer.WriteIntoSubarea(this.currentArea.PixelRange.XStart - this.pixelRange.XStart, this.currentArea.PixelRange.YStart - this.pixelRange.YStart, resumedImage);
                SetAreaIntoFinishStateAndSave();
                if (this.stopTrigger.IsCancellationRequested) return this.renderBuffer;
            }

            //Schritt 5: Render alle NotCreated-Areas
            this.State = RaytracerWorkingState.InWork;
            var notCreatedList = saveAreas.Where(x => x.CurrentState == SaveArea.State.NotCreated).ToList();
            foreach (var area in notCreatedList)
            {
                //Schritt 5.1: Erzeuge ImageBuffer-Objekt
                lock(this) //Locke das Schreiben auf die currentArea (Sie kann initial auch null sein)
                {
                    this.currentArea = area;
                    this.currentArea.CurrentState = SaveArea.State.InWork;
                }                
                var saveAreaImage = this.imageCreator.GetImage(area.PixelRange);
                this.renderBuffer.WriteIntoSubarea(area.PixelRange.XStart - this.pixelRange.XStart, area.PixelRange.YStart - this.pixelRange.YStart, saveAreaImage);

                //Schritt 5: Speichere das fertige Bild
                SetAreaIntoFinishStateAndSave();
                if (this.stopTrigger.IsCancellationRequested) return this.renderBuffer;
            }

            this.State = RaytracerWorkingState.Finish;

            return this.renderBuffer;
        }

        //Speichert das InWork oder Finish-Bild
        private void SetAreaIntoFinishStateAndSave()
        {
            lock (this)
            {
                //Speichere das InWork-Bild
                if (this.stopTrigger.IsCancellationRequested)
                {
                    if (this.autoSaveMode != RaytracerAutoSaveMode.Disabled)
                    {
                        SaveSaveAreaIntoFolder(this.currentArea);
                    }

                    return;
                }

                //Speichere das FinishBild
                string inWorkName = this.saveFolder + "\\" + this.currentArea.FileName;
                this.pixelFinishCounter += this.currentArea.PixelRange.Width * this.currentArea.PixelRange.Height;
                this.currentArea.CurrentState = SaveArea.State.Finish;
                if (this.autoSaveMode != RaytracerAutoSaveMode.Disabled)
                {
                    SaveSaveAreaIntoFolder(this.currentArea);
                }
                if (File.Exists(inWorkName)) File.Delete(inWorkName);
            }
        }

        //Speichert die aktuell in Bearbeitung befindliche SaveArea
        public void SaveToFolder()
        {
            lock (this) //Locke das Schreiben auf die currentArea (Sie kann initial auch null sein)
            {
                if (this.currentArea == null || this.currentArea.CurrentState != SaveArea.State.InWork) return;

                string inWorkName = this.saveFolder + "\\" + this.currentArea.FileName;
                this.currentArea.CurrentState = SaveArea.State.InSaving;
                SaveSaveAreaIntoFolder(this.currentArea);
                File.Delete(inWorkName);

                //Ohne diesen beiden Anweisungen sagt File.Move dass die inWorkName-Datei noch von ein anderen Thread gerade benutzt wird
                while (File.Exists(inWorkName)) ; 
                Thread.Sleep(10);

                string inSaveName = this.saveFolder + "\\" + this.currentArea.FileName;
                if (File.Exists(inSaveName)) //Wenn jemand Save aufruft bevor GetImage ein ImageBuffer angelegt hat
                {
                    File.Move(inSaveName, inWorkName);
                }
                
                this.currentArea.CurrentState = SaveArea.State.InWork;
            }            
        }

        //Speichere ein ImageBuffer oder ImageBufferSum-Objekt in eine Datei
        private void SaveSaveAreaIntoFolder(SaveArea saveArea)
        {
            string fileName = this.saveFolder + "\\" + saveArea.FileName;

            if (this.imageCreator is IImageCreatorPixel)
            {
                ImageBuffer data = this.imageCreator.GetProgressImage();
                if (data != null)
                    data.WriteToFile(fileName);
            }

            if (this.imageCreator is IImageCreatorFrame)
            {
                ImageBufferSum data = (this.imageCreator as IImageCreatorFrame).GetImageBufferSum();
                if (data != null)
                    data.WriteToFile(fileName);
            }
        }

        //Lädt von der Platte ein SaveArea und speichert es im renderBuffer
        private void LoadSaveAreaFromFolder(SaveArea saveArea)
        {
            string fileName = this.saveFolder + "\\" + saveArea.FileName;

            ImageBuffer areaBuffer = null;
            if (this.imageCreator is IImageCreatorPixel)
                areaBuffer = new ImageBuffer(fileName);

            if (this.imageCreator is IImageCreatorFrame)
                areaBuffer = new ImageBufferSum(fileName).GetScaledImage();

            this.renderBuffer.WriteIntoSubarea(saveArea.PixelRange.XStart - this.pixelRange.XStart, saveArea.PixelRange.YStart - this.pixelRange.YStart, areaBuffer);
        }

        //Lädt eine InWork-Datei von der Platte und rendert dort weiter
        private ImageBuffer GetResumedImage(SaveArea saveArea)
        {
            string fileName = this.saveFolder + "\\" + saveArea.FileName;

            if (this.imageCreator is IImageCreatorPixel)
            {
                this.State = RaytracerWorkingState.LoadingAfterResume;
                var initialData = new ImageBuffer(fileName);
                this.State = RaytracerWorkingState.InWork;
                return (this.imageCreator as IImageCreatorPixel).GetImageFromInitialData(initialData, saveArea.PixelRange);
            }


            if (this.imageCreator is IImageCreatorFrame)
            {
                this.State = RaytracerWorkingState.LoadingAfterResume;
                var initialData = new ImageBufferSum(fileName);
                this.State = RaytracerWorkingState.InWork;
                return (this.imageCreator as IImageCreatorFrame).GetImageFromInitialData(initialData, saveArea.PixelRange);
            }

            throw new Exception("Unknown type " + this.imageCreator.GetType());
        }

        

        public ImageBuffer GetProgressImage()
        {
            if (this.currentArea != null)
            {
                var subImage = this.imageCreator.GetProgressImage();
                if (subImage != null)
                {
                    this.renderBuffer.WriteIntoSubarea(
                    this.currentArea.PixelRange.XStart - this.pixelRange.XStart,
                    this.currentArea.PixelRange.YStart - this.pixelRange.YStart,
                    subImage);
                }                
            }
            
            return this.renderBuffer;
        }

        public float Progress
        {
            get
            {
                if (this.renderBuffer == null) return 0;

                if (this.currentArea != null && (this.currentArea.CurrentState == SaveArea.State.InWork ||this.currentArea.CurrentState == SaveArea.State.InSaving))
                {
                    return (this.pixelFinishCounter + (this.currentArea.PixelRange.Width * this.currentArea.PixelRange.Height) * this.imageCreator.Progress) / (float)(this.renderBuffer.Width * this.renderBuffer.Height);
                }

                return this.pixelFinishCounter / (float)(this.renderBuffer.Width * this.renderBuffer.Height);
            }
        }

        public string ProgressText
        {
            get
            {
                return this.imageCreator.ProgressText;
            }
        }

        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData)
        {
            return this.imageCreator.GetColorFromSinglePixelForDebuggingPurpose(debuggingData);
        }
    }

    class SaveArea
    {
        public enum State 
        { 
            NotCreated,     //Zugehörige Datei existiert nicht auf der Platte
            Finish,         //Dieser Bereich wurde zu 100% gerendert und gespeichert
            InWork,         //Dieser Bereich wurde gespeichert aber ist noch nicht 100%ig fertig
            InSaving        //Dieser Bereich wird gerade gespeichert
        }
        public ImagePixelRange PixelRange { get; private set; }
        public State CurrentState { get; set; }
        public string FileName  //StartX_StartY_Width_Height_{InWork|InSaving|Finish}.dat
        {
            get 
            { 
                return $"{PixelRange.XStart}_{PixelRange.YStart}_{PixelRange.Width}_{PixelRange.Height}_{CurrentState}.dat"; 
            }
        } 

        public SaveArea(ImagePixelRange pixelRange, string[] saveFolderFileNames)
        {
            this.PixelRange = pixelRange;
            this.CurrentState = State.NotCreated;

            string startsWith = $"{pixelRange.XStart}_{pixelRange.YStart}_{pixelRange.Width}_{pixelRange.Height}_";
            if (saveFolderFileNames.Any(x => x.StartsWith(startsWith)))
            {
                var stateFromFile = saveFolderFileNames
                        .Where(x => x.StartsWith(startsWith))
                        .Select(x => GetStateFromFileName(x))
                        .First();

                if (stateFromFile != State.InSaving)
                {
                    this.CurrentState = stateFromFile;//Ergibt Finish oder InWork
                }
            }
        }

        private State GetStateFromFileName(string fileName)
        {
            int index = fileName.LastIndexOf('_');
            string stateString = fileName.Substring(index + 1).Split('.')[0];
            return (State)Enum.Parse(typeof(State), stateString);
        }

    }
}
