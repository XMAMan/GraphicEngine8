using GraphicMinimal;
using ImageCreator;
using RaytracingColorEstimator;
using System;

namespace RaytracerMain
{
    static class Helper
    {
        //Wenn ich ohne ImageCreator direkt einzelne Pixelwerte bestimmen will
        public static IPixelEstimator CreateColorEstimator(this IPixelEstimator pixelEstimator, RaytracingFrame3DData data)
        {
            pixelEstimator.BuildUp(data);
            return pixelEstimator;
        }

        //Wenn ich für ein ganzes Bild mit ein IPixelEstimator die Farbwerte bestimmen will
        public static IMasterImageCreator CreateImageCreator(this IPixelEstimator pixelEstimator, RaytracingFrame3DData data)
        {
            pixelEstimator.BuildUp(data);

            //Lighttacing benötigt ein FrameImageCreator
            if (pixelEstimator.CreatesLigthPaths)
            {
                if (!(pixelEstimator is IFrameEstimator frameEstimator)) //Verwende ich Lighttracing ohne Photonmap?
                {
                    return MasterImageCreator.CreateInFrameMode(new PixelToFrameTranslator(pixelEstimator), data);
                }
                return MasterImageCreator.CreateInFrameMode(frameEstimator, data);
            }
            else
            {
                if (data.GlobalObjektPropertys.RaytracerRenderMode == RaytracerRenderMode.Frame)
                    return MasterImageCreator.CreateInFrameMode(new PixelToFrameTranslator(pixelEstimator), data);
                if (data.GlobalObjektPropertys.RaytracerRenderMode == RaytracerRenderMode.SmallBoxes)
                    return MasterImageCreator.CreateInPixelMode(pixelEstimator, data);
            }

            throw new Exception("Unbekannter Typ: " + pixelEstimator.GetType().Name);
        }
    }
}
