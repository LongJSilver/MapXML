using MapXML.Attributes;
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
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<OneNestedChild>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(3, handler.Results.Count);
            OneNestedChild svc = handler.GetResults<OneNestedChild>().First(n => n.Name.Equals("Parent"));
            Assert.IsNotNull(svc.Child);

            Assert.AreEqual(4, svc.Child.Integer);
            Assert.AreEqual("Child2", svc.Child.Name);

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<OneNestedChild>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        [TestMethod]
        public void NestedArray()
        {
            Stream s = GetTestXML("Nested");
            BaseTestHandler handler = new BaseTestHandler();
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

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<NestedChildrenArray>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        [TestMethod]
        public void SerializeNestedArray()
        {
            Stream s = GetTestXML("Nested");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<NestedChildrenArray>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            NestedChildrenArray svc = handler.GetResults<NestedChildrenArray>().First(n => n.Name.Equals("Parent"));
            var opt = XMLSerializer.OptionsBuilder().WithAdditionalRootNode("Tests").Build();
            XMLSerializer ser = new XMLSerializer(handler, opt);
            ser.AddItem("SimpleValue", svc);
            ser.Run();

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<NestedChildrenArray>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));

        }

        [TestMethod]
        public void NestedCollection()
        {
            Stream s = GetTestXML("Nested");
            BaseTestHandler handler = new BaseTestHandler();
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

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<NestedChildrenCollection>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        [TestMethod]
        public void NestedDictionary()
        {
            Stream s = GetTestXML("Nested");
            BaseTestHandler handler = new BaseTestHandler();
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

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<NestedChildrenDictionary>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }
        [TestMethod]
        public void SerializeNestedDictionary()
        {
            Stream s = GetTestXML("Nested");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<NestedChildrenDictionary>("SimpleValue");

            XMLDeserializer xdes = new XMLDeserializer(s, handler, null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            //*************//
            NestedChildrenDictionary svc = handler.GetResults<NestedChildrenDictionary>().First(n => n.Name.Equals("Parent"));

            var opt = XMLSerializer.OptionsBuilder().WithAdditionalRootNode("Tests").Build();
            XMLSerializer ser = new XMLSerializer(handler, opt);
            ser.AddItem("SimpleValue", svc);
            ser.Run();

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<NestedChildrenDictionary>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }
        private class OneNestedChild : IEquatable<OneNestedChild?>
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;

            [XMLChild("SimpleValue")]
            private OneNestedChild _child;
            public OneNestedChild Child => _child;

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as OneNestedChild);
            }

            public bool Equals(OneNestedChild? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       EqualityComparer<OneNestedChild>.Default.Equals(this._child, other._child);
            }
        }

        private class NestedChildrenArray : IEquatable<NestedChildrenArray?>
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;

            [XMLChild("SimpleValue")]
            public NestedChildrenArray[] Children;

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as NestedChildrenArray);
            }

            public bool Equals(NestedChildrenArray? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       BaseTestClass.Compare(this.Children, other.Children);
            }
        }

        private class NestedChildrenCollection : IEquatable<NestedChildrenCollection?>
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;

            [XMLChild("SimpleValue")]
            public ICollection<NestedChildrenCollection> Children;

            public NestedChildrenCollection()
            {
                Children = new Collection<NestedChildrenCollection>();
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as NestedChildrenCollection);
            }

            public bool Equals(NestedChildrenCollection? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       Compare(this.Children, other.Children);
            }

            public static bool operator ==(NestedChildrenCollection? left, NestedChildrenCollection? right)
            {
                return EqualityComparer<NestedChildrenCollection>.Default.Equals(left, right);
            }

            public static bool operator !=(NestedChildrenCollection? left, NestedChildrenCollection? right)
            {
                return !(left == right);
            }
        }

        private class NestedChildrenDictionary : IEquatable<NestedChildrenDictionary?>
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;

            [XMLMap("SimpleValue", XMLSourceType.Child, XMLMapAttribute.KeySourceTypes.NodeAttribute, "Number")]
            public IDictionary<double, NestedChildrenDictionary> Children;

            public NestedChildrenDictionary()
            {
                Children = new Dictionary<double, NestedChildrenDictionary>();
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as NestedChildrenDictionary);
            }

            public bool Equals(NestedChildrenDictionary? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       Compare(this.Children, other.Children);
            }
        }

    }

}
