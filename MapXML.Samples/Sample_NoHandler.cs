
namespace MapXML.Sample
{
    internal class Sample_NoHandler
    {
        static void Main(string[] args)
        {
            //Define options
            IDeserializationOptions opt =
            XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true) //<-- Allow implicit fields
                .Build();

            LibrarySample.Library library = new LibrarySample.Library(); //This will be the owner of the root node

            // Create an instance of the XMLDeserializer
            XMLDeserializer deserializer = new XMLDeserializer(LibrarySample.XML, Handler: null, RootNodeOwner: library, Options: opt);
            deserializer.Run();



            // Inspect the results
            library.PrintToConsole();                  
        }
    }
}

