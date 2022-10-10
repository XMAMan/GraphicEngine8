using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GraphicMinimal;

namespace GraphicGlobal
{
    //Zeichenfläche, welche in ein GraphicPanel als Kindelement eingebettet ist (Ein GraphicPanel für jeden Modus ein eigenes IDrawingPanel)
    public interface IDrawingPanel
    {
        Control DrawingControl { get; }        
    }

    //Zeichenbefehle werden im UI-Thread blockierend über die IGraphicPipeline gemacht
    public interface IDrawingSynchron
    {
        void ClearScreen(int textureId); //Hintergrundbild
        void ClearScreen(Color color);
        void FlipBuffer(); 				 // Kopiert die Daten vom Hintergrundpuffer in den Fordergrundpuffer
        Bitmap GetDataFromFrontbuffer();
        IGraphicPipeline Pipeline { get; }
    }

    public interface IDrawing3D : IDrawingSynchron
    {
        void Draw3DObjects(Frame3DData data); // Zeichnet Liste von 3D-Objekten in den Hintergrundpuffer
        void Enable2DModus();            // Muss gemacht werden, bevor man die 2D-Zeichenbefehle nutzen darf
        DrawingObject MouseHitTest(Frame3DData data, Point mousePosition); //Da man für den Mouse-Test Zeichenbefehle benötigt, steht er hier mit drin
    }

    public interface IDrawing2D
    {
        void DrawLine(Pen pen, Vector2D p1, Vector2D p2);
        void DrawPixel(Vector2D pos, Color color, float size);

        Size GetStringSize(float size, string text);
        void DrawString(int x, int y, Color color, float size, string text);

        void DrawRectangle(Pen pen, int x, int y, int width, int height);
        void DrawFillRectangle(int textureId, int x, int y, int width, int height, Color colorFactor);
        void DrawFillRectangle(int textureId, int x, int y, int width, int height, Color colorFactor, float angle);//x,y liegen in der Mitte, angle geht von 0 bis 360
        void DrawFillRectangle(int textureId, int x, int y, int width, int height, Color colorFactor, float zAngle, float yAngle);//x,y liegen in der Mitte, angle geht von 0 bis 360
        void DrawFillRectangle(Color color, int x, int y, int width, int height);
        void DrawFillRectangle(Color color, int x, int y, int width, int height, float angle);//x,y liegen in der Mitte, angle geht von 0 bis 360
        void DrawFillRectangle(Color color, int x, int y, int width, int height, float zAngle, float yAngle);//x,y liegen in der Mitte, angle geht von 0 bis 360

        void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor); //Nimmt ein Teilausschnitt aus ein Bild und zeichnet diesen
        void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor, float zAngle, float yAngle); //x,y liegen in der Mitte, angle geht von 0 bis 360

        void DrawPolygon(Pen pen, List<Vector2D> points);
        void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList);
        void DrawFillPolygon(Color color, List<Triangle2D> triangleList);

        void DrawCircle(Pen pen, Vector2D pos, int radius);         // pos ist die Mitte des Kreises
        void DrawFillCircle(Color color, Vector2D pos, int radius); // pos ist die Mitte des Kreises

        //xCount/yCount Anzahl Einzelbilder; xBild -> 1 .. xCount; yBild -> 1 .. yCount
        void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, Color colorFactor);

        //Nur in diesen rechteckigen Bereich darf man zeichnen (Clipping nennt man das, wenn man im 3D eine Clipping-Plane hat)
        void EnableScissorTesting(int x, int y, int width, int height);
        void DisableScissorTesting();

        //Textur-Spielerrein
        int GetTextureId(Bitmap bitmap);
        Bitmap GetTextureData(int textureID);

        //In Textur Rendern
        int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture); //Ein Framebuffer ist ein Kontainer, welche 0 bis N Farbtexturen enthält und 0 bis 1 Tiefentextur. Er steht am Ende der Grafikpipline. Vermuteter Alternativname: RenderTargets
        void EnableRenderToFramebuffer(int framebufferId);
        void DisableRenderToFramebuffer();
        int GetColorTextureIdFromFramebuffer(int framebufferId);
    }

    public interface IDrawingAsynchron
    {
        string ProgressText { get; }
        float ProgressPercent { get; }	// Hier kann abgefragt werden, wie lange das rendern noch dauert
        bool IsRaytracingNow { get; }
        void StartRaytracing(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured);
        void StartImageAnalyser(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, string outputFolder, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured);
        void StopRaytracing();
        RaytracerResultImage GetRaytracingImageSynchron(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange);
        void SaveCurrentRaytracingDataToFolder();  //Damit man wärend des Raytracens eine Sicherung der Daten manuell anlegen kann
        void UpdateProgressImage(float brightnessFactor, TonemappingMethod tonemapping); 
    }  

    public interface IRaytracingHelper
    {
        Vector3D GetColorFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount);
        List<Vector3D> GetNPixelSamples(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount);
        string GetFullPathsFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount);
        string GetPathContributionsForSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount);
        float GetBrightnessFactor(Frame3DData data, int imageWidth, int imageHeight);
        Vector3D GetColorFromSinglePixelForDebuggingPurpose(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, RaytracingDebuggingData debuggingData); //Zum nachstellen von Fehlern
        string GetFlippedWavefrontFile(Frame3DData data, int imageWidth, int imageHeight); //Dreht alle Dreiecke so, dass die Normale nach außen zeigt. Außen ist da, wo die Kamera und Lichtquellen ist
    }

}
