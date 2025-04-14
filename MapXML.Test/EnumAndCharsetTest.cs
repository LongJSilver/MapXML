using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass()]
    public class EnumAndCharsetTest : BaseTestClass
    {
        [TestMethod]
        public void EnumConversion()
        {
            Stream s = GetTestXML("EnumConversion");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<TestClass>("Enum");
            XMLDeserializer xdes = new XMLDeserializer(handler, s, null)
            {
                IgnoreRootNode = true
            };
            xdes.Run();
            Assert.AreEqual(1, handler.ResultCount);
            TestClass svc = handler.GetResults<TestClass>()[0];
            Assert.AreEqual(1, svc.Integer);
            Assert.AreEqual(TestClass.Values.Two, svc.EnumVariable);
        }

        [TestMethod]
        public void Charsets()
        {
            Stream s = GetTestXML("Charsets");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<TestClass>("Item");
            XMLDeserializer xdes = new XMLDeserializer(handler, s, null)
            {
                IgnoreRootNode = true
            };
            xdes.Run();
            var Results = handler.GetResults<TestClass>();
            Assert.AreNotEqual(12.0, Results.First(t => t.Name.Equals("Item11")).Number);
            Assert.AreEqual(12.0, Results.First(t => t.Name.Equals("Item12")).Number);

            Assert.AreEqual(12.0, Results.First(t => t.Name.Equals("Item21")).Number);
            Assert.AreNotEqual(12.0, Results.First(t => t.Name.Equals("Item22")).Number);
        }

        private class TestClass
        {
            internal enum Values
            {
                One = 0,
                Two
            }

            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("String")]
            public string Name;
            [XmlAttribute("Double")]
            public double Number;

            [XmlAttribute("EnumValue")]
            internal Values EnumVariable;
        }
        private class EncoderClass
        {
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;
        }

    }

}
