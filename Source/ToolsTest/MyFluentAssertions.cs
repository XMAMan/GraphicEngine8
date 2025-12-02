using System;

namespace ToolsTest
{
    //Ersatz für FluentAssertions da es unter .NET Framework 4.8 nicht mehr läuft
    internal static class MyFluentAssertions
    {
        public class ActionRunner
        {
            private readonly Action _action;
            public ActionRunner(Action action) 
            { 
                _action = action;
            }
            public void Throw<TException>(string message) where TException : Exception
            {
                try
                {
                    this._action();
                }catch (Exception ex)
                {
                    if(ex is TException && ex.Message == message)
                    {
                        return;
                    }
                    throw new Exception($"Expected exception of type {typeof(TException).FullName} but got {ex.GetType().FullName}.");
                }
                return;
            }
        }

        public static ActionRunner Should(this Action action)
        {
            return new ActionRunner(action);
        }
    }
}
