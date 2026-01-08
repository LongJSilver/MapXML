using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass()]
    public class AggregateMultipleDefinitionsTest : BaseTestClass
    {
        [TestMethod]
        public void SimpleLookup()
        {
            Stream s = GetTestXML("AggregateMultipleDefinitions");
            TestBaseHandler handler = new TestBaseHandler();
            handler.Associate<Test_WithLookup>("Tests");
            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(true).Build();
            XMLDeserializer xdes = new XMLDeserializer(s, handler, opt);
            xdes.Run();
            //---------------------------//

            Test_WithLookup result = handler.GetResults<Test_WithLookup>().First();

            var Cls1 = result.Classes.First(c => c.Name.Equals("Cls1"));
            Assert.AreEqual(3, Cls1.Props.Count);
            Assert.IsNotNull(Cls1.Props.FirstOrDefault(p => p.ID == 2));
            Assert.IsNotNull(Cls1.Props.FirstOrDefault(p => p.ID == 3));
            Assert.IsNotNull(Cls1.Props.FirstOrDefault(p => p.ID == 4));

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<Test_WithLookup>(handler, opt));
        }

        private class TestBaseHandler : BaseTestHandler
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

        private class Test_WithLookup : IEquatable<Test_WithLookup>
        {
            [XMLChild("Prop")]
            private List<Prop> _props = new List<Prop>();

            [XMLChild("Cls", DeserializationPolicy.Create, CanSerialize = true, CanDeserialize = false)]
            private List<Cls> _cls = new List<Cls>();
            [XMLNonSerialized]
            public IEnumerable<Prop> Properties => _props;
            [XMLNonSerialized]
            public IEnumerable<Cls> Classes => _cls;
            [XMLChild("Cls", AggregateMultipleDefinitions = AggregationPolicy.AggregateChildren)]
            public void AddClass(Cls c)
            {
                if (!_cls.Contains(c))
                    _cls.Add(c);
            }

            [XMLFunction]
            public Cls? GetClassByName(string Name)
            {
                return _cls.FirstOrDefault(c => c.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
            }
            public override bool Equals(object? obj)
            {
                return obj is Test_WithLookup lookup && Equals(lookup);
            }

            public bool Equals(Test_WithLookup? other)
            {

                return
                    other != null &&
                BaseTestClass.Compare(this._cls, other._cls) &&
                BaseTestClass.Compare(this._props, other._props);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this._props, this._cls);
            }

            [XMLFunction]
            public Prop? GetPropByID([XMLParameter("ID")] int id)
            {
                return _props.FirstOrDefault(p => p.ID == id);
            }

            public static bool operator ==(Test_WithLookup? left, Test_WithLookup? right)
            {
                return EqualityComparer<Test_WithLookup>.Default.Equals(left, right);
            }

            public static bool operator !=(Test_WithLookup? left, Test_WithLookup? right)
            {
                return !(left == right);
            }
        }

        private class Cls : IEquatable<Cls>
        {
            public string Name;
            public string Desc;

            [XMLChild("Prop", DeserializationPolicy.Lookup)]
            public List<Prop> Props = new List<Prop>();

            public override bool Equals(object? obj)
            {
                return obj is Cls cls && Equals(cls);

            }

            public bool Equals(Cls? other)
            {
                return other != null && this.Name == other.Name &&
                         this.Desc == other.Desc &&
                        BaseTestClass.Compare(this.Props, other.Props);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.Name, this.Desc, this.Props);
            }

            public static bool operator ==(Cls? left, Cls? right)
            {
                return EqualityComparer<Cls>.Default.Equals(left, right);
            }

            public static bool operator !=(Cls? left, Cls? right)
            {
                return !(left == right);
            }
        }

        private class Prop : IEquatable<Prop>
        {
            public int ID;
            public string? Name;

            public override bool Equals(object? obj)
            {
                return obj is Prop prop && Equals(prop);
            }

            public bool Equals(Prop? other)
            {
                return other != null && this.ID == other.ID &&
                         this.Name == other.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.ID, this.Name);
            }

            public static bool operator ==(Prop? left, Prop? right)
            {
                return EqualityComparer<Prop>.Default.Equals(left, right);
            }

            public static bool operator !=(Prop? left, Prop? right)
            {
                return !(left == right);
            }
        }

    }

}
