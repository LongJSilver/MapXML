using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapXML;
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
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<MixedContent>("MixedContent");
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: null);
            Assert.ThrowsException<XMLMixedContentException>(xdes.Run);

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
            DefaultHandler handler = new DefaultHandler();
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

        }
        [TestMethod]
        public void SimplePropValues()
        {
            Stream s = GetTestXML("SimpleValues");
            DefaultHandler handler = new DefaultHandler();
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
        }

        static DesSimpleValuesTest()
        {
        }
        private class SimpleValueClass
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
        }
        private class SimpleValuePropsClass
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

        }

    }

}
