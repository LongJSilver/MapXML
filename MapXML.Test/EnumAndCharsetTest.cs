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
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<TestClass>("Enum");
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
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
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<TestClass>("Item");
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
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

            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("String")]
            public string Name;
            [XMLAttribute("Double")]
            public double Number;

            [XMLAttribute("EnumValue")]
            internal Values EnumVariable;
        }
        private class EncoderClass
        {
            [XMLAttribute("Number")]
            public int Integer;
            [XMLAttribute("Decimal1")]
            public float Float;
            [XMLAttribute("Decimal2")]
            public double Double;
            [XMLAttribute("String")]
            public string Name;
        }

    }

}
