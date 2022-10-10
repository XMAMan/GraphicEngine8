using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;
using TextureHelper;

namespace ParticipatingMedia.Media.DensityField
{
    //Berechnet die Dichte der Wolke an ein Punkt
    //Als Input wird ein axialer Würfel gegeben. In diesen Würfel werden dann lauter zufällige Metabälle erzeugt, welche die Macrostruktor(Wolkenoberfläche)
    //beschreiben. Die Microstruktur (Dichte-Wert innerhalb der Metabälle) wird über Perlin-Noise und Turbulence erzeugt.
    class CloudDensityField : IDensityField
    {
        private BoundingBox worldSpaceBox;
        private FrameData frameData;
        private EbertNoiseGenerator noiseGenerator;
        private Metballs balls;

        private float densityScalingFactor = 0.5f; //params[1]   = 0.2..0.4
        private float powExponent = 0.5f; //params[2] = 0.5
        private float blendingBetweenMetaballAndTurbulence = 0.4f; //params[3] = 0.4
        private float turbulenceFactor = 0.7f; //params[4]       = 0.7
        private readonly int solidSpaceSize = 64;
        class FrameData
        {
            public float sin_theta_cloud, cos_theta_cloud, theta, path_x, path_y, path_z, scalar_x, /*scalar_y,*/ scalar_z;

            public FrameData(int frame_num)
            {
                SetFromFrameNumber(frame_num);
            }

            public void SetFromFrameNumber(int frame_num)
            {
                // create gentle swirling in the cloud 
                theta = (frame_num % 600) * 0.01047196f; // swirling effect 
                cos_theta_cloud = (float)Math.Cos(theta);
                sin_theta_cloud = (float)Math.Sin(theta);
                path_x = sin_theta_cloud * 0.005f * frame_num;
                path_y = 0.01215f * (float)frame_num;
                path_z = sin_theta_cloud * 0.0035f * frame_num;
                scalar_x = (0.5f + (float)frame_num * 0.010f);
                scalar_z = (float)frame_num * 0.0073f;
            }
        }

        public float MaxDensity { get; private set; }

        public CloudDensityField(BoundingBox worldSpaceBox, DescriptionForCloudMedia desc)
        {
            IRandom rand = new Rand(desc.RandomSeed);
            this.worldSpaceBox = worldSpaceBox;
            this.densityScalingFactor = desc.DensityScalingFactor;
            this.powExponent = desc.PowExponent;
            this.blendingBetweenMetaballAndTurbulence = desc.BlendingBetweenMetaballAndTurbulence;
            this.turbulenceFactor = desc.TurbulenceFactor;

            this.balls = new Metballs(worldSpaceBox, rand.Next(desc.MinMetaballCount, desc.MaxMetaballCount), rand, desc.ShellType);


            this.frameData = new FrameData(0);
            this.noiseGenerator = new EbertNoiseGenerator(rand, this.solidSpaceSize);

            this.MaxDensity = this.balls.GetMetaballPositions().Max(x => GetDensity(x));
        }

        private Vector3D TransformToSolidSpace(Vector3D point)
        {
            Vector3D t = point - this.worldSpaceBox.Min;
            return new Vector3D(t.X / this.worldSpaceBox.XSize, t.Y / this.worldSpaceBox.YSize, t.Z / this.worldSpaceBox.ZSize) * this.solidSpaceSize; //0..64 (Wird bestimmt durch die größe vom Perlin-Gitter)
        }

        //public static float MaxTurbulence = float.MinValue;
        //public static float MinTurbulence = float.MaxValue;
        //public static float MaxNoise = float.MinValue;
        //public static float MinNoise = float.MaxValue;

        //Quelle: Ebert Perlin Texturing and Modeling a Procedural Approach 1998 Seite 302 -> CumulusCloud
        public float GetDensity(Vector3D point)
        {
            Vector3D pnt = TransformToSolidSpace(point);  //location of point in cloud space
            Vector3D pnt_w = new Vector3D(point);           //location of point in world space 

            //return (float)this.balls.GetDensityFromMetaballs(pnt_w);

            // Add some noise to the point’s location 
            Vector3D noise = this.noiseGenerator.GetNoiseVector(pnt); // Use Darwyn Peachey’s noise 
            //pnt.X -= this.frameData.path_x - peach * this.frameData.scalar_x;
            //pnt.Y -= this.frameData.path_y + .5f * peach;
            //pnt.Z += this.frameData.path_z - peach * this.frameData.scalar_z;
            pnt_w.X += this.worldSpaceBox.XSize * this.turbulenceFactor * noise.X;
            pnt_w.Y -= this.worldSpaceBox.YSize * this.turbulenceFactor * noise.Y;
            pnt_w.Z += this.worldSpaceBox.ZSize * this.turbulenceFactor * noise.Z;

            return (float)this.balls.GetDensityFromMetaballs(pnt_w);

            /*
            // Perturb the location of the point before evaluating the 
            // implicit primitives. 
            float turb = this.noiseGenerator.Turbulence(pnt, 0.1f);

            //if (turb < MinTurbulence) MinTurbulence = turb;
            //if (turb > MaxTurbulence) MaxTurbulence = turb;
            //if (peach < MinNoise) MinNoise = peach;
            //if (peach > MaxNoise) MaxNoise = peach;

            float turb_amount = this.turbulenceFactor * turb;// * 0.01f;
            pnt_w.X += this.worldSpaceBox.XSize * turb_amount;
            pnt_w.Y -= this.worldSpaceBox.YSize * turb_amount;
            pnt_w.Z += this.worldSpaceBox.ZSize * turb_amount;

            //if (this.worldSpaceBox.IsPointInside(pnt_w) == false) return 0;

            float mdens = (float)this.balls.GetDensityFromMetaballs(pnt_w);
            //return mdens;

            float density = this.densityScalingFactor * (this.blendingBetweenMetaballAndTurbulence * mdens + (1.0f - this.blendingBetweenMetaballAndTurbulence) * turb * mdens);
            density = Math.Abs(density);
            density = (float)Math.Pow(density, this.powExponent);

            if (float.IsNaN(density) || density < 0) throw new Exception("Abnormal Cload-Density");
            return density;*/
        }

        //Ebert verät leider nicht, was diese Warp-Funktion macht
        /*public float GetDensityFromCirrusCloud(Vector3D point)
        {
            Vector3D pnt = TransformToSolidSpace(point);  //location of point in cloud space
            Vector3D pnt_w = new Vector3D(point);           //location of point in world space 


            // Add some noise to the point’s location 
            float peach = this.noiseGenerator.GetNoise(pnt); // Use Darwyn Peachey’s noise 
            pnt.X -= this.frameData.path_x - peach * this.frameData.scalar_x;
            pnt.Y = pnt.Y - this.frameData.path_y + .5f * peach;
            pnt.Z += this.frameData.path_z - peach * this.frameData.scalar_z;

            // Perturb the location of the point before evaluating the 
            // implicit primitives. 
            float turb = this.noiseGenerator.Turbulence(pnt, 0.1f);
            float turb_amount = this.turbulenceFactor * turb;
            pnt_w.X += this.worldSpaceBox.XSize * turb_amount;
            pnt_w.Y -= this.worldSpaceBox.YSize * turb_amount;
            pnt_w.Z += this.worldSpaceBox.ZSize * turb_amount;

            Vector3D jet_stream = new Vector3D(0, 0, 0);
            jet_stream.X += .2f * turb;
            jet_stream.Y += .3f * turb;
            jet_stream.Z += .25f * turb; // warp point along the jet stream vector 
            pnt_w = warp(jet_stream, pnt_w);

            float mdens = (float)GetDensityFromMetaballs(pnt_w);

            float density = this.densityScalingFactor * (this.blendingBetweenMetaballAndTurbulence * mdens + (1.0f - this.blendingBetweenMetaballAndTurbulence) * turb * mdens);
            density = Math.Abs(density);
            density = (float)Math.Pow(density, this.powExponent);

            if (float.IsNaN(density) || density < 0) throw new Exception("Abnormal Cload-Density");
            return density;
        }*/

        //Macrostruktur


    }

    class Metballs
    {
        private List<Metaball> balls;

        public Metballs(BoundingBox box, int count, IRandom rand, DescriptionForCloudMedia.CloudDrawingObject shellType)
        {
            if (shellType == DescriptionForCloudMedia.CloudDrawingObject.AxialCube)
                this.balls = CreateRandomMetaballsInCubeInWorldSpace(box, count, rand);
            else
                this.balls = CreateRandomMetaballsInSphereInWorldSpace(box, count, rand);
        }

        public IEnumerable<Vector3D> GetMetaballPositions()
        {
            return this.balls.Select(x => x.Position);
        }

        public float GetDensityFromMetaballs(Vector3D point)
        {
            return this.balls.Sum(x => x.Weight * x.GetBlendValue(point));
        }

        private List<Metaball> CreateRandomMetaballsInCubeInWorldSpace(BoundingBox box, int count, IRandom rand)
        {
            List<Metaball> balls = new List<Metaball>();

            Vector3D extend = box.Max - box.Min;
            float minExtend = extend.Min();

            float minRadius = 0.2f;//0..1 (1 = Es gibt eine große Kugel in der Box)
            float maxRadius = 0.8f;

            float minWeight = 0.1f;
            float maxWeight = 1.0f;

            float minPos = 0.0f; //Wenn 0 und 1, dann gehen Kugel bis zum Würfelrand ran
            float maxPos = 1.0f;

            float maxRadiusW = (minExtend / 2); //Wenn die Kugel größer, als die kleinste Würfelkantenlänge ist, dann passt sie nicht mehr rein

            for (int i = 0; i < count; i++)
            {
                float radius = (minRadius + (float)rand.NextDouble() * (maxRadius - minRadius)) * maxRadiusW;
                Vector3D position = new Vector3D(box.Min.X + radius + (minPos + (float)rand.NextDouble() * (maxPos - minPos)) * (extend.X - radius * 2),
                                             box.Min.Y + radius + (minPos + (float)rand.NextDouble() * (maxPos - minPos)) * (extend.Y - radius * 2),
                                             box.Min.Z + radius + (minPos + (float)rand.NextDouble() * (maxPos - minPos)) * (extend.Z - radius * 2));
                float weight = minWeight + (float)rand.NextDouble() * (maxWeight - minWeight);
                balls.Add(new Metaball(position, radius, weight));
            }

            return balls;
        }

        private List<Metaball> CreateRandomMetaballsInSphereInWorldSpace_(BoundingBox box, int count, IRandom rand)
        {
            List<Metaball> balls = new List<Metaball>();

            float minRadius = 0.2f;// 0.1f;//0..1 (1 = Es gibt eine große Kugel in der Box)
            float maxRadius = 0.5f;// 0.3f;

            float minWeight = 0.2f;
            float maxWeight = 1.0f;

            float maxRadiusW = box.RadiusInTheBox;

            for (int i = 0; i < count; i++)
            {
                float phi = 2 * (float)(Math.PI * rand.NextDouble());
                float theta = (float)(Math.Acos(1 - 2 * rand.NextDouble()));
                Vector3D direction = new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)(Math.Cos(theta)));

                float radius = (minRadius + (float)rand.NextDouble() * (maxRadius - minRadius)) * maxRadiusW;
                float t = (float)rand.NextDouble() * (maxRadiusW - radius);
                Vector3D position = box.Center + direction * t;
                float weight = minWeight + (float)rand.NextDouble() * (maxWeight - minWeight);
                balls.Add(new Metaball(position, radius, weight));
            }

            return balls;
        }

        //............................... Positionierung der Metabälle, indem man in der Mitte erst ein platziert und dann aus Zufallsrichtungen Bälle drauf schießt

        private List<Metaball> CreateRandomMetaballsInSphereInWorldSpace(BoundingBox box, int count, IRandom rand)
        {
            List<Metaball> balls = new List<Metaball>();

            float minRadius = 0.2f;// 0.1f;//0..1 (1 = Es gibt eine große Kugel in der Box)
            float maxRadius = 0.5f;// 0.3f;

            float minWeight = 0.2f;
            float maxWeight = 0.8f;

            float maxRadiusW = box.RadiusInTheBox;

            Frame frame = new Frame(new Vector3D(1,0,0), new Vector3D(0,1,0), new Vector3D(0,0,1));

            balls.Add(new Metaball(box.Center, (minRadius + (float)rand.NextDouble() * (maxRadius - minRadius)) * maxRadiusW, minWeight + (float)rand.NextDouble() * (maxWeight - minWeight)));

            for (int i = 0; i < count; i++)
            {
                double phi = rand.NextDouble() * 2 * Math.PI;
                double theta = rand.NextDouble() * Math.PI;
                Vector3D dir = frame.GetDirectionFromPhiAndTheta(theta, phi);
                Vector3D startPoint = box.Center - dir * maxRadiusW * 2;
                float radius = (minRadius + (float)rand.NextDouble() * (maxRadius - minRadius)) * maxRadiusW;
                float weight = minWeight + (float)rand.NextDouble() * (maxWeight - minWeight);
                var newBall = new Metaball(startPoint, radius, weight);
                float t = GetFirstIntersectionPointDistanze(newBall, dir, balls);
                if (t != float.MaxValue)
                {
                    Vector3D pos = newBall.Position + dir * (t + newBall.Radius * 0.9f); //Überschneide die Bälle noch etwas
                    float rad = newBall.Radius;
                    balls.Add(new Metaball(pos, rad, newBall.Weight));
                }
            }

            BoundingBox maxBox = balls.First().BBox();
            foreach (var ball in balls)
            {
                maxBox = new BoundingBox(maxBox, ball.BBox());
            }
            float boxRadius = maxBox.MaxEdge / 2;
            float scaleFactor = 1;// maxRadiusW / boxRadius;
            Vector3D moveToCenter = box.Center - maxBox.Center;
            return balls.Select(x => new Metaball(x.Position + moveToCenter, x.Radius * scaleFactor, x.Weight)).ToList();

            //return balls;
        }

        private float GetFirstIntersectionPointDistanze(Metaball movingBall, Vector3D moveDirection, List<Metaball> balls)
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

        private float GetIntersectionTimeFromMovingCircleWithStillCircle(Metaball movingBall, Vector3D moveDirection, Metaball fixBall)
        {
            Vector3D Pab = movingBall.Position - fixBall.Position;
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
    }

    class Metaball
    {
        public Vector3D Position { get; private set; }
        public float Radius { get; private set; }
        public float Weight { get; private set; }

        public BoundingBox BBox()
        {
            return new BoundingBox(this.Position - new Vector3D(1, 1, 1) * this.Radius, this.Position + new Vector3D(1, 1, 1) * this.Radius);
        }

        private float R2, R4, R6;

        public Metaball(Vector3D position, float radius, float weight)
        {
            this.Position = position;
            this.Radius = radius;
            this.R2 = radius * radius;
            this.R4 = R2 * R2;
            this.R6 = R4 * R2;
            this.Weight = weight;
        }

        public float GetBlendValue(Vector3D point)
        {
            float r2 = (this.Position - point).SquareLength();
            if (r2 > this.R2) return 0;
            float r4 = r2 * r2;
            float r6 = r4 * r2;
            return Math.Max(0, -4.0f / 9.0f * r6 / R6 + 17.0f / 9.0f * r4 / R4 - 22.0f / 9.0f * r2 / R2 + 1); //Standard Cubic Function (Wyvill, McPheeters and Wyvill 1986)
        }

        //Hiermit habe ich die Funktion geplottet
        /*[TestMethod]
        public void Metaball()
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot()
            {
                Function = x =>
                {
                    double radius = 1;
                    double R2 = radius * radius;
                    double R4 = R2 * R2;
                    double R6 = R4 * R2;
                    double r2 = x;
                    if (r2 > R2) return 0;
                    double r4 = r2 * r2;
                    double r6 = r4 * r2;
                    return Math.Max(0, -4.0f / 9.0f * r6 / R6 + 17.0f / 9.0f * r4 / R4 - 22.0f / 9.0f * r2 / R2 + 1); //Standard Cubic Function (Wyvill, McPheeters and Wyvill 1986)
                },
                Color = Color.Blue,
                Text = "Metaball"
            });

            FunctionPlotter plotter = new FunctionPlotter(0, 5, new Size(400, 300));
            plotter.PlotFunctions(functions).Save(WorkingDirectory + "Metaball.bmp");
        }*/
    }
}
