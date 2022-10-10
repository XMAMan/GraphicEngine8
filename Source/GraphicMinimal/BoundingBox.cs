using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphicMinimal
{
    public class BoundingBox
    {
        public Vector3D Min { get; private set; }
        public Vector3D Max { get; private set; }

        public BoundingBox(Vector3D min, Vector3D max)
        {
            this.Min = min;
            this.Max = max;
        }

        public BoundingBox(BoundingBox box1, BoundingBox box2)
        {
            this.Min = new Vector3D(Math.Min(box1.Min.X, box2.Min.X), Math.Min(box1.Min.Y, box2.Min.Y), Math.Min(box1.Min.Z, box2.Min.Z));
            this.Max = new Vector3D(Math.Max(box1.Max.X, box2.Max.X), Math.Max(box1.Max.Y, box2.Max.Y), Math.Max(box1.Max.Z, box2.Max.Z));
        }

        public BoundingBox(BoundingBox box1, Vector3D point)
        {
            this.Min = new Vector3D(Math.Min(box1.Min.X, point.X), Math.Min(box1.Min.Y, point.Y), Math.Min(box1.Min.Z, point.Z));
            this.Max = new Vector3D(Math.Max(box1.Max.X, point.X), Math.Max(box1.Max.Y, point.Y), Math.Max(box1.Max.Z, point.Z));
        }

        public BoundingBox(IEnumerable<BoundingBox> boxes)
        {
            this.Min = new Vector3D(boxes.Min(x => x.Min.X),
                                  boxes.Min(x => x.Min.Y),
                                  boxes.Min(x => x.Min.Z));

            this.Max = new Vector3D(boxes.Max(x => x.Max.X),
                                  boxes.Max(x => x.Max.Y),
                                  boxes.Max(x => x.Max.Z));
        }

        private Vector3D center = null;
        public Vector3D Center
        {
            get
            {
                if (this.center == null)
                {
                    this.center = this.Min + (this.Max - this.Min) / 2;
                }

                return this.center;
            }
        }

        private float radiusInTheBox = float.MinValue;
        public float RadiusInTheBox //Die Kugel liegt innerhalb der Box
        {
            get
            {
                if (this.radiusInTheBox == float.MinValue)
                {
                    this.radiusInTheBox = Math.Min(Math.Min(XSize, YSize), ZSize) / 2;
                }

                return this.radiusInTheBox;
            }
        }

        private float radiusOutTheBox = float.MinValue;
        public float RadiusOutTheBox    //Die Kugel umspannt die Box
        {
            get
            {
                if (this.radiusOutTheBox == float.MinValue)
                {
                    this.radiusOutTheBox = (this.Max - this.Min).Length() / 2;
                }

                return this.radiusOutTheBox;
            }
        }

        public float XSize
        {
            get
            {
                return this.Max.X - this.Min.X;
            }
        }

        public float YSize
        {
            get
            {
                return this.Max.Y - this.Min.Y;
            }
        }

        public float ZSize
        {
            get
            {
                return this.Max.Z - this.Min.Z;
            }
        }

        public float MaxEdge
        {
            get
            {
                return Math.Max(Math.Max(XSize, YSize), ZSize);
            }
        }

        public int MedEdgeIndex
        {
            get
            {
                if (this.XSize > this.YSize)
                {
                    if (this.XSize > this.ZSize)
                        return 0; //X
                    else
                        return 2; //Z
                }else
                {
                    if (this.YSize > this.ZSize)
                        return 1; //Y
                    else
                        return 2; //Z
                }
            }
        }

        public bool Contains(BoundingBox innerBox)
        {
            return innerBox.Min.X >= this.Min.X &&
                   innerBox.Min.Y >= this.Min.Y &&
                   innerBox.Min.Z >= this.Min.Z &&
                   innerBox.Max.X <= this.Max.X &&
                   innerBox.Max.Y <= this.Max.Y &&
                   innerBox.Max.Z <= this.Max.Z;
        }

        public bool IsPointInside(Vector3D point)
        {
            return point.X >= this.Min.X && point.X <= this.Max.X &&
                   point.Y >= this.Min.Y && point.Y <= this.Max.Y &&
                   point.Z >= this.Min.Z && point.Z <= this.Max.Z;

        }
    }
}
