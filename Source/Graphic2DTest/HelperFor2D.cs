using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Graphic2DTest
{
    //Beschreibt ein 2D-Bild, was auch animiert werden kann
    //Diese Klasse ist nötig, da ich nur so die 2D-Testscene sowohl im Tools-Projekt als uach im GraphicPanelsTest-Projekt nehmen kann
    //ohne dass ich im Tools-Projekt ein Verweis auf MSTest habe
    public static class HelperFor2D
    {
        public class TextureData
        {
            public Bitmap Image;
            public string TextureName;
        }

        //Erstellt im Texturspeicher des GrafikPanel2D vom aktuell ausgewählten 2D-Modus eine Mario-Textur 
        //Returnwert: Name der erstellten Textur. Existiert die Textur bereits, wird sie aktualisiert
        public static TextureData CreateMarioTexture(GraphicPanel2D graphic, string texturFile, float yAngle)
        {
            int frameBufferId = graphic.CreateFramebuffer(80, 80, true, false);
            graphic.EnableRenderToFramebuffer(frameBufferId);
            graphic.ClearScreen(Color.Transparent);
            //Nimm aus den großen Bild nur den Abschnitt, wo der Mario zu sehen ist und male in in den Framebuffer
            graphic.DrawImage(texturFile, graphic.Width / 2, graphic.Height / 2, graphic.Width, graphic.Height, 243, 202, 283 - 243, 240 - 202, true, Color.FromArgb(255, 255, 255), 0, yAngle);
            graphic.FlipBuffer();
            int colorTextureId = graphic.GetColorTextureIdFromFramebuffer(frameBufferId);
            Bitmap marioTexture = graphic.GetTextureData(colorTextureId);
            graphic.DisableRenderToFramebuffer();
            graphic.CreateOrUpdateNamedBitmapTexture("MarioBitmap", marioTexture);

            //marioTexture.Save("MarioTexture.bmp");
            //Color c = marioTexture.GetPixel(0, 0);

            return new TextureData()
            {
                TextureName = "MarioBitmap",
                Image = marioTexture
            };
        }

        public static Vertex2D[] TransformPolygon(Vertex2D[] polygon, Vector2D position)
        {
            return polygon.Select(x => new Vertex2D(x.Position + position, x.Textcoord)).ToArray();
        }

        public static void Draw2D(GraphicPanel2D graphic, string dataDirectory, int spriteNr, List<Vertex2D[]> voronioPolygons, List<Point> voronoiCellPoints, TextureData marioTex, bool showScreenAlpha)
        {
            /*graphic.ClearScreen(Color.White);
             foreach (var polygon in this.voronioPolygons)
             {
                 graphic.DrawFillPolygon(WorkingDirectory + "thumb_COLOURBOX5847554.jpg", false, Color.FromArgb(255, 255, 255), polygon.ToList());
                 graphic.DrawPolygon(new Pen(Color.Black, 2), polygon.Select(x => x.Position).ToList());
             }
             foreach (var point in this.voronoiCellPoints)
             {
                 graphic.DrawFillCircle(Color.Red, new Vector2D(point.X, point.Y), 2);
             }

             graphic.FlipBuffer();
             return;*/

            string text = graphic.Mode.ToString();
            graphic.ClearScreen(dataDirectory + "thumb_COLOURBOX5847554.jpg");

            Size size = graphic.GetStringSize(20, text);

            graphic.EnableScissorTesting(300, 20, 60, 25);
            graphic.DrawImage(dataDirectory + "nes_super_mario_bros.png", 300, 20, 60, 50, 243, 202, 283 - 243, 240 - 202, true, Color.FromArgb(255, 255, 255));
            graphic.DisableScissorTesting();
            graphic.DrawImage(dataDirectory + "nes_super_mario_bros.png", 300, 100, 60, 50, 243, 202, 283 - 243, 240 - 202, true, Color.FromArgb(255, 255, 255), 0, 180);

            graphic.DrawImage("MarioBitmap", 420, 50, 40, 40, 0, 0, 80, 80, true, Color.FromArgb(0, 255, 0));

            //Prüfe ab, dass der Alpha-Wert der Mario-Textur stimmt
            graphic.CreateOrUpdateNamedBitmapTexture("MarioBitmapAlpha", BitmapHelp.GetAlphaChannel(marioTex.Image));
            graphic.DrawFillRectangle("MarioBitmapAlpha", 420, 90, 40, 40, false, Color.FromArgb(255, 255, 255));

            foreach (var polygon in voronioPolygons)
            {
                graphic.DrawFillPolygon("MarioBitmap", false, Color.FromArgb(255, 255, 255), polygon.ToList());
                graphic.DrawPolygon(new Pen(Color.Black, 2), polygon.Select(x => x.Position).ToList());
            }
            foreach (var point in voronoiCellPoints)
            {
                graphic.DrawFillCircle(Color.Red, new Vector2D(point.X + 340, point.Y + 30), 2);
            }

            //graphic.DrawRectangle(new Pen(Color.Black, 3), 30, 30, size.Width, size.Height);
            graphic.DrawFillRectangle(dataDirectory + "Mario.png", 10, 50, 40, 40, true, Color.FromArgb(spriteNr % 255, 255, 255, 255));
            graphic.DrawString(30, 30, Color.Black, 10, text);
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 0), new Vector2D(graphic.Width, graphic.Height));
            graphic.DrawPixel(new Vector2D(30, 30), Color.Green, 5);
            graphic.DrawFillPolygon(dataDirectory + "Decal.bmp", new List<Vector2D>() { new Vector2D(100, 100), new Vector2D(110, 110), new Vector2D(120, 70), new Vector2D(100, 50), new Vector2D(70, 90) }, false, Color.FromArgb(255, 255, 255));
            graphic.DrawFillPolygon(dataDirectory + "Decal.bmp", new List<Vector2D>() { new Vector2D(100 + 100, 100), new Vector2D(110 + 100, 110), new Vector2D(120 + 100, 70), new Vector2D(100 + 100, 50), new Vector2D(70 + 100, 90) }, false, Color.FromArgb(spriteNr % 255, 255, 255, 255));
            graphic.DrawFillPolygon(Color.Red, new List<Vector2D>() { new Vector2D(100, 100 + 70), new Vector2D(110, 110 + 70), new Vector2D(120, 70 + 70), new Vector2D(100, 50 + 70), new Vector2D(70, 90 + 70) });
            graphic.DrawFillPolygon(Color.Green, new List<Vector2D>() { new Vector2D(100 + 100, 100 + 70), new Vector2D(110 + 100, 110 + 70), new Vector2D(120 + 100, 70 + 70), new Vector2D(100 + 100, 50 + 70), new Vector2D(70 + 100, 90 + 70) });
            graphic.DrawPolygon(new Pen(Color.BlueViolet, 3), new List<Vector2D>() { new Vector2D(100, 100), new Vector2D(110, 110), new Vector2D(120, 70), new Vector2D(100, 50), new Vector2D(70, 90) });
            graphic.DrawCircle(new Pen(Color.BurlyWood, 3), new Vector2D(40, 200), 35);
            graphic.DrawFillCircle(Color.BurlyWood, new Vector2D(40, 250), 25);
            graphic.DrawFillRectangle(dataDirectory + "Schildkroete.png", 200, 200, 30, 20, true, Color.FromArgb(255, 255, 255));
            graphic.DrawFillRectangle(dataDirectory + "Schildkroete.png", 240, 240, 40, 30, true, Color.FromArgb(255, 255, 255), 30);
            graphic.DrawFillRectangle(dataDirectory + "Schildkroete.png", 280, 280, 40, 30, true, Color.FromArgb(255, 255, 255), 30, 50);
            graphic.DrawFillRectangle(Color.Red, 200 + 70, 200, 30, 20);
            graphic.DrawFillRectangle(Color.Green, 240 + 70, 240, 40, 30, 30);
            graphic.DrawFillRectangle(Color.Blue, 280 + 70, 280, 40, 30, 30, 50);            

            //Ich teste hier den Fall, dass ein Dreieck mit P0.X==P1.X gezeichnet wird
            //Feststellung: OpenGL scheint eine Linie immer um ein Pixel nach Links zu verschieben. Wenn ich (3,3) angebe, zeichnet er bei (2,3)
            //Bei ein Dreieck ist die linke obere Ecke richtig und die rechte untere Ecke um ein Pixel nach links oben verschoben
            graphic.DrawPolygon(new Pen(Color.Red, 1), new List<Vector2D>() { new Vector2D(140, 200), new Vector2D(162, 200), new Vector2D(162, 211), new Vector2D(140, 211) });
            graphic.DrawFillPolygon(Color.Green, new List<Vector2D>() { new Vector2D(140, 200), new Vector2D(150, 200), new Vector2D(150, 210), new Vector2D(140, 210) });
            graphic.DrawFillPolygon(Color.Blue, new List<Vector2D>() { new Vector2D(151, 200), new Vector2D(161, 200), new Vector2D(161, 210), new Vector2D(151, 210) });

            //Vertikale Linien mit verschiedner Breite
            for (int i = 0; i < 5; i++)
            {
                graphic.DrawLine(new Pen(Color.Red, 1 + i), new Vector2D(130 + i * 20, 240), new Vector2D(130 + i * 20, 270));
                graphic.DrawPixel(new Vector2D(130 + i * 20, 235), Color.Green, i + 1);
                graphic.DrawPixel(new Vector2D(130 + i * 20, 235), Color.Red, 1);

                for (int j = 0; j <= i; j++)
                {
                    //Ein Pixel wird um eins nach oben verschoben
                    graphic.DrawPixel(new Vector2D(130 + i * 20 + j - (i + 1) / 2, 240 + j + 0.5f), Color.Yellow, 1);
                }
            }

            //Horizontale Linien mit verschiedner Breite
            for (int i = 0; i < 5; i++)
            {
                graphic.DrawLine(new Pen(Color.Red, 1 + i), new Vector2D(140, 300 + i * 10), new Vector2D(170, 300 + i * 10));
                graphic.DrawPixel(new Vector2D(130, 300 + i * 10), Color.Green, i + 1);
                graphic.DrawPixel(new Vector2D(130, 300 + i * 10), Color.Red, 1);

                for (int j = 0; j <= i; j++)
                {
                    //Ein Pixel wird um eins nach oben verschoben
                    graphic.DrawPixel(new Vector2D(140 + j, 300 + i * 10 + j - (i + 0.5f) / 2), Color.Yellow, 1);
                }
            }


            //int spriteNr = 0;
            graphic.DrawSprite(dataDirectory + "fire1.png", 11, 11, spriteNr % 11, spriteNr / 11, 20, 180, 40, 40, true, Color.FromArgb(spriteNr % 255, 255, 255, 255));

            //graphic.DrawLine(new Pen(Color.Blue, 5), new Vector2D(40, 200), new Vector2D(40, 250));
            graphic.FlipBuffer();

            if (showScreenAlpha)
            {
                //Prüfe ab, dass der Alpha-Wert der ScreenShoot-Funktion stimmt
                Bitmap screenAlpha = BitmapHelp.GetAlphaChannel(graphic.GetScreenShoot());
                graphic.CreateOrUpdateNamedBitmapTexture("ScreenAlpha", screenAlpha);
                graphic.DrawFillRectangle("ScreenAlpha", 420, 130, 40, 40, false, Color.FromArgb(255, 255, 255));
            }
        }
    }
}
