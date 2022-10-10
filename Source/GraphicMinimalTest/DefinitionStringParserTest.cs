using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GraphicMinimal;

namespace GraphicMinimalTest
{
    [TestClass]
    public class DefinitionStringParserTest
    {
        [TestMethod]
        public void Parse_NoParameters_OnlyMethodName()
        {
            var result = DefinitionStringParser.Parse("Ident()");

            Assert.AreEqual("Ident()", result.DefinitionString);
            Assert.AreEqual("Ident", result.FunctionName);
            Assert.AreEqual(0, result.Parameter.Length);
        }

        [TestMethod]
        public void Parse_OneInteger_ParameterLength1()
        {
            var result = DefinitionStringParser.Parse("Rotate(3)");

            Assert.AreEqual("Rotate(3)", result.DefinitionString);
            Assert.AreEqual("Rotate", result.FunctionName);
            Assert.AreEqual(1, result.Parameter.Length);
            Assert.AreEqual("3", result.Parameter[0]);
        }

        [TestMethod]
        public void Parse_TwoFloats_ParameterLength2()
        {
            var result = DefinitionStringParser.Parse("Scale(0.5, 3)");

            Assert.AreEqual("Scale(0.5, 3)", result.DefinitionString);
            Assert.AreEqual("Scale", result.FunctionName);
            Assert.AreEqual(2, result.Parameter.Length);
            Assert.AreEqual("0.5", result.Parameter[0]);
            Assert.AreEqual("3", result.Parameter[1]);
        }

        [TestMethod]
        public void Parse_Array_ParameterLength3()
        {
            var result = DefinitionStringParser.Parse("[1, 5, 9]");

            Assert.AreEqual("[1, 5, 9]", result.DefinitionString);
            Assert.AreEqual("Array", result.FunctionName);
            Assert.AreEqual(3, result.Parameter.Length);
            Assert.AreEqual("1", result.Parameter[0]);
            Assert.AreEqual("5", result.Parameter[1]);
            Assert.AreEqual("9", result.Parameter[2]);
        }

        [TestMethod]
        public void Parse_NumberOnly_TwoFunctions()
        {
            var result = DefinitionStringParser.Parse("123");

            Assert.AreEqual("123", result.DefinitionString);
            Assert.AreEqual("NumberOnly", result.FunctionName);
            Assert.AreEqual(1, result.Parameter.Length);
            Assert.AreEqual("123", result.Parameter[0]);
        }

        [TestMethod]
        public void Parse_NestedFunctions_TwoFunctions()
        {
            var result = DefinitionStringParser.Parse("Transpose(5, Rotate(3.4))");

            Assert.AreEqual("Transpose(5, Rotate(3.4))", result.DefinitionString);
            Assert.AreEqual("Transpose", result.FunctionName);
            Assert.AreEqual(2, result.Parameter.Length);
            Assert.AreEqual("5", result.Parameter[0]);
            Assert.IsTrue(result.Parameter[1] is DefinitionStringParserResult);

            var nested = result.Parameter[1] as DefinitionStringParserResult;
            Assert.AreEqual("Rotate(3.4)", nested.DefinitionString);
            Assert.AreEqual("Rotate", nested.FunctionName);
            Assert.AreEqual(1, nested.Parameter.Length);
            Assert.AreEqual("3.4", nested.Parameter[0]);
        }
    }
}
