namespace SkraprSharp.Modules.Cef
{
    using CefSharp;
    using NiL.JS.Core;
    using NiL.JS.Core.TypeProxing;

    [Module("Cef")]
    public class CefModule : IModule
    {
        public JSObject GetExports(JSObject thisBind)
        {
            var exports = JSObject.CreateObject();

            exports.DefineMember("settings")
                .Assign(new CefFunctions().__proto__ = TypeProxy.GetPrototype(typeof(CefFunctions)));

            exports.DefineMember("WebBrowser");
            exports["WebBrowser"] = TypeProxy.GetConstructor(typeof(WebBrowser));

            return exports;
        }

        [ModuleInitialize]
        public static void Initialize()
        {
            var cef = new CefSettings
            {
                LogSeverity = LogSeverity.Verbose
            };

            Cef.Initialize(cef, true, true);
        }

        [ModuleCleanup]
        public static void Cleanup()
        {
            // Clean up Chromium objects.  You need to call this in your application otherwise
            // you will get a crash when closing.
            Cef.Shutdown();
        }

    }
}
