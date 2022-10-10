using System;

namespace GraphicMinimal
{
    public abstract class MotionBlurMovementDescription
    {
        public static MotionBlurMovementDescription CreateFromString(string text)
        {
            var fields = text.Split(':');
            var parameter = fields[1].Split('|');
            switch (fields[0])
            {
                case "RotationMovementLinearDescription":
                    return new RotationMovementLinearDescription()
                    {
                        RotationStart = Convert.ToSingle(parameter[0]),
                        RotationEnd = Convert.ToSingle(parameter[1]),
                        Axis = Convert.ToInt32(parameter[2])
                    };
                case "RotationMovementEulerDescription":
                    return new RotationMovementEulerDescription()
                    {
                        RotationStart = Convert.ToSingle(parameter[0]),
                        RotationEnd = Convert.ToSingle(parameter[1]),
                        Factor = Convert.ToSingle(parameter[2]),
                        Axis = Convert.ToInt32(parameter[3])
                    };
                case "TranslationMovementLinearDescription":
                    return new TranslationMovementLinearDescription()
                    {
                        PositionStart = Vector3D.Parse(parameter[0]),
                        PositionEnd = Vector3D.Parse(parameter[1]),
                    };
                case "TranslationMovementEulerDescription":
                    return new TranslationMovementEulerDescription()
                    {
                        PositionStart = Vector3D.Parse(parameter[0]),
                        PositionEnd = Vector3D.Parse(parameter[1]),
                        Factor = Convert.ToSingle(parameter[2]),
                    };
                case "SizeMovementLinearDescription":
                    return new SizeMovementLinearDescription()
                    {
                        SizeStart = Convert.ToSingle(parameter[0]),
                        SizeEnd = Convert.ToSingle(parameter[1]),
                    };
                case "SizeMovementEulerDescription":
                    return new SizeMovementEulerDescription()
                    {
                        SizeStart = Convert.ToSingle(parameter[0]),
                        SizeEnd = Convert.ToSingle(parameter[1]),
                        Factor = Convert.ToSingle(parameter[2]),
                    };
            }

            throw new Exception("Can not parse " + text);
        }
    }

    public class RotationMovementLinearDescription : MotionBlurMovementDescription
    {
        public float RotationStart { get; set; }
        public float RotationEnd { get; set; }
        public int Axis; //0=X;1=Y;Z=2

        public override string ToString()
        {
            return "RotationMovementLinearDescription:" + RotationStart + "|" + RotationEnd + "|" + Axis;
        }
    }

    public class RotationMovementEulerDescription : MotionBlurMovementDescription
    {
        public float RotationStart { get; set; }
        public float RotationEnd { get; set; }
        public float Factor { get; set; }
        public int Axis { get; set; } //0=X;1=Y;Z=2

        public override string ToString()
        {
            return "RotationMovementEulerDescription:" + RotationStart + "|" + RotationEnd + "|" + Factor + "|" + Axis;
        }
    }

    public class TranslationMovementLinearDescription : MotionBlurMovementDescription
    {
        public Vector3D PositionStart { get; set; }
        public Vector3D PositionEnd { get; set; }

        public override string ToString()
        {
            return "TranslationMovementLinearDescription:" + PositionStart + "|" + PositionEnd;
        }
    }

    public class TranslationMovementEulerDescription : MotionBlurMovementDescription
    {
        public Vector3D PositionStart { get; set; }
        public Vector3D PositionEnd { get; set; }
        public float Factor { get; set; }

        public override string ToString()
        {
            return "TranslationMovementEulerDescription:" + PositionStart + "|" + PositionEnd + "|" + Factor;
        }
    }

    public class SizeMovementLinearDescription : MotionBlurMovementDescription
    {
        public float SizeStart { get; set; }
        public float SizeEnd { get; set; }

        public override string ToString()
        {
            return "SizeMovementLinearDescription:" + SizeStart + "|" + SizeEnd;
        }
    }

    public class SizeMovementEulerDescription : MotionBlurMovementDescription
    {
        public float SizeStart { get; set; }
        public float SizeEnd { get; set; }
        public float Factor { get; set; }

        public override string ToString()
        {
            return "SizeMovementEulerDescription:" + SizeStart + "|" + SizeEnd + "|" + Factor;
        }
    }

    public class FuncMotionBlueMovementDescription : MotionBlurMovementDescription
    {
        public Func<float, AffineMatrizes> GetTimeMatrizes;

        public override string ToString()
        {
            return "FuncMotionBlueMovementDescription";
        }
    }
}
