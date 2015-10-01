namespace SkraprSharp.Library
{
    using NiL.JS.BaseLibrary;
    using NiL.JS.Core;
    using Ninject;
    using System;

    public static class GlobalFunctions
    {
        /// <summary>
        /// Gets or sets the IKernel associated with GlobalFunctions
        /// </summary>
        internal static IKernel Kernel
        {
            get;
            set;
        }

        /// <summary>
        /// Represents a simple require implementation that returns strongly-typed modules defined via DI.
        /// </summary>
        /// <param name="thisBind"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JSObject require(JSObject thisBind, Arguments args)
        {
            var moduleName = args[0];
            string strModuleName = moduleName.IsExist ? moduleName.ToString() : "";

            if (string.IsNullOrWhiteSpace(strModuleName))
                throw new JSException(new Error("A module name must be specified as the first argument."));

            if (Kernel == null)
                throw new InvalidOperationException("Internal Error: Kernel must be set when defining the context.");

            var module = Kernel.TryGet<IModule>((m) => m.Get<string>("ModuleName") == strModuleName);
            if (module != null)
                return module.GetExports(thisBind);

            //TODO: Do we have to ctx.AttachModule(typeof(Library.WebBrowser));??

            return JSObject.Undefined;
        }
    }
}
