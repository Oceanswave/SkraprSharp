namespace SkraprSharp.Worker
{
    using NiL.JS.Core;
    using System;

    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                var ctx = Skrapr.InitializeSkraprContext();

                    //Console.WriteLine("Running...");
                    //var result = ctx.Eval("var cef = require('Cef'); var browser = new cef.WebBrowser(); browser.waitForBrowserInitialization(); browser.loadPage('http://www.google.com'); /*browser.jQuerify();*/ browser.takeScreenshot(); browser.dispose();");
                    //Console.WriteLine(result.ToString());
                    string input;
                    do
                    {
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
                Skrapr.ShutdownSkraprContext();
            }
        }

        //private static async void MainAsync(string cachePath = "cachePath1", double zoomLevel = 1.0)
        //{
        //    var browserSettings = new BrowserSettings();
        //    //Reduce rendering speed to one frame per second so it's easier to take screen shots
        //    browserSettings.WindowlessFrameRate = 1;
        //    var requestContextSettings = new RequestContextSettings { CachePath = cachePath };

        //    // RequestContext can be shared between browser instances and allows for custom settings
        //    // e.g. CachePath
        //    using (var requestContext = new RequestContext(requestContextSettings))
        //    using (var browser = new ChromiumWebBrowser("", browserSettings, requestContext))
        //    {
        //        await browser.WaitForBrowserInitializationAsync();

        //        await browser.LoadPageAsync(zoomLevel: 3);

        //        await browser.ResizeToContentAsync();

        //        await browser.LoadPageAsync(zoomLevel: 3, ignoreCache: false);

        //        var jQueryVersion = await browser.JQuerifyAsync();
        //        Debug.Assert(jQueryVersion == "1.11.1");

        //        // For Google.com pre-populate the search text box using jQuery;
        //        await browser.EvaluateScriptAsync("jQuery('#lst-ib').val('CefSharp Was Here!')");

        //        browser.Redraw();

        //        // Wait for the screenshot to be taken,
        //        // if one exists ignore it, wait for a new one to make sure we have the most up to date
        //        await browser.ScreenshotAsync(true).ContinueWith(DisplayBitmap);
        //    }
        //}
    }
}