﻿using MapXML.Attributes;
namespace MapXML.Tests
{
    [TestClass()]
    public class NamedTextContent_Lookup_Test : BaseTestClass
    {
        [TestMethod]
        public void LookupValues()
        {
            BaseTestHandler handler = new BaseTestHandler();
            handler.RegisterTypeConverter(typeof(Guid),
                (object guid, IFormatProvider ifp) =>
            {
                return ((Guid)guid).ToString("B", ifp).ToUpper();
            });

            handler.Associate("AnimalClasses", typeof(AnimalClasses), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(GetTestXML("NamedTextContent_Lookup"), handler, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            //*********************//
            var results = handler.GetResults<AnimalClasses>().FirstOrDefault();
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

            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<AnimalClasses>(handler, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        private class AnimalClasses : IEquatable<AnimalClasses?>
        {
            [XMLChild("AnimalInfo", SerializationOrder = 2)]
            public List<AnimalInfo> Animals { get; set; }

            [XMLMap("Habitat", XMLSourceType.Child, XMLMapAttribute.KeySourceTypes.ObjectMember, nameof(Habitat.ID), SerializationOrder = 1)]
            public Dictionary<Guid, Habitat> Habitats { get; set; }

            [XMLFunction()]
            internal Habitat FindHabitat([XMLParameter(nameof(Habitat.ID))] Guid id)
            => Habitats[id];

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalClasses);
            }

            public bool Equals(AnimalClasses? other)
            {
                return other is not null &&
                       BaseTestClass.Compare(this.Animals, other.Animals) &&
                       BaseTestClass.Compare(this.Habitats, other.Habitats);
            }

            public AnimalClasses()
            {
                Animals = new List<AnimalInfo>();
                Habitats = new Dictionary<Guid, Habitat>();
            }
        }

        public class AnimalInfo : IEquatable<AnimalInfo?>
        {
            [XMLChild("Habitat", DeserializationPolicy.Lookup)]
            public Habitat Habitat { get; set; }

            [XMLAttribute]
            public string Name { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalInfo);
            }

            public bool Equals(AnimalInfo? other)
            {
                return other is not null &&
                       EqualityComparer<Habitat>.Default.Equals(this.Habitat, other.Habitat) &&
                       this.Name == other.Name;
            }
        }

        public class Habitat : IEquatable<Habitat?>
        {

            [XMLAttribute]
            public string Name { get; set; }
            [XMLAttribute]
            public Guid ID { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as Habitat);
            }

            public bool Equals(Habitat? other)
            {
                return other is not null &&
                       this.Name == other.Name &&
                       this.ID.Equals(other.ID);
            }
        }
    }
}
