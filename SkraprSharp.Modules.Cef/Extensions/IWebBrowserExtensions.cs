namespace SkraprSharp.Extensions
{
    using CefSharp;
    using CefSharp.OffScreen;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains extension methods to add common functionality to CefSharp.IWebBrowser
    /// </summary>
    public static class IWebBrowserExtensions
    {
        #region Pre-defined scripts
        private const string InjectScriptJS = @"(function() {
function getScript(url,success) {
    var script=document.createElement('script');
    script.src=url;
    var head=document.getElementsByTagName('head')[0];
    var done=false;
        
    script.onload=script.onreadystatechange = function(){
        if ( !done && (!this.readyState
            || this.readyState == 'loaded'
            || this.readyState == 'complete') ) {
        done=true;
        
        script.onload = script.onreadystatechange = null;
        head.removeChild(script);
        if (typeof(success) != 'undefined' && success != null) {
            success();
          }
        }
    };
    head.appendChild(script);
}

getScript('{{ScriptUri}}', function() { {{SuccessCallback}} });
})();
";
        #endregion

        /// <summary>
        /// Executes the specified script (That returns a json.stringified string) and returns a dynamic result.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static async Task<dynamic> EvaluateScriptWithDynamicResultAsync(this IWebBrowser browser, string script)
        {
            var result = await browser.EvaluateScriptAsync(script);

            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            //If the result is a primitive, return that object.
            if (result.Result is decimal ||
                result.Result is long ||
                result.Result is bool)
                return result;

            var strResult = result.Result.ToString();

            if (string.IsNullOrWhiteSpace(strResult))
                return strResult;

            return JToken.Parse(strResult);
        }

        /// <summary>
        /// Executes the specified script (That returns a json.stringified string) and returns a strongly-typed result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="browser"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static async Task<T> EvaluateScriptWithResultAsync<T>(this IWebBrowser browser, string script)
        {
            var result = await browser.EvaluateScriptAsync(script);

            if (!result.Success)
                return default(T);

            string strResult;
            if (result.Result.GetType() != typeof(string))
                strResult = JsonConvert.SerializeObject(result.Result);
            else
                strResult = result.Result as string;

            if (string.IsNullOrWhiteSpace(strResult))
                return default(T);

            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(strResult, typeof(T));

            return JsonConvert.DeserializeObject<T>(strResult);
        }

        /// <summary>
        /// Evaluates the specified script (That returns a json.stringify-ed string) and populates the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="browser"></param>
        /// <param name="obj"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static async void EvaluateScriptPopulateObjectAsync<T>(this IWebBrowser browser, T obj, string script)
        {
            var result = await browser.EvaluateScriptAsync(script);

            if (!result.Success)
                return;

            string strResult;
            if (result.Result.GetType() != typeof(string))
                strResult = JsonConvert.SerializeObject(result.Result as string);
            else
                strResult = result.Result as string;

            if (strResult == null)
                return;

            JsonConvert.PopulateObject(strResult, obj);
        }

        /// <summary>
        /// Injects the javascript file at the specified url into the browser.
        /// </summary>
        /// <param name="scriptUrl">Url of the script to inject.</param>
        /// <param name="callbackScript">(Optional) When specified, script that is called after the javascript file is injected</param>
        /// <param name="continuationScript">(Optional) When specified, this function blocks until the continuation script returns true</param>
        /// <param name="timeout">The timeout</param>
        /// <returns></returns>
        public static async Task InjectScriptAsync(this IWebBrowser browser, string scriptUrl, string callbackScript = null, string continuationScript = null, TimeSpan? timeout = null, bool matchScheme = false)
        {
            if (browser == null)
                throw new ArgumentNullException("browser");

            if (string.IsNullOrWhiteSpace(scriptUrl))
                throw new ArgumentNullException("url");

            Uri scriptUri;
            if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out scriptUri))
                throw new ArgumentOutOfRangeException("Script url should be a valid absolute url.");

            var browserAddressUri = new Uri(browser.Address, UriKind.Absolute);
            if (browserAddressUri.Scheme.ToLowerInvariant() != scriptUri.Scheme.ToLowerInvariant())
            {
                if (!matchScheme)
                    throw new InvalidOperationException(string.Format("Browser address and script url scheme should match. The browser is currently pointed at a {0}, and the target is {1}", browserAddressUri.Scheme.ToLowerInvariant(), scriptUri.Scheme.ToLowerInvariant()));
                else
                {
                    var builder = new UriBuilder(scriptUri);
                    builder.Scheme = browserAddressUri.Scheme;
                    scriptUrl = builder.ToString();
                }
            }

            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(5);

            var scriptToExecute = InjectScriptJS.Replace("{{ScriptUri}}", scriptUrl);

            if (string.IsNullOrWhiteSpace(callbackScript))
                callbackScript = "";

            scriptToExecute = scriptToExecute.Replace("{{SuccessCallback}}", callbackScript);

            var injectResult = await browser.EvaluateScriptAsync(scriptToExecute, timeout);

            if (!injectResult.Success)
                throw new InvalidOperationException(injectResult.Message);

            //Wait until the browser finishes loading.
            await WaitUntilLoadedOrTimeoutAsync(browser, timeout);

            //If the continuation script is defined, Loop until continuation script evals to true or we timeout
            if (!string.IsNullOrWhiteSpace(continuationScript))
            {
                await WaitUntilEvalTrueOrTimeoutAsync(browser, continuationScript, timeout);
            }
        }

        /// <summary>
        /// Returns a value that indicates if the browser is currently at the specified address.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsCurrentAddress(this IWebBrowser browser, string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("address");

            if (string.IsNullOrWhiteSpace(browser.Address))
                return false;

            Uri targetUri = new Uri(address, UriKind.Absolute);
            Uri currentUri = new Uri(browser.Address, UriKind.Absolute);

            return targetUri == currentUri;
        }

        /// <summary>
        /// Loads the page at the specified address using the specified zoom level. If The browser is at the specfied address, the page is reloaded.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="address"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="ignoreCache"></param>
        /// <returns></returns>
        public static Task LoadPageAsync(this IWebBrowser browser, string address = "http://www.google.com", double zoomLevel = 1, bool ignoreCache = true)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(address);

            var tcs = new TaskCompletionSource<bool>();

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = (sender, args) =>
            {
                //Wait for while page to finish loading not just the first frame
                if (!args.IsLoading)
                {
                    browser.LoadingStateChanged -= handler;
                    tcs.TrySetResult(true);
                }
            };

            EventHandler<FrameLoadStartEventArgs> zoomLevelHandler = null;
            zoomLevelHandler = (sender, args) =>
            {
                var b = (ChromiumWebBrowser)sender;
                if (args.Frame.IsMain)
                {
                    b.SetZoomLevel(zoomLevel);
                    b.FrameLoadStart -= zoomLevelHandler;
                }
            };

            if (zoomLevel > 1)
                browser.FrameLoadStart += zoomLevelHandler;

            browser.LoadingStateChanged += handler;

            if (IsCurrentAddress(browser, address))
                browser.Reload(ignoreCache);
            else
                browser.Load(address);

            return tcs.Task;
        }

        /// <summary>
        /// Redraws the browser to match content.
        /// </summary>
        /// <param name="browser"></param>
        public static void Redraw(this IWebBrowser browser)
        {
            ////Gets a wrapper around the underlying CefBrowser instance
            var cefBrowser = browser.GetBrowser();
            
            //// Gets a wrapper around the CefBrowserHost instance
            var cefHost = cefBrowser.GetHost();

            ////You can call Invalidate to redraw/refresh the image
            cefHost.Invalidate(PaintElementType.View);
        }

        /// <summary>
        /// Blocks until the specified script evaluates to true or the specified maximum timeout duration is reached.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="script"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task WaitUntilEvalTrueOrTimeoutAsync(this IWebBrowser browser, string script, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentNullException("script");

            if (timeout.HasValue == false)
                timeout = TimeSpan.FromSeconds(5);

            var sw = Stopwatch.StartNew();
            JavascriptResponse continuationResult;
            do
            {
                continuationResult = await browser.EvaluateScriptAsync(script, timeout);

                if (sw.Elapsed >= timeout)
                    throw new TimeoutException(string.Format("The specified script did not evaluate to true in the specified duration. ({0})", timeout));

            } while (continuationResult.Success == false && (bool)continuationResult.Result == false);
        }

        /// <summary>
        /// Blocks until the browser reports that it is loaded or the specified maximum timeout duration is reached.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task WaitUntilLoadedOrTimeoutAsync(this IWebBrowser browser, TimeSpan? timeout = null)
        {
            //If the browser is not loading, short.
            if (browser.IsLoading == false)
                return;

            if (timeout == null)
                timeout = TimeSpan.FromSeconds(5);

            var tcs = new TaskCompletionSource<bool>();

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = (sender, args) =>
            {
                //Wait for while page to finish loading not just the first frame
                if (!args.IsLoading)
                {
                    browser.LoadingStateChanged -= handler;
                    tcs.TrySetResult(true);
                }
            };

            if (await Task.WhenAny(tcs.Task, Task.Delay(timeout.Value)) == tcs.Task)
            {
                await tcs.Task;
            }
            else
            {
                throw new TimeoutException("Timeed out while waiting for script to load.");
            }
        }

        #region pre-defined script injections

        /// <summary>
        /// Returns a value that indicates if jQuery is enabled on the current page.
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task<bool> IsJQueryEnabledAsync(this IWebBrowser browser)
        {
            var result = await browser.EvaluateScriptAsync("typeof jQuery;");

            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            return result.Result != null && (string)result.Result == "function";
        }

        /// <summary>
        /// Injects jQuery onto the current page and returns the version of jQuery injected.
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task<string> JQuerifyAsync(this IWebBrowser browser, TimeSpan? timeout = null)
        {
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(5);

            string jqUrl = "https://code.jquery.com/jquery-latest.min.js";

            await InjectScriptAsync(browser, jqUrl, callbackScript: "jQuery.noConflict();", continuationScript: "typeof jQuery !== 'undefined'", timeout: timeout, matchScheme: true);

            var jqueryVersionResult = await browser.EvaluateScriptAsync("jQuery.fn.jquery", timeout);

            if (!jqueryVersionResult.Success)
                throw new InvalidOperationException(jqueryVersionResult.Message);

            return jqueryVersionResult.Result as string;
        }

        /// <summary>
        /// Injects Uri.js into the current page.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task<bool> UriifyAsync(this IWebBrowser browser, TimeSpan? timeout = null)
        {
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(5);

            var result = await browser.EvaluateScriptAsync(@"
if (typeof URI !== 'undefined') {
    return true;
}
return false;", timeout);

            if (result.Success && (bool)result.Result)
                return true;

            await InjectScriptAsync(browser, "https://cdnjs.cloudflare.com/ajax/libs/URI.js/1.16.1/URI.min.js", timeout: timeout);

            return true;
        }

        /// <summary>
        /// Injects the SugarJS library into the current page.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task<bool> SugarifyAsync(this IWebBrowser browser, TimeSpan? timeout = null)
        {
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(5);

            //See if Sugar is already installed.
            var result = await browser.EvaluateScriptAsync(@"
if (typeof Date.create !== 'undefined') {
    return true;
}
return false;", timeout);

            if (result.Success && result.Result is bool && (bool)result.Result)
                return true;

            await InjectScriptAsync(browser, "https://cdnjs.cloudflare.com/ajax/libs/sugar/1.4.1/sugar.min.js", timeout: timeout);

            return true;
        }

        #endregion
    }
}