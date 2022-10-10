using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using GraphicPipelineCPU.Rasterizer;
using GraphicPipelineCPU.Shader;
using GraphicPipelineCPU.Shader.PixelShader;
using System.Drawing;

namespace GraphicPipelineCPU.DrawingHelper.Helper3D
{
    //Zeichnet ein Triangle-Array in den Farb- und Tiefenpuffer
    class TriangleDrawer
    {
        class TriangleShaderData
        {
            public IUniformVariables UniformVariables;
            public VertexShaderFunc VertexShader;
            public GeometryShaderFunc GeometryShader;
            public PixelShaderFunc PixelShader;
        }

        private PropertysForDrawing prop = null;

        public TriangleDrawer(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        public void DrawTriangleArray(Triangle[] triangles)
        {
            var shaderData = this.GetShaderDataForTriangleDrawing();

            foreach (var triangle in triangles)
            {
                DrawTriangle(triangle, shaderData);
            }
        }

        private TriangleShaderData GetShaderDataForTriangleDrawing()
        {
            if (prop.RenderToShadowTexture == false)
            {
                Matrix4x4 normalmatrix = Matrix4x4.Transpose(Matrix4x4.Invert(prop.ModelviewMatrix * prop.InverseCameraMatrix)); //Zur Transformation der Normalen von Objekt in Eye-Koordinaten
                Matrix4x4 objToWorld = prop.ModelviewMatrix * prop.InverseCameraMatrix;
                Matrix4x4 worldToObj = Matrix4x4.Invert(objToWorld);

                return new TriangleShaderData()
                {
                    UniformVariables = new ShaderDataForTriangleNormal()
                    {
                        WorldViewProj = prop.ModelviewMatrix * prop.ProjectionMatrix,
                        NormalMatrix = normalmatrix,
                        ObjToWorld = objToWorld,
                        WorldToObj = worldToObj,
                        ShadowMatrix = prop.ShadowMatrix,
                        TextureMatrix = prop.TextureMatrix
                    },

                    VertexShader = VertexShader.VertexShaderForTriangles,
                    GeometryShader = new GeometryShader(this.prop).ForTriangleNormal,
                    PixelShader = PixelShaderNormal,
                };
            }
            else
            {
                return new TriangleShaderData()
                {
                    UniformVariables = new ShaderDataForShadowMapCreation()
                    {
                        ShadowMatrix = prop.ShadowMatrix,
                        TextureMatrix = prop.TextureMatrix
                    },

                    VertexShader = VertexShader.VertexShaderForTrianglesShadowMapCreation,
                    GeometryShader = new GeometryShader(this.prop).ForTriangleShadowMapCreation,
                    PixelShader = PixelShaderCreateShadowMap,
                };
            }
        }

        private Color PixelShaderNormal(PixelShaderInput data)
        {
            if (prop.NormalSource == NormalSource.Parallax)
                return new PixelShaderParallaxMapping(prop).GetPixelColor(data);
            else
                return new PixelShaderNormal(prop).GetPixelColor(data);
        }

        private Color PixelShaderCreateShadowMap(PixelShaderInput data)
        {
            return new PixelShaderCreateShadowMap(prop).GetPixelColor(data);
        }

        private void DrawTriangle(Triangle triangle, TriangleShaderData shaderData)
        {
            var triangles = ObjectSpaceToWindowSpaceConverter.TransformTriangleFromObjectToWindowSpace(
                triangle,
                shaderData.UniformVariables,
                shaderData.VertexShader,
                shaderData.GeometryShader,
                prop.ViewPort);


            //So geht es nicht! Die gelbe Schrift im Spiegel bei ShadowsAndBlending ist dann dunkel weil die FlatNormale von der Schrift falsch ist.
            //Wenn man eine ReflectionMatrix verwendet, dann kann man anscheinend nicht mit Invert-Transpose arbeiten um eine Normalmatrix zu erzeugen.
            //Matrix4x4 normalmatrix = Matrix4x4.Transpose(Matrix4x4.Invert(prop.ModelviewMatrix * prop.InverseCameraMatrix));
            //Vector3D flatNormalWorldSpace = Vector3D.NormalizeWithoutZeroDivision(Matrix4x4.MultDirection(normalmatrix, triangle.Normal));

            //Achtung: Wenn beim Explosionseffekt der Geometry - Shader dem Dreieck eine neue Position gibt, dann ist die Normale hier falsch
            Vector3D triangleNormalWorldSpace = null;
            if (prop.NormalInterpolationMode == InterpolationMode.Flat)
            {
                Matrix4x4 objToWorld = prop.ModelviewMatrix * prop.InverseCameraMatrix;
                var p0 = Matrix4x4.MultPosition(objToWorld, triangle.V[0].Position);
                var p1 = Matrix4x4.MultPosition(objToWorld, triangle.V[1].Position);
                var p2 = Matrix4x4.MultPosition(objToWorld, triangle.V[2].Position);
                triangleNormalWorldSpace = Vector3D.NormalizeWithoutZeroDivision(Vector3D.Cross(p1 - p0, p2 - p0));
            }            

            foreach (var windowTriangle in triangles)
            {
                if (prop.CullFaceIsEnabled && !windowTriangle.IsCounterClockwise != prop.FrontFaceIsClockWise) return;   // Cull Face

                if (prop.WireframeModeIsActive == false)
                {
                    DrawWindowSpaceTriangle(windowTriangle, triangleNormalWorldSpace, shaderData.UniformVariables, shaderData.PixelShader);
                }
                else
                {
                    DrawWireTriangle(windowTriangle, triangleNormalWorldSpace, shaderData.UniformVariables, shaderData.PixelShader);
                }

            }
        }

        private void DrawWindowSpaceTriangle(WindowSpaceTriangle windowTriangle, Vector3D triangleNormalWorldSpace, IUniformVariables uniformVariables, PixelShaderFunc pixelShader)
        {
            var p0 = windowTriangle.W0;
            var p1 = windowTriangle.W1;
            var p2 = windowTriangle.W2;

            //Start: Rechne ab jetzt mit 1/z
            p0.V.StartOrEndPerspectiveDevision();
            p1.V.StartOrEndPerspectiveDevision();
            p2.V.StartOrEndPerspectiveDevision();

            if (prop.Deck0.TextureFilter != TextureFilter.Anisotroph)
            {
                //Nur Pixelmitte
                TriangleRasterizer.DrawWindowSpaceTriangle(p0.WindowPos.XY, p1.WindowPos.XY, p2.WindowPos.XY, (w0, w1, w2) =>
                {
                    var point = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, w0, w1, w2); //Interpoliere im (1/z)-Raum
                    point.V.StartOrEndPerspectiveDevision();//Ende: Gehe von (1/z) zurück nach z

                    DrawPixel(new PixelShaderInput()
                    {
                        PixelCenter = point,
                        UniformVariables = uniformVariables,
                        TriangleNormalWorldSpace = triangleNormalWorldSpace
                    }, pixelShader);
                }, null);
            }
            else
            {
                //Pixelmitte + 4 Eckpunkte
                TriangleRasterizer.DrawWindowSpaceTriangle(p0.WindowPos.XY, p1.WindowPos.XY, p2.WindowPos.XY, null, (pix) =>
                {
                    var c = windowTriangle.GetByzentricCoordinate(new Vector2D(pix.X + 0.5f, pix.Y + 0.5f));
                    var lo = windowTriangle.GetByzentricCoordinate(new Vector2D(pix.X + 0.0f, pix.Y + 0.0f));
                    var ro = windowTriangle.GetByzentricCoordinate(new Vector2D(pix.X + 1.0f, pix.Y + 0.0f));
                    var lu = windowTriangle.GetByzentricCoordinate(new Vector2D(pix.X + 0.0f, pix.Y + 1.0f));
                    var ru = windowTriangle.GetByzentricCoordinate(new Vector2D(pix.X + 1.0f, pix.Y + 1.0f));

                    var pc = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, c.X, c.Y, c.Z);     //Pixelmitte
                    var plo = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, lo.X, lo.Y, lo.Z); //Links oben
                    var pro = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, ro.X, ro.Y, ro.Z); //Rechts oben
                    var plu = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, lu.X, lu.Y, lu.Z); //Links unten
                    var pru = WindowSpacePoint.InterpolateByzentric(p0, p1, p2, ru.X, ru.Y, ru.Z); //Rechts unten

                    pc.V.StartOrEndPerspectiveDevision();
                    plo.V.StartOrEndPerspectiveDevision();
                    pro.V.StartOrEndPerspectiveDevision();
                    plu.V.StartOrEndPerspectiveDevision();
                    pru.V.StartOrEndPerspectiveDevision();

                    DrawPixel(new PixelShaderInput()
                    {
                        PixelCenter = pc,
                        PixelLeftTop = plo,
                        PixelRightTop = pro,
                        PixelLeftBottom = plu,
                        PixelRightBottom = pru,
                        UniformVariables = uniformVariables,
                        TriangleNormalWorldSpace = triangleNormalWorldSpace
                    }, pixelShader);
                });
            }
        }

        private void DrawWireTriangle(WindowSpaceTriangle windowTriangle, Vector3D triangleNormalWorldSpace, IUniformVariables uniformVariables, PixelShaderFunc pixelShader)
        {
            var p0 = windowTriangle.W0;
            var p1 = windowTriangle.W1;
            var p2 = windowTriangle.W2;

            DrawEdgeFromWireTriangle(p0, p1, uniformVariables, pixelShader, triangleNormalWorldSpace);
            DrawEdgeFromWireTriangle(p1, p2, uniformVariables, pixelShader, triangleNormalWorldSpace);
            DrawEdgeFromWireTriangle(p2, p0, uniformVariables, pixelShader, triangleNormalWorldSpace);
        }

        private void DrawEdgeFromWireTriangle(WindowSpacePoint p0, WindowSpacePoint p1, IUniformVariables uniformVariables, PixelShaderFunc pixelShader, Vector3D triangleNormalWorldSpace)
        {
            p0.V.StartOrEndPerspectiveDevision();
            p1.V.StartOrEndPerspectiveDevision();

            LineRasterizer.DrawLine(p0.XY, p1.XY, (pix, f) =>
            {
                var point = WindowSpacePoint.InterpolateLinear(p0, p1, f);
                point.V.StartOrEndPerspectiveDevision();

                if (point.XY.X >= 0 && point.XY.X < prop.Buffer.Width && point.XY.Y >= 0 && point.XY.Y < prop.Buffer.Height)
                {
                    DrawPixel(new PixelShaderInput()
                    {
                        PixelCenter = point,
                        UniformVariables = uniformVariables,
                        TriangleNormalWorldSpace = triangleNormalWorldSpace
                    }, pixelShader);
                }
            });

            p0.V.StartOrEndPerspectiveDevision();
            p1.V.StartOrEndPerspectiveDevision();
        }

        private void DrawPixel(PixelShaderInput data, PixelShaderFunc pixelShader)
        {
            Point pixPos = data.PixelCenter.XY;
            float windowZ = data.PixelCenter.Z;

            if (prop.DepthTestingIsEnabled)                                 // Dephtest
            {
                if (windowZ < 0 || windowZ > 1) return;                     // Pixel liegt nicht im Sichtbereich
                if (prop.Buffer.Depth[pixPos.X, pixPos.Y] < windowZ) return;  // Ein anderer Pixel liegt bereits da
            }

            Color color = pixelShader(data);
            if (color == Color.Transparent) return;

            if (prop.MouseHit.IsMouseHitTestActive(pixPos, windowZ)) return;

            //Schreibe/Lese in Stencilpuffer
            if (prop.StenciltestIsEnabled)                                   // Stencil
            {
                switch (prop.StencilFunction)
                {
                    case StencilFunction.WriteOneSideIncrease:
                        prop.Buffer.Stencil[pixPos.X, pixPos.Y]++;
                        break;

                    case StencilFunction.WriteOneSideDecrease:
                        if (prop.Buffer.Stencil[pixPos.X, pixPos.Y] > 0) prop.Buffer.Stencil[pixPos.X, pixPos.Y]--;
                        break;

                    case StencilFunction.ReadNotEqualZero:
                        if (prop.Buffer.Stencil[pixPos.X, pixPos.Y] == 0) return;//Stencil-Test-Fail
                        break;

                }
            }

            // Schreibe in Tiefenpuffer
            if (prop.WritingToDepthBuffer)
            {
                prop.Buffer.Depth[pixPos.X, pixPos.Y] = windowZ;
            }

            //Schreibe in Farbpuffer
            if (prop.WritingToColorBuffer)
            {
                if (prop.BlendingIsEnabled)                                    // Blending
                {
                    if (prop.BlendingMode == BlendingMode.WithAlpha)
                        color = ColorHelper.ColorAlphaBlending(prop.Buffer.Color[pixPos.X, pixPos.Y], color, prop.CurrentColor.W);
                }

                prop.Buffer.Color[pixPos.X, pixPos.Y] = color;
            }
        }
    }
}
