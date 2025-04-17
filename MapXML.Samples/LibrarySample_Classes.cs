
using MapXML.Attributes;
using System.Collections.Generic;
using static MapXML.Sample.LibrarySample;

namespace MapXML.Sample
{
    public static class LibrarySample
    {
        public const string XML = @"
<Library>
  <Book ISBN=""9783161484100"">
    <Title>The Art of Computer Programming</Title>
    <Author>Donald E. Knuth</Author>
    <PublishedYear>1968</PublishedYear>
  </Book>
  <Book ISBN=""9780131103627"">
    <Title>The C Programming Language</Title>
    <Author>Brian W. Kernighan &amp; Dennis M. Ritchie</Author>
    <PublishedYear>1978</PublishedYear>
  </Book>
</Library>
";

        public static void PrintToConsole(this Library library)
        {
            // Get the results
            foreach (var book in library.Books)
            {
                Console.WriteLine("=============");
                Console.WriteLine(book.ISBN);
                Console.WriteLine(book.Author);
                Console.WriteLine(book.Title);
                Console.WriteLine(book.PublishedYear);
            }
            Console.WriteLine("=============");
        }

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

}

