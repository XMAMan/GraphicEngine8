using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using GraphicMinimal;

namespace GraphicGlobal
{
    //Pipeline mit Puffer für Dreiecke und Texturen. Ausgabe erfolgt auf eine Zeichenfläche, welche am Ende der Pipeline dranhängt
    public interface IGraphicPipeline : IDrawing2D
    {
        int Width { get; }  //Größe der Zeichenfläche, welche am Ende der Pipeline hängt
        int Height { get; }

        #region 3D-Zeichenfunktionen
        Control DrawingControl { get; }
        bool UseDisplacementMapping { get; set; }
        NormalSource NormalSource { get; set; }
        void Use2DShader();
        void SetNormalInterpolationMode(InterpolationMode mode);
        void Resize(int width, int height);

        //Pufferoperationen
        void ClearColorBuffer(Color clearColor);
        void ClearColorDepthAndStencilBuffer(Color clearColor);
        void ClearDepthAndStencilBuffer();
        void ClearStencilBuffer();
        void EnableWritingToTheColorBuffer();
        void DisableWritingToTheColorBuffer();
        void EnableWritingToTheDepthBuffer();
        void DisableWritingToTheDepthBuffer();
        void FlippBuffer();
        Bitmap GetDataFromColorBuffer();
        Bitmap GetDataFromDepthBuffer();

        //Kamera
        void SetModelViewMatrixToIdentity();
        void SetModelViewMatrixToCamera(Camera camera);
        void SetProjectionMatrix3D(int screenWidth = 0, int screenHight = 0, float fov = 45, float zNear = 0.001f, float zFar = 3000);
        void SetProjectionMatrix2D(float left = 0, float right = 0, float bottom = 0, float top = 0, float znear = 0, float zfar = 0);
        void SetViewport(int startX, int startY, int width, int height); //startX,startY = Linke obere Ecke

        //2D-Textur
        new int GetTextureId(Bitmap bitmap);
        Size GetTextureSize(int textureId);
        void SetTexture(int textureID);
        void SetActiveTexture0(); //Farbtextur
        void SetActiveTexture1(); //Bumpmaptextur
        void SetTextureFilter(TextureFilter filter);
        void EnableTexturemapping();
        void DisableTexturemapping();

        //Textur-Spielerrein
        new Bitmap GetTextureData(int textureID);
        int CreateEmptyTexture(int width, int height);
        void CopyScreenToTexture(int textureID);

        //In Textur Rendern
        new int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture); //Ein Framebuffer ist ein Kontainer, welche 0 bis N Farbtexturen enthält und 0 bis 1 Tiefentextur. Er steht am Ende der Grafikpipline. Vermuteter Alternativname: RenderTargets
        new void EnableRenderToFramebuffer(int framebufferId);
        new void DisableRenderToFramebuffer();
        new int GetColorTextureIdFromFramebuffer(int framebufferId);
        int GetDepthTextureIdFromFramebuffer(int framebufferId);

        //Cubemapping (6 2D-Texturen)
        int CreateCubeMap(int cubeMapSize = 256);
        void EnableRenderToCubeMap(int cubemapID, int cubemapSide, Color clearColor);//Cubemap schreibend verwenden
        Bitmap GetColorDataFromCubeMapSide(int cubemapID, int cubemapSide);
        void DisableRenderToCubeMap();
        void EnableAndBindCubemapping(int cubemapID); //Cubemap lesend verwenden
        void DisableCubemapping();

        //Shadow-Mapping
        bool ReadFromShadowmap { set; }//Soll lesend auf Shadowtextur im normalen Renderpass zugegriffen werden?
        int CreateShadowmap(int width, int height);
        void EnableRenderToShadowmap(int shadowmapId);
        void BindShadowTexture(int shadowmapId);
        void DisableRenderToShadowmapTexture();
        bool IsRenderToShadowmapEnabled();
        void SetShadowmapMatrix(Matrix4x4 shadowMatrix);
        Bitmap GetShadowmapAsBitmap(int shadowmapId);

        //Texture-Einstellungen 
        void SetTextureMatrix(Matrix3x3 matrix3x3);                     //Textur verschieben/rotieren/skalieren
        void SetTextureScale(Vector2D scale);                           //Wird benötigt, damit beim Parallax-Effekt der Edge-Cutoff für Vierecke funktioniert (Es wird davon ausgegangen, dass man ein Viereck mit Texturkoordianten 0..1 darstellt)
        void SetTesselationFactor(float tesselationFactor);             // Wird bei Displacementmapping benötigt. In so viele Dreiecke wird Dreieck zerlegt
        void SetTextureHeighScaleFactor(float textureHeighScaleFactor);   // Höhenskalierung bei Displacement- und Parallaxmapping
        void SetTextureMode(TextureMode textureMode);

        //Dreiecke
        int GetTriangleArrayId(Triangle[] data);                                 // Speichert Dreiecksliste im Hauptspeicher oder Grafikspeicher und liefert ID darauf zurück
        void DrawTriangleArray(int triangleArrayId);                             // Zeichnet die Dreicke
        void RemoveTriangleArray(int triangleArrayId);                           // Löscht Dreicksliste auf Grafikkarte
        void DrawTriangleStrip(Vector3D v1, Vector3D v2, Vector3D v3, Vector3D v4);

        //Linie
        void DrawLine(Vector3D v1, Vector3D v2);
        void SetLineWidth(float lineWidth);

        //Pixel
        void DrawPoint(Vector3D position);
        void SetPointSize(float size);
        Color GetPixelColorFromColorBuffer(int x, int y);

        //Matrix
        Matrix4x4 GetInverseModelMatrix(Vector3D position, Vector3D orientation, float size);
        Matrix4x4 GetModelMatrix(Vector3D position, Vector3D orientation, float size);
        void PushMatrix();
        void PopMatrix();
        void PushProjectionMatrix();
        void PopProjectionMatrix();
        void MultMatrix(Matrix4x4 matrix);
        void Scale(float size);
        Matrix4x4 GetProjectionMatrix();
        Matrix4x4 GetModelViewMatrix();

        //Farbe
        void SetColor(float R, float G, float B, float A);
        void SetSpecularHighlightPowExponent(float specularHighlightPowExponent);

        //Licht
        void SetPositionOfAllLightsources(List<RasterizerLightsource> lights);
        void EnableLighting();
        void DisableLighting();

        //Blending
        void SetBlendingWithBlackColor();
        void SetBlendingWithAlpha();
        void DisableBlending();

        //Wirefram
        void EnableWireframe();
        void DisableWireframe();

        //Explosionseffekt
        void EnableExplosionEffect();
        void DisableExplosionEffect();
        float ExplosionsRadius { get; set; }
        int Time { get; set; }// Explosionseffekt braucht Timewert

        //Cull Face
        void EnableCullFace();
        void DisableCullFace();
        void SetFrontFaceConterClockWise();
        void SetFrontFaceClockWise();

        //Depth Test
        void EnableDepthTesting();
        void DisableDepthTesting();

        //Stencil
        Bitmap GetStencilTestImage();
        void EnableStencilTest();
        void DisableStencilTest();
        bool SetStencilWrite_TwoSide(); //Return: Wenn true, erhöhen Front-Faces den Stencil um 1, und Backfaces veringern um 1. Wenn nicht möglich, dann Rückgabe false
        void SetStencilWrite_Increase();//Beim zeichnen wird der Stencil um 1 erhöht
        void SetStencilWrite_Decrease();//Beim zeichnen wird der Stencil um 1 verringert
        void SetStencilRead_NotEqualZero();//Zeichne nur, wenn Stencil != 0 ist

        //Nur in diesen rechteckigen Bereich darf man zeichnen (Clipping nennt man das, wenn man im 3D eine Clipping-Plane hat)
        new void EnableScissorTesting(int x, int y, int width, int height);
        new void DisableScissorTesting();

        //Maus-Abfrage
        void StartMouseHitTest(Point mousePosition);                        // Links oben ist (0, 0)
        void AddObjektIdForMouseHitTest(int objektId);                      // Diese ID wird bei GetMouseHitTestResult zurück gegeben
        int GetMouseHitTestResult();                                        // -1, wenn kein Objekt angeklickt wurde, sonst objektID
        #endregion
    }

    //Beschreibt eine Lichtquelle, wie sie das IGraphicPipeline-Interface benötigt
    public class RasterizerLightsource : RasterizerLightSourceDescription
    {
        public Vector3D Position { get; private set; }                    // Position in Objktkoordinaten
        
        public RasterizerLightsource(RasterizerLightSourceDescription lightDescription, Vector3D position)
            : base(lightDescription)
        {
            this.Position = position;
        }

        public RasterizerLightsource(RasterizerLightsource copy)
            : base(copy)
        {
            this.Position = copy.Position;
        }
    }
}
