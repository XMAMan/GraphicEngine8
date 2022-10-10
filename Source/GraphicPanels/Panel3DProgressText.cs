using System;

namespace GraphicPanels
{
    //Erzeugt bei Rasterizern den Text für die FPS-Anzeige und bei Raytracing die Renderdauer
    public class Panel3DProgressText
    {
        private GraphicPanel3D graphicPanel;

        private DateTime lastTimerForFPS = DateTime.Now;
        private DateTime lastUpdateTime = DateTime.Now;
        private float lastProgressValue = 0;
        private string finishString = "";

        private int frameCounter = 0;

        public Panel3DProgressText(GraphicPanel3D graphicPanel)
        {
            this.graphicPanel = graphicPanel;
        }

        public string GetProgressText()
        {
            if (this.graphicPanel.IsRaytracingNow)
            {
                float progress = this.graphicPanel.ProgressPercent;
                double progessDelta = Math.Max(0, progress - this.lastProgressValue);
                double timeDiffInSeconds = (DateTime.Now - this.lastUpdateTime).TotalSeconds;
                if (progessDelta > 5 || timeDiffInSeconds > 10)
                {
                    double remainingSeconds = (100 - progress) * timeDiffInSeconds / progessDelta;
                    var finishTime = (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds)) ? DateTime.MaxValue : DateTime.Now.AddSeconds(remainingSeconds);
                    //this.finishString = $"Fertig in {(int)remainingSeconds} Sekunden um {finishTime}";
                    this.finishString = $"Fertig um {finishTime}";

                    this.lastUpdateTime = DateTime.Now;
                    this.lastProgressValue = progress;
                }

                
                if ((DateTime.Now - lastTimerForFPS).TotalSeconds > 10)
                {
                    this.graphicPanel.UpdateProgressImage();
                    lastTimerForFPS = DateTime.Now;
                }

                return this.graphicPanel.ProgressText + ": " + String.Format("{0:F3}", this.graphicPanel.ProgressPercent) + "% " + this.finishString;
            }else
            {
                if (GraphicPanel3D.IsRasterizerMode(this.graphicPanel.Mode))
                {
                    frameCounter++;
                    if ((DateTime.Now - lastTimerForFPS).TotalMilliseconds > 1000)
                    {
                        lastTimerForFPS = DateTime.Now;
                        string text = (frameCounter) + " FPS";
                        frameCounter = 0;
                        return text;
                    }
                }
                
            }

            return null;
        }
    }
}
