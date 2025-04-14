using MapXML.Utils;
using System.Reflection;

namespace MapXML.Tests
{
    public abstract class BaseTestClass
    {
        public Stream GetTestXML(String name)
        {
            Assembly a = typeof(BaseTestClass).Assembly;
            return a.GetManifestResourceStream($"MapXML.Test.DataFiles.{name}.xml")??throw new ArgumentException(nameof(name));
        }

        static BaseTestClass()
        {
        }
    }
}