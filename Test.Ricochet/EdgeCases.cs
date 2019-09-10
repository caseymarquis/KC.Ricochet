using KC.Ricochet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test.Ricochet
{
    public class EdgeCases
    {
        [Fact]
        public void FieldAndPropertyWithTheSameNameExceptCase()
        {
            KC.Ricochet.Util.GetPropsAndFields<Class1>();
        }

        public class Class1 {
            int a;
            int A { get; }
        }

        [Fact]
        public void PropertiesWithSameNameExceptCase() {
            KC.Ricochet.Util.GetPropsAndFields<Class2>();
        }

        public class Class2 {
            int a { get; set; }
            int A { get; }
        }

        [Fact]
        public void ShadowedFields() {
            var props = KC.Ricochet.Util.GetPropsAndFields<Class4>();
            var x = 0;
        }

        public class Class3 {
            int a { get; set; }
        }

        public class Class4 : Class3 {
            int a { get; set; }
        }

        [Fact]
        public void ReadOnlyFields() {
            var props = Util.GetPropsAndFields<Class5>();
            Assert.Single(props);
        }

        public class Class5 {
            private int i;
            private readonly int j;
        }

    }
}
