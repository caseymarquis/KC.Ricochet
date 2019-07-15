using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Reflection;

namespace KC.Ricochet
{
    public class PropertyAndFieldAccessor
    {
        private static DateTime EpochDateTime = new DateTime(1970, 1, 1);
        private static DateTimeOffset EpochDateTimeOffset = new DateTimeOffset(EpochDateTime, TimeSpan.Zero);

        public Type Type { get; set; }
        public TypeInfo TypeInfo { get; set; }
        public MemberInfo MemberInfo { get; set; }

        /// <summary>
        /// If RicochetMark is used, then any markers on
        /// properties will be stored here.
        /// </summary>
        public string[] Markers = new string[] { };

        public string Name
        {
            get { return MemberInfo.Name; }
        }

        public bool IsStringConvertible { get; internal set; }
        public bool IsDoubleConvertible { get; internal set; }

        public bool IsPublic { get; internal set; }

        public bool IsProperty { get; internal set; }
        public bool IsField { get; internal set; }

        public bool IsValueOrString { get; internal set; }
        public bool IsIEnumberableOfValueOrString { get; internal set; }
        public bool IsDictionaryOfValueOrString { get; internal set; }

        public bool IsClass { get; internal set; }
        public bool IsIEnumberableOfClass { get; internal set; }
        public bool IsDictionaryOfClass { get; internal set; }

        internal StringConvertibleType ValueType { get; set; }

        public string GetValAsString(object from) {
            if (!IsStringConvertible) {
                throw new Exception("Type is not string convertible.");
            }
            var obj = this.GetVal(from);
            if (ValueType == StringConvertibleType.tString) {
                return (string)obj;
            }
            return obj.ToString();
        }

        public void SetValFromString(object on, string to) {
            if (!IsStringConvertible) {
                throw new Exception("Type is not string convertible.");
            }
            object val = null;
            switch (this.ValueType) {
                case StringConvertibleType.tBool:
                    val = bool.Parse(to);
                    break;
                case StringConvertibleType.tDateTime:
                    val = DateTime.Parse(to);
                    break;
                case StringConvertibleType.tDateTimeOffset:
                    val = DateTimeOffset.Parse(to);
                    break;
                case StringConvertibleType.tDecimal:
                    val = decimal.Parse(to);
                    break;
                case StringConvertibleType.tDouble:
                    val = double.Parse(to);
                    break;
                case StringConvertibleType.tFloat:
                    val = float.Parse(to);
                    break;
                case StringConvertibleType.tInt:
                    val = int.Parse(to);
                    break;
                case StringConvertibleType.tLong:
                    val = long.Parse(to);
                    break;
                case StringConvertibleType.tString:
                    val = to;
                    break;
                case StringConvertibleType.tGuid:
                    val = Guid.Parse(to);
                    break;
                case StringConvertibleType.tTimeSpan:
                    val = TimeSpan.Parse(to);
                    break;
            }
            this.SetVal(on, val);
        }

        public double GetValAsDouble(object from)
        {
            if (!IsDoubleConvertible)
            {
                throw new Exception("Type is not double convertible.");
            }
            var obj = this.GetVal(from);
            switch (this.ValueType)
            {
                case StringConvertibleType.tDateTime:
                    {
                        var val = (DateTime)obj;
                        return (val - EpochDateTime).TotalMilliseconds;
                    }
                case StringConvertibleType.tDateTimeOffset:
                    {
                        var val = (DateTimeOffset)obj;
                        return (val - EpochDateTimeOffset).TotalMilliseconds;
                    }
                case StringConvertibleType.tDouble:
                    return (double)obj;
                case StringConvertibleType.tFloat:
                    return (double)((float)obj);
                case StringConvertibleType.tInt:
                    return (double)((int)obj);
                case StringConvertibleType.tTimeSpan:
                    return ((TimeSpan)obj).TotalMilliseconds;
            }
            throw new NotImplementedException("Type was double convertible, but not covered by GetValAsDoubleFunction. Report issue on github.");
        }

        public void SetValFromDouble(object on, double to)
        {
            if (!IsDoubleConvertible)
            {
                throw new Exception("Type is not double convertible.");
            }

            object val = null;
            switch (this.ValueType)
            {
                case StringConvertibleType.tDateTime:
                    val = EpochDateTime + TimeSpan.FromMilliseconds(to);
                    break;
                case StringConvertibleType.tDateTimeOffset:
                    val = EpochDateTimeOffset + TimeSpan.FromMilliseconds(to);
                    break;
                case StringConvertibleType.tDouble:
                    val = to;
                    break;
                case StringConvertibleType.tFloat:
                    val = (float)to;
                    break;
                case StringConvertibleType.tInt:
                    val = (int)to;
                    break;
                case StringConvertibleType.tTimeSpan:
                    val = TimeSpan.FromMilliseconds(to);
                    break;
            }
            this.SetVal(on, val);
        }

        internal Func<object, object> m_Get_From;
        public object GetVal(object from)
        {
            return m_Get_From(from);
        }

        internal Action<object, object> m_Set_On_To;
        public void SetVal(object on, object to)
        {
            m_Set_On_To(on, to);
        }

        public bool IsEqual(object a, object b)
        {
            return object.Equals(this.GetVal(a), this.GetVal(b));
        }

        public override string ToString()
        {
            return MemberInfo.Name;
        }

        public void Copy(object _from, object _to)
        {
            var newValue = this.GetVal(_from);
            this.SetVal(_to, newValue);
        }

        private object defaultVal;
        private bool defaultValIsSet;
        public void SetDefault(object _on)
        {
            if (!defaultValIsSet)
            {
                if (this.TypeInfo.IsValueType)
                {
                    defaultVal = Activator.CreateInstance(Type);
                }
                else
                {
                    defaultVal = null;
                }
                defaultValIsSet = true;
            }
            m_Set_On_To(_on, defaultVal);
        }

    }

}

