using KC.Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Test.Ricochet
{
    public class PropAndFieldTests
    {
        [Fact]
        public void Main()
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

            var members = PropertyAndFieldCache.Get(typeof(SomeClass));

            //Test inherited property detection:
            Assert.Contains(members, x => x.Name == "SomeOtherString");
            Assert.Contains(members, x => x.Name == nameof(SomeBaseClass.SomeString));

            //Test Get:
            var someStringProp = members.First(x => x.Name.Contains("SomeString"));
            Assert.Equal("bb", someStringProp.GetVal(b));
            Assert.Equal("a", someStringProp.GetVal(a));

            //Test Set:
            someStringProp.SetVal(b, "c");
            Assert.Equal("c", b.SomeString);

            //Test Classification:
            Assert.Equal(15, members.Count());
            Assert.Equal(11, members.Where(x => x.IsProperty && x.IsPublic).Count());
            Assert.Equal(2, members.Where(x => x.IsDictionaryOfClass).Count());
            Assert.Single(members.Where(x => x.IsIEnumberableOfClass));
            Assert.Equal(2, members.Where(x => x.IsClass).Count());
            Assert.Single(members.Where(x => x.IsDictionaryOfValueOrString));
            Assert.Single(members.Where(x => x.IsIEnumberableOfValueOrString));
            Assert.Equal(8, members.Where(x => x.IsValueOrString).Count());

            foreach (var prop in members) {
                prop.Copy(b, a);
            }

            Assert.Equal(a.ClassDict, b.ClassDict);
            Assert.Equal(a.ClassList, b.ClassList);
            Assert.Equal(a.IntList, b.IntList);
            Assert.Equal(a.SomeFloat, b.SomeFloat);
            Assert.Equal(a.SomeInt, b.SomeInt);
            Assert.Equal(a.SomeString, b.SomeString);
            Assert.Equal(a.ValueDict, b.ValueDict);
            Assert.Equal(a.WeirdDict, b.WeirdDict);

            //Test IsEqual
            foreach (var prop in members) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test string conversions.
            foreach (var prop in members) {
                if (prop.IsStringConvertible) {
                    var s = prop.GetValAsString(a);
                    prop.SetValFromString(a, s);
                }
            }

            foreach (var prop in members) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test double conversions.
            foreach (var prop in members) {
                if (prop.IsDoubleConvertible) {
                    var d = prop.GetValAsDouble(a);
                    prop.SetValFromDouble(a, d);
                }
            }

            foreach (var prop in members) {
                Assert.True(prop.IsEqual(a, b));
            }

            //Test Markers
            Assert.Single(members.Where(x => x.Markers.Contains("Special!")));
            Assert.Single(members.Where(x => x.Markers.Contains("Multiple")));
            Assert.Single(members.Where(x => x.Markers.Contains("extends")));
        }

        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class ExtendsRicochetMark : RicochetMark {
            public ExtendsRicochetMark() : base("extends") { }
        }

        class SomeBaseClass {
            public string SomeString { get; set; }
            private string SomeOtherString { get; set; }
            public long GetPrivateSet { get; private set; }
            protected virtual int DefaultVirtual => 5;
            protected virtual int OverridenVirtual => 6;
        }

        class SomeGenericClass<T> {
            T value;
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
            public SomeGenericClass<int> SomeGenericClass {get; set;}

            private int privateField;
            internal int internalField;
            public int PublicField;

            [RicochetIgnore]
            private int IgnoreMeProp { get; set; }
            [RicochetIgnore]
            internal int IgnoreMeProp2 { get; set; }

            [RicochetIgnore]
            public int IgnoreMeAttriProp { get; set; }

            protected override int OverridenVirtual => 7;
        }

    }
}
