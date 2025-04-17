using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapXML;
using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass]
    public class Des_RootNodeCornerCases : BaseTestClass
    {
        [TestMethod]
        public void IgnoreRoot_WithOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(true).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler);
        }

        [TestMethod]
        public void IgnoreRoot_WithoutOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases_ExtraRoot");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = null;// new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(true).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler);
        }


        [TestMethod]
        public void DontIgnoreRoot_WithOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(false).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler);
        }

        [TestMethod]
        public void DontIgnoreRoot_WithoutOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            DefaultHandler handler = new DefaultHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = null;// new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(false).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler);
        }

        private void TestResults(DefaultHandler handler)
        {
            // Retrieve the deserialized results as a list of Movie objects
            Assert.AreEqual(1, handler.GetResults<MovieCollection>().Count, "The number of Movie Collections must be 1.");

            var collection = handler.GetResults<MovieCollection>().First();
            var movies = collection.Movies;

            // Assert that the correct number of movies were deserialized
            Assert.AreEqual(3, movies.Count, "The number of deserialized movies does not match the expected count.");
            int i = 0;

            // Validate the content of each movie
           
            Assert.AreEqual("The Fellowship of the Ring", movies[i].Title);
            Assert.AreEqual("Peter Jackson", movies[i].Director);
            Assert.AreEqual(2001, movies[i].ReleaseYear);
            Assert.AreEqual("Fantasy", movies[i].Genre);
            i++;

            Assert.AreEqual("The Two Towers", movies[i].Title);
            Assert.AreEqual("Peter Jackson", movies[i].Director);
            Assert.AreEqual(2002, movies[i].ReleaseYear);
            Assert.AreEqual("Fantasy", movies[i].Genre);
            Assert.AreEqual("The Fellowship of the Ring", movies[i].Prequel?.Title);
            i++;

            Assert.AreEqual("The Return of the King", movies[i].Title);
            Assert.AreEqual("Peter Jackson", movies[i].Director);
            Assert.AreEqual(2003, movies[i].ReleaseYear);
            Assert.AreEqual("Fantasy", movies[i].Genre);
            Assert.AreEqual("The Two Towers", movies[i].Prequel?.Title);
            i++;

        }
    }

    public class MovieCollection
    {
        [XmlChild("Movie")]
        public List<Movie> Movies { get; set; } = new List<Movie>();

        [XMLFunction]
        public Movie? GetMovie([XMLParameter("Title")] string title)
        {
            return Movies.FirstOrDefault(m => m.Title == title);
        }
    }

    public class Movie
    {
        public string Title { get; set; }
        public string Director { get; set; }
        public int ReleaseYear { get; set; }
        public string Genre { get; set; }

        [XmlChild("Prequel", DeserializationPolicy.Lookup)]
        public Movie Prequel { get; set; }
    }


}
