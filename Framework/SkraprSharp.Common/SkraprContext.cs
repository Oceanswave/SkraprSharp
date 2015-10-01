namespace SkraprSharp
{
    using NiL.JS.Core;
    using NiL.JS.Core.Functions;
    using Ninject;
    using Ninject.Extensions.Conventions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Represents top-level functions associated with a Skrapr.
    /// </summary>
    public sealed class SkraprContext
    {
        private List<IDisposable> m_ensureDisposedObjects = new List<IDisposable>();

        public IKernel Kernel
        {
            get;
            set;
        }

        public Context Initialize(string binDirectory = null, string modulesDirectoryName = "modules")
        {
            if (string.IsNullOrWhiteSpace(binDirectory))
                binDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            var modulesPath = Path.Combine(binDirectory, modulesDirectoryName);

            //TODO: Allow the Kernel to be injected.
            if (Kernel == null)
                Kernel = new StandardKernel();

            //Set the Kernel on the GlobalFunction object for use by globals.
            Library.GlobalFunctions.Kernel = Kernel;

            Kernel.Bind(x => x
                .FromAssembliesInPath(modulesPath)
                .SelectAllClasses()
                .InheritedFrom<IModule>()
                .BindAllInterfaces()
                .Configure((c, moduleType) =>
                {
                    c.WithMetadata("ModuleType", moduleType);
                    var moduleAttribute = moduleType.GetCustomAttributes(typeof(ModuleAttribute), false).FirstOrDefault() as ModuleAttribute;

                    if (moduleAttribute == null)
                        return;

                    c.WithMetadata("ModuleName", moduleAttribute.Name);
                })
                );

            //Execute ModuleInitialize methods on all modules.
            var bindings = Kernel.GetBindings(typeof(IModule));
            foreach(var binding in bindings)
            {
                var initializeMethods = binding.Metadata.Get<Type>("ModuleType").GetMethods(BindingFlags.Public | BindingFlags.Static).Where(mi => mi.CustomAttributes.Any(ca => ca.AttributeType == typeof(ModuleInitializeAttribute)));
                foreach(var initializeMethod in initializeMethods)
                {
                    initializeMethod.Invoke(null, BindingFlags.Default, null, null, null);
                }
            }

            var ctx = new Context();

            ctx.AttachModule(typeof(Library.Promise));
            ctx.DefineVariable("require").Assign(new ExternalFunction(Library.GlobalFunctions.require));

            return ctx;
        }

        public void EnsureDisposed(IDisposable obj)
        {
            if (m_ensureDisposedObjects.Contains(obj) == false)
                m_ensureDisposedObjects.Add(obj);
        }

        /// <summary>
        /// Disposes of any resources, executes module cleanup routines.
        /// </summary>
        public void Shutdown()
        {
            //For all objects registered with ensure disposed, dispose of them
            //TODO: Check for an 'IsDisposed' method and don't disposed if the value is true.
            foreach (var obj in m_ensureDisposedObjects) {
                if (obj != null)
                    obj.Dispose();
            }

            //Execute ModuleCleanup methods on all modules.
            var bindings = Kernel.GetBindings(typeof(IModule));
            foreach (var binding in bindings)
            {
                var cleanupMethods = binding.Metadata.Get<Type>("ModuleType").GetMethods(BindingFlags.Public | BindingFlags.Static).Where(mi => mi.CustomAttributes.Any(ca => ca.AttributeType == typeof(ModuleCleanupAttribute)));
                foreach (var cleanupMethod in cleanupMethods)
                {
                    cleanupMethod.Invoke(null, BindingFlags.Default, null, null, null);
                }
            }
        }
    }
}
