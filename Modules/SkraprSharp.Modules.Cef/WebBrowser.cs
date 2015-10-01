namespace SkraprSharp.Modules.Cef
{
    using CefSharp;
    using CefSharp.OffScreen;
    using Extensions;
    using NiL.JS.Core;
    using NiL.JS.Core.Modules;
    using NiL.JS.Core.TypeProxing;
    using Library;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NiL.JS.BaseLibrary;

    public sealed class WebBrowser : CustomType, IDisposable
    {
        private ChromiumWebBrowser m_webBrowser;

        [DoNotEnumerate]
        [Hidden]
        public WebBrowser()
        {
            //TODO: Register this object for SkraprContext.ensureDisposal
            DispatcherThread = new DispatcherThread();
        }

        [DoNotEnumerate]
        public WebBrowser(Arguments args) : this()
        {
            var address = args["address"].Value as string;

            DispatcherThread.StartNew(() =>
            {
                //TODO: Add ability to define browser settings.
                if (address != null)
                    m_webBrowser = new ChromiumWebBrowser(address);
                else
                    m_webBrowser = new ChromiumWebBrowser("");

            }).Wait();

            //Ensure that the web browser is initialized.
            using (var evt = new ManualResetEvent(false))
            {
                m_webBrowser.BrowserInitialized += (o, e) => evt.Set();

                DispatcherThread.StartNew(() =>
                {

                    if (m_webBrowser.IsBrowserInitialized)
                    {
                        evt.Set();
                    }
                });

                evt.WaitOne();
            }
        }

        private DispatcherThread DispatcherThread
        {
            get;
            set;
        }

        public bool isBrowserInitialized
        {
            get { return m_webBrowser.IsBrowserInitialized; }
        }

        public bool isLoading
        {
            get { return m_webBrowser.IsLoading; }
        }

        public Promise evaluateScript(string script)
        {
            var task = m_webBrowser.EvaluateScriptAsync(script)
                .ContinueWith<JSObject>(t =>
                {
                    var jsResult = t.Result;
                    if (jsResult.Success)
                        return jsResult.Result.ToString();
                    else
                        throw new JSException(new Error(jsResult.Message));
                });
            return new Promise(task);
        }

        public Promise loadPage(string address)
        {
            var task = m_webBrowser.LoadPageAsync(address)
                .ContinueWith<JSObject>(t =>
                {
                    return this;
                });

            return new Promise(task);
        }

        public Promise injectScript(string scriptUrl)
        {
            var task = m_webBrowser.InjectScriptAsync(scriptUrl, matchScheme: true)
                .ContinueWith<JSObject>(t =>
                {
                    return this;
                });
            return new Promise(task);
        }

        public Promise jQuerify()
        {
            var task = m_webBrowser.JQuerifyAsync()
                .ContinueWith<JSObject>(t =>
                {
                    return t.Result;
                });

            return new Promise(task);
        }

        public void redraw()
        {
            m_webBrowser.Redraw();
        }

        public Promise takeScreenshot()
        {
            //Force the browser to redraw. Without this call, ScreenshotAsync blocks indefinately.
            m_webBrowser.Redraw();

            var task = m_webBrowser.ScreenshotAsync(true)
                .ContinueWith<JSObject>(t =>
                {
                    DisplayBitmap(t);
                    return this;
                });
            return new Promise(task);
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
        }

        public Promise waitUntilLoadedOrTimeout(string timeout)
        {
            TimeSpan? tsTimeout = null;
            if (!string.IsNullOrEmpty(timeout))
                tsTimeout = TimeSpan.Parse(timeout);

            var task = m_webBrowser.WaitUntilLoadedOrTimeoutAsync(tsTimeout)
                .ContinueWith<JSObject>(t =>
                {
                    return this;
                });

            return new Promise(task);
        }

        public Promise waitForBrowserInitialization()
        {
            var task = m_webBrowser.WaitForBrowserInitializationAsync()
                .ContinueWith<JSObject>(t =>
                {
                    return this;
                });

            return new Promise(task);
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
                    if (m_webBrowser != null)
                    {
                        m_webBrowser.Dispose();
                        m_webBrowser = null;
                    }

                    if (DispatcherThread != null)
                    {
                        DispatcherThread.Dispose();
                        m_webBrowser = null;
                    }
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
