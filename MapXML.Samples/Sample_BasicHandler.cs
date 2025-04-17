using static MapXML.Sample.LibrarySample;

namespace MapXML.Sample
{
    internal class Sample_BasicHandler
    {    
        static void Main(string[] args)
        {
            //Define options
            IDeserializationOptions opt =
            XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true) //<-- Allow implicit fields
                .Build();

            //Create a minimal handler to define the root type
            var h = new DefaultHandler();
            h.Associate("Library", typeof(Library)); //tell the system what 'Library' nodes should map to

            // Create an instance of the XMLDeserializer
            XMLDeserializer deserializer = new XMLDeserializer(XML, Handler: h, Options: opt);
            deserializer.Run();

            // Inspect the results
            IReadOnlyList<Library> results = h.GetResults<Library>();
            Library library = results.First();
            library.PrintToConsole();
        }
    }
}

