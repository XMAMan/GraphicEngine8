﻿using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;
using System.Drawing;
using BitmapHelper;

namespace GraphicPanels
{
    //Ein Windows-Control, was als Kindelement eine Sammlung von IDrawingPanel/IDrawingSynchron/IDrawing2D-Elementen enthält
    public class GraphicPanel2D : GraphicPanel
    {
        private Mode2D mode = Mode2D.CPU;
        public Mode2D Mode
        {
            get
            {
                return this.mode;
            }
            set
            {
                this.mode = value;
                SwitchMode(value);
                GetPanel<IDrawing3D>().Enable2DModus();
            }
        }

        protected override T GetPanel<T>()
        {
            var panel = this.controls.GetPanel(this.mode);
            if ((panel is T) == false) throw new InterfaceFromDrawingPanelNotSupportedException(typeof(T), this.mode.ToString(), "The mode " + this.mode + " does not support the Interface " + typeof(T).Name);
            return (T)this.controls.GetPanel(this.mode);
        }

        public override void ClearScreen(string backgroundImage)
        {
            GetPanel<IDrawing3D>().Enable2DModus();
            base.ClearScreen(backgroundImage);
        }

        public override void ClearScreen(Color color)
        {
            GetPanel<IDrawing3D>().Enable2DModus();
            base.ClearScreen(color);
        }

        public Bitmap GetScreenShoot()
        {
            return BitmapHelp.SetAlpha(this.GetPanel<IDrawingSynchron>().GetDataFromFrontbuffer(), 255);
        }

        public static List<Vertex2D[]> GetVoronoiPolygons(Size imageSize, List<Point> cellPoints)
        {
            return BitmapHelp.GetVoronoiPolygons(imageSize, cellPoints);
        }

        public static List<Point> GetRandomPointList(int cellPointCount, int maxX, int maxY, Random rand)
        {
            List<Point> cellPoints = new List<Point>();
            for (int i = 0; i < cellPointCount; i++)
            {
                Point P = new Point((int)(rand.NextDouble() * maxX), (int)(rand.NextDouble() * maxY));
                if (!cellPoints.Contains(P)) cellPoints.Add(P);
            }
            return cellPoints;
        }

        public static Bitmap TransformColorToMaxAlpha(Bitmap image, Color color, float colorBias)
        {
            return BitmapHelp.TransformColorToMaxAlpha(image, color, colorBias);
        }

        public float ZValue2D
        {
            get => GetPanel<IDrawing2D>().ZValue2D;
            set => GetPanel<IDrawing2D>().ZValue2D = value;
        }

        public void EnableDepthTesting()
        {
            GetPanel<IDrawing2D>().EnableDepthTesting();
        }
        public void DisableDepthTesting()
        {
            GetPanel<IDrawing2D>().DisableDepthTesting();
        }

        public void EnableWritingToTheDepthBuffer()
        {
            GetPanel<IDrawing2D>().EnableWritingToTheDepthBuffer();
        }

        public void DisableWritingToTheDepthBuffer()
        {
            GetPanel<IDrawing2D>().DisableWritingToTheDepthBuffer();
        }

        public void SetTransformationMatrixToIdentity()
        {
            GetPanel<IDrawing3D>().Pipeline.SetModelViewMatrixToIdentity();
        }

        public void MultTransformationMatrix(Matrix4x4 matrix)
        {
            GetPanel<IDrawing3D>().Pipeline.MultMatrix(matrix);
        }

        public Matrix4x4 GetTransformationMatrix()
        {
            return GetPanel<IDrawing3D>().Pipeline.GetModelViewMatrix();
        }

        public void PushMatrix()
        {
            GetPanel<IDrawing3D>().Pipeline.PushMatrix();
        }

        public void PopMatrix()
        {
            GetPanel<IDrawing3D>().Pipeline.PopMatrix();
        }
    }
}
