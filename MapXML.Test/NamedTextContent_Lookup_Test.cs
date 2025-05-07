using MapXML.Attributes;
using MapXML.Utils;
namespace MapXML.Tests
{
    [TestClass()]
    public class NamedTextContent_Lookup_Test : BaseTestClass
    {
        [TestMethod]
        public void LookupValues()
        {
            BaseTestHandler handl = new BaseTestHandler();
            handl.RegisterTypeConverter(typeof(Guid),
                (object guid, IFormatProvider ifp) =>
            {
                return ((Guid)guid).ToString("B", ifp).ToUpper();
            });

            handl.Associate("AnimalClasses", typeof(AnimalClasses), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(GetTestXML("NamedTextContent_Lookup"), handl, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            //*********************//
            var results = handl.GetResults<AnimalClasses>().FirstOrDefault();
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Habitats.Count);
            Assert.AreEqual(2, results.Animals.Count);

            var forestHabitat = results.Habitats[new Guid("B81A9EE7-6F1B-4348-BF43-D309EB3CB87E")];
            Assert.AreEqual("Forest", forestHabitat.Name);

            var swampHabitat = results.Habitats[new Guid("01EC8206-4DB3-4717-8E6A-7060BFA04F07")];
            Assert.AreEqual("Swamp", swampHabitat.Name);

            var lion = results.Animals.FirstOrDefault(a => a.Name == "Lion");
            Assert.IsNotNull(lion);
            Assert.AreEqual(forestHabitat, lion.Habitat);

            var alligator = results.Animals.FirstOrDefault(a => a.Name == "Alligator");
            Assert.IsNotNull(alligator);
            Assert.AreEqual(swampHabitat, alligator.Habitat);
            //*********************//


            var opt = XMLSerializer.OptionsBuilder().WithAdditionalRootNode("xml").Build();
            XMLSerializer ser = new XMLSerializer(handl, opt);
            handl.GetResults<object>().ForEach(o => ser.AddItem("AnimalClasses", o));
            ser.Run();

        }

        private class AnimalClasses
        {
            [XMLChild("AnimalInfo", SerializationOrder = 2)]
            public List<AnimalInfo> Animals { get; set; }

            [XMLMap("Habitat", XMLSourceType.Child, XMLMapAttribute.KeySourceTypes.ObjectMember, nameof(Habitat.ID), SerializationOrder = 1)]
            public Dictionary<Guid, Habitat> Habitats { get; set; }

            [XMLFunction()]
            internal Habitat FindHabitat(Guid id)
            => Habitats[id];

            public AnimalClasses()
            {
                Animals = new List<AnimalInfo>();
                Habitats = new Dictionary<Guid, Habitat>();
            }
        }

        public class AnimalInfo
        {
            [XMLChild("Habitat", DeserializationPolicy.Lookup)]
            public Habitat Habitat { get; set; }

            [XMLAttribute]
            public string Name { get; set; }
        }

        public class Habitat
        {
            [XMLAttribute]
            public string Name { get; set; }
            [XMLAttribute]
            public Guid ID { get; set; }
        }
    }
}
