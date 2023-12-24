using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using GraphicMinimal;
using GraphicGlobal;
using System.IO;
using BitmapHelper;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;

namespace GraphicPanels
{
    //Hat Menge von IDrawingPanels, wo jedes für sich ein Control hat. 
    //GraphicPanel hat immer nur ein IDrawingPanel aktiv und ist für das Umschalten zwischen den verschiedenen IDrawingPanels zuständig
    //Da keine Mehrfachvererbung in C# erlaubt ist, habe ich hier auch alle 2D-Zeichenfunktion mit drin, da diese sowohl im GraphicPanel2D als auch GraphicPanel3D enthalten sein sollen
    public partial class GraphicPanel : UserControl
    {
        private enum MouseEventName
        {
            MouseMove,
            MouseClick,
            MouseDown,
            MouseUp,
            MouseDoubleClick,
            MouseWheel,            
        }

        private enum ControlEventName
        {
            SizeChanged,
            MouseEnter,
            MouseLeave
        }

        private Dictionary<MouseEventName, List<MouseEventHandler>> events = new Dictionary<MouseEventName, List<MouseEventHandler>>();
        private Dictionary<ControlEventName, List<EventHandler>> controlEvents = new Dictionary<ControlEventName, List<EventHandler>>();

        internal DrawingPanelContainer controls; // Zeigt Anzeigedaten an

        public GraphicPanel()
        {
            InitializeComponent();

            this.controls = new DrawingPanelContainer(ControlWasCreatedHandler);

            foreach (var e in Enum.GetValues(typeof(MouseEventName)).Cast<MouseEventName>())
            {
                events.Add(e, new List<MouseEventHandler>());
            }

            foreach (var e in Enum.GetValues(typeof(ControlEventName)).Cast<ControlEventName>())
            {
                controlEvents.Add(e, new List<EventHandler>());
            }
        }

        protected void SwitchMode(Mode3D mode)
        {
            this.Controls.Clear();
            this.Controls.Add(this.controls.GetPanel(mode).DrawingControl);            
        }

        protected void SwitchMode(Mode2D mode)
        {
            this.Controls.Clear();
            this.Controls.Add(this.controls.GetPanel(mode).DrawingControl);
        }

        protected virtual T GetPanel<T>() { throw new NotImplementedException(); }

        private void ControlWasCreatedHandler(Control control)
        {
            foreach (var eventName in this.events.Keys)
            {
                foreach (var eventHandler in this.events[eventName])
                {
                    AddEventHandlerToControl(control, eventName, eventHandler);
                }
            }
            foreach (var eventName in this.controlEvents.Keys)
            {
                foreach (var eventHandler in this.controlEvents[eventName])
                {
                    AddEventHandlerToControl(control, eventName, eventHandler);
                }
            }
        }

        private void AddControlEventHandler(ControlEventName eventName, EventHandler handler)
        {
            this.controlEvents[eventName].Add(handler);

            foreach (var c in this.controls.GetAllLoadedControls())
            {
                AddEventHandlerToControl(c, eventName, handler);
            }
        }
        private void RemoveControlEventHandler(ControlEventName eventName, EventHandler handler)
        {
            this.controlEvents[eventName].Remove(handler);

            foreach (var c in this.controls.GetAllLoadedControls())
            {
                RemoveEventHandlerFromControl(c, eventName, handler);
            }
        }

        private void AddMouseEventHandler(MouseEventName eventName, MouseEventHandler handler)
        {
            this.events[eventName].Add(handler);

            foreach (var c in this.controls.GetAllLoadedControls())
            {
                AddEventHandlerToControl(c, eventName, handler);
            }
        }

        private void RemoveMouseEventHandler(MouseEventName eventName, MouseEventHandler handler)
        {
            this.events[eventName].Remove(handler);

            foreach (var c in this.controls.GetAllLoadedControls())
            {
                RemoveEventHandlerFromControl(c, eventName, handler);
            }
        }

        private void AddEventHandlerToControl(Control control, ControlEventName eventName, EventHandler handler)
        {
            switch (eventName)
            {
                case ControlEventName.SizeChanged:
                    control.SizeChanged += handler;
                    break;
                case ControlEventName.MouseEnter:
                    control.MouseEnter += handler;
                    break;
                case ControlEventName.MouseLeave:
                    control.MouseLeave += handler;
                    break;

                default:
                    throw new ArgumentException("Unknown EventName: " + eventName.ToString());
            }
        }

        private void RemoveEventHandlerFromControl(Control control, ControlEventName eventName, EventHandler handler)
        {
            switch (eventName)
            {
                case ControlEventName.SizeChanged:
                    control.SizeChanged -= handler;
                    break;
                case ControlEventName.MouseEnter:
                    control.MouseEnter -= handler;
                    break;
                case ControlEventName.MouseLeave:
                    control.MouseLeave -= handler;
                    break;

                default:
                    throw new ArgumentException("Unknown EventName: " + eventName.ToString());
            }
        }

        private void AddEventHandlerToControl(Control control, MouseEventName eventName, MouseEventHandler handler)
        {
            switch (eventName)
            {
                case MouseEventName.MouseMove:
                    control.MouseMove += handler;
                    break;
                case MouseEventName.MouseClick:
                    control.MouseClick += handler;
                    break;
                case MouseEventName.MouseDown:
                    control.MouseDown += handler;
                    break;
                case MouseEventName.MouseUp:
                    control.MouseUp += handler;
                    break;
                case MouseEventName.MouseDoubleClick:
                    control.MouseDoubleClick += handler;
                    break;
                case MouseEventName.MouseWheel:
                    control.MouseWheel += handler;
                    break;                
                default:
                    throw new ArgumentException("Unknown EventName: " + eventName.ToString());
            }
        }

        private void RemoveEventHandlerFromControl(Control control, MouseEventName eventName, MouseEventHandler handler)
        {
            switch (eventName)
            {
                case MouseEventName.MouseMove:
                    control.MouseMove -= handler;
                    break;
                case MouseEventName.MouseClick:
                    control.MouseClick -= handler;
                    break;
                case MouseEventName.MouseDown:
                    control.MouseDown -= handler;
                    break;
                case MouseEventName.MouseUp:
                    control.MouseUp -= handler;
                    break;
                case MouseEventName.MouseDoubleClick:
                    control.MouseDoubleClick -= handler;
                    break;
                case MouseEventName.MouseWheel:
                    control.MouseWheel -= handler;
                    break;
                
                default:
                    throw new ArgumentException("Unknown EventName: " + eventName.ToString());
            }
        }

        public new event EventHandler SizeChanged
        {
            add
            {
                //Die Zeichenroutingen für das Panel haben nur dann ein Effekt, wenn sie aus ein Maus-Handler oder Timer gerufen werden
                //Ruft man ihn aus dem SizeChanged-Handler direkt, dann bleibt das Fenster schwarz.
                //Ich erzeuge hier eine Action, welche ein Task im GUI-Threadaufruft, welcher dann dann hier übergebenen EventHandler aufruft,
                //welcher dann die eigentliche User-Zeichenroutine aufruft
                EventHandler action = (sender, e) =>
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        value(sender, e);
                    }));
                };
                
                AddControlEventHandler(ControlEventName.SizeChanged, action);
            }
            remove
            {
                RemoveControlEventHandler(ControlEventName.SizeChanged, value);
            }
        }

        public new event EventHandler MouseEnter
        {
            add
            {
                AddControlEventHandler(ControlEventName.MouseEnter, value);
            }
            remove
            {
                RemoveControlEventHandler(ControlEventName.MouseEnter, value);
            }
        }

        public new event EventHandler MouseLeave
        {
            add
            {
                AddControlEventHandler(ControlEventName.MouseLeave, value);
            }
            remove
            {
                RemoveControlEventHandler(ControlEventName.MouseLeave, value);
            }
        }

        public new event MouseEventHandler MouseMove
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseMove, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseMove, value);
            }
        }

        public new event MouseEventHandler MouseClick
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseClick, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseClick, value);
            }
        }

        public new event MouseEventHandler MouseDown
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseDown, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseDown, value);
            }
        }

        public new event MouseEventHandler MouseUp
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseUp, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseUp, value);
            }
        }

        public new event MouseEventHandler MouseDoubleClick
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseDoubleClick, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseDoubleClick, value);
            }
        }

        public new event MouseEventHandler MouseWheel
        {
            add
            {
                AddMouseEventHandler(MouseEventName.MouseWheel, value);
            }
            remove
            {
                RemoveMouseEventHandler(MouseEventName.MouseWheel, value);
            }
        }

        

        public virtual void ClearScreen(string backgroundImage)
        {
            if (IsColorString(backgroundImage))
            {
                this.GetPanel<IDrawingSynchron>().ClearScreen(StringToColor(backgroundImage));
            }
            else
            {
                this.GetPanel<IDrawingSynchron>().ClearScreen(GetTextureId(backgroundImage, false));
            }            
        }

        public virtual void ClearScreen(Color color)
        {
            this.GetPanel<IDrawingSynchron>().ClearScreen(color);
        }

        public void FlipBuffer()
        {
            this.GetPanel<IDrawingSynchron>().FlipBuffer();
        }

        #region IDrawing2D

        //Die gleiche Funktion gibts im PixelHelper nochmal. Ich denke ich muss diese Klasse in ein Projekt, was über GraphicMinimal aber unter  Rasterizer/RaytracrerMain liegt auslagern
        private static bool IsColorString(string colorString)
        {
            return (colorString.Length == 7 || colorString.Length == 9) && colorString[0] == '#';
        }
        private static Color StringToColor(string colorString)
        {
            int[] color = new int[] { 255, 255, 255, 255 };

            color[0] = int.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            color[1] = int.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            color[2] = int.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            if (colorString.Length == 9) color[3] = int.Parse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(color[3], color[0], color[1], color[2]);
        }

        //Speichert für jeden Grafikmodus den Texturname-TexturGrafikmodus-InternID-Schlüssel
        private Dictionary<string, int> textureCache = new Dictionary<string, int>();

        private int GetTextureId(string textureFile, bool makeFirstPixelTransparent)
        {
            string key = textureFile + makeFirstPixelTransparent + this.GetPanel<IDrawingSynchron>().Pipeline.GetType().ToString();

            if (this.textureCache.ContainsKey(key) == false)
            {
                Bitmap image = null;
                if (this.bitmapNames.ContainsKey(textureFile))
                {
                    image = this.bitmapNames[textureFile];
                }
                else
                {
                    if (!File.Exists(textureFile)) throw new FileNotFoundException(textureFile);
                    image = new Bitmap(textureFile);
                }
                if (makeFirstPixelTransparent) image = BitmapHelp.TransformColorToMaxAlpha(image, image.GetPixel(0, 0));
                int textureId = this.GetPanel<IDrawing2D>().GetTextureId(image);
                this.textureCache.Add(key, textureId);
            }

            return this.textureCache[key];
        }

        //Speichert "Named-Bitmaps". Das ist unabhängig vom Grafikmodus
        private Dictionary<string, Bitmap> bitmapNames = new Dictionary<string, Bitmap>();

        //Damit kann ich neben Texturen aus Dateien auch beliebige andere Bitmaps bei den 2D-Zeichenfunktionen nehmen
        //Verwendung: Erst mit CreateOrUpdateNamedBitmapTexture("DeinTexturName", new Bitmap(12,78)); Bitmap-Textur anlegen
        //Dann mit DrawImage("DeinTexturName", ...)/ DrawFillPolygon("DeinTexturName",...) die Textur verwenden
        public void CreateOrUpdateNamedBitmapTexture(string nameWhichIsUsedForThe2DDrawingMethods, Bitmap texture)
        {
            string key = nameWhichIsUsedForThe2DDrawingMethods;

            if (this.bitmapNames.ContainsKey(key) == false)
            {
                this.bitmapNames.Add(key, texture);
            }
            else
            {
                this.bitmapNames[key] = texture; //Update Textur
                //Wenn LoadTexture nochmal angewendet wird, dann werden die alten Texturdaten hiermit gelöscht
                var updatedKeys = this.textureCache.Keys.Where(x => x.StartsWith(nameWhichIsUsedForThe2DDrawingMethods)).ToList();
                foreach (var remkey in updatedKeys)
                {
                    this.textureCache.Remove(remkey); //Gehe über alle Grafikmodi, wo dieses Bitmap bereits geladen wurde und lösche es dort
                }
            }
        }

        public bool IsNamedBitmapTextureAvailable(string nameWhichIsUsedForThe2DDrawingMethods)
        {
            return this.bitmapNames.ContainsKey(nameWhichIsUsedForThe2DDrawingMethods);
        }

        public Size GetTextureSize(string nameWhichIsUsedForThe2DDrawingMethods)
        {
            string key = nameWhichIsUsedForThe2DDrawingMethods;
            return new Size(this.bitmapNames[key].Width, this.bitmapNames[key].Height);
        }

        public void DrawLine(Pen pen, Vector2D p1, Vector2D p2)
        {
            this.GetPanel<IDrawing2D>().DrawLine(pen, p1, p2);
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            this.GetPanel<IDrawing2D>().DrawPixel(pos, color, size);
        }

        public Size GetStringSize(float size, string text)
        {
            return this.GetPanel<IDrawing2D>().GetStringSize(size, text);
        }

        public void DrawString(float x, float y, Color color, float size, string text)
        {
            this.GetPanel<IDrawing2D>().DrawString(x, y, color, size, text);
        }

        public void DrawString(Vector2D position, Color color, float size, string text)
        {
            DrawString(position.X, position.Y, color, size, text);
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            this.GetPanel<IDrawing2D>().DrawRectangle(pen, x, y, width, height);
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            this.GetPanel<IDrawing2D>().DrawPolygon(pen, points);
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            this.GetPanel<IDrawing2D>().DrawCircle(pen, pos, radius);
        }

        public void DrawCircle(Pen pen, Vector2D pos, float radius)
        {
            DrawCircle(pen, pos, (int)radius);
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            this.GetPanel<IDrawing2D>().DrawFillCircle(color, pos, radius);
        }

        public void DrawFillCircle(Color color, Vector2D pos, float radius)
        {
            DrawFillCircle(color, pos, (int)radius);
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            this.GetPanel<IDrawing2D>().DrawCircleArc(pen, pos, radius, startAngle, endAngle, withBorderLines);
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            this.GetPanel<IDrawing2D>().DrawFillCircleArc(color, pos, radius, startAngle, endAngle);
        }

        public void DrawImage(string texture, float x, float y, float width, float height, float sourceX, float sourceY, float sourceWidth, float sourceHeight, bool makeFirstPixelTransparent, Color colorFactor)
        {
            this.GetPanel<IDrawing2D>().DrawImage(GetTextureId(texture, makeFirstPixelTransparent), x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor);
        }

        public void DrawImage(string texture, float x, float y, float width, float height, float sourceX, float sourceY, float sourceWidth, float sourceHeight, bool makeFirstPixelTransparent, Color colorFactor, float zAngle, float yAngle)
        {
            this.GetPanel<IDrawing2D>().DrawImage(GetTextureId(texture, makeFirstPixelTransparent), x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor, zAngle, yAngle);
        }

        public void DrawFillRectangle(string texture, float x, float y, float width, float height, bool makeFirstPixelTransparent, Color colorFactor)
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(GetTextureId(texture, makeFirstPixelTransparent), x, y, width, height, colorFactor);
        }

        public void DrawFillRectangle(string texture, float x, float y, float width, float height, bool makeFirstPixelTransparent, Color colorFactor, float angle)
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(GetTextureId(texture, makeFirstPixelTransparent), x, y, width, height, colorFactor, angle);
        }

        public void DrawFillRectangle(string texture, float x, float y, float width, float height, bool makeFirstPixelTransparent, Color colorFactor, float zAngle, float yAngle)
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(GetTextureId(texture, makeFirstPixelTransparent), x, y, width, height, colorFactor, zAngle, yAngle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(color, x, y, width, height);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(color, x, y, width, height, angle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            this.GetPanel<IDrawing2D>().DrawFillRectangle(color, x, y, width, height, zAngle, yAngle);
        }

        public void DrawFillPolygon(string texture, List<Vector2D> points, bool makeFirstPixelTransparent, Color colorFactor)
        {
            List<Triangle2D> triangleList = new Polygon(points).TransformToTriangleList();
            this.GetPanel<IDrawing2D>().DrawFillPolygon(GetTextureId(texture, makeFirstPixelTransparent), colorFactor, triangleList);
        }

        public void DrawFillPolygon(string texture, List<Vector2D> points, bool makeFirstPixelTransparent, Color colorFactor, RectangleF texturRec)
        {
            List<Triangle2D> triangleList = new Polygon(points).TransformToTriangleList(texturRec);
            DrawFillPolygon(texture, makeFirstPixelTransparent, colorFactor, triangleList);
        }

        private void DrawFillPolygon(string texture, bool makeFirstPixelTransparent, Color colorFactor, List<Triangle2D> triangleList)
        {
            this.GetPanel<IDrawing2D>().DrawFillPolygon(GetTextureId(texture, makeFirstPixelTransparent), colorFactor, triangleList);
        }

        public void DrawFillPolygon(string texture, bool makeFirstPixelTransparent, Color colorFactor, List<Vertex2D> points)
        {
            List<Triangle2D> triangleList = new Polygon(points).TransformToTriangleList();
            this.GetPanel<IDrawing2D>().DrawFillPolygon(GetTextureId(texture, makeFirstPixelTransparent), colorFactor, triangleList);
        }

        public void DrawFillPolygon(Color color, List<Vector2D> points)
        {
            List<Triangle2D> triangleList = new Polygon(points).TransformToTriangleList();
            DrawFillPolygon(color, triangleList);
        }

        private void DrawFillPolygon(Color color, List<Triangle2D> triangleList)        
        {
            this.GetPanel<IDrawing2D>().DrawFillPolygon(color, triangleList);
        }

        public void DrawSprite(string spriteFile, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, bool makeFirstPixelTransparent, Color colorFactor)
        {
            this.GetPanel<IDrawing2D>().DrawSprite(GetTextureId(spriteFile, makeFirstPixelTransparent), xCount, yCount, xBild, yBild, x, y, width, height, colorFactor);
        }

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            this.GetPanel<IDrawing2D>().EnableScissorTesting(x, y, width, height);
        }

        public void DisableScissorTesting()
        {
            this.GetPanel<IDrawing2D>().DisableScissorTesting();
        }

        public Bitmap GetTextureData(int textureID)
        {
            return this.GetPanel<IDrawing2D>().GetTextureData(textureID);
        }

        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            return this.GetPanel<IDrawing2D>().CreateFramebuffer(width, height, withColorTexture, withDepthTexture);
        }

        public void EnableRenderToFramebuffer(int framebufferId)
        {
            this.GetPanel<IDrawing2D>().EnableRenderToFramebuffer(framebufferId);
        }

        public void DisableRenderToFramebuffer()
        {
            this.GetPanel<IDrawing2D>().DisableRenderToFramebuffer();
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return this.GetPanel<IDrawing2D>().GetColorTextureIdFromFramebuffer(framebufferId);
        }

#endregion
    }
}
