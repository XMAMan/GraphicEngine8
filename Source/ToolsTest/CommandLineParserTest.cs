using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using Tools;
using System.IO;
using GraphicPanels;
using FluentAssertions;
using Tools.CommandLineParsing;

namespace ToolsTest
{
    [TestClass]
    public class CommandLineParserTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;

        [TestMethod]
        public void CreateImage_ValidArgumentsRequiredOnly()
        {
            string args = $"CreateImage -sceneFile {DataDirectory + "02_NoWindowRoom_json.txt"} -output image.bmp -renderMod Direct3D_11 -width 100 -height 120";
            var parsed = Args.ParseAction<CommandLineParser>(args.Split(' '));
            Assert.AreEqual(parsed.ActionArgsProperty.Name, "CreateImage");
            Assert.IsTrue(parsed.ActionArgs is CreateImageArgs);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).SceneFile, new FileInfo($"{DataDirectory + "02_NoWindowRoom_json.txt"}").FullName);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Output, "image.bmp");
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).RenderMod, Mode3D.Direct3D_11);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Width, 100);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Height, 120);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).SampleCount, -1);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).CloseWindowAfterRendering, true);
        }

        [TestMethod]
        public void CreateImage_ValidArgumentsWithOptionalArguments()
        {
            string args = $"CreateImage -sceneFile {DataDirectory + "02_NoWindowRoom_json.txt"} -output image.bmp -renderMod Direct3D_11 -width 100 -height 120 -pixelRange [20;30;50;70] -closeWindowAfterRendering false";
            var parsed = Args.ParseAction<CommandLineParser>(args.Split(' '));
            Assert.AreEqual(parsed.ActionArgsProperty.Name, "CreateImage");
            Assert.IsTrue(parsed.ActionArgs is CreateImageArgs);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).SceneFile, new FileInfo($"{DataDirectory + "02_NoWindowRoom_json.txt"}").FullName);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Output, "image.bmp");
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).RenderMod, Mode3D.Direct3D_11);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Width, 100);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).Height, 120);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).SampleCount, -1);
            Assert.AreEqual((parsed.ActionArgs as CreateImageArgs).CloseWindowAfterRendering, false);

            var r = (parsed.ActionArgs as CreateImageArgs).PixelRange;
            Assert.AreEqual(20, r.XStart);
            Assert.AreEqual(30, r.YStart);
            Assert.AreEqual(50 - 20, r.Width);
            Assert.AreEqual(70 - 30, r.Height);
        }

        [TestMethod]
        public void CreateImage_InvalidSceneFilePath_ThrowsArgException()
        {
            string args = $"CreateImage -sceneFile {DataDirectory + "NotAvailable_json.txt"} -output image.bmp -renderMod Direct3D_11 -width 100 -height 120";

            Action action = () => Args.ParseAction<CommandLineParser>(args.Split(' '));
            action
                .Should().Throw<PowerArgs.ValidationArgException>()
                .WithMessage(@"File not found - ..\..\..\..\Data\NotAvailable_json.txt");

        }

        [TestMethod]
        public void CreateImage_InvalidOutputFileType_ThrowsArgException()
        {
            string args = $"CreateImage -sceneFile {DataDirectory + "02_NoWindowRoom_json.txt"} -output image.hdr2 -renderMod Direct3D_11 -width 100 -height 120";

            Action action = () => Args.ParseAction<CommandLineParser>(args.Split(' '));
            action
                .Should().Throw<PowerArgs.ValidationArgException>()
                .WithMessage("Invalid argument: image.hdr2");

        }
    }
}
