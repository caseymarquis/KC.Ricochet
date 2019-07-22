using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KC.Ricochet
{
    public struct InfoWithLevel {
        public MemberInfo Info;
        public int Level;

        public PropertyInfo PropertyInfo => Info as PropertyInfo;
        public FieldInfo FieldInfo => Info as FieldInfo;

        public InfoWithLevel(MemberInfo info, int level) {
            Info = info;
            Level = level;
        }
    }

    //http://stackoverflow.com/questions/35370384/how-to-get-declared-and-inherited-members-from-typeinfo
    public static class TypeInfoAllMemberExtensions
    {
        public static IEnumerable<ConstructorInfo> GetAllConstructors(this TypeInfo typeInfo, BindingFlags flags)
            => typeInfo.GetConstructors(flags); //We shouldn't get base constructors.

        public static IEnumerable<InfoWithLevel> GetAllFields(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetFields(flags));

        public static IEnumerable<InfoWithLevel> GetAllProperties(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetProperties(flags));

        private static IEnumerable<InfoWithLevel> GetAll(TypeInfo typeInfo, Func<TypeInfo, IEnumerable<MemberInfo>> accessor)
        {
            var level = 0;
            while (typeInfo != null)
            {
                foreach (var t in accessor(typeInfo))
                {
                    yield return new InfoWithLevel(t, level);
                }
                level--;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }
    }
}
