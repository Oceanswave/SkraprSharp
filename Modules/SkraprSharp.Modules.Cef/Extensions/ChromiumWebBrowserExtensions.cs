namespace SkraprSharp.Extensions
{
    using CefSharp;
    using CefSharp.OffScreen;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains extension methods to add common functionality to CefSharp.Offscreen.ChromiumWebBrowser
    /// </summary>
    public static class ChromiumWebBrowserExtensions
    {
        /// <summary>
        /// Resizes the browser to the size of the content, taking into account zoom level. This method must be called prior to page load.
        /// </summary>
        /// <returns></returns>
        public static async Task ResizeToContentAsync(this ChromiumWebBrowser browser)
        {
            var width = await browser.EvaluateScriptAsync("document.body.clientWidth");
            var height = await browser.EvaluateScriptAsync("document.body.clientHeight");

            var zoomLevel = await browser.GetZoomLevelAsync();
            int finalWidth = (int)Math.Round((int)width.Result * zoomLevel, 0);
            int finalHeight = (int)Math.Round((int)height.Result * zoomLevel, 0);

            browser.Size = new System.Drawing.Size(finalWidth, finalHeight);

            browser.Redraw();
            await browser.WaitForBrowserInitializationAsync();
        }

        /// <summary>
        /// Waits for browser initialization to complete.
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static Task WaitForBrowserInitializationAsync(this ChromiumWebBrowser browser)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (browser.IsBrowserInitialized)
                tcs.SetResult(true);
            else
            {
                EventHandler browserInitializedChangeHandler = null;
                browserInitializedChangeHandler = (sender, args) =>
                {
                    var b = (ChromiumWebBrowser)sender;
                    if (b.IsBrowserInitialized)
                        tcs.SetResult(true);
                };

                browser.BrowserInitialized += browserInitializedChangeHandler;
            }

            return tcs.Task;
        }
    }
}
