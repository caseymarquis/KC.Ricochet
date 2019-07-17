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
        public void CreateNew() {
            void check(ITestConstructor normal, ITestConstructor reflection) {
                Assert.Equal(normal.a, reflection.a);
                Assert.Equal(normal.b, reflection.b);
            }

            check(new PublicConstructors(), InstantiatorCache.Get<PublicConstructors>().New());
            check(new PublicConstructors(7), InstantiatorCache.Get<PublicConstructors>(typeof(int)).New(7));
            check(new PublicConstructors(8, "nine"), InstantiatorCache.Get<PublicConstructors>(typeof(int), typeof(string)).New(8, "nine"));

            check(new PublicConstructors(), InstantiatorCache.Get<PrivateConstructors>().New());
            check(new PublicConstructors(7), InstantiatorCache.Get<PrivateConstructors>(typeof(int)).New(7));
            check(new PublicConstructors(8, "nine"), InstantiatorCache.Get<PrivateConstructors>(typeof(int), typeof(string)).New(8, "nine"));
        }

        [Fact]
        public void CreateNew_NoMatchingConstructor() {
            Assert.Throws<ApplicationException>(() => {
                var result = InstantiatorCache.Get<PublicConstructors>(typeof(double)).New((double)2.2);
            });
        }

        [Fact]
        public void CreateNew_WrongTypes() {
            Assert.Throws<InvalidCastException>(() => {
                var result = InstantiatorCache.Get<PublicConstructors>(typeof(int)).New((double)2.2);
            });
        }

        [Fact]
        public void CreateNew_WrongNumberOfArgs() {
            Assert.Throws<ApplicationException>(() => {
                var result = InstantiatorCache.Get<PublicConstructors>(typeof(int)).New();
            });
            Assert.Throws<ApplicationException>(() => {
                var result = InstantiatorCache.Get<PublicConstructors>().New(7);
            });
        }

        public interface ITestConstructor {
            int a { get; set; }
            string b { get; set; }
        }

        public class BaseConstructor {
            public BaseConstructor() {
            }
            public BaseConstructor(double d) {
            }
        }

        public class PublicConstructors : BaseConstructor, ITestConstructor {
            public int a { get; set; } = 0;
            public string b { get; set; } = "0";
            public PublicConstructors() {
                a = 1;
                b = "1";
            }
            public PublicConstructors(int a) {
                this.a = a;
                this.b = "2";
            }
            public PublicConstructors(int a, string b) {
                this.a = a;
                this.b = b;
            }
        }

        public class PrivateConstructors : ITestConstructor {
            public int a { get; set; } = 0;
            public string b { get; set; } = "0";
            private PrivateConstructors() {
                a = 1;
                b = "1";
            }
            private PrivateConstructors(int a) {
                this.a = a;
                this.b = "2";
            }
            private PrivateConstructors(int a, string b) {
                this.a = a;
                this.b = b;
            }
        }

        [Fact]
        public void PropertiesAndFields()
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
            Assert.Equal(13, members.Count());
            Assert.Equal(9, members.Where(x => x.IsProperty && x.IsPublic).Count());
            Assert.Equal(2, members.Where(x => x.IsDictionaryOfClass).Count());
            Assert.Single(members.Where(x => x.IsIEnumberableOfClass));
            Assert.Single(members.Where(x => x.IsClass));
            Assert.Single(members.Where(x => x.IsDictionaryOfValueOrString));
            Assert.Single(members.Where(x => x.IsIEnumberableOfValueOrString));
            Assert.Equal(7, members.Where(x => x.IsValueOrString).Count());

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

            private int privateField;
            internal int internalField;
            public int PublicField;

            [RicochetIgnore]
            private int IgnoreMeProp { get; set; }
            [RicochetIgnore]
            internal int IgnoreMeProp2 { get; set; }

            [RicochetIgnore]
            public int IgnoreMeAttriProp { get; set; }
        }

    }
}
