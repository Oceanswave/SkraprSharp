namespace SkraprSharp.Worker
{
    using NiL.JS.Core;
    using System;

    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //Console.TreatControlCAsInput = true;
            var skraprContext = new SkraprContext();

            try
            {
                var ctx = skraprContext.Initialize();

                string input;
                do
                {
                    Console.Write(">");
                    input = Console.ReadLine();

                    if (input != "Q")
                    {
                        try
                        {
                            var inputResult = ctx.Eval(input);
                            Console.WriteLine(inputResult.ToString());
                        }
                        catch (JSException ex)
                        {
                            Console.Error.WriteLine(ex.Message);
                        }
                    }
                }
                while (input != "Q");
            }
            finally
            {
                skraprContext.Shutdown();
            }
        }
    }
}