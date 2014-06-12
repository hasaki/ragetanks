using System;
using System.Reflection;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace StructureMap.Configuration
{
    public class GraphBuilder : IGraphBuilder
    {
        private readonly AssemblyScanner _assemblyScanner;
        private readonly PluginGraph _pluginGraph;
        private readonly Container _systemContainer;
        private readonly PluginGraph _systemGraph;


        public GraphBuilder(Registry[] registries)
            : this(registries, new PluginGraph())
        {
        }

        public GraphBuilder(Registry[] registries, PluginGraph pluginGraph)
        {
            _pluginGraph = pluginGraph;
            _assemblyScanner = new AssemblyScanner();
            _pluginGraph.AddScanner(_assemblyScanner);

            foreach (Registry registry in registries)
            {
                registry.ConfigurePluginGraph(_pluginGraph);
            }

            _systemGraph = new SystemRegistry().Build();
            _systemContainer = new Container(_systemGraph);
        }

        #region IGraphBuilder Members

        public void FinishFamilies()
        {
            _pluginGraph.Seal();
        }

        public PluginGraph PluginGraph { get { return _pluginGraph; } }

        public void AddAssembly(string assemblyName)
        {
            try
            {
                Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);
                _assemblyScanner.Assembly(assembly);
            }
            catch (Exception ex)
            {
                _pluginGraph.Log.RegisterError(101, ex, assemblyName);
            }
        }

        public void AddRegistry(string registryTypeName)
        {
            _pluginGraph.Log.Try(() =>
            {
                Type type = new TypePath(registryTypeName).FindType();
                var registry = (Registry) Activator.CreateInstance(type);
                registry.ConfigurePluginGraph(_pluginGraph);
            }).AndReportErrorAs(290, registryTypeName);
        }

        public IProfileBuilder GetProfileBuilder()
        {
            return new ProfileBuilder(_pluginGraph);
        }

        public void ConfigureFamily(TypePath pluginTypePath, Action<PluginFamily> action)
        {
            try
            {
                Type pluginType = pluginTypePath.FindType();
                PluginFamily family = _pluginGraph.FindFamily(pluginType);
                action(family);
            }
            catch (Exception ex)
            {
                _pluginGraph.Log.RegisterError(103, ex, pluginTypePath.AssemblyQualifiedName);
            }
        }


        public void WithSystemObject<T>(InstanceMemento memento, string context, Action<T> action)
        {
            try
            {
                var systemObject = (T) buildSystemObject(typeof (T), memento);
                action(systemObject);
            }
            catch (Exception ex)
            {
                _pluginGraph.Log.RegisterError(130, ex, context);
            }
        }

        public void WithType(TypePath path, string context, Action<Type> action)
        {
            _pluginGraph.Log.WithType(path, context, action);
        }

        #endregion

        private object buildSystemObject(Type type, InstanceMemento memento)
        {
            Instance instance = memento.ReadInstance(_systemGraph, type);
            return _systemContainer.GetInstance(type, instance);
        }
    }
}