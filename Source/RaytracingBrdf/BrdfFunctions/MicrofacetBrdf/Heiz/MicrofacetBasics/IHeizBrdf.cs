using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics
{
    //Eine Brdf welche man für Metall oder Glas nutzen kann und welche die Sample- Pdf- und Verteilungsfunktionen von Heiz nutzt
    interface IHeizBrdf : IMicrofacetBasics
    {
        double Pdf_wm(Vector3D lightGoingInDirection, Vector3D micronormal);
        Vector3D SampleVisibleMicroNormal(Vector3D directionToCamera, double u1, double u2);
    }
}
