using System;
using System.Collections.Generic;
using GraphicMinimal;

namespace Radiosity
{
    static class CornerColorCalculator
    {

        public static void SetCornerColors(List<IPatch> patches, Func<IPatch, Vector3D> patchToColorHandler)
        {
            ColorPatchList list = new ColorPatchList();
            foreach (var patch in patches)
            {
                list.AddColorPatch(new ColorPatch(patch));
            }
            list.SetAllCornerColors(patchToColorHandler);
        }
    }

    class ColorPatchList
    {
        private readonly List<ColorPatch> patches = new List<ColorPatch>();

        public void AddColorPatch(ColorPatch patch)
        {
            foreach (var p in this.patches)
            {
                if (p.Patch.RayHeigh == patch.Patch.RayHeigh)
                {
                    for (int i = 0;i<p.Patch.CornerPoints.Length;i++)
                    {
                        for (int j = 0;j<patch.Patch.CornerPoints.Length;j++)
                        {
                            if ((p.Patch.CornerPoints[i] - patch.Patch.CornerPoints[j]).Length() < 0.0001f && p.Patch.Normal * patch.Patch.Normal > 0.9f)
                            {
                                p.AddAssociation(i, patch);
                                patch.AddAssociation(j, p);
                            }
                        }
                    }
                }
            }

            this.patches.Add(patch);
        }

        public void SetAllCornerColors(Func<IPatch, Vector3D> patchToColorHandler)
        {
            foreach (var p in this.patches)
            {
                p.SetAllCornerColors(patchToColorHandler);
            }
        }
    }

    class ColorPatch
    {
        public IPatch Patch;
        private readonly List<ColorPatch>[] cornerPointList;

        public ColorPatch(IPatch patch)
        {
            this.Patch = patch;
            this.cornerPointList = new List<ColorPatch>[patch.CornerPoints.Length];
            for (int i = 0; i < this.cornerPointList.Length; i++)
            {
                this.cornerPointList[i] = new List<ColorPatch>();
            }
        }

        public void AddAssociation(int thisCornerIndex, ColorPatch otherPatch)
        {
            this.cornerPointList[thisCornerIndex].Add(otherPatch);
        }

        public void SetAllCornerColors(Func<IPatch, Vector3D> patchToColorHandler)
        {
            for (int i = 0; i < this.cornerPointList.Length; i++)
            {
                SetColorForCornerPoint(i, patchToColorHandler);
            }
        }

        private void SetColorForCornerPoint(int index, Func<IPatch, Vector3D> patchToColorHandler)
        {
            Vector3D sum = patchToColorHandler(this.Patch);
            foreach (var p in this.cornerPointList[index])
            {
                sum += patchToColorHandler(p.Patch);
            }

            this.Patch.SetCornerColor(index, sum / (this.cornerPointList[index].Count + 1));
        }
    }
}
