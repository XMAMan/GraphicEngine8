using Microsoft.VisualStudio.TestTools.UnitTesting;
using Radiosity._03_ViewFactor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiosityTest
{
    [TestClass]
    public class VisibleMatrixTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void WriteReadWithRandomValues()
        {
            int size = 123;
            var sut = new VisibleMatrix(size);

            Random rand = new Random(0);
            for (int x = 0; x < size; x++)
                for (int y=0;y<size;y++)
                {
                    bool isVisible = rand.Next(2) == 0;
                    sut[x, y] = isVisible ? VisibleMatrix.VisibleValue.Visible : VisibleMatrix.VisibleValue.NotVisible;
                }

            sut.WriteToFile(WorkingDirectory + "RadiosityVisibleMatrix.dat");

            var sut1 = new VisibleMatrix(WorkingDirectory + "RadiosityVisibleMatrix.dat");

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Assert.AreEqual(sut[x, y], sut1[x, y]);
                }
        }
    }
}
