using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace GraphicPipelineDirect3D11
{
    static class BlendStateHelper
    {
        public static BlendState InitialValue(Device device)
        {
            //output = sourcecolor * [sourceoption] [Add | Sub | Max | Min] destinationcolor * [destinationoption]
            //sourcecolor ist die Farbe, wie sie aus dem Pixelshader kommt
            //destinationcolor ist die Farbe, die sich vor dem Blending im Farbpuffer befindet
            BlendStateDescription blendDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendDesc.RenderTargets[0].BlendOperation = BlendOperation.Add;
            blendDesc.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
            blendDesc.RenderTargets[0].SourceBlendAlpha = BlendOption.Zero;
            blendDesc.RenderTargets[0].DestinationBlendAlpha = BlendOption.Zero;
            blendDesc.RenderTargets[0].SourceBlend = BlendOption.SourceColor;
            blendDesc.RenderTargets[0].DestinationBlend = BlendOption.One;
            blendDesc.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            blendDesc.RenderTargets[0].BlendEnable = false;
            return BlendState.FromDescription(device, blendDesc);
        }

        public static BlendState SetBlendState(Device device, bool blendEnable, ColorWriteMaskFlags renderTargetWriteMask)
        {
            //output = sourcecolor * [sourceoption] [Add | Sub | Max | Min] destinationcolor * [destinationoption]
            //sourcecolor ist die Farbe, wie sie aus dem Pixelshader kommt
            //destinationcolor ist die Farbe, die sich vor dem Blending im Farbpuffer befindet
            BlendStateDescription blendDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false, // If true -> nicer looking anti-aliased transparency when doing multisampled anti-aliasing
                IndependentBlendEnable = false // If set to FALSE, only the RenderTarget[0] members are used. RenderTarget[1..7] are ignored.
            };

            blendDesc.RenderTargets[0].BlendOperation = BlendOperation.Add;
            blendDesc.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
            blendDesc.RenderTargets[0].SourceBlendAlpha = BlendOption.One; //BlendOption.Zero -> http://stackoverflow.com/questions/14491824/dx11-alpha-blending-when-rendering-to-a-texture
            blendDesc.RenderTargets[0].DestinationBlendAlpha = BlendOption.InverseSourceAlpha;    //http://stackoverflow.com/questions/27929483/directx-render-to-texture-alpha-blending

            //Quelle: http://takinginitiative.wordpress.com/2010/04/09/directx-10-tutorial-6-transparency-and-alpha-blending/
            blendDesc.RenderTargets[0].SourceBlend = BlendOption.SourceAlpha;
            blendDesc.RenderTargets[0].DestinationBlend = BlendOption.InverseSourceAlpha;

            blendDesc.RenderTargets[0].RenderTargetWriteMask = renderTargetWriteMask;//Schreiben im Farbpuffer erlaubt?

            blendDesc.RenderTargets[0].BlendEnable = blendEnable;
            return BlendState.FromDescription(device, blendDesc);
        }
    }
}
