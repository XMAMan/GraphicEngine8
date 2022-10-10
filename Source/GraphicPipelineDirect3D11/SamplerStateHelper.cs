using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace GraphicPipelineDirect3D11
{
    class SamplerStateHelper
    {
        public static SamplerState InitialValue(Device device)
        {
            return SamplerState.FromDescription(device, new SamplerDescription()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MaximumAnisotropy = 4,
            });
        }

        public static SamplerState SetFilter(Device device, SamplerState oldState, Filter newValue)
        {
            return SamplerState.FromDescription(device, new SamplerDescription()
            {
                Filter = newValue,
                AddressU = oldState.Description.AddressU,
                AddressV = oldState.Description.AddressV,
                AddressW = oldState.Description.AddressW,
                MaximumAnisotropy = oldState.Description.MaximumAnisotropy,
            });
        }

        public static SamplerState SetAddressUVW(Device device, SamplerState oldState, TextureAddressMode newValue)
        {
            return SamplerState.FromDescription(device, new SamplerDescription()
            {
                Filter = oldState.Description.Filter,
                AddressU = newValue,
                AddressV = newValue,
                AddressW = newValue,
                MaximumAnisotropy = oldState.Description.MaximumAnisotropy,
            });
        }
    }
}
