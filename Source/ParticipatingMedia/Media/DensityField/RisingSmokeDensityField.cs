using System;
using GraphicGlobal;
using GraphicMinimal;
using TextureHelper;

namespace ParticipatingMedia.Media.DensityField
{
    class RisingSmokeDensityField : IDensityField
    {
        public float MaxDensity { get; private set; } = 1;

        private BoundingBox worldSpaceBox;
        private DescriptionForRisingSmokeMedia desc;

        private Vector3D worldSpaceCylinderOrigin;
        private float worldSpaceCylinderRadius;
        private float worldSpaceCylinderHeight;
        private EbertNoiseGenerator3D noise;

        //worldSpaceBox = Ich gehe davon aus, das der Rauch ein Zylinder ist, dessen Achse bei Y liegt
        public RisingSmokeDensityField(BoundingBox worldSpaceBox, DescriptionForRisingSmokeMedia desc)
        {
            this.worldSpaceBox = worldSpaceBox;
            this.desc = desc;
            this.noise = new EbertNoiseGenerator3D(new BoundingBox(new Vector3D(-1,0,-1), new Vector3D(1,1,1)), new Rand(0), 1);

            this.worldSpaceCylinderOrigin = worldSpaceBox.Min + new Vector3D(worldSpaceBox.XSize / 2, 0, worldSpaceBox.ZSize / 2);
            this.worldSpaceCylinderRadius = Math.Max(worldSpaceBox.XSize, worldSpaceBox.ZSize) / 2;
            this.worldSpaceCylinderHeight = worldSpaceBox.YSize;
        }

        public float GetDensity(Vector3D point)
        {
            Vector3D local = ToLocalSpace(point);

            float coneRadius1 = (1 - local.Y) * this.desc.MinRadius + local.Y * this.desc.MaxRadius; //Radius geht von desc.Radius bei Höhe 0 bis 1 bei Höhe 1
            float turbulenceFactor = coneRadius1 * this.desc.Turbulence;

            var wind = desc.WindDirection * coneRadius1;

            local.X -= wind.X;
            local.Z -= wind.Y;

            local += this.noise.GetNoiseVector(local) * turbulenceFactor;

            float coneRadius2 = (1 - local.Y) * this.desc.MinRadius + local.Y * this.desc.MaxRadius;

            if (new Vector2D(local.X, local.Z).Length() < coneRadius2) return Math.Min(1, Math.Max(0, (1 - local.Y)));

            //Spirale
            /*float helixRadius = local.Y * (1 - this.desc.Radius); //Radius geht von 0 (Bei Höhe 0) bis (1 - desc.Radius) (Bei Höhe 1)
            double phi = local.Y * 2 * Math.PI * this.desc.HelixCount;
            Vector3D helixPoint = new Vector3D((float)Math.Cos(phi) * helixRadius, local.Y, (float)Math.Sin(phi) * helixRadius);
            local -= helixPoint;
            if (local.Length() < this.desc.Radius) return 1;*/

            return 0;
        }

        /*private Vector3D CreateRandomPointInUnitSphere(IRandom rand)
        {
            double d, x, y, z;
            do
            {
                x = rand.NextDouble() * 2 - 1;
                y = rand.NextDouble() * 2 - 1;
                z = rand.NextDouble() * 2 - 1;
                d = x * x + y * y + z * z;
            }while(d > 1) ;

            return new Vector3D((float)x, (float)y, (float)z);
        }*/

        //Im Lokalspace hat der Zylinder ein Radius und Höhe von 1; Der untere Mittelpunkt liegt bei (0,0,0)
        private Vector3D ToLocalSpace(Vector3D point)
        {
            Vector3D l = (point - this.worldSpaceCylinderOrigin);
            return new Vector3D(l.X / this.worldSpaceCylinderRadius, l.Y / this.worldSpaceCylinderHeight, l.Z / this.worldSpaceCylinderRadius);
        }
    }

    class RisingSmokeDensityField1 : IDensityField
    {
        public float MaxDensity { get; private set; } = 1;

        public RisingSmokeDensityField1(BoundingBox worldSpaceBox, DescriptionForRisingSmokeMedia1 desc)
        {
            this.desc = desc;
            this.worldSpaceBox = worldSpaceBox;
            this.noiseGenerator = new EbertNoiseGenerator(new Rand(desc.RandomSeed), this.solidSpaceSize);
            this.axis = new Vector3D(0, worldSpaceBox.YSize, 0);
            this.start = worldSpaceBox.Center - this.axis / 2;
            float cylinderRadius = (this.start - worldSpaceBox.Min).Length();
            this.smallRadius = cylinderRadius * desc.MinRadius;
            this.bigRadius = cylinderRadius;// - this.smallRadius;

            this.smallRadiusSq = this.smallRadius * this.smallRadius;
            this.helixLength = worldSpaceBox.YSize;
            this.max_turb_length = this.helixLength * 0.93f;

            double theta_swirl = 45.0 * Math.PI / 180.0; // swirling effect 
            double cos_theta = Math.Cos(theta_swirl);
            double sin_theta = Math.Sin(theta_swirl);
            this.cos_theta2 = 0.01f * (float)cos_theta;
            this.sin_theta2 = 0.0075f * (float)sin_theta;
            this.heightToStartAddingTurbulence = this.helixLength * 0.11f;
        }

        private DescriptionForRisingSmokeMedia1 desc;
        private BoundingBox worldSpaceBox;
        private readonly int solidSpaceSize = 64;
        private EbertNoiseGenerator noiseGenerator;
        private Vector3D start; //Startpunkt von der Axe (bottom)
        private Vector3D axis; //Ist die Mitte vom Zylinder
        private float bigRadius;   //big_radius
        private float smallRadius; //radius
        private float smallRadiusSq; //rad_sq
        private float helixLength; //end_d_ramp = d_ramp_length
        private float max_turb_length;
        private float cos_theta2;
        private float sin_theta2;
        private float heightToStartAddingTurbulence;

        public float GetDensity(Vector3D point)
        {
            Vector3D pnt = TransformToSolidSpace(point);  //location of point in cloud space
            Vector3D pnt_world = new Vector3D(point);           //location of point in world space 

            float height = pnt_world.Y - this.start.Y + this.noiseGenerator.GetNoise(pnt) * this.smallRadius;
            // We don’t want smoke below the bottom of the column 
            if (height < 0) return 0;
            height -= this.heightToStartAddingTurbulence;
            if (height < 0) height = 0;
            // calculate the eased turbulence, taking into account the value 
            // may be greater than 1, which ease won’t handle. 
            float t_ease = height / this.max_turb_length;
            if (t_ease > 1)
            {
                t_ease = (int)t_ease + (float)Ease(t_ease - (int)t_ease, 0.001, 0.999);
                if (t_ease > 2.5) t_ease = 2.5f;
            }else
            {
                t_ease = (float)Ease(t_ease, 0.5, 0.999);
            }
            // Calculate the amount of turbulence to add in 
            float fast_turb = this.noiseGenerator.Turbulence(pnt, 0.1f);
            float turb_amount = (fast_turb - 0.875f) * (0.2f + 0.8f * t_ease);
            float path_turb = fast_turb * (0.2f + 0.8f * t_ease);
            // add turbulence to the height and see if it is above the top 
            height += 0.1f * turb_amount;
            if (height > this.helixLength) return 0;

            //increase the radius of the column as the smoke rises 
            float rad_sq2 = float.NaN;
            if (height <= 0)
            {
                rad_sq2 = this.smallRadiusSq * 0.25f;
            }else if (height <= this.helixLength)
            {
                rad_sq2 = (0.5f + 0.5f * ((float)Ease(height / (1.75 * this.helixLength), 0.5, 0.5))) * this.smallRadius;
                rad_sq2 *= rad_sq2;
            }

            // **************************************************** 
            // move along a helical path 
            // ****************************************************

            // calculate the path based on the unperturbed flow: helical path
            Vector3D hel_path = new Vector3D(
                (float)(cos_theta2 * (1 + path_turb) * (1 + Math.Cos(pnt_world.Y * Math.PI * 2 * this.desc.HelixCount) * 0.11) * (1 + t_ease * 0.1) + this.bigRadius * path_turb),
                 -path_turb,
                (float)(sin_theta2 * (1 + path_turb) * (1 + Math.Sin(pnt_world.Y * Math.PI * 2 * this.desc.HelixCount) * 0.085) * (1 + t_ease * 0.1) + 0.03 * path_turb)               
                );
            Vector3D direction2 = pnt_world + hel_path;

            //adjusting the center point for ramping off the density based on 
            //the turbulence of the moved point 
            turb_amount *= this.bigRadius;
            Vector3D center = new Vector3D(start.X - turb_amount, start.Y, start.Z + 0.75f * turb_amount);
            //calculate the radial distance from the center and ramp off the
            // density based on this distance squared. 
            float diffX = center.X - direction2.X;
            float diffZ = center.Z - direction2.Z;
            float dist_sq = diffX * diffX + diffZ * diffZ;
            if (dist_sq > rad_sq2) return 0;
            float density = (1 - dist_sq / rad_sq2 + fast_turb * 0.05f) * this.MaxDensity;
            if (height > 0)
                density *= (float)(1 - Ease((height - 0) / (this.helixLength), 0.5, 0.5));

            return Math.Abs(density);
            //Vector3D pnt = pnt_world - this.start; //LokalPoint

            //float h = pnt.Y / this.axis.Y;
            //double phi = h * 2 * Math.PI * this.desc.HelixCount;
            //Vector3D p = new Vector3D((float)Math.Cos(phi) * this.bigRadius, pnt.Y, (float)Math.Sin(phi) * this.bigRadius);
            //float d = (pnt - p).Betrag();
            //if (d > this.smallRadius  * 5) return 0;
            //return 1;
        }

        private Vector3D TransformToSolidSpace(Vector3D point)
        {
            Vector3D t = point - this.worldSpaceBox.Min;
            return new Vector3D(t.X / this.worldSpaceBox.XSize, t.Y / this.worldSpaceBox.YSize, t.Z / this.worldSpaceBox.ZSize) * this.solidSpaceSize; //0..64 (Wird bestimmt durch die größe vom Perlin-Gitter)
        }



        // *=====================================================================*
        // * --------------------------------------------------------------------*
        // * ease-in/ease-out                                                    *
        // * --------------------------------------------------------------------*
        // * By Dr.Richard E.Parent, The Ohio State University                 *
        // * (parent @cis.ohio-state.edu)                                         *
        // * --------------------------------------------------------------------*
        // * using parabolic blending at the end points                          *
        // * first leg has constant acceleration from 0 to v during time 0 to t1 *
        // * second leg has constant velocity of v during time t1 to t2          *
        // * third leg has constant deceleration from v to 0 during time t2 to 1 *
        // * these are integrated to get the 'distance' traveled at any time     *
        // * --------------------------------------------------------------------*
        private double Ease(double t, double t1, double t2)
        {
            double v = 2 / (1 + t2 - t1);   //constant velocity attained
            double a1 = v / t1;             //acceleration of first leg
            double a2 = -v / (1 - t2);      //deceleration of last leg

            double a, b, c, rt;

            if (t < t1)
            {
                rt = 0.5 * a1 * t * t;      //pos = 1/2 * acc * t*t
            }
            else if (t < t2)
            {
                a = 0.5 * a1 * t1 * t1;     //distance from first leg
                b = v * (t - t1);           //distance = vel * time  of second leg
                rt = a + b;
            }
            else
            {
                a = 0.5 * a1 * t1 * t1;     //distance from first leg
                b = v * (t2 - t1);          //distance from second leg
                c = ((v + v + (t - t2) * a2) / 2) * (t - t2); //distance = ave vel. * time
                rt = a + b + c;
            }

            return rt;
        }
    }
}
