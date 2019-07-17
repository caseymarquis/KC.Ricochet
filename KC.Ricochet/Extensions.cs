using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KC.Ricochet
{
    //http://stackoverflow.com/questions/35370384/how-to-get-declared-and-inherited-members-from-typeinfo
    public static class TypeInfoAllMemberExtensions
    {
        public static IEnumerable<ConstructorInfo> GetAllConstructors(this TypeInfo typeInfo, BindingFlags flags)
            => typeInfo.GetConstructors(flags); //We shouldn't get base constructors.

        public static IEnumerable<EventInfo> GetAllEvents(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetEvents(flags));

        public static IEnumerable<FieldInfo> GetAllFields(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetFields(flags));

        public static IEnumerable<MemberInfo> GetAllMembers(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetMembers(flags));

        public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetMethods(flags));

        public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo typeInfo, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            => GetAll(typeInfo, ti => ti.GetProperties(flags));

        private static IEnumerable<T> GetAll<T>(TypeInfo typeInfo, Func<TypeInfo, IEnumerable<T>> accessor)
        {
            while (typeInfo != null)
            {
                foreach (var t in accessor(typeInfo))
                {
                    yield return t;
                }

                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }
    }
}
