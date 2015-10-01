namespace SkraprSharp.Library
{
    using CefSharp;
    using CefSharp.OffScreen;
    using Extensions;
    using NiL.JS.Core;
    using NiL.JS.Core.Modules;
    using NiL.JS.Core.TypeProxing;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class WebBrowser : CustomType, IDisposable
    {
        private readonly ChromiumWebBrowser m_webBrowser;

        [DoNotEnumerate]
        public WebBrowser(Arguments args)
        {
            var address = args["address"].Value as string;

            //TODO: Add ability to define browser settings.
            if (address != null)
                m_webBrowser = new ChromiumWebBrowser(address);
            else
                m_webBrowser = new ChromiumWebBrowser();
        }

        public bool isBrowserInitialized
        {
            get { return m_webBrowser.IsBrowserInitialized; }
        }

        public bool isLoading
        {
            get { return m_webBrowser.IsLoading; }
        }

        public void evaluateScript(string script)
        {
            m_webBrowser.EvaluateScriptAsync(script).Wait();
        }

        public void loadPage(string address)
        {
            m_webBrowser.LoadPageAsync(address).Wait();
        }

        public string jQuerify()
        {
            var jQuerify = m_webBrowser.JQuerifyAsync();
            jQuerify.Wait();
            return jQuerify.Result;
        }

        public void redraw()
        {
            m_webBrowser.Redraw();
        }

        public void takeScreenshot()
        {
            var task = m_webBrowser.ScreenshotAsync(true);
            task.Wait(5000);
            DisplayBitmap(task);
        }

        private static void DisplayBitmap(Task<Bitmap> task)
        {
            if (task.Status != TaskStatus.RanToCompletion)
                return;

            // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot" + DateTime.Now.Ticks + ".png");

            Console.WriteLine();
            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

            var bitmap = task.Result;

            // Save the Bitmap to the path.
            // The image type is auto-detected via the ".png" extension.
            bitmap.Save(screenshotPath);

            // We no longer need the Bitmap.
            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
            bitmap.Dispose();

            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

            // Tell Windows to launch the saved image.
            Process.Start(screenshotPath);

            Console.WriteLine("Image viewer launched.  Press any key to exit.");
        }

        public void waitUntilLoadedOrTimeout(string timeout)
        {
            TimeSpan? tsTimeout = null;
            if (!string.IsNullOrEmpty(timeout))
                tsTimeout = TimeSpan.Parse(timeout);

            m_webBrowser.WaitUntilLoadedOrTimeoutAsync(tsTimeout).Wait();
        }

        public void waitForBrowserInitialization()
        {
            m_webBrowser.WaitForBrowserInitializationAsync().Wait();
        }

        public void dispose()
        {
            Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_webBrowser.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WebBrowser() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        [Hidden]
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
