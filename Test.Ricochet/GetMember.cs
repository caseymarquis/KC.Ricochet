using KC.Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test.Ricochet {
    public class GetMembers {
        [Fact]
        public void GetMember_Success() {
            var a = new A();
            var onlyB = RicochetUtil.GetMember<B>(a);
            Assert.NotNull(onlyB);
        }

        [Fact]
        public void GetMember_ThrowFromMissing() {
            var a = new A();
            Assert.Throws<ArgumentException>(() => {
                var c = RicochetUtil.GetMember<C>(a);
            });
        }

        [Fact]
        public void GetMember_ThrowFromAmbiguous() {
            var b = new B();
            Assert.Throws<ArgumentException>(() => {
                var c = RicochetUtil.GetMember<C>(b);
            });
        }

        [Fact]
        public void GetNestedMember_Success() {
            var a = new A();
            var c = RicochetUtil.GetNestedMember(a)
                .Where<B>()
                .Where<C>(x => x.Name == "c1")
                .Result;
            Assert.NotNull(c);
        }

        [Fact]
        public void GetNestedMember_ThrowFromMissing() {
            var a = new A();
            Assert.Throws<ArgumentException>(() => {
                var c = RicochetUtil.GetNestedMember(a)
                    .Where<B>()
                    .Where<C>(x => x.Name == "c4")
                    .Result;
            });
        }

        [Fact]
        public void GetNestedMember_ThrowFromAmbiguous() {
            var a = new A();
            Assert.Throws<ArgumentException>(() => {
                var c = RicochetUtil.GetNestedMember(a)
                    .Where<B>()
                    .Where<C>()
                    .Result;
            });
        }

        class A {
            B b1 = new B();
        }

        class B {
            C c1 = new C();
            C c2 = new C();
            C c3 = new C();
        }

        class C{}
    }
}
