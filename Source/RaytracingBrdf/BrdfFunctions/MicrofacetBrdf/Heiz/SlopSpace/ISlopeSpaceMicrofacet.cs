using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace
{
    //Quelle: Importance Sampling Microfacet-Based BSDFs using the_2014 (Eric Heiz)
    //Meine Erklärungen: Forschungen\Microfaset Brdfs_2018_2019\Microfast-Mitschriften.odt
    interface ISlopeSpaceMicrofacet
    {
        float GetSlopeDistribution(float xm, float ym);
        float GetVisibleSlopeDistribution(float phi, float theta, float xm, float ym);
        Vector3D SampleMicronormalAnalytical(Vector3D inputDirection, float u1, float u2);
        Vector3D SampleMicronormalFromTableData(Vector3D inputDirection, float u1, float u2);
    }
}
