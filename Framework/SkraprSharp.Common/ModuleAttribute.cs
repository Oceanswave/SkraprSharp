namespace SkraprSharp
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleAttribute : Attribute
    {
        public ModuleAttribute(string name)
        {
            Name = name;
        }
        public string Name
        {
            get;
            set;
        }
    }
}
