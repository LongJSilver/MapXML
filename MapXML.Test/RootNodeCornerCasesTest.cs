using MapXML.Attributes;

namespace MapXML.Tests
{
    [TestClass]
    public class RootNodeCornerCasesTest : BaseTestClass
    {
        [TestMethod]
        public void IgnoreRoot_WithOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(true).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler, opt);
        }

        [TestMethod]
        public void IgnoreRoot_WithoutOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases_ExtraRoot");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = null;// new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(true).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler, opt);
        }


        [TestMethod]
        public void DontIgnoreRoot_WithOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(false).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler, opt);
        }

        [TestMethod]
        public void DontIgnoreRoot_WithoutOwner()
        {
            Stream s = GetTestXML("RootNodeCornerCases");
            BaseTestHandler handler = new BaseTestHandler();
            handler.Associate<MovieCollection>("MovieCollection");

            object? owner = null;// new MovieCollection();
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(true)
                .IgnoreRootNode(false).Build();

            //****************//
            XMLDeserializer xdes = new XMLDeserializer(s, handler, RootNodeOwner: owner, opt);
            xdes.Run();

            TestResults(handler, opt);
        }

        private void TestResults(BaseTestHandler handler, IDeserializationOptions opt)
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


            // ROUND TRIP SERIALIZATION TEST  -----//
            Assert.IsTrue(RoundTripSerializerTest<MovieCollection>(handler, opt));
        }
    }

    public class MovieCollection : IEquatable<MovieCollection?>
    {
        [XMLChild("Movie")]
        public List<Movie> Movies { get; set; } = new List<Movie>();

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as MovieCollection);
        }

        public bool Equals(MovieCollection? other)
        {
            return other is not null &&
                   BaseTestClass.Compare(this.Movies, other.Movies);
        }

        [XMLFunction]
        public Movie? GetMovie([XMLParameter("Title")] string title)
        {
            return Movies.FirstOrDefault(m => m.Title == title);
        }

    }

    public class Movie : IEquatable<Movie?>
    {
        public string Title { get; set; }
        public string Director { get; set; }
        public int ReleaseYear { get; set; }
        public string Genre { get; set; }

        [XMLChild("Prequel", DeserializationPolicy.Lookup)]
        public Movie Prequel { get; set; }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as Movie);
        }

        public bool Equals(Movie? other)
        {
            return other is not null &&
                   this.Title == other.Title &&
                   this.Director == other.Director &&
                   this.ReleaseYear == other.ReleaseYear &&
                   this.Genre == other.Genre &&
                   EqualityComparer<Movie>.Default.Equals(this.Prequel, other.Prequel);
        }
    }


}
