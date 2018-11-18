using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace KC.Ricochet
{
    public class Util
    {
        public static PropertyCache GetProps<T>()
            where T : class {
            return PropertyCache.Get<T>();
        }

        public static PropertyCache GetProps(Type t) {
            return PropertyCache.Get(t);
        }

        public static IEnumerable<T> ShallowCopyRange<T>(IEnumerable<T> originalItems) where T : class, new() {
            var ret = new List<T>(originalItems.Count());
            var props = Util.GetProps<T>();
            foreach (var item in originalItems) {
                ret.Add(Util.ShallowCopyItem(item, props));
            }
            return ret;
        }

        public static T ShallowCopyItem<T>(T item, PropertyCache props = null) where T : class, new() {
            if (props == null) {
                props = GetProps<T>();
            }
            var newT = new T();
            foreach (var prop in props.ValueAndStringProperties) {
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

        public static void CopyProps<T, U>(T fromT, U toU, bool ignoreCase = true, bool copyNullProperties = false) where T : class where U : class {
            var fromProps = Util.GetProps<T>();
            var toProps = Util.GetProps<U>();

            foreach (var toProp in toProps.ValueAndStringProperties) {
                var fromProp = fromProps.ValueAndStringProperties.FirstOrDefault(x => string.Compare(x.Name, toProp.Name, true) == 0);
                if (fromProp == null) {
                    continue;
                }

                var fromValue = fromProp.GetVal(fromT);
                if (!copyNullProperties && object.Equals(fromValue, null)) {
                    continue;
                }

                toProp.SetVal(toU, fromValue);
            }
        }
    }
}
