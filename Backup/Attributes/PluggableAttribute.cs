using System;

namespace StructureMap
{
    /// <summary>
    /// Used to implicitly mark a class as a Plugin candidate for StructureMap
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluggableAttribute : Attribute
    {
        public PluggableAttribute(string concreteKey)
        {
            ConcreteKey = concreteKey;
        }


        /// <summary>
        /// The ConcreteKey alias of the Type
        /// </summary>
        public string ConcreteKey { get; set; }

        /// <summary>
        /// Determines whether a Type object is marked as Pluggable
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static bool MarkedAsPluggable(Type objectType)
        {
            var att = GetCustomAttribute(objectType, typeof (PluggableAttribute), false) as PluggableAttribute;
            return (att != null);
        }
    }
}