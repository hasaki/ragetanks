using System;

namespace StructureMap.Pipeline
{
    public class NulloObjectCache : IObjectCache
    {
        public object Locker { get { return new object(); } }

        public int Count { get { return 0; } }

        public bool Has(Type pluginType, Instance instance)
        {
            return false;
        }

        public void Eject(Type pluginType, Instance instance)
        {
        }

        public object Get(Type pluginType, Instance instance)
        {
            return null;
        }

        public void Set(Type pluginType, Instance instance, object value)
        {
            // no-op
        }

        public void DisposeAndClear()
        {
            // no-op
        }
    }
}