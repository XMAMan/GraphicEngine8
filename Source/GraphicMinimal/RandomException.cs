using System;

namespace GraphicMinimal
{
    //Diese Exception wird geworfen, wenn eine Methode mit ein ganz bestimmten Random-Zustand gerufen wird
    public class RandomException : Exception
    {
        public string RandomObjectBase64Coded { get; private set; }

        public RandomException(string randomObjectBase64Coded)
        {
            this.RandomObjectBase64Coded = randomObjectBase64Coded;
        }

        public RandomException(string randomObjectBase64Coded, string message)
            : base(message)
        {
            this.RandomObjectBase64Coded = randomObjectBase64Coded;
        }

        public RandomException(string randomObjectBase64Coded, string message, Exception inner)
            : base(message, inner)
        {
            this.RandomObjectBase64Coded = randomObjectBase64Coded;
        }
    }
}
