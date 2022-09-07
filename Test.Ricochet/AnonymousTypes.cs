using KC.Ricochet;
using System;
using System.Linq;
using Xunit;

namespace Test.Ricochet {
    public class AnonymousTypes {
        [Fact]
        public void GetAnonymousTypeMembers() {
            var anonType = new {
                A = "1",
                B = 2,
                C = new DateTime(2000, 1, 1),
            };
            var props = RicochetUtil.GetPropsAndFields(anonType.GetType());
            Assert.NotEmpty(props);
            Assert.Equal(3, props.Count());
        }
    }
}
