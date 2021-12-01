using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace KC.Ricochet {
    public static class RicochetUtil {
        public static IEnumerable<PropertyAndFieldAccessor> GetPropsAndFields<T>(Func<PropertyAndFieldAccessor, bool> predicate = null)
            where T : class {
            return PropertyAndFieldCache.Get<T>(predicate);
        }

        public static IEnumerable<PropertyAndFieldAccessor> GetPropsAndFields(Type t, Func<PropertyAndFieldAccessor, bool> predicate = null) {
            return PropertyAndFieldCache.Get(t, predicate);
        }

        private static Func<PropertyAndFieldAccessor, bool> publicValueProps = (x) => x.IsProperty && x.IsValueOrString && x.IsPublic;
        public static IEnumerable<PropertyAndFieldAccessor> GetPublicValueProps<T>()
            where T : class {
            return GetPropsAndFields<T>(publicValueProps);
        }

        public static IEnumerable<PropertyAndFieldAccessor> GetPublicValueProps(Type t) {
            return GetPropsAndFields(t, publicValueProps);
        }

        public static IEnumerable<T> ShallowCopyRange<T>(IEnumerable<T> originalItems) where T : class, new() {
            var ret = new List<T>(originalItems.Count());
            var props = RicochetUtil.GetPropsAndFields<T>();
            foreach (var item in originalItems) {
                ret.Add(props.ShallowCopyItem(item));
            }
            return ret;
        }

        public static T ShallowCopyItem<T>(this IEnumerable<PropertyAndFieldAccessor> members, T item) where T : class, new() {
            var newT = new T();
            foreach (var prop in members) {
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

        public static T GetMember<T>(object instance, Func<PropertyAndFieldAccessor, bool> predicate = null) {
            predicate = predicate ?? (x => true);
            var props = GetPropsAndFields(instance.GetType(), x => predicate(x) && x.Type == typeof(T));
            if (!props.Any()) {
                throw new ArgumentException($"{instance.GetType().Name} does not have a member of type {typeof(T).Name} matching the predicate.");
            }
            if (props.Count() > 1) {
                throw new ArgumentException($"{instance.GetType().Name} has more than one member of type {typeof(T).Name} matching the predicate.");
            }
            return (T)props.First().GetVal(instance);
        }

        public ref struct NestedMemberSelector<T> {
            public T Result;

            public NestedMemberSelector<U> Where<U>(Func<PropertyAndFieldAccessor, bool> predicate = null) {
                predicate = predicate ?? (x => true);
                var props = RicochetUtil.GetPropsAndFields(typeof(T)).Where(x => predicate(x) && x.Type == typeof(U));
                if (!props.Any()) {
                    throw new ArgumentException($"{typeof(T).Name} does not have a member of type {typeof(U).Name} matching the predicate.");
                }
                if (props.Count() > 1) {
                    throw new ArgumentException($"{typeof(T).Name} has more than one member of type {typeof(U).Name} matching the predicate.");
                }
                var value = props.First().GetVal(Result);
                return new NestedMemberSelector<U> {
                    Result = (U)value,
                };
            }
        }

        public static NestedMemberSelector<T> GetNestedMember<T>(T instance) {
            return new NestedMemberSelector<T>() {
                Result = instance,
            };
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

        public static void CopyPublicValueProps<T, U>(T fromT, U toU, bool ignoreCase = true, bool copyNullMembers = false) where T : class where U : class {
            Copy(fromT, toU, publicValueProps, ignoreCase, copyNullMembers);
        }

        public static void Copy<T, U>(T fromT, U toU, Func<PropertyAndFieldAccessor, bool> predicate = null, bool ignoreCase = true, bool copyNullMembers = false) where T : class where U : class {
            var fromProps = RicochetUtil.GetPropsAndFields<T>();
            var toProps = RicochetUtil.GetPropsAndFields<U>();
            if (predicate != null) {
                fromProps = fromProps.Where(predicate);
                toProps = toProps.Where(predicate);
            }

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

        public static TypedInstantiator<T> GetConstructor<T>(params Type[] parameterTypes) {
            return InstantiatorCache.Get<T>(parameterTypes);
        }

        public static Instantiator GetConstructor(Type classType, params Type[] parameterTypes) {
            return InstantiatorCache.Get(classType, parameterTypes);
        }

        public static IEnumerable<TypedInstantiator<T>> GetAllConstructors<T>() {
            return InstantiatorCache.GetAll<T>();
        }

        public static IEnumerable<Instantiator> GetAllConstructors(Type classType) {
            return InstantiatorCache.GetAll(classType);
        }
    }
}
