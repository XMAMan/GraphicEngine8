using System;
using System.Collections.Generic;
using GraphicMinimal;

namespace Radiosity
{
    static class PatchIlluminator
    {
        public static void Illuminate(List<IPatch> patches, int stepCount, Action<string, float> progressChanged)
        {
            foreach (var patch in patches)
            {
                patch.InputRadiosity = new Vector3D(0, 0, 0);
                patch.OutputRadiosity = new Vector3D(0, 0, 0);
            }

            for (int i = 0; i < stepCount; i++)
            {
                progressChanged("Beleuchte", i * 100.0f / stepCount);
                RadiosityStep(patches);
            }
        }

        //Flux = So viel Photonen sendet die gesamte Fläche aus 
        //Radiosity = So viel Photonen wird pro Flächenpunkt ausgesendet
        //Versende die Energie, indem die Flux-Energie übertragen wird (Ganze Fläche strahlt auf ganze anderer Fläche)
        private static void RadiosityStep(List<IPatch> patches)
        {
            foreach (var patch in patches)
            {
                float albedo = patch.RayHeigh.Propertys.Albedo;
                patch.OutputRadiosity = Vector3D.Mult(patch.ColorOnCenterPoint, patch.InputRadiosity) * albedo;
                if (patch.IsLightSource) patch.OutputRadiosity += new Vector3D(1, 1, 1) * patch.EmissionPerPatch / patch.SurfaceArea;
                patch.InputRadiosity = new Vector3D(0, 0, 0);
            }

            foreach (var sender in patches)
            {
                Vector3D senderColor = sender.OutputRadiosity;//Das hier ist die Photonencount pro Fläche.
                foreach (var empfänger in sender.ViewFaktors)
                {
                    //senderColor * sender.SurvaceArea = So viel Flux sendet die Senderfäche in Richtung Empfänger-Center-Point aus
                    //Mit 'Flux aus Sender' * empfänger.Patch.SurvaceArea berechne ich die Empfänger-Flux
                    empfänger.Patch.InputRadiosity += senderColor * empfänger.FormFactor; //Addiere Fluxwerte
                    //Ich vermute, dass 'empfänger.Faktor * sender.SurvaceArea * empfänger.Patch.SurvaceArea' nie größer als 1 sein darf. Das passiert dann, wenn zwei
                    //Flächen sehr nah zueinander sind. Dann führt die Division durch r² zu einen großen Geometryterm. Eine Fläche kann aber nicht mehr, als 100%
                    //seiner Photonen zu einer anderen Fläche senden.
                }
            }

            foreach (var patch in patches)
            {
                patch.InputRadiosity /= patch.SurfaceArea; //Umrechnen von Flux in Radiosity
            }
        }
    }
}
