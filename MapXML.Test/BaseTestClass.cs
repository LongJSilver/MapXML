using MapXML.Utils;
using System.Collections;
using System.Reflection;

namespace MapXML.Tests
{
    public abstract class BaseTestClass
    {
        public Stream GetTestXML(String name)
        {
            Assembly a = typeof(BaseTestClass).Assembly;
            return a.GetManifestResourceStream($"MapXML.Test.DataFiles.{name}.xml") ?? throw new ArgumentException(nameof(name));
        }

        static BaseTestClass()
        {
        }

        public static bool Compare<K, V>(IEnumerable<KeyValuePair<K, V>> first,
            IEnumerable<KeyValuePair<K, V>> second)
            where K : IEquatable<K> where V : IEquatable<V>
            => Compare(
                            first.Select(kvp => (kvp.Key, kvp.Value)),
                            second.Select(kvp => (kvp.Key, kvp.Value))
                );

        public static bool Compare<T>(IEnumerable<T> first, IEnumerable<T> second) where T : IEquatable<T>
        {
            if (first == null && second == null) return true;
            if (first == null || second == null) return false;

            if (first.Count() != second.Count()) return false;

            List<T> firstList = new(first);
            List<T> secondList = new(second);

            foreach (var item in firstList.ToList())
            {
                if (secondList.Remove(item))
                {
                    firstList.Remove(item);
                }
            }

            return firstList.Count == 0 && secondList.Count == 0;
        }


        public static bool RoundTripSerializerTest<T>(
            BaseTestHandler dh, IDeserializationOptions OriginalDeserializationOptions) where T : IEquatable<T>
        {
            IList<(string nodeName, T firstLevelItem, Type t)> FirstLevelItems =
                dh.GetResultInfo(1).Where(item => item.Result is T)
                .Select(info => (info.NodeName, (T)info.Result, info.RegisteredType)).ToList();

            dh.ClearResults();

            var serOptionsBuilder = XMLSerializer.OptionsBuilder(OriginalDeserializationOptions);
            if (FirstLevelItems.Count > 1 || OriginalDeserializationOptions.IgnoreRootNode)
            {
                serOptionsBuilder.WithAdditionalRootNode("xml");
            }

            XMLSerializer ser = new XMLSerializer(dh, serOptionsBuilder.Build());
            foreach (var item in FirstLevelItems)
            {
                ser.AddItem(item.nodeName, item.firstLevelItem);
            }

            ser.Run();
            using Stream ms = ser.ResultStream;
            HashSet<string> AlreadyAssociated = new HashSet<string>();
            foreach (var item in FirstLevelItems)
            {
                if (AlreadyAssociated.Contains(item.nodeName)) continue;
                dh.Associate(item.nodeName, item.t);
                AlreadyAssociated.Add(item.nodeName);
            }

            XMLDeserializer xdes = new XMLDeserializer(ms, dh, OriginalDeserializationOptions);

            xdes.Run();

            var deserializedItems = dh.GetResults<T>(1);
            return Compare(FirstLevelItems.Select(i => i.firstLevelItem), deserializedItems);

        }

    }

    public class BaseTestHandler : DefaultHandler
    {
        public struct ResultInfo
        {
            public readonly string NodeName;
            public readonly int Level;
            public readonly object Result;
            public readonly Type RegisteredType;

            public ResultInfo(string nodeName, int level, object result, Type registeredType)
            {
                this.NodeName = nodeName;
                this.Level = level;
                this.Result = result;
                this.RegisteredType = registeredType;
            }
        }

        public IReadOnlyList<ResultInfo> GetResultInfo(int level = -1)
        {
            var list = new List<ResultInfo>();
            foreach (KeyValuePair<string, IList<(int level, object result)>> item in ResultsByNode)
            {
                var registeredType = QuickAssociations[item.Key].targetType;
                foreach (var element in item.Value)
                {
                    if (level != -1 && element.level != level) continue;
                    list.Add(new ResultInfo(item.Key, element.level, element.result, registeredType));
                }
            }
            return list;
        }

        public void ClearResults()
        {
            AllResults.Clear();
            ResultsByNode.Clear();
        }

    }
}