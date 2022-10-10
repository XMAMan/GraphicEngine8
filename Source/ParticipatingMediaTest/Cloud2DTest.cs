using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TextureHelper;

namespace ParticipatingMediaTest
{
    [TestClass]
    public class Cloud2DTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void Create2DCloud()
        {
            int size = 256;
            Bitmap image = new Bitmap(size, size);

            Cloud2D cloud = new Cloud2D(size);

            for (int x=0;x<size;x++)
                for (int y=0;y<size;y++)
                {
                    if (cloud.IsInsideFromCloud(new Vector2D(x,y)))
                    {
                        image.SetPixel(x, y, Color.Black);
                    }
                }

            image.Save(WorkingDirectory + "Cloud2D.bmp");
        }

        class Cloud2D
        {
            private int imageSize;
            private int noiseSize;
            private EbertNoiseGenerator noiseGenerator;
            private List<MetaBall> balls;

            public Cloud2D(int size)
            {
                this.imageSize = size;
                this.noiseSize = 128;
                this.balls = CreateBalls(10);
                this.noiseGenerator = new EbertNoiseGenerator(new Rand(0), this.noiseSize);
            }

            private Vector2D ToLocalSpace(Vector2D point)
            {
                return point / imageSize * noiseSize;
            }

            public bool IsInsideFromCloud(Vector2D point)
            {
                //return IsInsideFromMacroCloud(point);

                Vector2D localPnt = ToLocalSpace(point);
                float noise1 = this.noiseGenerator.GetNoise(new Vector3D(localPnt.X, localPnt.Y, imageSize / 2));
                float noise2 = this.noiseGenerator.GetNoise(new Vector3D(localPnt.Y, localPnt.X, imageSize / 2));
                point.X += imageSize * noise1 * 0.1f;
                point.Y += imageSize * noise2 * 0.1f;
                return IsInsideFromMacroCloud(point);
            }

            private bool IsInsideFromMacroCloud(Vector2D point)
            {
                return balls.Any(x => x.IsInsideFromBall(point));
            }

            //http://twobitcoder.blogspot.com/2010/04/circle-collision-detection.html -> Kollision zwischen zwei Kreisen, die sich beide bewegen

            //Der movingBall bewegt sich mit linearer Geschwindigkeit moveDirection. Diese Funktion gibt den Zeitpunkt zurück, wann es den fixBall trifft. Wenn kein Treffer, dann NaN
            //A(t) = Pa + tVa       movingBall
            //B(t) = Pb             fixBall
            //d(t)=abs(A(t) - Pb) - (Ra + Rb)	-> Distanz zum Zeitpunkt t
            //0 = sqrt((A(t)-Pb)²) - (Ra + Rb)
            //(Ra + Rb)² = (A(t)-Pb)²
            //(Ra + Rb)² = (Pa + t* Va - Pb)²
            //(Ra + Rb)² = (Pa - Pb + t* Va )²
            //Pab = Pa - Pb
            //(Ra + Rb)² = (Pab + t* Va )²
            //(Ra + Rb)² = Pab² + 2*Pab* t*Va + t²*Va²
            //0 = t²*Va² + t*2*Pab* Va + Pab² - (Ra + Rb)²
            //a = Va²
            //b = 2*Pab* Va
            //c = Pab² - (Ra + Rb)²
            //t = (-b +- sqrt(b²-4ac)) / (2a)
            //discriminant = b²-4ac

            private float GetIntersectionTimeFromMovingCircleWithStillCircle(MetaBall movingBall, Vector2D moveDirection, MetaBall fixBall)
            {
                Vector2D Pab = movingBall.Center - fixBall.Center;
                float rab = movingBall.Radius + fixBall.Radius;

                float a = moveDirection * moveDirection;
                float b = 2 * Pab * moveDirection;
                float c = (Pab * Pab) - rab * rab;
                float discriminant = b * b - 4 * a * c;
                if (discriminant < 0) return float.NaN;
                float sqrt = (float)Math.Sqrt(discriminant);
                float t0 = (-b - sqrt) / (2 * a);
                float t1 = (-b + sqrt) / (2 * a);
                return Math.Min(t0, t1);
            }

            private float GetFirstIntersectionPointDistanze(MetaBall movingBall, Vector2D moveDirection, List<MetaBall> balls)
            {
                float min = float.MaxValue;
                foreach (var fix in balls)
                {
                    float t = GetIntersectionTimeFromMovingCircleWithStillCircle(movingBall, moveDirection, fix);
                    if (float.IsNaN(t) == false)
                    {
                        if (t < min) min = t;
                    }
                }

                return min;
            }


            private List<MetaBall> CreateBalls(int count)
            {
                List<MetaBall> balls = new List<MetaBall>();
                Random rand = new Random(0);

                Vector2D imageCenter = new Vector2D(imageSize / 2, imageSize / 2);

                float maxRadius = imageSize / 2 * 0.3f;

                balls.Add(new MetaBall() { Center = imageCenter, Radius = (float)rand.NextDouble() * maxRadius });

                for (int i=0;i<count;i++)
                {
                    double phi = rand.NextDouble() * 2 * Math.PI;
                    Vector2D dir = Vector2D.DirectionFromPhi(phi);
                    Vector2D startPoint = imageCenter - dir * imageSize;
                    var newBall = new MetaBall() { Center = startPoint, Radius = (float)rand.NextDouble() * maxRadius };
                    float t = GetFirstIntersectionPointDistanze(newBall, dir, balls);
                    if (t != float.MaxValue)
                    {
                        balls.Add(new MetaBall() { Center = newBall.Center + dir * (t + newBall.Radius * 0.9f), Radius = newBall.Radius } );
                    }
                }

                BoundingBox2D maxBox = balls.First().BBox();
                foreach (var ball in balls)
                {
                    maxBox = new BoundingBox2D(maxBox, ball.BBox());
                }
                float boxRadius = maxBox.MaxEdge() / 2;
                float scaleFactor = (imageSize / 2) / boxRadius;
                Vector2D moveToCenter = imageCenter - maxBox.Center();
                return balls.Select(x => new MetaBall() { Center = x.Center + moveToCenter, Radius = x.Radius * scaleFactor }).ToList();

                //return balls;
            }

            class BoundingBox2D
            {
                public Vector2D Min;
                public Vector2D Max;

                public BoundingBox2D(Vector2D min, Vector2D max)
                {
                    this.Min = min;
                    this.Max = max;
                }

                public BoundingBox2D(BoundingBox2D box1, BoundingBox2D box2)
                {
                    this.Min = new Vector2D(Math.Min(box1.Min.X, box2.Min.X), Math.Min(box1.Min.Y, box2.Min.Y));
                    this.Max = new Vector2D(Math.Max(box1.Max.X, box2.Max.X), Math.Max(box1.Max.Y, box2.Max.Y));
                }

                public float MaxEdge()
                {
                    return Math.Max(Max.X - Min.X, Max.Y - Min.Y);
                }

                public Vector2D Center()
                {
                    return new Vector2D(Min.X + (Max.X - Min.X) / 2, Min.Y + (Max.Y - Min.Y) / 2);
                }
            }

            class MetaBall
            {
                public Vector2D Center;
                public float Radius;

                public BoundingBox2D BBox()
                {
                    return new BoundingBox2D(Center - new Vector2D(1, 1) * Radius, Center + new Vector2D(1, 1) * Radius);
                }

                public bool IsInsideFromBall(Vector2D point)
                {
                    return (Center - point).Length() < Radius;
                }
            }
        }

        
    }
}
