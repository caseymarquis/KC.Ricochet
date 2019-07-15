using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace KC.Ricochet
{
    public class Util
    {
        public static PropertyAndFieldCache GetProps<T>()
            where T : class {
            return PropertyAndFieldCache.Get<T>();
        }

        public static PropertyAndFieldCache GetProps(Type t) {
            return PropertyAndFieldCache.Get(t);
        }

        public static IEnumerable<T> ShallowCopyRange<T>(IEnumerable<T> originalItems) where T : class, new() {
            var ret = new List<T>(originalItems.Count());
            var props = Util.GetProps<T>();
            foreach (var item in originalItems) {
                ret.Add(Util.ShallowCopyItem(item, props));
            }
            return ret;
        }

        private static Func<PropertyAndFieldAccessor, bool> defaultPredicate = (x) => x.IsProperty && x.IsValueOrString && x.IsPublic;

        public static T ShallowCopyItem<T>(T item, PropertyAndFieldCache props = null, Func<PropertyAndFieldAccessor, bool> predicate = null) where T : class, new() {
            props = props ?? GetProps<T>();
            predicate = predicate ?? defaultPredicate;
            var newT = new T();
            foreach (var prop in props.Members.Where(predicate)) {
                prop.Copy(item, newT);
            }
            return newT;
        }

        public static string GetPropertyName<T, U>(Expression<Func<T, U>> getProperty) {
            var name = GetPropertyNameOrNull(getProperty);
            if (name == null) {
                throw new Exception("Could not find property name!");
            }
            return name;
        }

        public static string GetPropertyNameOrNull<T, U>(Expression<Func<T, U>> getProperty) {
            string name;
            if (getProperty.Body is MemberExpression) {
                var expression = (MemberExpression)getProperty.Body;
                name = expression.Member.Name;
            }
            else {
                var op = ((UnaryExpression)getProperty.Body).Operand;
                name = ((MemberExpression)op).Member.Name;
            }
            return name;
        }

        public static void Copy<T, U>(T fromT, U toU, bool ignoreCase = true, bool copyNullMembers = false, Func<PropertyAndFieldAccessor, bool> predicate = null) where T : class where U : class {
            predicate = predicate ?? defaultPredicate;
            var fromProps = Util.GetProps<T>().Members.Where(predicate);
            var toProps = Util.GetProps<U>().Members.Where(predicate);

            foreach (var toProp in toProps) {
                var fromProp = fromProps.FirstOrDefault(x => string.Compare(x.Name, toProp.Name, ignoreCase) == 0);
                if (fromProp == null) {
                    continue;
                }

                var fromValue = fromProp.GetVal(fromT);
                if (!copyNullMembers && object.Equals(fromValue, null)) {
                    continue;
                }

                toProp.SetVal(toU, fromValue);
            }
        }
    }
}
