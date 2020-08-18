using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Statiq.Feeds.Syndication.Rdf
{
    public class RdfSequence
    {
        private readonly RdfFeed _target = null;

        public RdfSequence()
        {
            _target = null;
        }

        public RdfSequence(RdfFeed target)
        {
            _target = target;
        }

        [DefaultValue(null)]
        [XmlArray("Seq", Namespace=RdfFeedBase.NamespaceRdf)]
        public List<RdfResource> Items
        {
            get
            {
                if (_target is null
                    || _target.Items is null
                    || _target.Items.Count == 0)
                {
                    return null;
                }
                List<RdfResource> items = new List<RdfResource>(_target.Items.Count);
                foreach (RdfBase item in _target.Items)
                {
                    items.Add(new RdfResource(item));
                }
                return items;
            }

            set
            {
            }
        }

        [XmlIgnore]
        public bool ItemsSpecified
        {
            get
            {
                List<RdfResource> items = Items;
                return items?.Count > 0;
            }

            set
            {
            }
        }
    }
}