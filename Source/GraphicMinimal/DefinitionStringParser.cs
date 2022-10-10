using System;
using System.Collections.Generic;

namespace GraphicMinimal
{
    //Parst ein Definition-String
    //Ident()       -> Name='Ident';  Parameter = new string[0]
    //Rotate(3)     -> Name='Rotate'; Parameter = new string[] {"3"}
    //Scale(0.5,3)  -> Name='Scale';  Parameter = new string[] {"0.5", "3"}
    //[1,5,9]       -> Name='Array';  Parameter = new string[] {"1", "5", "9"}
    //123           -> Name='NumberOnly'; Parameter = new string[]{"123"}
    public class DefinitionStringParser
    {
        public static DefinitionStringParserResult Parse(string definition)
        {
            DefinitionTokenReader reader = new DefinitionTokenReader(definition);

            return DefinitionStringParser.Parse(reader); 
        }

        private static DefinitionStringParserResult Parse(DefinitionTokenReader reader)
        {
            int startIndex = reader.Index;

            reader.ReadWhiteSpace();

            string number = reader.ReadNumber();
            if (string.IsNullOrEmpty(number) == false) return new DefinitionStringParserResult(reader.Definition.Substring(startIndex, reader.Index- startIndex), "NumberOnly", new object[] { number });

            char c = reader.GetCurrentChar();
            
            if (c== '[') return ParseArray(startIndex, reader);

            return ParseFunction(startIndex, reader);
        }

        private static DefinitionStringParserResult ParseFunction(int startIndex, DefinitionTokenReader reader)
        {
            string functionName = reader.ReadName();
            reader.ReadChar('(');

            List<object> numbers = ReadNumbers(reader, ')');

            return new DefinitionStringParserResult(reader.Definition.Substring(startIndex, reader.Index - startIndex), functionName, numbers.ToArray());
        }

        private static DefinitionStringParserResult ParseArray(int startIndex, DefinitionTokenReader reader)
        {
            reader.ReadChar('[');
            List<object> numbers = ReadNumbers(reader, ']');
            return new DefinitionStringParserResult(reader.Definition.Substring(startIndex, reader.Index - startIndex), "Array", numbers.ToArray());
        }

        //Ließt so lange Zahlen, bis ein endChar kommt
        private static List<object> ReadNumbers(DefinitionTokenReader reader, char endChar)
        {
            List<object> numbers = new List<object>();
            while (true)
            {
                reader.ReadWhiteSpace();

                string number = reader.ReadNumber();
                if (string.IsNullOrEmpty(number))
                {
                    char c = reader.GetCurrentChar();
                    if (c == endChar)
                    {
                        reader.ReadChar(endChar);
                        break;
                    }

                    numbers.Add(Parse(reader));
                }
                else
                {
                    numbers.Add(number);
                }
                
                reader.ReadWhiteSpace();

                char comma = reader.ReadChar(',', endChar);
                if (comma == endChar) break;
            }

            return numbers;
        }
    }

    public class DefinitionStringParserResult
    {
        public string DefinitionString { get; private set; }
        public string FunctionName { get; private set; }
        public object[] Parameter;

        public DefinitionStringParserResult(string definitionString, string functionName, object[] parameter)
        {
            this.DefinitionString = definitionString;
            this.FunctionName = functionName;
            this.Parameter = parameter;
        }
    }

    //Ließt die Token von ein Definition-String
    class DefinitionTokenReader
    {
        public string Definition { get; private set; }
        public int Index { get; private set; } = 0;
        public DefinitionTokenReader(string definition)
        {
            this.Definition = definition;
        }

        public char GetCurrentChar()
        {
            return this.Definition[this.Index];
        }

        public void ReadChar(char c)
        {
            if (this.Index >= this.Definition.Length || this.Definition[this.Index] != c) throw new Exception($"Could not read {c}");
            Index++;
        }

        public char ReadChar(char c1, char c2)
        {
            if (this.Index >= this.Definition.Length ||
                (this.Definition[this.Index] != c1 &&
                this.Definition[this.Index] != c2)) throw new Exception($"Could not read {c1} or {c2}");
            
            char c = this.Definition[this.Index];

            Index++;

            return c;
        }

        public string ReadName()
        {
            if (this.Index >= this.Definition.Length) return null;

            int startIndex = this.Index;
            while (this.Index < this.Definition.Length && IsText(this.Definition[this.Index])) this.Index++;

            return this.Definition.Substring(startIndex, Index - startIndex);
        }

        private bool IsText(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public string ReadWhiteSpace()
        {
            if (this.Index >= this.Definition.Length) return null;

            int startIndex = this.Index;
            while (this.Index < this.Definition.Length && IsWhiteSpace(this.Definition[this.Index])) this.Index++;

            return this.Definition.Substring(startIndex, Index - startIndex);
        }

        private bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }


        public string ReadNumber()
        {
            if (this.Index >= this.Definition.Length) return null;

            int startIndex = this.Index;
            while (this.Index < this.Definition.Length && IsNumber(this.Definition[this.Index])) this.Index++;

            return this.Definition.Substring(startIndex, Index - startIndex);
        }

        private bool IsNumber(char c)
        {
            return c == '-' || c == '+' || c == '.' || (c >= '0' && c <= '9');
        }
    }
}
