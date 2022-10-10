using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution
{
    interface ISlopeDistribution
    {
        float P22(float xm, float ym); //Slope-Dichtefunktion für Microfacet mit Roughness-Parametern alphaX == alphaY == 1
        Vector2D SampleVisibleSlope(double thetaInputDirection, double u1, double u2);//Die Slope-Verteilung wird so gesampelt, dass ein Slope, welche aus der Inputdirection (theta; phi=0) mit  alphaX == alphaY == 1 ein Slope gesampelt wird
        float SlopeMax { get; }//Beim erstellen der Inversen CDF-Tabelle wird im SlopeSpace von Xm/Ym=-SlopeMax .. +SlopeMax gearbeitet
    }
}
