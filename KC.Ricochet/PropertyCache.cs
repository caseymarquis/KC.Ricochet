using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace KC.Ricochet
{

    public class PropertyCache
    {
        private static object lockCaches = new object();
        private static Dictionary<Type, PropertyCache> caches = new Dictionary<Type, PropertyCache>();

        public List<PropertyAccessor> AllProperties = new List<PropertyAccessor>();

        public List<PropertyAccessor> ValueAndStringProperties = new List<PropertyAccessor>();
        public List<PropertyAccessor> ValueAndStringIEnumerables = new List<PropertyAccessor>();
        public List<PropertyAccessor> ValueAndStringDicts = new List<PropertyAccessor>();

        public List<PropertyAccessor> ClassProperties = new List<PropertyAccessor>();
        public List<PropertyAccessor> ClassIEnumerables = new List<PropertyAccessor>();
        public List<PropertyAccessor> ClassDicts = new List<PropertyAccessor>();

        public static PropertyCache Get<T>() {
            return Get(typeof(T));
        }

        public static PropertyCache Get(Type classType)
        {
            PropertyCache ret = null;
            lock (lockCaches)
            {
                if (!caches.TryGetValue(classType, out ret))
                {
                    ret = new PropertyCache(classType);
                    caches[classType] = ret;
                }
                return ret;
            }
        }

        public PropertyCache(Type classType)
        {
            var allPropInfo = classType.GetTypeInfo().GetAllProperties().Where(x => x.CanRead && x.CanWrite);
            
            //object obj
            var objectParameterExpr = Expression.Parameter(typeof(object), "obj");
            //object obj => (classType)obj
            var typeCastParameterExpr = Expression.Convert(objectParameterExpr, classType);
            foreach (var propertyInfo in allPropInfo)
            {
                var ignoreAttrs = propertyInfo.GetCustomAttributes()
                    .Where(x =>
                    {
                        var t = x.GetType();
                        return t == typeof(RicochetIgnore) || t.GetTypeInfo().IsSubclassOf(typeof(RicochetIgnore));
                    });
                if (ignoreAttrs.Count() > 0)
                {
                    continue;
                }

                var tProperty = propertyInfo.PropertyType;

                //object obj => ((classType)obj).PropertyName
                var propertyExpr = Expression.Property(typeCastParameterExpr, propertyInfo.Name);

                //object newValue
                var valueExpr = Expression.Parameter(typeof(object), "newValue");

                //object newValue => (PropertyType)newValue
                var valueCast = Expression.Convert(valueExpr, propertyExpr.Type);

                //(object obj, object newValue) => ((classType)obj).PropertyName = (PropertyType)newValue 
                var assignExpr = Expression.Assign(propertyExpr, valueCast);

                //object obj => (object) (((classType)obj).PropertyName)
                var convertedExpr = Expression.Convert(propertyExpr, typeof(object));

                var getExpr = Expression.Lambda<Func<object, object>>(convertedExpr, objectParameterExpr);
                var setExpr = Expression.Lambda<Action<object, object>>(assignExpr, objectParameterExpr, valueExpr);

                var getFunc = getExpr.Compile();
                var setFunc = setExpr.Compile();

                var newProp = new PropertyAccessor
                {
                    PropertyInfo = propertyInfo,
                    TypeInfo = propertyInfo.PropertyType.GetTypeInfo(),
                    m_Get_From = getExpr.Compile(),
                    m_Set_On_To = setExpr.Compile()
                };

                var markAttrs = propertyInfo.GetCustomAttributes()
                    .Where(x => 
                        {
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
                
                AllProperties.Add(newProp);
            }

            foreach (var prop in AllProperties)
            {
                var pType = prop.PropertyInfo.PropertyType;
                if (pType == typeof(string) || prop.TypeInfo.IsValueType)
                {
                    //String is also enumerable, so it's best to do this first.
                    this.ValueAndStringProperties.Add(prop);
                    if (pType == typeof(string))
                    {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tString;
                    }
                    else if (pType == typeof(int))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tInt;
                    }
                    else if (pType == typeof(long))
                    {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tLong;
                    }
                    else if (pType == typeof(float))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tFloat;
                    }
                    else if (pType == typeof(double))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDouble;
                    }
                    else if (pType == typeof(bool))
                    {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tBool;
                    }
                    else if (pType == typeof(decimal))
                    {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tDecimal;
                    }
                    else if (pType == typeof(DateTime))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDateTime;
                    }
                    else if (pType == typeof(DateTimeOffset))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tDateTimeOffset;
                    }
                    else if (pType == typeof(TimeSpan))
                    {
                        prop.IsStringConvertible = true;
                        prop.IsDoubleConvertible = true;
                        prop.ValueType = StringConvertibleType.tTimeSpan;
                    }
                    else if (pType == typeof(Guid)) {
                        prop.IsStringConvertible = true;
                        prop.ValueType = StringConvertibleType.tGuid;
                    }

                }
                else
                {
                    //Not a string or value type.
                    //Covers every type of collection we care about supporting.
                    var propIsEnumerable = prop.TypeInfo.ImplementedInterfaces.Where(x => x == typeof(IEnumerable)).FirstOrDefault() != null;
                    if (propIsEnumerable)
                    {
                        //We're some sort of collection.
                        //You'll notice below that we only support IEnumerables with
                        //one generic argument and dictionaries. No current interest
                        //in expanding that.
                        if (prop.TypeInfo.IsGenericType)
                        {
                            var genericArgs = prop.TypeInfo.GenericTypeArguments;
                            if (genericArgs.Length == 1)
                            {
                                var arg0 = genericArgs[0];
                                if (arg0.GetTypeInfo().IsValueType || arg0 == typeof(string))
                                {
                                    this.ValueAndStringIEnumerables.Add(prop);
                                }
                                else
                                {
                                    this.ClassIEnumerables.Add(prop);
                                }
                            }
                            else if (genericArgs.Length == 2)
                            {
                                if (pType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                                {
                                    var valType = genericArgs[1];
                                    if (valType.GetTypeInfo().IsValueType || valType == typeof(string))
                                    {
                                        this.ValueAndStringDicts.Add(prop);
                                    }
                                    else
                                    {
                                        this.ClassDicts.Add(prop);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //We assume we're a class.
                        ClassProperties.Add(prop);
                    }
                }
            }
        }
        
    }
}
