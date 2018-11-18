using KC.Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Test.Ricochet
{
    public class UnitTest1
    {
        [Fact]
        public void NotExactlyAUnit()
        {
            var a = new SomeClass() {
                SomeString = "a",
                SomeInt = 0,
                SomeFloat = 0.0F,
                IntList = new List<int>(),
                ClassList = new List<SomeClass>(),
                ValueDict = new Dictionary<int, int>(),
                ClassDict = new Dictionary<int, SomeClass>(),
                WeirdDict = new Dictionary<SomeClass, SomeClass>(),
                SomeOtherClass = null,
            };

            var b = new SomeClass() {
                SomeString = "bb",
                SomeInt = 1,
                SomeFloat = 1.0F,
                IntList = new List<int>(),
                ClassList = new List<SomeClass>(),
                ValueDict = new Dictionary<int, int>(),
                ClassDict = new Dictionary<int, SomeClass>(),
                WeirdDict = new Dictionary<SomeClass, SomeClass>(),
                SomeOtherClass = a,
            };

            var cache = PropertyCache.Get(typeof(SomeClass));

            //Test inherited property detection:
            Assert.Contains(cache.AllProperties, x => x.Name == nameof(SomeBaseClass.SomeString));

            //Test Get:
            var someStringProp = cache.ValueAndStringProperties.First(x => x.Name.Contains("SomeString"));
            Assert.Equal("bb", someStringProp.GetVal(b));
            Assert.Equal("a", someStringProp.GetVal(a));

            //Test Set:
            someStringProp.SetVal(b, "c");
            Assert.Equal("c", b.SomeString);

            //Test Classification:
            Assert.Equal(9, cache.AllProperties.Count);
            Assert.Equal(2, cache.ClassDicts.Count);
            Assert.Single(cache.ClassIEnumerables);
            Assert.Single(cache.ClassProperties);
            Assert.Single(cache.ValueAndStringDicts);
            Assert.Single(cache.ValueAndStringIEnumerables);
            Assert.Equal(3, cache.ValueAndStringProperties.Count);

            //Test Copy:
            Action<List<PropertyAccessor>> copyProps = (list) =>
            {
                foreach (var prop in list) {
                    prop.Copy(b, a);
                }
            };

            copyProps(cache.ClassDicts);
            copyProps(cache.ClassIEnumerables);
            copyProps(cache.ClassProperties);
            copyProps(cache.ValueAndStringDicts);
            copyProps(cache.ValueAndStringIEnumerables);
            copyProps(cache.ValueAndStringProperties);

            Assert.Equal(a.ClassDict, b.ClassDict);
            Assert.Equal(a.ClassList, b.ClassList);
            Assert.Equal(a.IntList, b.IntList);
            Assert.Equal(a.SomeFloat, b.SomeFloat);
            Assert.Equal(a.SomeInt, b.SomeInt);
            Assert.Equal(a.SomeString, b.SomeString);
            Assert.Equal(a.ValueDict, b.ValueDict);
            Assert.Equal(a.WeirdDict, b.WeirdDict);

            //Test IsEqual
            foreach (var prop in cache.AllProperties) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test string conversions.
            foreach (var prop in cache.AllProperties) {
                if (prop.IsStringConvertible) {
                    var s = prop.GetValAsString(a);
                    prop.SetValFromString(a, s);
                }
            }

            foreach (var prop in cache.AllProperties) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test double conversions.
            foreach (var prop in cache.AllProperties) {
                if (prop.IsDoubleConvertible) {
                    var d = prop.GetValAsDouble(a);
                    prop.SetValFromDouble(a, d);
                }
            }

            foreach (var prop in cache.AllProperties) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test Markers
            Assert.Single(cache.AllProperties.Where(x => x.Markers.Contains("Special!")));
            Assert.Single(cache.AllProperties.Where(x => x.Markers.Contains("Multiple")));
            Assert.Single(cache.AllProperties.Where(x => x.Markers.Contains("extends")));
        }

        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class ExtendsRicochetMark : RicochetMark {
            public ExtendsRicochetMark() : base("extends") { }
        }

        class SomeBaseClass {
            public string SomeString { get; set; }
        }

        class SomeClass : SomeBaseClass {
            [RicochetMark("Special!")]
            public int SomeInt { get; set; }
            [RicochetMark("Multiple")]
            [ExtendsRicochetMark]
            public float SomeFloat { get; set; }
            public List<int> IntList { get; set; }
            public List<SomeClass> ClassList { get; set; }
            public Dictionary<int, int> ValueDict { get; set; }
            public Dictionary<int, SomeClass> ClassDict { get; set; }
            public Dictionary<SomeClass, SomeClass> WeirdDict { get; set; }
            public SomeClass SomeOtherClass { get; set; }

            private int IgnoreMe;
            internal int IgnoreMe2;
            public int IgnoreMe3;

            [RicochetIgnore]
            private int IgnoreMeProp { get; set; }
            [RicochetIgnore]
            internal int IgnoreMeProp2 { get; set; }

            [RicochetIgnore]
            public int IgnoreMeAttriProp { get; set; }
        }

    }
}
