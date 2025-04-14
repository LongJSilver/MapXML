using MapXML.Attributes;
using MapXML.Utils;

namespace MapXML.Tests
{
    [TestClass()]
    public class NamedTextContent_Create_Test : BaseTestClass
    {
        [TestMethod]
        public void CreatedValues()
        {
            DefaultHandler handl = new _handl();
            handl.RegisterTypeConverter(typeof(Guid),
                (object guid, IFormatProvider ifp) =>
            {
                return ((Guid)guid).ToString("B", ifp).ToUpper();
            });

            handl.Associate("AnimalClasses", typeof(AnimalClasses), DeserializationPolicy.Create);
            XMLDeserializer xdes = new XMLDeserializer(handl, GetTestXML("NamedTextContent_Create"), null)
            {
                IgnoreRootNode = true
            };
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


            XMLSerializer ser = new XMLSerializer(handl, null, "xml");
            handl.GetResults<object>().ForEach(o => ser.AddItem("AnimalClasses", o));
            ser.Run();

        }

        private class _handl : DefaultHandler
        {
            const string PATTERN = @"Relation\d+";

            public override bool InfoForNode(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, out DeserializationPolicy policy, out Type result)
            {
                // Special case for handling Relation nodes dynamically based on their names (e.g., Relation1, Relation2, etc.)
                // This is necessary because the standard attribute-based procedure cannot handle dynamic node names
                // that follow a specific pattern. By using a regex pattern, we can correctly identify and deserialize
                // these nodes into the appropriate Relation objects.
                if (state.CurrentInstance is RelationDetails && System.Text.RegularExpressions.Regex.IsMatch(nodeName, PATTERN))
                {
                    result = typeof(Relation);
                    policy = DeserializationPolicy.Create;
                    return true;
                }

                return base.InfoForNode(state, nodeName, attributes, out policy, out result);
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

        private class AnimalClasses
        {
            [XmlChild("AnimalClass")]
            public List<AnimalClass> Classes { get; set; }


            public AnimalClasses()
            {
                Classes = new List<AnimalClass>();
            }
        }

        public class AnimalClass
        {
            [XmlAttribute]
            public string Type { get; set; }
            [XmlChild]
            public AnimalInfo AnimalInfo { get; set; }
            [XmlChild]
            public AnimalDetails AnimalDetails { get; set; }
            [XmlChild]
            public ClassInfo ClassInfo { get; set; }


            /*
            <AnimalClass Type="Mammal">
              <AnimalInfo>
                <Habitat>Forest</Habitat>
              </AnimalInfo>
              <AnimalDetails>
                <UniqueID>{A1B2C3D4-E5F6-7890-GH12-IJKL34567890}</UniqueID>
                <NameTemplate>{AnimalName}</NameTemplate>
                <RelationDetails>
                  <Relation1>
                    <ParentID>{12345678-90AB-CDEF-1234-567890ABCDEF}</ParentID>
                    <RelationType>Parent</RelationType>
                  </Relation1>
                </RelationDetails>
              </AnimalDetails>
              <ClassInfo>
                <Class>
                  <ClassID>{09876543-21FE-DCBA-0987-654321FEDCBA}</ClassID>
                  <Habitat>Forest</Habitat>
                </Class>
              </ClassInfo>
            </AnimalClass> 

             */
        }

        public class AnimalInfo
        {
            [XmlChild]
            public string Habitat { get; set; }
        }

        public class AnimalDetails
        {
            [XmlChild]
            public Guid UniqueID { get; set; }
            [XmlChild]
            public string NameTemplate { get; set; }
            [XmlChild]
            public RelationDetails RelationDetails { get; set; }
        }

        public class RelationDetails
        {
            [XmlChild]
            public List<Relation> Relations { get; set; }
            public RelationDetails()
            {
                Relations = new List<Relation>();
            }

        }

        public class Relation
        {
            [XmlChild]
            public Guid ParentID { get; set; }
            [XmlChild]
            public string RelationType { get; set; }
        }

        public class ClassInfo
        {
            [XmlChild("Class")]
            public List<Class> Classes { get; set; }
            public ClassInfo()
            {
                Classes = new List<Class>();
            }
        }

        public class Class
        {
            [XmlChild]
            public Guid ClassID { get; set; }
            [XmlChild]
            public string Habitat { get; set; }
        }

    }
}
