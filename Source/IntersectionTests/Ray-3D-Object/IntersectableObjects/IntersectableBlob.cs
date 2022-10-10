using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    //Ein Blob, den man mit ein Strahl treffen kann
    public class IntersectableBlob : IIntersecableObject
    {
        public IIntersectableRayDrawingObject RayHeigh { get; private set; } //Die Lichtquelle muss beim Schattenstrahltest wissen, ob Lichtquelle-RayHeigh == IntersectionPoint.RayHeigh
        public Vector3D AABBCenterPoint { get; private set; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        public Vector3D MinPoint { get; private set; }
        public Vector3D MaxPoint { get; private set; }

        #region Private-Variablen

        protected Vector3D[] centerList;
        protected float size;
        private readonly float invSizeSquare; // invSizeSquare = 1.0f / (size * size);

        // A second degree polynom is defined by its coeficient
        // a * x^2 + b * x + c
        struct Poly
        {
            public float a, b, c, fDistance, fDeltaFInvSquare;
        }
        private const int zoneNumber = 10;//Anzahl der Kugeln, in die eine Blob-Kugel unterteilt ist

        public IntersectableBlob(Vector3D[] centerList, float sphereRadius, IIntersectableRayDrawingObject rayHeigh)
        {
            this.RayHeigh = rayHeigh;
            //zoneTab-Array initialisieren
            float fLastGamma = 0.0f, fLastBeta = 0.0f;
            float fLastInvRSquare = 0.0f;
            for (int i = 0; i < zoneNumber - 1; i++)
            {
                float fInvRSquare = 1.0f / zoneTab[i + 1].fCoef;
                zoneTab[i].fDeltaFInvSquare = fInvRSquare - fLastInvRSquare;
                // fGamma is the ramp between the entry point and the exit point.
                // We only store the difference compared to the previous zone
                // that way we can reconstruct the estimate more easily later..
                float temp = (fLastInvRSquare - fInvRSquare) / (zoneTab[i].fCoef - zoneTab[i + 1].fCoef);
                zoneTab[i].fGamma = temp - fLastGamma;
                fLastGamma = temp;

                // fBeta is the value of the line approaching the curve for dist = 0 (f = fGamma * x + fBeta)
                // similarly we only store the difference with the fBeta of the previous curve
                zoneTab[i].fBeta = fInvRSquare - fLastGamma * zoneTab[i + 1].fCoef - fLastBeta;
                fLastBeta = zoneTab[i].fBeta + fLastBeta;

                fLastInvRSquare = fInvRSquare;
            };
            // The last zone acts as a simple terminator 
            // (no need to evaluate the field there, because we know that it exceed
            // the equipotential value.. by design)
            zoneTab[zoneNumber - 1].fGamma = 0.0f;
            zoneTab[zoneNumber - 1].fBeta = 0.0f;

            //Eigener Scheiß^^
            this.centerList = centerList;
            this.size = sphereRadius;
            this.invSizeSquare = 1.0f / (sphereRadius * sphereRadius);
            if (centerList.Length < 1) throw new ArgumentException("centerList.Length muss >= 1 sein");
            Vector3D minPoint = new Vector3D(centerList[0]);
            Vector3D maxPoint = new Vector3D(centerList[0]);
            foreach (Vector3D V in centerList)
            {
                if (V.X < minPoint.X) minPoint.X = V.X;
                if (V.Y < minPoint.Y) minPoint.Y = V.Y;
                if (V.Z < minPoint.Z) minPoint.Z = V.Z;
                if (V.X > maxPoint.X) maxPoint.X = V.X;
                if (V.Y > maxPoint.Y) maxPoint.Y = V.Y;
                if (V.Z > maxPoint.Z) maxPoint.Z = V.Z;
            }
            minPoint -= new Vector3D(sphereRadius, sphereRadius, sphereRadius);
            maxPoint += new Vector3D(sphereRadius, sphereRadius, sphereRadius);

            this.MinPoint = minPoint;
            this.MaxPoint = maxPoint;
            this.AABBCenterPoint = this.MinPoint + (this.MaxPoint - this.MinPoint) / 2;
        }

        //Prüft, ob der Strahl den Blob trifft. Returnwert: Wenn getroffen, Schnittpunkt zwischen Strahl und Blob, sonst null
        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            float t = 2000;

            // Having a static structure helps performance more than two times !
            // It obviously wouldn't work if we were running in multiple threads..
            // But it helps considerably for now
            List<Poly> polynomMap = new List<Poly>();

            float rSquare, rInvSquare;
            rSquare = size * size;
            rInvSquare = invSizeSquare;
            float maxEstimatedPotential = 0.0f;

            // outside of all the influence spheres, the potential is zero
            float A = 0.0f;
            float B = 0.0f;
            float C = 0.0f;

            for (int i = 0; i < centerList.Length; i++)
            {
                Vector3D currentPoint = centerList[i];

                Vector3D vDist = currentPoint - ray.Start;
                A = 1.0f;
                B = -2.0f * ray.Direction * vDist;
                C = vDist * vDist;
                // Accelerate delta computation by keeping common computation outside of the loop
                float BSquareOverFourMinusC = 0.25f * B * B - C;
                float MinusBOverTwo = -0.5f * B;
                float ATimeInvSquare = A * rInvSquare;
                float BTimeInvSquare = B * rInvSquare;
                float CTimeInvSquare = C * rInvSquare;

                // the current sphere, has N zones of influences
                // we go through each one of them, as long as we've detected
                // that the intersecting ray has hit them
                // Since all the influence zones of many spheres
                // are imbricated, we compute the influence of the current sphere
                // by computing the delta of the previous polygon
                // that way, even if we reorder the zones later by their distance
                // on the ray, we can still have our estimate of 
                // the potential function.
                // What is implicit here is that it only works because we've approximated
                // 1/dist^2 by a linear function of dist^2
                for (int j = 0; j < zoneNumber - 1; j++)
                {
                    // We compute the "delta" of the second degree equation for the current
                    // spheric zone. If it's negative it means there is no intersection
                    // of that spheric zone with the intersecting ray
                    float fDelta = BSquareOverFourMinusC + zoneTab[j].fCoef * rSquare;
                    if (fDelta < 0.0f)
                    {
                        // Zones go from bigger to smaller, so that if we don't hit the current one,
                        // there is no chance we hit the smaller one
                        break;
                    }
                    float sqrtDelta = (float)Math.Sqrt(fDelta);
                    float t0 = MinusBOverTwo - sqrtDelta;
                    float t1 = MinusBOverTwo + sqrtDelta;

                    // because we took the square root (a positive number), it's implicit that 
                    // t0 is smaller than t1, so we know which is the entering point (into the current
                    // sphere) and which is the exiting point.
                    Poly poly0 = new Poly()
                    {
                        a = zoneTab[j].fGamma * ATimeInvSquare,
                        b = zoneTab[j].fGamma * BTimeInvSquare,
                        c = zoneTab[j].fGamma * CTimeInvSquare + zoneTab[j].fBeta,
                        fDistance = t0,
                        fDeltaFInvSquare = zoneTab[j].fDeltaFInvSquare
                    };
                    Poly poly1 = new Poly()
                    {
                        a = -poly0.a,
                        b = -poly0.b,
                        c = -poly0.c,
                        fDistance = t1,
                        fDeltaFInvSquare = -poly0.fDeltaFInvSquare
                    };

                    maxEstimatedPotential += zoneTab[j].fDeltaFInvSquare;

                    // just put them in the vector at the end
                    // we'll sort all those point by distance later
                    polynomMap.Add(poly0);
                    polynomMap.Add(poly1);
                }
            }

            if (polynomMap.Count < 2 || maxEstimatedPotential < 1.0f)
            {
                return null;
            }

            // sort the various entry/exit points per distance
            // by going from the smaller distance to the bigger
            // we can reconstruct the field approximately along the way
            polynomMap.Sort(delegate(Poly p1, Poly p2) { return p1.fDistance.CompareTo(p2.fDistance); });

            maxEstimatedPotential = 0.0f;
            bool bResult = false;

            A = 0;
            B = 0;
            C = 0;
            for (int i = 0; i < polynomMap.Count - 1; i++)
            {
                // A * x2 + B * y + C, defines the condition under which the intersecting
                // ray intersects the equipotential surface. It works because we designed it that way
                // (refer to the article).
                A += polynomMap[i].a;
                B += polynomMap[i].b;
                C += polynomMap[i].c;
                maxEstimatedPotential += polynomMap[i].fDeltaFInvSquare;
                if (maxEstimatedPotential < 1.0f)
                {
                    // No chance that the potential will hit 1.0f in this zone, go to the next zone
                    // just go to the next zone, we may have more luck
                    continue;
                }
                float fZoneStart = polynomMap[i].fDistance;
                float fZoneEnd = polynomMap[i + 1].fDistance;

                // the current zone limits may be outside the ray start and the ray end
                // if that's the case just go to the next zone, we may have more luck
                if (t > fZoneStart && 0.01f < fZoneEnd)
                {
                    // This is the exact resolution of the second degree
                    // equation that we've built
                    // of course after all the approximation we've done
                    // we're not going to have the exact point on the iso surface
                    // but we should be close enough to not see artifacts
                    float fDelta = B * B - 4.0f * A * (C - 1.0f);
                    if (fDelta < 0.0f)
                    {
                        continue;
                    }

                    float fInvA = (0.5f / A);
                    float fSqrtDelta = (float)Math.Sqrt(fDelta);

                    float t0 = fInvA * (-B - fSqrtDelta);
                    float t1 = fInvA * (-B + fSqrtDelta);
                    if ((t0 > 0.01f) && (t0 >= fZoneStart) && (t0 < fZoneEnd) && (t0 <= t))
                    {
                        t = t0;
                        bResult = true;
                    }

                    if ((t1 > 0.01f) && (t1 >= fZoneStart) && (t1 < fZoneEnd) && (t1 <= t))
                    {
                        t = t1;
                        bResult = true;
                    }

                    if (bResult)
                    {
                        return GetSimpleIntersectionPointFromDistanceValue(ray, t);
                    }
                }
            }
            return null;
        }

        

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            List<IIntersectionPointSimple> returnList = new List<IIntersectionPointSimple>();
            float t = 2000;

            // Having a static structure helps performance more than two times !
            // It obviously wouldn't work if we were running in multiple threads..
            // But it helps considerably for now
            List<Poly> polynomMap = new List<Poly>();

            float rSquare, rInvSquare;
            rSquare = size * size;
            rInvSquare = invSizeSquare;
            float maxEstimatedPotential = 0.0f;

            // outside of all the influence spheres, the potential is zero
            float A = 0.0f;
            float B = 0.0f;
            float C = 0.0f;

            for (int i = 0; i < centerList.Length; i++)
            {
                Vector3D currentPoint = centerList[i];

                Vector3D vDist = currentPoint - ray.Start;
                A = 1.0f;
                B = -2.0f * ray.Direction * vDist;
                C = vDist * vDist;
                // Accelerate delta computation by keeping common computation outside of the loop
                float BSquareOverFourMinusC = 0.25f * B * B - C;
                float MinusBOverTwo = -0.5f * B;
                float ATimeInvSquare = A * rInvSquare;
                float BTimeInvSquare = B * rInvSquare;
                float CTimeInvSquare = C * rInvSquare;

                // the current sphere, has N zones of influences
                // we go through each one of them, as long as we've detected
                // that the intersecting ray has hit them
                // Since all the influence zones of many spheres
                // are imbricated, we compute the influence of the current sphere
                // by computing the delta of the previous polygon
                // that way, even if we reorder the zones later by their distance
                // on the ray, we can still have our estimate of 
                // the potential function.
                // What is implicit here is that it only works because we've approximated
                // 1/dist^2 by a linear function of dist^2
                for (int j = 0; j < zoneNumber - 1; j++)
                {
                    // We compute the "delta" of the second degree equation for the current
                    // spheric zone. If it's negative it means there is no intersection
                    // of that spheric zone with the intersecting ray
                    float fDelta = BSquareOverFourMinusC + zoneTab[j].fCoef * rSquare;
                    if (fDelta < 0.0f)
                    {
                        // Zones go from bigger to smaller, so that if we don't hit the current one,
                        // there is no chance we hit the smaller one
                        break;
                    }
                    float sqrtDelta = (float)Math.Sqrt(fDelta);
                    float t0 = MinusBOverTwo - sqrtDelta;
                    float t1 = MinusBOverTwo + sqrtDelta;

                    // because we took the square root (a positive number), it's implicit that 
                    // t0 is smaller than t1, so we know which is the entering point (into the current
                    // sphere) and which is the exiting point.
                    Poly poly0 = new Poly()
                    {
                        a = zoneTab[j].fGamma * ATimeInvSquare,
                        b = zoneTab[j].fGamma * BTimeInvSquare,
                        c = zoneTab[j].fGamma * CTimeInvSquare + zoneTab[j].fBeta,
                        fDistance = t0,
                        fDeltaFInvSquare = zoneTab[j].fDeltaFInvSquare
                    };
                    Poly poly1 = new Poly()
                    {
                        a = -poly0.a,
                        b = -poly0.b,
                        c = -poly0.c,
                        fDistance = t1,
                        fDeltaFInvSquare = -poly0.fDeltaFInvSquare
                    };

                    maxEstimatedPotential += zoneTab[j].fDeltaFInvSquare;

                    // just put them in the vector at the end
                    // we'll sort all those point by distance later
                    polynomMap.Add(poly0);
                    polynomMap.Add(poly1);
                }
            }

            if (polynomMap.Count < 2 || maxEstimatedPotential < 1.0f)
            {
                return null;
            }

            // sort the various entry/exit points per distance
            // by going from the smaller distance to the bigger
            // we can reconstruct the field approximately along the way
            polynomMap.Sort(delegate (Poly p1, Poly p2) { return p1.fDistance.CompareTo(p2.fDistance); });

            maxEstimatedPotential = 0.0f;
            bool bResult = false;

            A = 0;
            B = 0;
            C = 0;
            for (int i = 0; i < polynomMap.Count - 1; i++)
            {
                // A * x2 + B * y + C, defines the condition under which the intersecting
                // ray intersects the equipotential surface. It works because we designed it that way
                // (refer to the article).
                A += polynomMap[i].a;
                B += polynomMap[i].b;
                C += polynomMap[i].c;
                maxEstimatedPotential += polynomMap[i].fDeltaFInvSquare;
                if (maxEstimatedPotential < 1.0f)
                {
                    // No chance that the potential will hit 1.0f in this zone, go to the next zone
                    // just go to the next zone, we may have more luck
                    continue;
                }
                float fZoneStart = polynomMap[i].fDistance;
                float fZoneEnd = polynomMap[i + 1].fDistance;

                // the current zone limits may be outside the ray start and the ray end
                // if that's the case just go to the next zone, we may have more luck
                if (t > fZoneStart && 0.01f < fZoneEnd)
                {
                    // This is the exact resolution of the second degree
                    // equation that we've built
                    // of course after all the approximation we've done
                    // we're not going to have the exact point on the iso surface
                    // but we should be close enough to not see artifacts
                    float fDelta = B * B - 4.0f * A * (C - 1.0f);
                    if (fDelta < 0.0f)
                    {
                        continue;
                    }

                    float fInvA = (0.5f / A);
                    float fSqrtDelta = (float)Math.Sqrt(fDelta);

                    float t0 = fInvA * (-B - fSqrtDelta);
                    float t1 = fInvA * (-B + fSqrtDelta);
                    if ((t0 > 0.01f) && (t0 >= fZoneStart) && (t0 < fZoneEnd))// && (t0 <= t))
                    {
                        t = t0;
                        bResult = true;
                    }

                    if ((t1 > 0.01f) && (t1 >= fZoneStart) && (t1 < fZoneEnd))// && (t1 <= t))
                    {
                        t = t1;
                        bResult = true;
                    }

                    if (bResult)
                    {
                        returnList.Add(GetSimpleIntersectionPointFromDistanceValue(ray, t));
                    }
                }
            }
            return returnList;
        }

        private IIntersectionPointSimple GetSimpleIntersectionPointFromDistanceValue(Ray ray, float distance)
        {
            Vector3D position = ray.Start + ray.Direction * distance;

            CalculateTextueCoordinates(position, out Vector3D gradient, out Vector3D texCoord);
            Vector3D normal = Vector3D.Normalize(gradient);

            Vertex vertexPoint = new Vertex(position, normal, null, texCoord.X, texCoord.Y);

            //Berechne Farbe, wenn nötig
            if (this.RayHeigh.Propertys.BlackIsTransparent)
            {
                if (this.RayHeigh.IsBlackColor(texCoord.X, texCoord.Y, position)) return null;
            }

            return new BlobIntersectionPoint(this, position, distance, vertexPoint, ray.Direction);
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            BlobIntersectionPoint point = (BlobIntersectionPoint)simplePoint;

            return this.RayHeigh.CreateIntersectionPoint(point.Point, point.Point.Normal, point.Point.Normal, point.RayDirection, point.ParallaxPoint, this);
        }

        #endregion

        #region Private Methoden

        // Space around a source of potential is divided into concentric spheric zones
        // Each zone will define the gamma and beta number that approaches
        // linearly (f(x) = gamma * x + beta) the curves of 1 / dist^2
        // Since those coefficients are independant of the actual size of the spheres
        // we can compute it once and only once in the initBlobZones function

        // fDeltaInvSquare is the maximum value that the current point source in the current
        // zone contributes to the potential field (defined incrementally)
        // Adding them for each zone that we entered and exit will give us
        // a conservative estimate of the value of that field per zone
        // which allows us to exit early later if there is no chance
        // that the potential hits our equipotential value.
        struct ZoneTabStruct
        {
            public float fCoef, fDeltaFInvSquare, fGamma, fBeta;
        }

        readonly ZoneTabStruct[] zoneTab = new ZoneTabStruct[zoneNumber] 
        {   
            new ZoneTabStruct(){fCoef = 10.0f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 5.0f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 3.33333f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 2.5f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 2.0f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 1.66667f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 1.42857f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 1.25f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 1.1111f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0},
            new ZoneTabStruct(){fCoef = 1.0f,fDeltaFInvSquare = 0, fGamma = 0, fBeta = 0}
        };

        #endregion

        protected void CalculateTextueCoordinates(Vector3D position, out Vector3D gradient, out Vector3D texCoord)
        {
            Matrix4x4 normalMatrix = Matrix4x4.Ident();
            float texturScaleFaktorX = 1;
            float texturScaleFaktorY = 1;

            gradient = new Vector3D(0, 0, 0);
            texCoord = new Vector3D(0, 0, 0);
            float fRSquare = size * size;
            for (int i = 0; i < centerList.Length; i++)
            {
                // This is the true formula of the gradient in the
                // potential field and not an estimation.
                // gradient = normal to the iso surface
                Vector3D normal = position - centerList[i];
                float fDistSquare = normal * normal;
                if (fDistSquare <= 0.001f)
                    continue;
                float fDistFour = fDistSquare * fDistSquare;
                normal = (fRSquare / fDistFour) * normal;

                gradient += normal;

                normal = Vector3D.Normalize(normal);
                Vector3D spinNormal = Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, new Vector3D(normal.X, -normal.Y, -normal.Z)));

                float texcoordV = Vector3D.AngleDegree(new Vector3D(0, 0, 1), new Vector3D(0, spinNormal.Y, spinNormal.Z));
                if (spinNormal.Y > 0) texcoordV = 180 - texcoordV;
                Vector3D drehSpass = Vector3D.RotateVerticalDirectionAroundAxis(new Vector3D(0, 0, 1), new Vector3D(1, 0, 0), texcoordV);

                float texcoordU = Vector3D.AngleDegree(drehSpass, spinNormal);

                texcoordU /= 180.0f;
                texcoordV /= 180.0f;

                texcoordU *= texturScaleFaktorX;
                texcoordV *= texturScaleFaktorY;

                texCoord += new Vector3D(texcoordU, texcoordV, 0);
            }
        }
    }
}
