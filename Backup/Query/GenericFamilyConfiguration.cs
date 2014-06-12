using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace StructureMap.Query
{
    public class GenericFamilyConfiguration : IPluginTypeConfiguration, IFamily
    {
        private readonly PluginFamily _family;

        public GenericFamilyConfiguration(PluginFamily family)
        {
            _family = family;
        }

        void IFamily.Eject(Instance instance)
        {
        }

        object IFamily.Build(Instance instance)
        {
            return null;
        }

        bool IFamily.HasBeenCreated(Instance instance)
        {
            return false;
        }

        public Type PluginType { get { return _family.PluginType; } }

        /// <summary>
        /// The "instance" that will be used when Container.GetInstance(PluginType) is called.
        /// See <see cref="InstanceRef">InstanceRef</see> for more information
        /// </summary>
        public InstanceRef Default
        {
            get
            {
                Instance defaultInstance = _family.GetDefaultInstance();
                return defaultInstance == null ? null : new InstanceRef(defaultInstance, this);
            }
        }

        /// <summary>
        /// The build "policy" for this PluginType.  Used by the WhatDoIHave() diagnostics methods
        /// </summary>
        public string Lifecycle { get { return _family.Lifecycle.ToName(); } }

        /// <summary>
        /// All of the <see cref="InstanceRef">InstanceRef</see>'s registered
        /// for this PluginType
        /// </summary>
        public IEnumerable<InstanceRef> Instances { get { return _family.Instances.Select(x => new InstanceRef(x, this)).ToArray(); } }

        /// <summary>
        /// Simply query to see if there are any implementations registered
        /// </summary>
        /// <returns></returns>
        public bool HasImplementations()
        {
            return _family.InstanceCount > 0;
        }

        public void EjectAndRemove(InstanceRef instance)
        {
            if (instance == null) return;
            _family.RemoveInstance(instance.Instance);
        }

        public void EjectAndRemoveAll()
        {
            _family.RemoveAll();
        }
    }
}