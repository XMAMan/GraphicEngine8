using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace GraphicPipelineDirect3D11
{
    static class DepthStencilStateHelper
    {
        public static DepthStencilState InitialValue(Device device)
        {
            return DepthStencilState.FromDescription(device,
                    new DepthStencilStateDescription()
                    {
                        IsDepthEnabled = true,
                        IsStencilEnabled = false,
                        DepthWriteMask = DepthWriteMask.All,
                        DepthComparison = Comparison.Less,
                        FrontFace = new DepthStencilOperationDescription()
                        {
                            Comparison = Comparison.Never,
                            DepthFailOperation = StencilOperation.Zero,
                            FailOperation = StencilOperation.Zero,
                            PassOperation = StencilOperation.Zero
                        },
                        BackFace = new DepthStencilOperationDescription()
                        {
                            Comparison = Comparison.Always,
                            DepthFailOperation = StencilOperation.Zero,
                            FailOperation = StencilOperation.Zero,
                            PassOperation = StencilOperation.Keep
                        },
                        StencilWriteMask = 0xFF,
                        StencilReadMask = 0xFF
                    });
        }

        public static DepthStencilState SetWriteMask(Device device, DepthStencilState oldState, DepthWriteMask newValue)
        {
            return DepthStencilState.FromDescription(device,
                    new DepthStencilStateDescription()
                    {
                        DepthWriteMask = newValue,
                        IsDepthEnabled = oldState.Description.IsDepthEnabled,
                        IsStencilEnabled = oldState.Description.IsStencilEnabled,
                        DepthComparison = oldState.Description.DepthComparison,
                        BackFace = oldState.Description.BackFace,
                        FrontFace = oldState.Description.FrontFace,
                        StencilReadMask = oldState.Description.StencilReadMask,
                        StencilWriteMask = oldState.Description.StencilWriteMask
                    });
        }

        public static DepthStencilState SetDepthIsEnabled(Device device, DepthStencilState oldState, bool newValue)
        {
            return DepthStencilState.FromDescription(device,
                   new DepthStencilStateDescription()
                   {
                       IsDepthEnabled = newValue,
                       IsStencilEnabled = oldState.Description.IsStencilEnabled,
                       DepthWriteMask = oldState.Description.DepthWriteMask,
                       DepthComparison = oldState.Description.DepthComparison,
                       BackFace = oldState.Description.BackFace,
                       FrontFace = oldState.Description.FrontFace,
                       StencilReadMask = oldState.Description.StencilReadMask,
                       StencilWriteMask = oldState.Description.StencilWriteMask
                   });
        }

        public static DepthStencilState SetIsStencilEnabled(Device device, DepthStencilState oldState, bool newValue)
        {
            return DepthStencilState.FromDescription(device,
                     new DepthStencilStateDescription()
                     {
                         IsStencilEnabled = newValue,
                         IsDepthEnabled = oldState.Description.IsDepthEnabled,
                         DepthWriteMask = oldState.Description.DepthWriteMask,
                         DepthComparison = Comparison.LessEqual,//m_context.OutputMerger.DepthStencilState.Description.DepthComparison,
                         BackFace = oldState.Description.BackFace,
                         FrontFace = oldState.Description.FrontFace,
                         StencilReadMask = oldState.Description.StencilReadMask,
                         StencilWriteMask = oldState.Description.StencilWriteMask
                     });
        }

        public static DepthStencilState SetStencilReadParameters(Device device, DepthStencilState oldState,
            byte stencilReadMask,
            StencilOperation backFacePassOperation,
            Comparison backFaceComparison,
            StencilOperation frontFacePassOperation,
            Comparison frontFaceComparison)
        {
            return DepthStencilState.FromDescription(device,
                    new DepthStencilStateDescription()
                    {
                        IsDepthEnabled = oldState.Description.IsDepthEnabled,
                        IsStencilEnabled = oldState.Description.IsStencilEnabled,
                        DepthWriteMask = oldState.Description.DepthWriteMask,
                        DepthComparison = oldState.Description.DepthComparison,
                        StencilWriteMask = oldState.Description.StencilWriteMask,

                        StencilReadMask = 0xFF,
                        BackFace = new DepthStencilOperationDescription()
                        {
                            PassOperation = backFacePassOperation, // Does not update the stencil-buffer entry
                            Comparison = backFaceComparison,       // If the source data is not equal to the destination data, the comparison passes
                            FailOperation = StencilOperation.Keep, // Sets the stencil-buffer entry to 0
                            DepthFailOperation = StencilOperation.Keep
                        },
                        FrontFace = new DepthStencilOperationDescription()
                        {
                            PassOperation = frontFacePassOperation,
                            Comparison = frontFaceComparison,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Keep
                        }
                    });
        }

        public static DepthStencilState SetStencilWriteParameters(Device device, DepthStencilState oldState,
            byte stencilWriteMask,
            StencilOperation backFacePassOperation,
            Comparison backFaceComparison,
            StencilOperation frontFacePassOperation,
            Comparison frontFaceComparison)
        {
            return DepthStencilState.FromDescription(device,
                    new DepthStencilStateDescription()
                    {
                        IsDepthEnabled = oldState.Description.IsDepthEnabled,
                        IsStencilEnabled = oldState.Description.IsStencilEnabled,
                        DepthWriteMask = oldState.Description.DepthWriteMask,
                        DepthComparison = oldState.Description.DepthComparison,
                        StencilReadMask = oldState.Description.StencilReadMask,

                        StencilWriteMask = stencilWriteMask,
                        BackFace = new DepthStencilOperationDescription()
                        {
                            PassOperation = backFacePassOperation,
                            Comparison = backFaceComparison,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Keep
                        },
                        FrontFace = new DepthStencilOperationDescription()
                        {
                            PassOperation = frontFacePassOperation,
                            Comparison = frontFaceComparison,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Keep
                        }
                    });
        }

        public static DepthStencilState SetStencilWriteParameters(Device device, DepthStencilState oldState,
            byte stencilWriteMask,
            StencilOperation frontFacePassOperation,
            Comparison frontFaceComparison)
        {
            return DepthStencilState.FromDescription(device,
                    new DepthStencilStateDescription()
                    {
                        IsDepthEnabled = oldState.Description.IsDepthEnabled,
                        IsStencilEnabled = oldState.Description.IsStencilEnabled,
                        DepthWriteMask = oldState.Description.DepthWriteMask,
                        DepthComparison = oldState.Description.DepthComparison,
                        BackFace = oldState.Description.BackFace,
                        StencilReadMask = oldState.Description.StencilReadMask,

                        StencilWriteMask = 0xFF,
                        FrontFace = new DepthStencilOperationDescription()
                        {
                            PassOperation = frontFacePassOperation,
                            Comparison = frontFaceComparison,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Keep
                        }
                    });
        }
    }
}
