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

            exports.DefineMember("WebBrowser")
                .Assign(TypeProxy.GetConstructor(typeof(Library.WebBrowser)));

            return exports;
        }

        [ModuleInitialize]
        public static void Initialize()
        {
            var cef = new CefSettings();
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
