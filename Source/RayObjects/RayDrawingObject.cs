using GraphicMinimal;
using TextureHelper;
using System.Drawing;
using IntersectionTests;
using BitmapHelper;
using GraphicGlobal;
using ParticipatingMedia.Media;
using System;

namespace RayObjects
{
    //Entspricht ein DrawingObject aus dem Frame3DData-Objekt
    //Speichert die IRaytracerDrawingProps und hilft beim Textureauslesen
    public class RayDrawingObject : IIntersectableRayDrawingObject
    {
        private readonly Vector3D colorStringColor = null;
        private readonly IProceduralTexture proceduralTexture;
        private readonly IProceduralNormalmap proceduralNormalmap;
        private readonly ColorTexture colorTexture = null;            
        private readonly ColorTexture bumpmap = null;
        public ParallaxMapping ParallaxMap { get; private set; }

        #region Constructor
        public RayDrawingObject(IRaytracerDrawingProps propertys, BoundingBox boundingBox, IParticipatingMedia media)
        {
            this.Propertys = propertys;
            this.Media = media;

            //Lade Farb-Textur/ColorString
            if (this.Propertys.Color.Type == ColorSource.ColorString)
            {
                this.colorStringColor = this.Propertys.Color.As<ColorFromRgb>().Rgb;
            }                

            if (this.Propertys.Color.Type == ColorSource.Texture)            
            {
                string textureFile = this.Propertys.Color.As<ColorFromTexture>().TextureFile;
                if (BitmapHelp.IsHdrImageName(textureFile) == false)
                {
                    this.colorTexture = new ColorTexture(textureFile);
                }                
            }

            if (this.Propertys.Color.Type == ColorSource.Procedural)
            {
                this.proceduralTexture = CreateProceduralTexture(boundingBox);                
            }

            //Normalmap
            if (this.Propertys.NormalSource.Type == NormalSource.Normalmap)
            {
                var normalSource = this.Propertys.NormalSource.As<NormalFromMap>();

                if (normalSource.ConvertNormalMapFromColor)
                    this.bumpmap = new ColorTexture(BitmapHelp.GetBumpmapFromColor(new Bitmap(normalSource.NormalMap)));
                else
                    this.bumpmap = new ColorTexture(normalSource.NormalMap);
            }

            //Parallax
            if (this.Propertys.NormalSource.Type == NormalSource.Parallax)
            {
                var normalSource = this.Propertys.NormalSource.As<NormalFromParallax>();

                if (normalSource.ConvertNormalMapFromColor)
                    this.bumpmap = new ColorTexture(BitmapHelp.GetBumpmapFromColor(new Bitmap(normalSource.ParallaxMap)));
                else
                    this.bumpmap = new ColorTexture(normalSource.ParallaxMap);

                //Erzeuge Parallaxmap (Normalmap + Heightmap)
                this.ParallaxMap = new ParallaxMapping(this.bumpmap, normalSource.TexturHeightFactor, normalSource.TextureMatrix, normalSource.TextureMode, normalSource.IsParallaxEdgeCutoffEnabled);
            }

            //Lade Roughnessmap wenn vorhanden (Wenn nicht dann befindet sich überall gleichmäßig eine Microfacet-Struktur)
            if (this.Propertys.NormalSource.Type == NormalSource.Microfacet)
            {
                var normalSource = this.Propertys.NormalSource.As<NormalFromMicrofacet>();

                if (normalSource.RoughnessMap != null)
                    this.bumpmap = new ColorTexture(normalSource.RoughnessMap);
            }

            //Procedural-Normalmap
            if (this.Propertys.NormalSource.Type == NormalSource.Procedural)
            {
                this.proceduralNormalmap = CreateProceduralNormalmap(boundingBox);
            }
        }

        private IProceduralTexture CreateProceduralTexture(BoundingBox boundingBox)
        {
            Vector3D colorStringColor = PixelHelper.StringToColorVector(this.Propertys.Color.As<ColorFromProcedure>().ColorString);

            switch (this.Propertys.Color.As<ColorFromProcedure>().ColorProceduralFunction)
            {
                case ColorProceduralFunction.Hatch:
                    return new ProceduralTextureHatch(colorStringColor, boundingBox.Center);

                case ColorProceduralFunction.Wood:
                    return new ProceduralTextureWood(boundingBox.Center, 10.0f);

                case ColorProceduralFunction.Tile:
                    return new ProceduralTextureTile();

                case ColorProceduralFunction.ToonShader:
                    return new ProceduralTextureToonShader(colorStringColor, boundingBox.Center, this.Propertys.Size);
            }
            throw new Exception("Unknown ColorProceduralFunction " + this.Propertys.Color.As<ColorFromProcedure>().ColorProceduralFunction);
        }

        private IProceduralNormalmap CreateProceduralNormalmap(BoundingBox boundingBox)
        {
            var nor = this.Propertys.NormalSource.As<NormalFromProcedure>();

            switch (nor.Function.NormalProceduralFunction)
            {
                case NormalProceduralFunction.PerlinNoise:
                    return new ProceduralTexturePerlin(boundingBox, (nor.Function as NormalProceduralFunctionPerlinNoise).NormalNoiseFactor);

                case NormalProceduralFunction.SinForU:
                    return new ProceduralTextureSinU(nor.TextureMatrix);

                case NormalProceduralFunction.SinUCosV:
                    return new ProceduralTextureSinUCosV(nor.TextureMatrix);

                case NormalProceduralFunction.Tent:
                    return new ProceduralTextureTent(nor.TextureMatrix);
            }
            throw new Exception("Unknown NormalProceduralFunction " + nor.Function.NormalProceduralFunction);
        }
        #endregion

        #region IIntersectableRayDrawingObject

        public IRaytracerDrawingProps Propertys { get; private set; }

        public IParticipatingMedia Media { get; private set; } //Dieses Medium befindet sich innerhalb vom RayDrawingObject

        public Vector3D GetColor(float textcoordU, float textcoordV, Vector3D position)
        {
            if (this.Propertys.Color.Type == ColorSource.ColorString)
            {
                return this.colorStringColor;
            }                

            if (this.Propertys.Color.Type == ColorSource.Texture)
            {
                var tex = this.Propertys.Color.As<ColorFromTexture>();
                Vector3D texCoords = tex.TextureMatrix * new Vector3D(textcoordU, textcoordV, 1);
                Color color = this.colorTexture.ReadColorFromTexture(texCoords.X, texCoords.Y, tex.TextureFilter == TextureFilter.Linear || this.Propertys.NormalSource.Type == NormalSource.Parallax, tex.TextureMode);
                return PixelHelper.ColorToVector(color);
            }

            if (this.Propertys.Color.Type == ColorSource.Procedural)
            {
                return this.proceduralTexture.GetColor(position);
            }

            throw new Exception("Unknown Colortype " + this.Propertys.Color.Type);
        }

        private Vector3D GetBumpmapColor(float textcoordU, float textcoordV)
        {
            if (this.bumpmap == null) return null;
            var tex = this.Propertys.NormalSource.As<NormalMapFromFile>();
            Vector3D texCoords = tex.TextureMatrix * new Vector3D(textcoordU, textcoordV, 1);
            Color color = this.bumpmap.ReadColorFromTexture(texCoords.X, texCoords.Y, false, tex.TextureMode);
            return PixelHelper.ColorToVector(color);
        }

        private Vector3D GetBumpnormal(float textcoordU, float textcoordV, Matrix4x4 tangentToEyeMatrix)
        {
            var tex = this.Propertys.NormalSource.As<NormalMapFromFile>();
            Vector3D texCoords = tex.TextureMatrix * new Vector3D(textcoordU, textcoordV, 1);
            return TextureHelper.TextureHelper.TransformBumpNormalFromTangentToWorldSpace(this.bumpmap.ReadColorFromTexture(texCoords.X, texCoords.Y, false, tex.TextureMode), tangentToEyeMatrix);
        }

        public bool IsBlackColor(float textcoordU, float textcoordV, Vector3D position)
        {
            Vector3D color = GetColor(textcoordU, textcoordV, position);
            return color.X + color.Y + color.Z < 0.1f;
        }

        public IntersectionPoint CreateIntersectionPoint(Vertex point, Vector3D orientedFlatNormal, Vector3D notOrientedFlatNormal, Vector3D rayDirection, ParallaxPoint parallaxPoint, IIntersecableObject intersectedObject)
        {
            if (this.Propertys.TextureCoordSource.Type == TexturCoordSource.Procedural)
            {
                Vector2D texCoords = (this.Propertys.TextureCoordSource as ProceduralTextureCoordSource).TextureCoordsProceduralFunction.Map(point.Position);
                point.TexcoordU = texCoords.X;
                point.TexcoordV = texCoords.Y;
            }

            if (this.Propertys.NormalSource.Type == NormalSource.Procedural)
            {
                point.Normal = this.proceduralNormalmap.GetNormal(point);
            }

            if (this.Propertys.NormalSource.Type == NormalSource.Normalmap)
            {
                Matrix4x4 tangentToEyeMatrix = Matrix4x4.TBNMatrix(point.Normal, point.Tangent);
                var shadedNormal = this.GetBumpnormal(point.TexcoordU, point.TexcoordV, tangentToEyeMatrix);
                if (shadedNormal * rayDirection > 0) shadedNormal = point.Normal;
                point.Normal = shadedNormal;
            }

            if (this.Propertys.NormalSource.Type == NormalSource.Parallax)
            {
                if (parallaxPoint == null)
                {                    
                    parallaxPoint = this.ParallaxMap.GetParallaxIntersectionPointFromOutToIn(point, rayDirection);
                }

                point.TexcoordU = parallaxPoint.TexureCoords.X;
                point.TexcoordV = parallaxPoint.TexureCoords.Y;
                point.Normal = parallaxPoint.Normal;

                if (point.Normal * rayDirection > 0) throw new Exception("Shaded normal points in the wrong direction");
            }

            Vector3D color = this.GetColor(point.TexcoordU, point.TexcoordV, point.Position);
            return new IntersectionPoint(point, color, this.GetBumpmapColor(point.TexcoordU, point.TexcoordV), notOrientedFlatNormal, orientedFlatNormal, parallaxPoint, intersectedObject, this);
        }

        #endregion
    }
}
