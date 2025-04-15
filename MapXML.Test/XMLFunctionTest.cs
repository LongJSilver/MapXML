using MapXML.Attributes;
using MapXML.Utils;

namespace MapXML.Tests
{
    [TestClass()]
    public class XMLFunctionTests : BaseTestClass
    {
        [TestMethod]
        public void SimpleLookup()
        {
            Stream s = GetTestXML("XMLFunctions");
            TestBaseHandler handler = new TestBaseHandler();
            handler.Associate<Test_WithLookup>("Tests");
            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(true).Build();
            XMLDeserializer xdes = new XMLDeserializer(handler, s, opt);
            xdes.Run();
            //---------------------------//

            Test_WithLookup result = handler.GetResults<Test_WithLookup>().First();

            var Cls1 = result.Classes.First(c => c.Name.Equals("Cls1"));
            Assert.AreEqual(2, Cls1.Props.Count);
            Assert.IsNotNull(Cls1.Props.FirstOrDefault(p => p.ID == 2));
            Assert.IsNotNull(Cls1.Props.FirstOrDefault(p => p.ID == 3));
        }
        [TestMethod]
        public void SerializeSimpleLookup()
        {
            Stream s = GetTestXML("XMLFunctions");
            TestBaseHandler cont = new TestBaseHandler();
            cont.Associate<Test_WithLookup>("Tests");


            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(true).Build();
            XMLDeserializer xdes = new XMLDeserializer(cont, s, opt);

            xdes.Run();
            //---------------------------//


            XMLSerializer ser = new XMLSerializer(cont);
            cont.GetResults<Test_WithLookup>().ForEach(c => ser.AddItem("Tests", c));
            ser.Run();
        }

        [TestMethod]
        public void LookupFunction()
        {
            Stream s = GetTestXML("XMLFunctions");
            TestBaseHandler handler = new TestBaseHandler();
            handler.Associate<Test_WithLookup>("Tests");

            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(true).Build();
            XMLDeserializer xdes = new XMLDeserializer(handler, s, opt);
            xdes.Run();
            //---------------------------//
            Test_WithLookup result = handler.GetResults<Test_WithLookup>().First();
            var cls1 = result.Classes.First(c => c.Name.Equals("Cls1"));
            Assert.AreEqual(2, cls1.Props.Count);
            Assert.IsNotNull(cls1.Props.FirstOrDefault(p => p.ID == 2));
            Assert.IsNotNull(cls1.Props.FirstOrDefault(p => p.ID == 3));
        }

        [TestMethod]
        public void SerializeLookupFunction()
        {
            Stream s = GetTestXML("XMLFunctions");
            TestBaseHandler handler = new TestBaseHandler();
            handler.Associate<Test_WithLookup>("Tests");

            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(true).Build();
            XMLDeserializer xdes = new XMLDeserializer(handler, s, opt);
            xdes.Run();
            //---------------------------//
            //---------------------------// 
            XMLSerializer ser = new XMLSerializer(handler);
            ser.AddItem("Tests", handler.GetResults<Test_WithLookup>().First());
            ser.Run();
        }

        private class TestBaseHandler : DefaultHandler
        {
            public override bool Lookup_FromAttributes(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, Type targetClass,
                out object? result)
            {
                if (nodeName.Equals("Prop"))
                {
                    result = GetResults<Prop>().FirstOrDefault(p => p.ID == int.Parse(attributes["ID"]));
                    return result != null;
                }
                result = default;
                return false;
            }

            public override bool GetLookupAttributes(IXMLState state, string parentNode, string targetNode, object item, out IReadOnlyDictionary<string, string> result)
            {
                if (targetNode.Equals("Prop"))
                {
                    Prop p = item as Prop;
                    result = new Dictionary<string, string>
                    {
                        ["ID"] = p.ID.ToString()
                    };
                    return result != null;
                }
                else
                {

                    result = default;
                    return false;
                }
            }

        }

        private class Test_WithLookup
        {
            [XmlChild("Prop")]
            private List<Prop> _props = new List<Prop>();

            [XmlChild("Cls", DeserializationPolicy.Create, CanSerialize = true, CanDeserialize = false)]
            private List<Cls> _cls = new List<Cls>();
            public IEnumerable<Prop> Properties => _props;
            public IEnumerable<Cls> Classes => _cls;
            [XmlChild("Cls")]
            public void AddClass(Cls c)
            {
                _cls.Add(c);
            }

            [XMLFunction]
            public Prop? GetPropByID([XMLParameter("ID")] int id)
            {
                return _props.FirstOrDefault(p => p.ID == id);
            }
        }

        private class Cls
        {
            public string Name;
            public string Desc;

            [XmlChild("Prop", DeserializationPolicy.Lookup)]
            public List<Prop> Props = new List<Prop>();
        }

        private class Prop
        {
            public int ID;
            public string Name;
        }

    }

}
