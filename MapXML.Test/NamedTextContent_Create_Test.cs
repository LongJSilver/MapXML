using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass()]
    public class NamedTextContent_Create_Test : BaseTestClass
    {
        [TestMethod]
        public void CreatedValues()
        {
            BaseTestHandler handl = new _handl();
            handl.RegisterTypeConverter(typeof(Guid),
                (object guid, IFormatProvider? ifp) =>
            {
                return ((Guid)guid).ToString("B", ifp).ToUpper();
            });

            handl.Associate("AnimalClasses", typeof(AnimalClasses), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(GetTestXML("NamedTextContent_Create"), handl, RootNodeOwner: null, XMLDeserializer.DefaultOptions_IgnoreRootNode);
            xdes.Run();

            var result = handl.GetResults<AnimalClasses>().FirstOrDefault();
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.Classes.Count);
            var animalClass = result.Classes.First();
            Assert.AreEqual("Mammal", animalClass.Type);
            Assert.IsNotNull(animalClass.AnimalInfo);
            Assert.AreEqual("Forest", animalClass.AnimalInfo.Habitat);

            Assert.IsNotNull(animalClass.AnimalDetails);
            Assert.AreEqual(Guid.Parse("{B81A9EE7-6F1B-4348-BF43-D309EB3CB87E}"), animalClass.AnimalDetails.UniqueID);
            Assert.AreEqual("{AnimalName}", animalClass.AnimalDetails.NameTemplate);
            Assert.IsNotNull(animalClass.AnimalDetails.RelationDetails);
            Assert.AreEqual(1, animalClass.AnimalDetails.RelationDetails.Relations.Count);

            var relation = animalClass.AnimalDetails.RelationDetails.Relations.First();
            Assert.AreEqual(Guid.Parse("{E92B790A-F6E3-4462-A385-D788A0FD98EB}"), relation.ParentID);
            Assert.AreEqual("Parent", relation.RelationType);

            Assert.IsNotNull(animalClass.ClassInfo);
            Assert.AreEqual(1, animalClass.ClassInfo.Classes.Count);

            var classInfo = animalClass.ClassInfo.Classes.First();
            Assert.AreEqual(Guid.Parse("{01EC8206-4DB3-4717-8E6A-7060BFA04F07}"), classInfo.ClassID);
            Assert.AreEqual("Forest", classInfo.Habitat);


            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<AnimalClasses>(handl, XMLDeserializer.DefaultOptions_IgnoreRootNode));
        }

        private class _handl : BaseTestHandler
        {
            const string PATTERN = @"Relation\d+";

            public override bool InfoForNode(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, out ElementMappingInfo info)
            {
                // Special case for handling Relation nodes dynamically based on their names (e.g., Relation1, Relation2, etc.)
                // This is necessary because the standard attribute-based procedure cannot handle dynamic node names
                // that follow a specific pattern. By using a regex pattern, we can correctly identify and deserialize
                // these nodes into the appropriate Relation objects.
                if (state.CurrentInstance is RelationDetails && System.Text.RegularExpressions.Regex.IsMatch(nodeName, PATTERN))
                {
                    info = new ElementMappingInfo(DeserializationPolicy.Create, typeof(Relation));
                    return true;
                }

                return base.InfoForNode(state, nodeName, attributes, out info);
            }

            public override void Finalized(IXMLState state, string nodeName, object result)
            {
                // The specialized "Finalized" implementation is needed here because the
                // standard deserialization process cannot handle nodes with names that follow a pattern (e.g., Relation1, Relation2, etc.).
                //
                // SiuWhen such a node is
                // encountered, it is deserialized into a Relation object and added to the Relations list
                // of the parent RelationDetails instance. This dynamic handling is necessary .
                if (state.GetParent() is RelationDetails rd && System.Text.RegularExpressions.Regex.IsMatch(nodeName, PATTERN))
                {
                    rd.Relations.Add((Relation)result);
                }
                base.Finalized(state, nodeName, result);
            }
        }

        private class AnimalClasses : IEquatable<AnimalClasses?>
        {
            [XMLChild("AnimalClass")]
            public List<AnimalClass> Classes { get; set; }


            public AnimalClasses()
            {
                Classes = new List<AnimalClass>();
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalClasses);
            }

            public bool Equals(AnimalClasses? other)
            {
                return other is not null &&
                       BaseTestClass.Compare(this.Classes, other.Classes);
            }
        }

        public class AnimalClass : IEquatable<AnimalClass?>
        {
            [XMLAttribute]
            public string Type { get; set; }
            [XMLChild]
            public AnimalInfo AnimalInfo { get; set; }
            [XMLChild]
            public AnimalDetails AnimalDetails { get; set; }
            [XMLChild]
            public ClassInfo ClassInfo { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalClass);
            }

            public bool Equals(AnimalClass? other)
            {
                return other is not null &&
                       this.Type == other.Type &&
                       EqualityComparer<AnimalInfo>.Default.Equals(this.AnimalInfo, other.AnimalInfo) &&
                       EqualityComparer<AnimalDetails>.Default.Equals(this.AnimalDetails, other.AnimalDetails) &&
                       EqualityComparer<ClassInfo>.Default.Equals(this.ClassInfo, other.ClassInfo);
            }
        }

        public class AnimalInfo : IEquatable<AnimalInfo?>
        {

            [XMLChild]
            public string Habitat { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalInfo);
            }

            public bool Equals(AnimalInfo? other)
            {
                return other is not null &&
                       this.Habitat == other.Habitat;
            }
        }

        public class AnimalDetails : IEquatable<AnimalDetails?>
        {
            [XMLChild]
            public Guid UniqueID { get; set; }
            [XMLChild]
            public string NameTemplate { get; set; }
            [XMLChild]
            public RelationDetails RelationDetails { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnimalDetails);
            }

            public bool Equals(AnimalDetails? other)
            {
                return other is not null &&
                       this.UniqueID.Equals(other.UniqueID) &&
                       this.NameTemplate == other.NameTemplate &&
                       EqualityComparer<RelationDetails>.Default.Equals(this.RelationDetails, other.RelationDetails);
            }
        }

        public class RelationDetails : IEquatable<RelationDetails?>
        {
            [XMLChild("Relation")]
            public List<Relation> Relations { get; set; }
            public RelationDetails()
            {
                Relations = new List<Relation>();
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as RelationDetails);
            }

            public bool Equals(RelationDetails? other)
            {
                return other is not null &&
                       BaseTestClass.Compare(this.Relations, other.Relations);
            }
        }

        public class Relation : IEquatable<Relation?>
        {
            [XMLChild]
            public Guid ParentID { get; set; }
            [XMLChild]
            public string RelationType { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as Relation);
            }

            public bool Equals(Relation? other)
            {
                return other is not null &&
                       this.ParentID.Equals(other.ParentID) &&
                       this.RelationType == other.RelationType;
            }

        }

        public class ClassInfo : IEquatable<ClassInfo?>
        {

            [XMLChild("Class")]
            public List<Class> Classes { get; set; }
            public ClassInfo()
            {
                Classes = new List<Class>();
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as ClassInfo);
            }

            public bool Equals(ClassInfo? other)
            {
                return other is not null &&
                       BaseTestClass.Compare(this.Classes, other.Classes);
            }

        }

        public class Class : IEquatable<Class?>
        {
            [XMLChild]
            public Guid ClassID { get; set; }
            [XMLChild]
            public string Habitat { get; set; }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as Class);
            }

            public bool Equals(Class? other)
            {
                return other is not null &&
                       this.ClassID.Equals(other.ClassID) &&
                       this.Habitat == other.Habitat;
            }
        }

    }
}
