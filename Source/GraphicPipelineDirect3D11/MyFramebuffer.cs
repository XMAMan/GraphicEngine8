using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace GraphicPipelineDirect3D11
{
    class MyFramebuffer
    {
        public RenderTargetView RenderTargetView = (RenderTargetView)null; //Farbpuffer
        public DepthStencilView DepthStencilView = null; //Tiefenpuffer
        public ShaderResourceView ShaderResourceViewColor = null; //Texture, in die gerendert wird
        public ShaderResourceView ShaderResourceViewDepth = null;
        public int Width, Height;
        public int TextureIdColor;
        public int TextureIdDepth;

        public MyFramebuffer(SlimDX.Direct3D11.Device m_device, int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            this.Width = width;
            this.Height = height;

            if (withColorTexture)
            {
                CreateColorTexture(m_device, width, height, out this.RenderTargetView, out this.ShaderResourceViewColor);
            }

            if (withDepthTexture)
            {
                CreateDepthTexture(m_device, width, height, out this.DepthStencilView, out this.ShaderResourceViewDepth);
            }
        }

        private void CreateColorTexture(SlimDX.Direct3D11.Device m_device, int width, int height, out RenderTargetView renderTargetView, out ShaderResourceView shaderResourceView)
        {
            //DXGI_FORMAT_R32G32_SINT = A two-component, 64-bit signed-integer format that supports 32 bits for the red channel and 32 bits for the green channel.
            //DXGI_FORMAT_R32G8X24_TYPELESS = A two-component, 64-bit typeless format that supports 32 bits for the red channel, 8 bits for the green channel, and 24 bits are unused.
            //DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 32-bit depth, 8-bit stencil, and 24 bits are unused.
            //R..Red
            //G..Green
            //B..Blue
            //D..Depth
            //S..Stencil
            //X..Unused

            // create the render target texture
            var texture2D = new Texture2D(m_device, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.B8G8R8A8_UNorm,
                Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                //OptionFlags = ResourceOptionFlags.GenerateMipMaps
            });

            renderTargetView = new RenderTargetView(m_device, texture2D, new RenderTargetViewDescription()
            {
                Format = texture2D.Description.Format,
                Dimension = RenderTargetViewDimension.Texture2D,
                MipSlice = 0
            });

            shaderResourceView = new ShaderResourceView(m_device, texture2D, new ShaderResourceViewDescription()
            {
                Format = texture2D.Description.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MostDetailedMip = 0,
                MipLevels = 1
            });

            texture2D.Dispose();
        }

        private void CreateDepthTexture(SlimDX.Direct3D11.Device m_device, int width, int height, out DepthStencilView depthStencilView, out ShaderResourceView shaderResourceView)
        {
            bool use24Bit = true;

            // create the depth/stencil texture
            var depthTex = new Texture2D(m_device, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Format = use24Bit ? Format.R24G8_Typeless : Format.R32_Typeless,
                Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            //A view is a format-specific way to look at the data in a resource. The view determines what data to look at, and how it is cast when read.
            // you cannot create a resource-view description using any format with _TYPELESS in the name
            //a DXGI_FORMAT_R32G32B32_TYPELESS resource can be viewed with one of these typed formats: DXGI_FORMAT_R32G32B32_FLOAT, DXGI_FORMAT_R32G32B32_UINT
            depthStencilView = new DepthStencilView(m_device, depthTex, new DepthStencilViewDescription()
            {
                Format = use24Bit ? SlimDX.DXGI.Format.D24_UNorm_S8_UInt : SlimDX.DXGI.Format.D32_Float, //Erlaubte Werte: DXGI_FORMAT_D16_UNORM, DXGI_FORMAT_D24_UNORM_S8_UINT, DXGI_FORMAT_D32_FLOAT, DXGI_FORMAT_D32_FLOAT_S8X24_UINT, DXGI_FORMAT_UNKNOWN (typeless nicht erlaubt); If the format chosen is DXGI_FORMAT_UNKNOWN, then the format of the parent resource is used.
                Flags = DepthStencilViewFlags.None,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0,

            });

            shaderResourceView = new ShaderResourceView(m_device, depthTex, new ShaderResourceViewDescription
            {
                Format = use24Bit ? Format.R24_UNorm_X8_Typeless : Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MipLevels = 1,
                MostDetailedMip = 0
            });

            depthTex.Dispose();
        }

        public void Dispose()
        {
            if (RenderTargetView != null) RenderTargetView.Dispose();
            if (DepthStencilView != null) DepthStencilView.Dispose();
            if (ShaderResourceViewColor != null) ShaderResourceViewColor.Dispose();
            if (ShaderResourceViewDepth != null) ShaderResourceViewDepth.Dispose();
        }
    }
}
