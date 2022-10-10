using GraphicMinimal;
using System;
using System.Collections.Generic;

namespace GraphicGlobal
{
    public static class IntersectionHelper2D
    {
        public static Vector2D IntersectionPointRayCircle(Vector2D rayStart, Vector2D rayDirection, Vector2D circleCenter, float circleRadius)
        {
            rayStart -= circleCenter;
            float a = rayDirection.X * rayDirection.X + rayDirection.Y * rayDirection.Y;
            float b = 2 * (rayStart.X * rayDirection.X + rayStart.Y * rayDirection.Y);
            float c = rayStart.X * rayStart.X + rayStart.Y * rayStart.Y - circleRadius * circleRadius;
            float p = b / a;
            float q = c / a;

            float pHalf = p / 2;
            float leftSide = -pHalf;
            float rightSide = (float)Math.Sqrt(pHalf * pHalf - q);
            if (float.IsNaN(rightSide)) return null;
            float t1 = leftSide - rightSide;
            float t2 = leftSide + rightSide;
            if (t1 > 0)
            {
                if (t2 > 0)
                {
                    return rayStart + rayDirection * Math.Min(t1, t2);
                }else
                {
                    return rayStart + rayDirection * t1;
                }
            }else
            {
                if (t2 > 0)
                {
                    return rayStart + rayDirection * t2;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
