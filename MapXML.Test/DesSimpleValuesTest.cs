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
            XMLDeserializer xdes = new XMLDeserializer(handler, s, owner: null);
            Assert.ThrowsException<XMLMixedContentException>(xdes.Run);

        }
        internal class MixedContent
        {
            [XmlChild("f")]
            public List<F> Children { get; set; }
            [XmlTextContent()]
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
            XMLDeserializer xdes = new XMLDeserializer(handler, s, owner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
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
            XMLDeserializer xdes = new XMLDeserializer(handler, s, owner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
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
            [XmlAttribute("Number")]
            public int Integer;
            [XmlAttribute("Decimal1")]
            public float Float;
            [XmlAttribute("Decimal2")]
            public double Double;
            [XmlAttribute("String")]
            public string Name;

            [XmlTextContent]
            public DateTime Date;
        }
        private class SimpleValuePropsClass
        {
            [XmlAttribute("Number")]
            public int Integer { get; set; }
            [XmlAttribute("Decimal1")]
            public float Float { get; set; }
            [XmlAttribute("Decimal2")]
            public double Double { get; set; }
            [XmlAttribute("String")]
            public string Name { get; set; }


            [XmlTextContent]
            public DateTime Date { get; set; }

        }

    }

}
