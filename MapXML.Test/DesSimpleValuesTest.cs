using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass]
    public class DesSimpleValuesTest : BaseTestClass
    {
        [TestMethod]
        public void FailOnMixedContent()
        {
            Stream s = GetTestXML("MixedContent");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<MixedContent>("MixedContent");
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null);

            SerializationException thrown = Assert.ThrowsException<SerializationException>(xdes.Run);
            // Assert that the inner exception is of the expected type
            Assert.IsNotNull(thrown.InnerException, "Inner exception is null.");
            Assert.IsInstanceOfType(thrown.InnerException, typeof(MixedContentException), "Inner exception is not of the expected type.");

        }
        internal class MixedContent
        {
            [XMLChild("f")]
            public List<F> Children { get; set; }
            [XMLTextContent()]
            public string Text;
            public MixedContent()
            {
                Children = new List<F>();
            }
        }

        internal class F
        {

        }


        [TestMethod]
        public void SimpleValues()
        {
            Stream s = GetTestXML("SimpleValues");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate("SimpleValue", typeof(SimpleValueClass), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(1, handler.ResultCount);
            SimpleValueClass svc = handler.GetResults<SimpleValueClass>()[0];
            Assert.AreEqual(1, svc.Integer);
            Assert.AreEqual("ASDF", svc.Name);
            Assert.AreEqual(12.0, svc.Float);
            Assert.AreEqual(13.0, svc.Double);
            Assert.AreEqual(DateTime.Parse("2023-10-05"), svc.Date);

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<SimpleValueClass>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        [TestMethod]
        public void SimpleValues_IgnoreNodes()
        {
            Stream s = GetTestXML("SimpleValues_UnhandledNodes");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate("SimpleValue", typeof(SimpleValueClass), DeserializationPolicy.Create);
            var options = XMLDeserializer.OptionsBuilder(XMLDeserializer.DefaultOptions_IgnoreRootNode)
                .AllowUnhandledNodes(true)
                .Build();
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, options);
            xdes.Run();
            Assert.AreEqual(1, handler.ResultCount);
            SimpleValueClass svc = handler.GetResults<SimpleValueClass>()[0];
            Assert.AreEqual(1, svc.Integer);
            Assert.AreEqual("ASDF", svc.Name);
            Assert.AreEqual(12.0, svc.Float);
            Assert.AreEqual(13.0, svc.Double);
            Assert.AreEqual(DateTime.Parse("2023-10-05"), svc.Date);
        }

        [TestMethod]
        public void SimpleValues_DoNOTIgnoreNodes()
        {
            Stream s = GetTestXML("SimpleValues_UnhandledNodes");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate("SimpleValue", typeof(SimpleValueClass), DeserializationPolicy.Create);
            var options = XMLDeserializer.OptionsBuilder(XMLDeserializer.DefaultOptions_IgnoreRootNode)
                .AllowUnhandledNodes(false)
                .Build();
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, options);

            SerializationException thrown = Assert.ThrowsException<UnhandledNodeException>(xdes.Run);
        }

        [TestMethod]
        public void SimplePropValues()
        {
            Stream s = GetTestXML("SimpleValues");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate("SimpleValue", typeof(SimpleValuePropsClass), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();
            Assert.AreEqual(1, handler.ResultCount);
            SimpleValuePropsClass svc = handler.GetResults<SimpleValuePropsClass>()[0];
            Assert.AreEqual(1, svc.Integer);
            Assert.AreEqual("ASDF", svc.Name);
            Assert.AreEqual(12.0, svc.Float);
            Assert.AreEqual(13.0, svc.Double);
            Assert.AreEqual(DateTime.Parse("2023-10-05"), svc.Date);

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<SimpleValuePropsClass>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        static DesSimpleValuesTest()
        {
        }
        private class SimpleValueClass : IEquatable<SimpleValueClass?>
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;

            [XMLTextContent]
            public DateTime Date;

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as SimpleValueClass);
            }

            public bool Equals(SimpleValueClass? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       this.Date == other.Date;
            }
        }
        private class SimpleValuePropsClass : IEquatable<SimpleValuePropsClass?>
        {
            [XMLAttribute("Number")]
            public int Integer { get; set; }
            [XMLAttribute("Decimal1")]
            public float Float { get; set; }
            [XMLAttribute("Decimal2")]
            public double Double { get; set; }
            [XMLAttribute("String")]
            public string Name { get; set; }

            [XMLTextContent]
            public DateTime Date { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as SimpleValuePropsClass);
            }

            public bool Equals(SimpleValuePropsClass? other)
            {
                return other is not null &&
                       this.Integer == other.Integer &&
                       this.Float == other.Float &&
                       this.Double == other.Double &&
                       this.Name == other.Name &&
                       this.Date == other.Date;
            }
        }

    }

}
