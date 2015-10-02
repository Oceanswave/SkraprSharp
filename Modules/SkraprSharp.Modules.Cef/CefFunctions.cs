namespace SkraprSharp.Modules.Cef
{
    using NiL.JS.Core;
    using NiL.JS.Core.TypeProxing;

    public class CefFunctions : CustomType
    {
        public string chromiumVersion
        {
            get
            {
                return CefSharp.Cef.ChromiumVersion;
            }
        }

        public JSObject addCrossOriginWhitelistEntry(string sourceOrigin, string targetProtocol, string targetDomain, bool allowTargetSubdomains)
        {
            return CefSharp.Cef.AddCrossOriginWhitelistEntry(sourceOrigin, targetProtocol, targetDomain, allowTargetSubdomains);
        }
    }
}
