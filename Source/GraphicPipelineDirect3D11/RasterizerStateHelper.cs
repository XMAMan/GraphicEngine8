using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace GraphicPipelineDirect3D11
{
    static class RasterizerStateHelper
    {
        public static RasterizerState InitialValue(Device device)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                    new RasterizerStateDescription()
                    {
                        FillMode = FillMode.Solid,
                        CullMode = CullMode.Back,
                        IsDepthClipEnabled = true,
                        IsFrontCounterclockwise = true,
                        IsScissorEnabled = false,
                    });
        }

        public static RasterizerState SetDepthClipEnabled(Device device, RasterizerState oldState, bool newValue)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                    new RasterizerStateDescription()
                    {
                        FillMode = oldState.Description.FillMode,
                        CullMode = oldState.Description.CullMode,
                        IsDepthClipEnabled = newValue,
                        IsFrontCounterclockwise = oldState.Description.IsFrontCounterclockwise,
                        IsScissorEnabled = oldState.Description.IsScissorEnabled
                    });
        }

        public static RasterizerState SetFrontCounterClockwise(Device device, RasterizerState oldState, bool newValue)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                    new RasterizerStateDescription()
                    {
                        FillMode = oldState.Description.FillMode,
                        CullMode = oldState.Description.CullMode,
                        IsDepthClipEnabled = oldState.Description.IsDepthClipEnabled,
                        IsFrontCounterclockwise = newValue,
                        IsScissorEnabled = oldState.Description.IsScissorEnabled
                    });
        }

        public static RasterizerState SetCullMode(Device device, RasterizerState oldState, CullMode newValue)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                    new RasterizerStateDescription()
                    {
                        FillMode = oldState.Description.FillMode,
                        CullMode = newValue,
                        IsFrontCounterclockwise = oldState.Description.IsFrontCounterclockwise,
                        IsDepthClipEnabled = oldState.Description.IsDepthClipEnabled,
                        IsScissorEnabled = oldState.Description.IsScissorEnabled
                    });
        }

        public static RasterizerState SetFillMode(Device device, RasterizerState oldState, FillMode newValue)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                   new RasterizerStateDescription()
                   {
                       FillMode = newValue,
                       CullMode = oldState.Description.CullMode,
                       IsDepthClipEnabled = oldState.Description.IsDepthClipEnabled,
                       IsFrontCounterclockwise = oldState.Description.IsFrontCounterclockwise,
                       IsScissorEnabled = oldState.Description.IsScissorEnabled
                   });
        }

        public static RasterizerState SetIsScissorEnabled(Device device, RasterizerState oldState, bool newValue)
        {
            return RasterizerState.FromDescription(device,//Wireframe, Solid, CullMode
                    new RasterizerStateDescription()
                    {
                        FillMode = oldState.Description.FillMode,
                        CullMode = oldState.Description.CullMode,
                        IsDepthClipEnabled = oldState.Description.IsDepthClipEnabled,
                        IsFrontCounterclockwise = oldState.Description.IsFrontCounterclockwise,
                        IsScissorEnabled = newValue
                    });
        }
    }
}
