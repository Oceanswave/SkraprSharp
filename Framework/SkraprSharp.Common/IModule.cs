namespace SkraprSharp
{
    using NiL.JS.Core;

    /// <summary>
    /// Represents a module.
    /// </summary>
    public interface IModule
    {
        JSObject GetExports(JSObject thisBind);
    }
}
