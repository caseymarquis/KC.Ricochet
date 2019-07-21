using KC.Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Test.Ricochet
{
    public class Constructors
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
    }
}
