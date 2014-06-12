using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace StructureMap.Source
{
    public class XmlTemplater
    {
        private readonly string[] _substitutions;
        private readonly string _templateXml;

        public XmlTemplater(XmlNode templateNode)
        {
            _templateXml = templateNode.OuterXml;

            XmlAttribute substitutionAttribute = templateNode.Attributes[InstanceMemento.SUBSTITUTIONS_ATTRIBUTE];
            if (substitutionAttribute == null)
            {
                var collector = new Collector(templateNode);
                _substitutions = collector.FindSubstitutions();
            }
            else
            {
                _substitutions = substitutionAttribute.InnerText.Split(',');
            }
        }

        public string[] Substitutions { get { return _substitutions; } }

        public XmlNode SubstituteTemplates(XmlNode node, InstanceMemento memento)
        {
            var builder = new StringBuilder(_templateXml);
            foreach (string substitution in _substitutions)
            {
                string substitutionValue = memento.GetProperty(substitution);
                makeSubstitution(substitution, substitutionValue, builder);
            }

            var document = new XmlDocument();
            document.LoadXml(builder.ToString());

            return document.DocumentElement;
        }

        private void makeSubstitution(string substitution, string substitutionValue, StringBuilder builder)
        {
            string searchValue = string.Format("{{{0}}}", substitution);

            if (substitutionValue == string.Empty)
            {
                substitutionValue = InstanceMemento.EMPTY_STRING;
            }

            builder.Replace(searchValue, substitutionValue);
        }

        #region Nested type: Collector

        private class Collector
        {
            private readonly List<string> _substitutionList = new List<string>();
            private readonly XmlNode _templateNode;

            public Collector(XmlNode templateNode)
            {
                _templateNode = templateNode;
            }

            public string[] FindSubstitutions()
            {
                searchNode(_templateNode);

                return _substitutionList.ToArray();
            }

            private void searchNode(XmlNode node)
            {
                if (node == null)
                {
                    return;
                }

                foreach (XmlAttribute att in node.Attributes)
                {
                    examineAttributeValue(att.InnerText);
                }

                foreach (XmlNode childNode in node.ChildNodes)
                {
                    searchNode(childNode);
                }
            }

            private void examineAttributeValue(string attributeValue)
            {
                if (attributeValue.StartsWith("{") && attributeValue.EndsWith("}"))
                {
                    string substitution = attributeValue.Replace("{", "");
                    substitution = substitution.Replace("}", "");

                    addSubstitution(substitution);
                }
            }

            private void addSubstitution(string substitution)
            {
                if (!_substitutionList.Contains(substitution))
                {
                    _substitutionList.Add(substitution);
                }
            }
        }

        #endregion
    }
}