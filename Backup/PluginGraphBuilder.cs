using System;
using System.Xml;
using StructureMap.Configuration;
using StructureMap.Configuration.DSL;
using StructureMap.Diagnostics;
using StructureMap.Graph;

namespace StructureMap
{
    /// <summary>
    /// Reads configuration XML documents and builds the structures necessary to initialize
    /// the Container/IInstanceFactory/InstanceBuilder/ObjectInstanceActivator objects
    /// </summary>
    public class PluginGraphBuilder
    {
        #region statics

        // Only used in testing
        public static PluginGraph BuildFromXml(XmlDocument document)
        {
            var log = new GraphLog();

            ConfigurationParser[] parsers = ConfigurationParserBuilder.GetParsers(document.DocumentElement, log);
            var builder = new PluginGraphBuilder(parsers, new Registry[0], log);

            return builder.Build();
        }

        #endregion

        private readonly PluginGraph _graph;
        private readonly ConfigurationParser[] _parsers;
        private readonly Registry[] _registries = new Registry[0];

        #region constructors

        public PluginGraphBuilder(ConfigurationParser parser)
            : this(new[] {parser}, new Registry[0], new GraphLog())
        {
        }

        public PluginGraphBuilder(ConfigurationParser[] parsers, Registry[] registries, GraphLog log)
        {
            _parsers = parsers;
            _registries = registries;
            _graph = new PluginGraph();
            _graph.Log = log;
        }

        #endregion

        /// <summary>
        /// Reads the configuration information and returns the PluginGraph definition of
        /// plugin families and plugin's
        /// </summary>
        /// <returns></returns>
        public PluginGraph Build()
        {
            var graphBuilder = new GraphBuilder(_registries, _graph);

            forAllParsers(p =>
            {
                _graph.Log.StartSource(p.Description);
                p.ParseAssemblies(graphBuilder);
                p.ParseRegistries(graphBuilder);
            });

            forAllParsers(p => p.Parse(graphBuilder));

            _graph.Seal();

            return _graph;
        }

        private void forAllParsers(Action<ConfigurationParser> action)
        {
            foreach (ConfigurationParser parser in _parsers)
            {
                action(parser);
            }
        }
    }
}