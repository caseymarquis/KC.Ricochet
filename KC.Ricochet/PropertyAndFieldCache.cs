using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;

namespace KC.Ricochet {

    public class PropertyAndFieldCache {
        private static object lockCaches = new object();
        private static Dictionary<Type, PropertyAndFieldCache> caches = new Dictionary<Type, PropertyAndFieldCache>();

        public IEnumerable<PropertyAndFieldAccessor> Members => m_PropertiesAndFields;
        private List<PropertyAndFieldAccessor> m_PropertiesAndFields = new List<PropertyAndFieldAccessor>();

        public static IEnumerable<PropertyAndFieldAccessor> Get<T>(Func<PropertyAndFieldAccessor, bool> predicate = null) {
            return Get(typeof(T), predicate);
        }

        public static IEnumerable<PropertyAndFieldAccessor> Get(Type classType, Func<PropertyAndFieldAccessor, bool> predicate = null) {
            PropertyAndFieldCache ret = null;
            lock (lockCaches) {
                if (!caches.TryGetValue(classType, out ret)) {
                    ret = new PropertyAndFieldCache(classType);
                    caches[classType] = ret;
                }
            }
            if (predicate == null) {
                return ret.Members;
            }
            return ret.Members.Where(predicate);
        }

        public PropertyAndFieldCache(Type classType) {
            var typeInfo = classType.GetTypeInfo();

            var flagOnAll = BindingFlags.DeclaredOnly | BindingFlags.Instance;
            var allPublicProperties = typeInfo.GetAllProperties(BindingFlags.Public | flagOnAll).Where(x => x.PropertyInfo.CanRead && x.PropertyInfo.CanWrite).ToArray();
            var allNonPublicProperties = typeInfo.GetAllProperties(BindingFlags.NonPublic | flagOnAll).Where(x => x.PropertyInfo.CanRead && x.PropertyInfo.CanWrite).ToArray();
            var allPublicFields = typeInfo.GetAllFields(BindingFlags.Public | flagOnAll).ToArray();
            var allNonPublicFields = typeInfo.GetAllFields(BindingFlags.NonPublic | flagOnAll).ToArray();

            addMembers(allPublicProperties, areProperties: true, arePublic: true);
            addMembers(allNonPublicProperties, areProperties: true, arePublic: false);
            addMembers(allPublicFields, areProperties: false, arePublic: true);
            addMembers(allNonPublicFields, areProperties: false, arePublic: false);

            void addMembers(IEnumerable<InfoWithLevel> memberInfos, bool areProperties, bool arePublic) {
                //object obj
                var objectParameterExpr = Expression.Parameter(typeof(object), "obj");
                foreach (var memberInfo in memberInfos) {
                    if (!areProperties) {
                        if (memberInfo.FieldInfo.IsInitOnly) {
                            continue;
                        }
                    }
                    try {
                        var ignoreAttrs = memberInfo.Info.GetCustomAttributes()
                            .Where(x => {
                                var t = x.GetType();
                                return t == typeof(RicochetIgnore)
                                || t.GetTypeInfo().IsSubclassOf(typeof(RicochetIgnore))
                                || t == typeof(CompilerGeneratedAttribute);
                            });
                        if (ignoreAttrs.Count() > 0) {
                            continue;
                        }

                        //object obj => (classType)obj
                        var typeCastParameterExpr = Expression.Convert(objectParameterExpr, memberInfo.Info.DeclaringType);

                        var tPropertyOrField = areProperties ? memberInfo.PropertyInfo.PropertyType : memberInfo.FieldInfo.FieldType;

                        //object obj => ((classType)obj).PropertyName
                        //NOTE: We can't use Expression.PropertyOrField as it is not case sensitive, and can confuse properties and fields with identical names.
                        var propertyOrFieldExpr = areProperties? Expression.Property(typeCastParameterExpr, memberInfo.PropertyInfo) : Expression.Field(typeCastParameterExpr, memberInfo.FieldInfo);
                        //object newValue
                        var valueExpr = Expression.Parameter(typeof(object), "newValue");

                        //object newValue => (PropertyType)newValue
                        var valueCast = Expression.Convert(valueExpr, propertyOrFieldExpr.Type);

                        //(object obj, object newValue) => ((classType)obj).PropertyName = (PropertyType)newValue 
                        var assignExpr = Expression.Assign(propertyOrFieldExpr, valueCast);

                        //object obj => (object) (((classType)obj).PropertyName)
                        var convertedExpr = Expression.Convert(propertyOrFieldExpr, typeof(object));

                        var getExpr = Expression.Lambda<Func<object, object>>(convertedExpr, objectParameterExpr);
                        var setExpr = Expression.Lambda<Action<object, object>>(assignExpr, objectParameterExpr, valueExpr);

                        var newProp = new PropertyAndFieldAccessor {
                            IsProperty = areProperties,
                            IsField = !areProperties,
                            IsPublic = arePublic,
                            Type = tPropertyOrField,
                            TypeInfo = tPropertyOrField.GetTypeInfo(),
                            MemberInfo = memberInfo.Info,
                            ClassDepth = memberInfo.Level,
                            m_Get_From = getExpr.Compile(),
                            m_Set_On_To = setExpr.Compile(),
                        };

                        var markAttrs = memberInfo.Info.GetCustomAttributes()
                            .Where(x => {
                                var t = x.GetType();
                                return t == typeof(RicochetMark) || t.GetTypeInfo().IsSubclassOf(typeof(RicochetMark));
                            });
                        foreach (var attr in markAttrs) {
                            var markAttr = attr as RicochetMark;
                            if (markAttr != null && markAttr.TextValues != null) {
                                if (markAttr.TextValues.Length > 0) {
                                    newProp.Markers = newProp.Markers.Concat(markAttr.TextValues).ToArray();
                                }
                            }

                        }

                        m_PropertiesAndFields.Add(newProp);
                    }
                    catch (Exception ex) {
                        throw new ApplicationException($"{classType.Name}.{memberInfo.Info.Name}: Failed to compile getter or setter. To proceed, mark the member with [RicochetIgnore]. Please create an issue at github on caseymarquis/KC.Ricochet if you believe you've found a solvable edge case.", ex);
                    }
                }
            }

            foreach (var prop in Members) {
                var pType = prop.Type;
                if (pType == typeof(string) || prop.TypeInfo.IsValueType) {
                    //String is also enumerable, so it's best to do this first.
                    prop.IsValueOrString = true;
                    if (pType == typeof(string)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tString;
                    }
                    else if (pType == typeof(int)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tInt;
                    }
                    else if (pType == typeof(long)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tLong;
                    }
                    else if (pType == typeof(float)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tFloat;
                    }
                    else if (pType == typeof(double)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDouble;
                    }
                    else if (pType == typeof(bool)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tBool;
                    }
                    else if (pType == typeof(decimal)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tDecimal;
                    }
                    else if (pType == typeof(DateTime)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDateTime;
                    }
                    else if (pType == typeof(DateTimeOffset)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDateTimeOffset;
                    }
                    else if (pType == typeof(TimeSpan)) {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tTimeSpan;
                    }
                    else if (pType == typeof(Guid)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tGuid;
                    }

                }
                else {
                    //Not a string or value type.
                    //Covers every type of collection we care about supporting.
                    var propIsEnumerable = prop.TypeInfo.ImplementedInterfaces.Where(x => x == typeof(IEnumerable)).FirstOrDefault() != null;
                    if (propIsEnumerable) {
                        //We're some sort of collection.
                        //You'll notice below that we only support IEnumerables with
                        //one generic argument and dictionaries. No current interest
                        //in expanding that.
                        if (prop.TypeInfo.IsGenericType) {
                            var genericArgs = prop.TypeInfo.GenericTypeArguments;
                            if (genericArgs.Length == 1) {
                                var arg0 = genericArgs[0];
                                if (arg0.GetTypeInfo().IsValueType || arg0 == typeof(string)) {
                                    prop.IsIEnumberableOfValueOrString = true;
                                }
                                else {
                                    prop.IsIEnumberableOfClass = true;
                                }
                            }
                            else if (genericArgs.Length == 2) {
                                if (pType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                                    var valType = genericArgs[1];
                                    if (valType.GetTypeInfo().IsValueType || valType == typeof(string)) {
                                        prop.IsDictionaryOfValueOrString = true;
                                    }
                                    else {
                                        prop.IsDictionaryOfClass = true;
                                    }
                                }
                            }
                        }
                    }
                    else {
                        //We assume we're a class.
                        prop.IsClass = true;
                    }
                }
            }
        }

    }
}
