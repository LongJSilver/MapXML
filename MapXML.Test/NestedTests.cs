using MapXML.Attributes;
using MapXML.Utils;
using System.Collections.ObjectModel;

namespace MapXML.Tests
{
    [TestClass()]
    public class NestedTest : BaseTestClass
    {
        [TestMethod]
        public void OneNestedNode()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<OneNestedChild>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner:null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(3, handler.Results.Count);
            OneNestedChild svc = handler.GetResults<OneNestedChild>().First(n => n.Name.Equals("Parent"));
            Assert.IsNotNull(svc.Child);

            Assert.AreEqual(4, svc.Child.Integer);
            Assert.AreEqual("Child2", svc.Child.Name);
        }

        [TestMethod]
        public void NestedArray()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<NestedChildrenArray>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, null, XMLDeserializer.DefaultOptions_IgnoreRootNode);

            xdes.Run();
            Assert.AreEqual(3, handler.Results.Count);
            NestedChildrenArray svc = handler.GetResults<NestedChildrenArray>().First(n => n.Name.Equals("Parent"));
            Assert.IsNotNull(svc.Children);

            Assert.AreEqual(2, svc.Children.Length);

            Assert.AreEqual(3, svc.Children[0].Integer);
            Assert.AreEqual("Child1", svc.Children[0].Name);

            Assert.AreEqual(4, svc.Children[1].Integer);
            Assert.AreEqual("Child2", svc.Children[1].Name);
        }

        [TestMethod]
        public void SerializeNestedArray()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<NestedChildrenArray>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            NestedChildrenArray svc = handler.GetResults<NestedChildrenArray>().First(n => n.Name.Equals("Parent"));
            var opt = XMLSerializer.OptionsBuilder().WithAdditionalRootNode("Tests").Build();
            XMLSerializer ser = new XMLSerializer(handler, opt);
            ser.AddItem("SimpleValue", svc);
            ser.Run();

        }

        [TestMethod]
        public void NestedCollection()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<NestedChildrenCollection>("SimpleValue");

      
            XMLDeserializer xdes = new XMLDeserializer(s, handler, null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(3, handler.Results.Count);
            NestedChildrenCollection svc = handler.GetResults<NestedChildrenCollection>().First(n => n.Name.Equals("Parent"));
            Assert.IsNotNull(svc.Children);

            Assert.AreEqual(2, svc.Children.Count);

            Assert.AreEqual(3, svc.Children.ElementAt(0).Integer);
            Assert.AreEqual("Child1", svc.Children.ElementAt(0).Name);

            Assert.AreEqual(4, svc.Children.ElementAt(1).Integer);
            Assert.AreEqual("Child2", svc.Children.ElementAt(1).Name);
        }

        [TestMethod]
        public void NestedDictionary()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<NestedChildrenDictionary>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(3, handler.Results.Count);
            NestedChildrenDictionary svc = handler.GetResults<NestedChildrenDictionary>().First(n => n.Name.Equals("Parent"));
            Assert.IsNotNull(svc.Children);

            Assert.AreEqual(2, svc.Children.Count);

            Assert.AreEqual(3, svc.Children[3.0].Integer);
            Assert.AreEqual("Child1", svc.Children[3.0].Name);

            Assert.AreEqual(4, svc.Children[4.0].Integer);
            Assert.AreEqual("Child2", svc.Children[4.0].Name);
        }
        [TestMethod]
        public void SerializeNestedDictionary()
        {
            Stream s = GetTestXML("Nested");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<NestedChildrenDictionary>("SimpleValue");
         
            XMLDeserializer xdes = new XMLDeserializer(s, handler, null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            //*************//
            NestedChildrenDictionary svc = handler.GetResults<NestedChildrenDictionary>().First(n => n.Name.Equals("Parent"));

            var opt = XMLSerializer.OptionsBuilder().WithAdditionalRootNode("Tests").Build();
            XMLSerializer ser = new XMLSerializer(handler, opt);
            ser.AddItem("SimpleValue", svc);
            ser.Run();
        }
        private class OneNestedChild
        {
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;

            [XmlChild("SimpleValue")]
            private OneNestedChild _child;
            public OneNestedChild Child => _child;
        }
        private class NestedChildrenArray
        {
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;

            [XmlChild("SimpleValue")]
            public NestedChildrenArray[] Children;
        }
        private class NestedChildrenCollection
        {
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;

            [XmlChild("SimpleValue")]
            public ICollection<NestedChildrenCollection> Children;

            public NestedChildrenCollection()
            {
                Children = new Collection<NestedChildrenCollection>();
            }
        }
        private class NestedChildrenDictionary
        {
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;

            [XmlMap("SimpleValue", XmlSourceType.Child, XmlMapAttribute.KeySourceTypes.NodeAttribute, "Number")]
            public IDictionary<double, NestedChildrenDictionary> Children;

            public NestedChildrenDictionary()
            {
                Children = new Dictionary<double, NestedChildrenDictionary>();
            }
        }

    }

}
