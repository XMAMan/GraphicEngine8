using System;
using GraphicMinimal;

namespace RayObjects
{
    static class MotionBlureMovementBuilder
    {
        public static IMotionBlurMovement Build(MotionBlurMovementDescription description, IRaytracerDrawingProps propertys)
        {
            var affinePosition = new AffinePosition()
            {
                Position = propertys.Position,
                Orientation = propertys.Orientation,
                Size = propertys.Size
            };

            if (description is RotationMovementLinearDescription) return new RotationMovementLinear(affinePosition, description as RotationMovementLinearDescription);
            if (description is RotationMovementEulerDescription) return new RotationMovementEuler(affinePosition, description as RotationMovementEulerDescription);
            if (description is TranslationMovementLinearDescription) return new TranslationMovementLinear(affinePosition, description as TranslationMovementLinearDescription);
            if (description is TranslationMovementEulerDescription) return new TranslationMovementEuler(affinePosition, description as TranslationMovementEulerDescription);
            if (description is SizeMovementLinearDescription) return new SizeMovementLinear(affinePosition, description as SizeMovementLinearDescription);
            if (description is SizeMovementEulerDescription) return new SizeMovementEuler(affinePosition, description as SizeMovementEulerDescription);
            if (description is FuncMotionBlueMovementDescription) return new FuncMotionBlueMovement(description as FuncMotionBlueMovementDescription);

            throw new Exception("Der Typ " + description.GetType() + " ist unbekannt");
        }
    }

    abstract class MotionBlureMovement
    {
        protected AffinePosition affinePosition;

        public MotionBlureMovement(AffinePosition affinePosition)
        {
            this.affinePosition = affinePosition;
        }

        public AffineMatrizes GetMotionMatrizes(float time)
        {
            var m = GetAffinePosition(time);

            Matrix4x4 normalObjectToWorld = Matrix4x4.NormalRotate(m.Orientation);
            Matrix4x4 objectToWorld = Matrix4x4.Model(m.Position, m.Orientation, m.Size);

            Matrix4x4 worldToObject = Matrix4x4.InverseModel(m.Position, m.Orientation, m.Size);

            return new AffineMatrizes()
            {
                ObjectToWorldMatrix = objectToWorld,
                NormalObjectToWorldMatrix = normalObjectToWorld,
                WorldToObjectMatrix = worldToObject
            };
        }

        protected abstract AffinePosition GetAffinePosition(float time);
    }

    

    class AffinePosition
    {
        public Vector3D Position;
        public Vector3D Orientation;
        public float Size;
    }

    class RotationMovementLinear : MotionBlureMovement, IMotionBlurMovement
    {
        private float rotationStart;
        private float rotationEnd;
        private int axis;

        public RotationMovementLinear(AffinePosition affinePosition, RotationMovementLinearDescription description)
            : base(affinePosition)
        {
            this.rotationStart = description.RotationStart;
            this.rotationEnd = description.RotationEnd;
            this.axis = description.Axis;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            Vector3D orientation = new Vector3D(0, 0, 0);
            orientation[this.axis] = (1 - time) * this.rotationStart + time * this.rotationEnd;

            return new AffinePosition()
            {
                Position = this.affinePosition.Position,
                Orientation = orientation,
                Size = this.affinePosition.Size
            };
        }
    }

    class RotationMovementEuler : MotionBlureMovement, IMotionBlurMovement
    {
        private float rotationStart;
        private float rotationEnd;
        private float factor;
        private int axis;
        public RotationMovementEuler(AffinePosition affinePosition, RotationMovementEulerDescription description)
            : base(affinePosition)
        {
            this.rotationStart = description.RotationStart;
            this.rotationEnd = description.RotationEnd;
            this.factor = description.Factor;
            this.axis = description.Axis;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            float f = 1.0f / (float)Math.Exp(time * this.factor);
            Vector3D orientation = new Vector3D(0, 0, 0);
            orientation[this.axis] = (1 - f) * this.rotationStart + f * this.rotationEnd;

            return new AffinePosition()
            {
                Position = this.affinePosition.Position,
                Orientation = orientation,
                Size = this.affinePosition.Size
            };
        }
    }

    class TranslationMovementLinear : MotionBlureMovement, IMotionBlurMovement
    {
        private Vector3D positionStart;
        private Vector3D positionEnd;

        public TranslationMovementLinear(AffinePosition affinePosition, TranslationMovementLinearDescription description)
            : base(affinePosition)
        {
            this.positionStart = description.PositionStart;
            this.positionEnd = description.PositionEnd;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            return new AffinePosition()
            {
                Position = (1 - time) * this.positionStart + time * this.positionEnd,
                Orientation = this.affinePosition.Orientation,
                Size = this.affinePosition.Size
            };
        }
    }

    class TranslationMovementEuler : MotionBlureMovement, IMotionBlurMovement
    {
        private Vector3D positionStart;
        private Vector3D positionEnd;
        private float factor;

        public TranslationMovementEuler(AffinePosition affinePosition, TranslationMovementEulerDescription description)
            : base(affinePosition)
        {
            this.positionStart = description.PositionStart;
            this.positionEnd = description.PositionEnd;
            this.factor = description.Factor;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            float f = 1.0f / (float)Math.Exp(time * this.factor);
            return new AffinePosition()
            {
                Position = (1 - f) * this.positionStart + f * this.positionEnd,
                Orientation = this.affinePosition.Orientation,
                Size = this.affinePosition.Size
            };
        }
    }

    class SizeMovementLinear : MotionBlureMovement, IMotionBlurMovement
    {
        private float sizeStart;
        private float sizeEnd;

        public SizeMovementLinear(AffinePosition affinePosition, SizeMovementLinearDescription description)
            : base(affinePosition)
        {
            this.sizeStart = description.SizeStart;
            this.sizeEnd = description.SizeEnd;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            return new AffinePosition()
            {
                Position = this.affinePosition.Position,
                Orientation = this.affinePosition.Orientation,
                Size = (1 - time) * this.sizeStart + time * this.sizeEnd
            };
        }
    }

    class SizeMovementEuler : MotionBlureMovement, IMotionBlurMovement
    {
        private float sizeStart;
        private float sizeEnd;
        private float factor;

        public SizeMovementEuler(AffinePosition affinePosition, SizeMovementEulerDescription description)
            : base(affinePosition)
        {
            this.sizeStart = description.SizeStart;
            this.sizeEnd = description.SizeEnd;
            this.factor = description.Factor;
        }

        protected override AffinePosition GetAffinePosition(float time)
        {
            float f = 1.0f / (float)Math.Exp(time * this.factor);
            return new AffinePosition()
            {
                Position = this.affinePosition.Position,
                Orientation = this.affinePosition.Orientation,
                Size = (1 - f) * this.sizeStart + f * this.sizeEnd
            };
        }
    }

    class FuncMotionBlueMovement : IMotionBlurMovement
    {
        private FuncMotionBlueMovementDescription description;
        public FuncMotionBlueMovement(FuncMotionBlueMovementDescription description)
        {
            this.description = description;
        }
        public AffineMatrizes GetMotionMatrizes(float time)
        {
            return this.description.GetTimeMatrizes(time);
        }
    }
}
