using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using BitmapHelper;
using GraphicGlobal;

namespace Rasterizer
{
    class RasterizerDrawingObject
    {
        public RasterizerTriangleData TriangleData { get; private set; }
        public IRasterizerDrawingProps DrawingProps { get; private set; }

        private IGraphicPipeline pipeline;
        private ColorTextureCache colorTextureCache;
        private BumpTextureCache bumpTextureCache;
        private int cubemapId = -1;             // Wenn Reflektion über Cubemapping gemacht werden soll, wird hier die ID gespeichert
        private int triangleArrayId = 0;

        public int GetCubemapID()
        {
            if (this.cubemapId == -1) this.cubemapId = this.pipeline.CreateCubeMap(256);
            return cubemapId;
        }

        public RasterizerDrawingObject(DrawingObject drawingObject, IGraphicPipeline pipeline)
        {
            this.TriangleData = new RasterizerTriangleData(drawingObject.TriangleData);
            this.DrawingProps = drawingObject.DrawingProps as IRasterizerDrawingProps;
            this.pipeline = pipeline;
            this.colorTextureCache = new ColorTextureCache(this.pipeline.GetTextureId);
            this.bumpTextureCache = new BumpTextureCache(this.pipeline.GetTextureId);
        }

        public void Draw(List<LightSource> lightsources, Plane mirrorPlane, IRasterizerGlobalDrawingProps globalProps)
        {
            Matrix4x4 objToWorld = GetObjectToWorldMatrix(mirrorPlane);

            if (globalProps.UseFrustumCulling && this.pipeline.IsRenderToShadowmapEnabled() == false && FrustumCulling.IsBoundingSphereInFrustum(this.DrawingProps.Position, this.TriangleData.Radius * this.DrawingProps.Size) == false)
                return; //Objekt liegt außerhalb vom Sichtbereich

            this.pipeline.UseDisplacementMapping = this.DrawingProps.DisplacementData.UseDisplacementMapping;
            this.pipeline.NormalSource = this.DrawingProps.NormalSource.Type;
            this.pipeline.SetNormalInterpolationMode(this.DrawingProps.NormalInterpolation);
            if (this.DrawingProps.HasExplosionEffect)
            {
                this.pipeline.EnableExplosionEffect();
                this.pipeline.ExplosionsRadius = globalProps.ExplosionRadius;
            }
            this.pipeline.SetSpecularHighlightPowExponent(this.DrawingProps.SpecularHighlightPowExponent);
            bool drawStencilShadow = mirrorPlane == null && this.DrawingProps.RasterizerLightSource == null && globalProps.ShadowsForRasterizer == RasterizerShadowMode.Stencil && this.DrawingProps.HasStencilShadow;
            DrawObjekt(lightsources, drawStencilShadow, objToWorld);
            this.pipeline.DisableExplosionEffect();
        }

        public void DrawSiolette(float lineWidth, Vector3D cameraPosition, float r, float g, float b)
        {
            Matrix4x4 objToWorld = GetObjectToWorldMatrix(null);

            this.pipeline.SetColor(r, g, b, 1);
            this.pipeline.SetLineWidth(lineWidth);
            DrawSiolette(cameraPosition, objToWorld);
            this.pipeline.SetLineWidth(1);
        }

        private Matrix4x4 GetObjectToWorldMatrix(Plane mirrorPlane)
        {
            Matrix4x4 objToWorld = null;

            if (this.DrawingProps.HasBillboardEffect)
            {
                objToWorld = Matrix4x4.BilboardMatrixFromCameraMatrix(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size, this.pipeline.GetModelViewMatrix());
            }
            else
            {
                objToWorld = pipeline.GetModelMatrix(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size);
            }

            //Spiegel Modelviewmatrix an Ebene
            if (mirrorPlane != null)
            {
                objToWorld = objToWorld * Matrix4x4.Reflect(mirrorPlane.A, mirrorPlane.B, mirrorPlane.C, mirrorPlane.D);
            }

            return objToWorld;
        }

        //Size wird einerseits als Parameter 'size' übergegben, anderseits steckt es nochmal in den Matrizen 'mv_obj_to_welt' und 'inverse_mv'obj_to_wetl': Grund: Size wird fürs Frustum Culling und Normalenskalierung benötigt
        private void DrawObjekt(List<LightSource> lightsources, bool drawStencilShadow, Matrix4x4 objToWorld)
        {
            this.pipeline.PushMatrix();
            this.pipeline.MultMatrix(objToWorld);

            //Bumpmap
            this.pipeline.SetActiveTexture1();
            int bumpmapId = this.bumpTextureCache.GetEntry(this.DrawingProps.NormalSource);
            if (bumpmapId != default(int) &&
                (this.pipeline.NormalSource == NormalSource.Normalmap ||
                 this.pipeline.NormalSource == NormalSource.Parallax ||
                 this.pipeline.UseDisplacementMapping))
            {
                this.pipeline.SetTexture(bumpmapId);
                this.pipeline.EnableTexturemapping();
                this.pipeline.SetTextureFilter(TextureFilter.Linear);

                this.pipeline.SetTextureMatrix(this.DrawingProps.NormalSource.As<NormalMapFromFile>().TextureMatrix);
                this.pipeline.SetTextureScale(Matrix3x3.GetTexturScale(this.DrawingProps.NormalSource.As<NormalMapFromFile>().TextureMatrix));
            }
            else
            {
                this.pipeline.DisableTexturemapping();
            }

            //Cubemap
            if (this.cubemapId > 0) this.pipeline.EnableAndBindCubemapping(this.cubemapId); else this.pipeline.DisableCubemapping();

            //Shadowmap
            if (lightsources != null && lightsources.Any())
            {
                this.pipeline.SetShadowmapMatrix(objToWorld * GetShadowMatrixFromLight(lightsources[0], this.pipeline.IsRenderToShadowmapEnabled() == false));
                if (lightsources[0].ShadowMapId >= 0)
                {
                    this.pipeline.BindShadowTexture(lightsources[0].ShadowMapId);
                }
            }
            this.pipeline.SetActiveTexture0();

            //Stencil-Schatten
            if (lightsources != null && drawStencilShadow)
            {
                var currentNormalModel = this.pipeline.NormalSource;
                this.pipeline.NormalSource = NormalSource.ObjectData;
                this.pipeline.DisableLighting();
                var inverseModelToWorld = Matrix4x4.InverseModel(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size);
                foreach (var light in lightsources) //Male für jede Lichtquelle den Schatten für das Objekt 
                {
                    if (light.CreateShadows)
                    {
                        Vector3D lightPosInObjCoords = Matrix4x4.MultPosition(inverseModelToWorld, light.Position);// Lichtvektor in Objektkoordinatensystem tranformieren
                        DrawStencilShadow(lightPosInObjCoords);
                    }                    
                }
                this.pipeline.NormalSource = currentNormalModel;
            }

            //Licht
            if (this.DrawingProps.CanReceiveLight == false || lightsources == null) //Objektbeleuchtung            
                this.pipeline.DisableLighting();
            else
                this.pipeline.EnableLighting();

            //Textur
            var textureData = this.colorTextureCache.GetEntry(this.DrawingProps.Color);
            if (textureData is TextureEntry)
            {
                this.pipeline.SetColor(1, 1, 1, 1); //4. Wert = Alphawert welcher 1 sein muss, wenn es null durchsichtig sein soll
                this.pipeline.SetTexture((textureData as TextureEntry).TextureId);
                this.pipeline.EnableTexturemapping();

                var tex = this.DrawingProps.Color.As<ColorFromTexture>();

                if (this.pipeline.NormalSource == NormalSource.Parallax)
                {
                    this.pipeline.SetTextureFilter(TextureFilter.Linear);
                }
                else
                {
                    this.pipeline.SetTextureFilter(tex.TextureFilter);
                }
                this.pipeline.SetTextureMode(tex.TextureMode);

                //Beim laden der Bumpmap wird auch die TextureMatrix und der TextureScale gesetzt. Die Farbtextur überschreibt das dann hier
                this.pipeline.SetTextureMatrix(tex.TextureMatrix);
                this.pipeline.SetTextureScale(Matrix3x3.GetTexturScale(tex.TextureMatrix));
            }
            else //Farbfestlegung über glColor wenn keine Texture angegeben
            {
                this.pipeline.DisableTexturemapping();
                var color = (textureData as ColorEntry).Color;
                this.pipeline.SetColor(color[0], color[1], color[2], color[3]);
            }

            //Blending
            if (this.DrawingProps.Opacity > 0)	//Glaseffekte/Wassereffekte (0 = Komplett durchsichtig; 1 = Komplett undurchsichtig)
            {
                this.pipeline.SetBlendingWithAlpha();
                if (textureData is TextureEntry)
                    this.pipeline.SetColor(1, 1, 1, this.DrawingProps.Opacity);
                else
                {
                    var color = (textureData as ColorEntry).Color;
                    this.pipeline.SetColor(color[0], color[1], color[2], this.DrawingProps.Opacity);
                }
            }

            //Schwarz ist Transparent
            if (this.DrawingProps.BlackIsTransparent)
            {
                this.pipeline.SetBlendingWithBlackColor();
            }

            //TesselationFactor / TextureHeighScaleFactor
            if (this.DrawingProps.DisplacementData.UseDisplacementMapping)
            {
                this.pipeline.SetTesselationFactor(this.DrawingProps.DisplacementData.TesselationFaktor);
                this.pipeline.SetTextureHeighScaleFactor(this.DrawingProps.DisplacementData.DisplacementHeight);
            }
            else if (this.DrawingProps.NormalSource.Type == NormalSource.Parallax)
            {
                //Der TesselationFaktor wird beim Parallax-Mapping als Schalter misbraucht um zu sagen, ob EdgeCutoff gemacht werden soll oder nicht
                this.pipeline.SetTesselationFactor(this.DrawingProps.NormalSource.As<NormalFromParallax>().IsParallaxEdgeCutoffEnabled ? 1 : 0);
                this.pipeline.SetTextureHeighScaleFactor(this.DrawingProps.NormalSource.As<NormalFromParallax>().TexturHeightFactor);
            }


            if (this.DrawingProps.IsWireFrame) this.pipeline.EnableWireframe();
            DrawObjectWithoutShadows();
            if (this.DrawingProps.IsWireFrame) this.pipeline.DisableWireframe();

            this.pipeline.DisableBlending();
            this.pipeline.PopMatrix();
        }

        private static Matrix4x4 GetShadowMatrixFromLight(RasterizerLightsource light, bool withBias)
        {
            //var projectionMatrix = Matrix4x4.ProjectionMatrixOrtho(-40,40,-40,40,0,300);
            var projectionMatrix = Matrix4x4.ProjectionMatrixPerspective(90, 1, 0.1f, 3000.0f);
            Vector3D cameraUp = Vector3D.Normalize(Vector3D.Cross((Math.Abs(light.SpotDirection.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), light.SpotDirection));
            var modelviewMatrix = Matrix4x4.LookAt(light.Position, light.SpotDirection, cameraUp);

            var shadowMatrix = modelviewMatrix * projectionMatrix;

            if (withBias)
            {
                //Die Bias-Matrix teilt x,y und z durch w und Addiert w/2 hinzu. w bleibt unverändert
                //wenn ich nun diesen vec4 durch w teile, erhalte ich x/w/2 + 0.5
                //Das heißt es ist egel, ob ich erst durch w teile und dann *0.5 + 0.5 rechne, oder erst * Biasmatrix und dann / w teile
                var bias = new Matrix4x4(new float[]{0.5f, 0.0f, 0.0f, 0.0f, 
                                                     0.0f, 0.5f, 0.0f, 0.0f, 
                                                     0.0f, 0.0f, 0.5f, 0.0f, 
                                                     0.5f, 0.5f, 0.5f, 1.0f});

                shadowMatrix = shadowMatrix * bias;
            }

            return shadowMatrix;
        }

        private void DrawObjectWithoutShadows()
        {
            if (this.DrawingProps.ShowFromTwoSides == false)
                this.pipeline.EnableCullFace();
            else
                this.pipeline.DisableCullFace();

            this.pipeline.EnableDepthTesting();

            if (this.triangleArrayId == 0)
                this.triangleArrayId = this.pipeline.GetTriangleArrayId(this.TriangleData.Triangles.ToArray());

            this.pipeline.DrawTriangleArray(this.triangleArrayId);
        }

        //lightPos liegt in Objektkoordianten vor
        private void DrawStencilShadow(Vector3D lightPos)
        {
            float shadowOpacity = 0.5f;//0...kein Schatten, 1...komplett schwarzer Schatten

            //1. Schaue welche Dreiecke aus Sicht der Lichtquelle sichtbar sind
            foreach (TriangleWithNeighborReferences triangle in this.TriangleData.Triangles)
            {
                if (triangle.IsPointAboveTrianglePlane(lightPos)) triangle.Visible = true; else triangle.Visible = false;
            }

            //2. Schreibe den Schatten CounterClockWise in den Stencilpuffer mit Increase
            this.pipeline.DisableWritingToTheDepthBuffer();
            this.pipeline.EnableStencilTest();
            this.pipeline.ClearStencilBuffer();
            this.pipeline.DisableWritingToTheColorBuffer();
            this.pipeline.SetFrontFaceConterClockWise();
            this.pipeline.SetColor(0, 0, 0, 1);

            bool twoSideStencil = this.pipeline.SetStencilWrite_TwoSide();
            if (twoSideStencil == false)
            {
                this.pipeline.SetStencilWrite_Increase();
                this.pipeline.EnableCullFace();
            }
            else
            {
                this.pipeline.DisableCullFace();
            }
            DrawShadowTriangleStripes(lightPos);

            //3. Schreibe ClockWise den Schatten in den Stencilpuffer mit Decrease
            if (twoSideStencil == false)
            {
                this.pipeline.SetFrontFaceClockWise();
                this.pipeline.SetStencilWrite_Decrease();
                DrawShadowTriangleStripes(lightPos);
                this.pipeline.SetFrontFaceConterClockWise();
            }
            this.pipeline.EnableWritingToTheColorBuffer();

            //4. draw a shadowing rectangle covering the entire screen
            this.pipeline.SetBlendingWithAlpha();
            this.pipeline.SetColor(0, 0, 0, shadowOpacity);
            this.pipeline.DisableTexturemapping();
            this.pipeline.DisableCullFace();
            this.pipeline.DisableDepthTesting();
            this.pipeline.SetStencilRead_NotEqualZero();
            this.pipeline.PushMatrix();
            this.pipeline.SetModelViewMatrixToIdentity();
            this.pipeline.DrawTriangleStrip(new Vector3D(-10.1f, +10.1f, -1.10f),
                                      new Vector3D(-10.1f, -10.1f, -1.10f),
                                      new Vector3D(+10.1f, +10.1f, -1.10f),
                                      new Vector3D(+10.1f, -10.1f, -1.10f));
            this.pipeline.PopMatrix();
            this.pipeline.DisableBlending();
            this.pipeline.EnableDepthTesting();

            this.pipeline.EnableWritingToTheDepthBuffer();
            this.pipeline.DisableStencilTest();
        }

        private void DrawShadowTriangleStripes(Vector3D lightPos)
        {
            foreach (TriangleWithNeighborReferences triangle in this.TriangleData.Triangles)
            {
                if (triangle.Visible)
                    for (int j = 0; j < 3; j++)
                        if (triangle.Neighbors[j] == null || triangle.Neighbors[j].Visible == false)
                        {
                            var P1 = triangle.V[j].Position;
                            var P2 = triangle.V[(j + 1) % 3].Position;
                            var P3 = P1 + (P1 - lightPos) * 100;
                            var P4 = P2 + (P2 - lightPos) * 100;
                            this.pipeline.DrawTriangleStrip(P1, P3, P2, P4);
                        }
            }
        }

        private void DrawSiolette(Vector3D cameraPosition, Matrix4x4 objToWorld)
        {
            var inverseModelToWorld = Matrix4x4.InverseModel(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size);
            Vector3D cameraPosInObjCoords = Matrix4x4.MultPosition(inverseModelToWorld, cameraPosition);// Kameraposition in Objektkoordinatensystem tranformieren

            foreach (TriangleWithNeighborReferences triangle in this.TriangleData.Triangles)
            {
                if (triangle.IsPointAboveTrianglePlane(cameraPosInObjCoords)) triangle.Visible = true; else triangle.Visible = false;
            }

            this.pipeline.PushMatrix();
            this.pipeline.MultMatrix(objToWorld);
            this.pipeline.DisableLighting();
            this.pipeline.SetActiveTexture0();
            this.pipeline.DisableTexturemapping();
            this.pipeline.EnableWritingToTheDepthBuffer();
            this.pipeline.DisableBlending();
  
            foreach (TriangleWithNeighborReferences triangle in this.TriangleData.Triangles)
            {
                if (triangle.Visible)
                    for (int j = 0; j < 3; j++)
                        if (triangle.Neighbors[j] == null || triangle.Neighbors[j].Visible == false)
                        {
                            Vector3D P1 = triangle.V[j].Position;
                            Vector3D P2 = triangle.V[(j + 1) % 3].Position;

                            //this.pipeline.SetColor(1, 1, 0, 1); //Wenn ich neben den Linien noch die Eckpunkte mit malen will
                            this.pipeline.DrawLine(P1, P2);
                            //this.pipeline.SetColor(1, 0, 0, 1); this.pipeline.SetPointSize(5); this.pipeline.DrawPoint(P1); this.pipeline.DrawPoint(P2);
                        }
            }

            this.pipeline.EnableDepthTesting();
            
            this.pipeline.PopMatrix();
        }

        public override string ToString()
        {
            return this.TriangleData.Name;
        }
    }
}
