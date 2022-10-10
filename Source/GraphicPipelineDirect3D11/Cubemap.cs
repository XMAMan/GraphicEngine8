using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace GraphicPipelineDirect3D11
{
    //Hilft beim erstellen/umschalten/speichern der ganzen Cubemaps
    class CubemapControlData
    {
        public Cubemap CurrentCubmapToRenderIn = null;

        public RenderTargetView renderTargetViewBevoreEnableWriteToCubemap;
        public DepthStencilView depthStencelViewBevoreEnableWriteToCubemap;
        public Viewport viewportBevoreEnableWriteToCubemap;

        private Dictionary<int, Cubemap> cubemaps = new Dictionary<int, Cubemap>(); //[CubemapID | 6 2D-Texturen]

        public int CreateCubeMap(SlimDX.Direct3D11.Device m_device, int cubeMapSize = 256)
        {
            int newID = 1;
            if (this.cubemaps.Keys.Count > 0)
                newID = this.cubemaps.Keys.Max() + 1;

            this.cubemaps.Add(newID, new Cubemap(m_device, cubeMapSize));
            return newID;
        }

        public Cubemap this[int cubemapId]
        {
            get
            {
                return this.cubemaps[cubemapId];
            }
        }

        public void Dispose()
        {
            foreach (var cub in this.cubemaps.Values)
            {
                cub.DynamicCubeMapDSV.Dispose();
                cub.TextureArrayShaderResourceView.Dispose();
                cub.DynamicCubeMapSRV.Dispose();
            }
        }
    }

    //In der Grafikkarte liegen 6 2D-Texturen als Byte-Block von Daten vor
    //Ich kann nun diese Daten verschieden interpretieren
    //Die Renderpipeline malt seine Farben in eine RenderTargetView. Ich kann also an 6 verschiedenen Stellen diesen Datenblock als RenderTargetView interpretieren
    //Wenn die Pipeline dann Color-Daten ausgibt, werden diese also an 6 verschiedenen Stellen im Speicher geschrieben.
    //Der Shader kann das ganze entweder als TextureCube interpretieren oder als Texture2DArray. Bei einer TextureCube macht er das Cubemapping 
    //beim Textur-Samplen selber. Beim Texture2DArray muss ich selber den Index und die UV-Koordinate berechnen.
    class Cubemap
    {
        //public RenderTargetView renderTargetViewBevoreEnableWriteToCubemap;
        //public DepthStencilView depthStencelViewBevoreEnableWriteToCubemap;
        //public Viewport viewportBevoreEnableWriteToCubemap;

        public RenderTargetView[] DynamicCubeMapRTV;
        public ShaderResourceView DynamicCubeMapSRV;
        public ShaderResourceView TextureArrayShaderResourceView;
        public DepthStencilView DynamicCubeMapDSV;
        public int CubeMapSize;

        public Cubemap(SlimDX.Direct3D11.Device m_device, int cubeMapSize)
        {
            RenderTargetView[] _dynamicCubeMapRTV = new RenderTargetView[6];
            ShaderResourceView _dynamicCubeMapSRV;
            DepthStencilView _dynamicCubeMapDSV;

            // create the render target cube map texture
            var texDesc = new Texture2DDescription()
            {
                Width = cubeMapSize,
                Height = cubeMapSize,
                MipLevels = 0,
                ArraySize = 6,
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.TextureCube | ResourceOptionFlags.GenerateMipMaps
            };
            var cubeTex = new Texture2D(m_device, texDesc);
            // create the render target view array
            var rtvDesc = new RenderTargetViewDescription()
            {
                Format = texDesc.Format,
                Dimension = RenderTargetViewDimension.Texture2DArray,
                ArraySize = 1,
                MipSlice = 0,
            };

            for (int i = 0; i < 6; i++)
            {
                rtvDesc.FirstArraySlice = i;
                _dynamicCubeMapRTV[i] = new RenderTargetView(m_device, cubeTex, rtvDesc);
            }
            // Create the shader resource view that we will bind to our effect for the cubemap
            var srvDesc = new ShaderResourceViewDescription()
            {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.TextureCube,
                MostDetailedMip = 0,
                MipLevels = -1, 
            };

            _dynamicCubeMapSRV = new ShaderResourceView(m_device, cubeTex, srvDesc);

            this.TextureArrayShaderResourceView = new ShaderResourceView(m_device, cubeTex, new ShaderResourceViewDescription()
            {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                ArraySize = 6,
                MostDetailedMip = 0,
                MipLevels = 1,
                FirstArraySlice = 0
            });


            // release the texture, now that it is saved to the views
            cubeTex.Dispose(); //Marshal.ReleaseComObject(cubeTex);
            // create the depth/stencil texture
            var depthTexDesc = new Texture2DDescription()
            {
                Width = cubeMapSize,
                Height = cubeMapSize,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.D24_UNorm_S8_UInt, //Format.D32_Float,
                Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            var depthTex = new Texture2D(m_device, depthTexDesc);
            var dsvDesc = new DepthStencilViewDescription()
            {
                Format = depthTexDesc.Format,
                Flags = DepthStencilViewFlags.None,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0,

            };
            _dynamicCubeMapDSV = new DepthStencilView(m_device, depthTex, dsvDesc);

            depthTex.Dispose();//Marshal.ReleaseComObject(depthTex);

            this.DynamicCubeMapRTV = _dynamicCubeMapRTV;
            this.DynamicCubeMapSRV = _dynamicCubeMapSRV;
            this.DynamicCubeMapDSV = _dynamicCubeMapDSV;
            this.CubeMapSize = cubeMapSize;
        }
    }
}
