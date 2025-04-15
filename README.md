# MapXML

**MapXML** is a lightweight .NET library designed for mapping and transforming XML structures into strongly-typed objects and vice-versa. It provides a framework for defining XML structure, resolving types, and performing automated parsing and object instantiation.

## Projects

- **MapXML** — Core library
- **MapXML.Test** — Unit test suite using MSTest

## Getting Started

To use MapXML in your project:

```bash
git clone https://github.com/LongJSilver/MapXML.git
```

Then open the MapXML.sln in Visual Studio or run with the .NET CLI.

## Sample Use Case

Below is an example that shows a small XML document and its corresponding C# object graph.

**Example XML**
```xml
<Library>
  <Book ISBN="9783161484100">
    <Title>The Art of Computer Programming</Title>
    <Author>Donald E. Knuth</Author>
    <PublishedYear>1968</PublishedYear>
  </Book>
  <Book ISBN="9780131103627">
    <Title>The C Programming Language</Title>
    <Author>Brian W. Kernighan &amp; Dennis M. Ritchie</Author>
    <PublishedYear>1978</PublishedYear>
  </Book>
</Library>
```

**CLR Objects**
``` csharp

using System.Collections.Generic;

namespace MapXMLExample
{
     public class Library
    {
        // A collection to hold all Book objects
        [XmlChild("Book")]
        public List<Book> Books { get; set; } = new List<Book>();
    }

    public class Book
    {
        // XML attribute mapped to a property
        public string ISBN { get; set; }

        // XML child elements mapped to properties
        public string Title { get; set; }
        public string Author { get; set; }
        public int PublishedYear { get; set; }
    }
}
```

**Deserialization Code**

``` csharp
           
//Define options
IDeserializationOptions opt = 
XMLDeserializer.OptionsBuilder()
    .AllowImplicitFields(true) //<-- Allow implicit fields
    .Build();

//Create a minimal handler to define the root type
var h = new DefaultHandler();
h.Associate("Library", typeof(Library)); //tell the system what 'Library' nodes should map to

// Create an instance of the XMLDeserializer, assuming the XML is stored in the 'xmlString' variable
XMLDeserializer deserializer = new XMLDeserializer(xmlString, Handler:h, Options:opt);
deserializer.Run();

// Get the results
IReadOnlyList<Library> results = h.GetResults<Library>();

```