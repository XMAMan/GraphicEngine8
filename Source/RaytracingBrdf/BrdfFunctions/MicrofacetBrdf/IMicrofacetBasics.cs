using GraphicMinimal;
using IntersectionTests;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf
{
    //Unabhängig ob man Glas oder Metall machen will braucht man diese Funktionen
    interface IMicrofacetBasics
    {
        Vector3D MacroNormal { get; }
        IntersectionPoint HitPoint { get; }
        double G1(Vector3D micronormal, Vector3D directionToLightOrCamera);
        double G2(Vector3D micronormal, Vector3D directionToLight, Vector3D directionToCamera);
        double NormalDistribution(Vector3D micronormal);
        float JacobianDeterminantForReflection(Vector3D outDirection, Vector3D micronormal);
        float JacobianDeterminantForRefraction(Vector3D inDirection, Vector3D outDirection, Vector3D micronormal, float ni, float no);
        double BrdfWeightAfterSampling(Vector3D i, Vector3D o, Vector3D microNormal);
    }
}
