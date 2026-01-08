using Fliport.Data;
using MapXML.Attributes;
using MapXML.Utils;
using System.Xml;
using System.Xml.XPath;
namespace MapXML.Tests
{
    [TestClass()]
    public class CosmeticOptionsTest : BaseTestClass
    {
        [TestMethod]
        public void PreferTextNodesForLookup()
        {
            BaseTestHandler handler = new BaseTestHandler();
            handler.RegisterTypeConverter(typeof(Guid),
                (object guid, IFormatProvider ifp) =>
            {
                return ((Guid)guid).ToString("B", ifp).ToUpper();
            });

            handler.Associate("AnimalClasses", typeof(AnimalClasses), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(GetTestXML("NamedTextContent_Lookup"), handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            var results = handler.GetResults<AnimalClasses>().FirstOrDefault();

            //*********************//
            // TEST 1: Serialize with Text Node preference
            ISerializationOptions opt = XMLSerializer.OptionsBuilder()
                .PreferTextNodesForLookups(true).Build();

            XMLSerializer ser = new XMLSerializer(handler, opt);
            handler.GetResults<object>().ForEach(o => ser.AddItem("AnimalClasses", o));
            ser.Run();

            string XML_PrefersTextNodes = ser.Result;

            // Use XPath to check that the lookup values are serialized as text nodes
            var doc1 = new XmlDocument();
            doc1.LoadXml(XML_PrefersTextNodes);
            XPathNavigator? nav1 = doc1.CreateNavigator();
            // Select all AnimalInfo/Habitat nodes and check they have text content, not attribute
            XPathNodeIterator habitatNodes1 = nav1!.Select("//AnimalInfo/Habitat");
            while (habitatNodes1.MoveNext())
            {
                XPathNavigator? node = habitatNodes1.Current;
                Assert.IsNotNull(node);
                // Should have text content
                string? text = node!.Value?.Trim();
                Assert.IsFalse(string.IsNullOrEmpty(text), "Habitat node should have text content.");
                // Should not have any attributes
                Assert.AreEqual(0, node.Select("@*").Count, "Habitat node should not have attributes.");
            }

            //*********************//
            // TEST 2: Serialize with Attribute preference
            opt = XMLSerializer.OptionsBuilder()
               .PreferTextNodesForLookups(false).Build();

            ser = new XMLSerializer(handler, opt);
            handler.GetResults<object>().ForEach(o => ser.AddItem("AnimalClasses", o));
            ser.Run();

            string XML_PrefersAttributes = ser.Result;

            // Use XPath to check that the lookup values are serialized as attributes
            XmlDocument doc2 = new XmlDocument();
            doc2.LoadXml(XML_PrefersAttributes);
            XPathNavigator? nav2 = doc2.CreateNavigator();
            // Select all AnimalInfo/Habitat nodes and check they have an attribute (e.g. "ID" or similar)
            XPathNodeIterator habitatNodes2 = nav2!.Select("//AnimalInfo/Habitat");
            while (habitatNodes2.MoveNext())
            {
                XPathNavigator? node = habitatNodes2.Current;
                Assert.IsNotNull(node);
                // Check for "ID" attribute
                var idAttribute = node!.GetAttribute("ID", "");
                Assert.IsTrue(!string.IsNullOrEmpty(idAttribute));

                string? text = node!.Value?.Trim();
                Assert.IsTrue(string.IsNullOrEmpty(text), "Habitat node should not have text content when using attributes.");
            }
        }

        private class AnimalClasses : IEquatable<AnimalClasses?>
        {
            [XMLChild("AnimalInfo", SerializationOrder = 2)]
            public List<AnimalInfo> Animals { get; set; }

            [XMLMap("Habitat", XMLSourceType.Child, XMLMapAttribute.KeySourceTypes.ObjectMember, nameof(Habitat.ID), SerializationOrder = 1)]
            public Dictionary<Guid, Habitat> Habitats { get; set; }

            [XMLFunction()]
            internal Habitat FindHabitat([XMLParameter(nameof(Habitat.ID))] Guid id)
            => Habitats[id];

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalClasses);
            }

            public bool Equals(AnimalClasses? other)
            {
                return other is not null &&
                       BaseTestClass.Compare(this.Animals, other.Animals) &&
                       BaseTestClass.Compare(this.Habitats, other.Habitats);
            }

            public AnimalClasses()
            {
                Animals = new List<AnimalInfo>();
                Habitats = new Dictionary<Guid, Habitat>();
            }
        }

        public class AnimalInfo : IEquatable<AnimalInfo?>
        {
            [XMLChild("Habitat", DeserializationPolicy.Lookup)]
            public Habitat Habitat { get; set; }

            [XMLAttribute]
            public string Name { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalInfo);
            }

            public bool Equals(AnimalInfo? other)
            {
                return other is not null &&
                       EqualityComparer<Habitat>.Default.Equals(this.Habitat, other.Habitat) &&
                       this.Name == other.Name;
            }
        }

        public class Habitat : IEquatable<Habitat?>
        {

            [XMLAttribute]
            public string Name { get; set; }
            [XMLAttribute]
            public Guid ID { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as Habitat);
            }

            public bool Equals(Habitat? other)
            {
                return other is not null &&
                       this.Name == other.Name &&
                       this.ID.Equals(other.ID);
            }
        }
    }
}
