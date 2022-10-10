using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics
{
    //Eine Brdf welche man für Metall oder Glas nutzen kann und welche die Sample- Pdf- und Verteilungsfunktionen von Walter nutzt
    interface IWalterBrdf : IMicrofacetBasics
    {
        double Pdf_wm(Vector3D micronormal);
        Vector3D SampleMicroNormal(double u1, double u2);
    }
}
