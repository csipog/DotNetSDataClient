﻿// Copyright (c) 1997-2014, SalesLogix NA, LLC. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Saleslogix.SData.Client.Utilities;
using SimpleJson;
using SimpleJson.Reflection;

#if !NET_2_0
using System.Runtime.Serialization;
#endif

namespace Saleslogix.SData.Client.Content
{
    public static class ContentHelper
    {
        private static readonly IDictionary<Type, IDictionary<SDataProtocolProperty, ReflectionUtils.GetDelegate>> _getProtocolValueCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<SDataProtocolProperty, ReflectionUtils.GetDelegate>>(ProtocolValueGetterFactory);
        private static readonly IDictionary<Type, IDictionary<SDataProtocolProperty, ReflectionUtils.SetDelegate>> _setProtocolValueCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<SDataProtocolProperty, ReflectionUtils.SetDelegate>>(ProtocolValueSetterFactory);

        public static object Serialize(object value, INamingScheme namingScheme = null)
        {
            object result;
            new SerializerStrategy(namingScheme ?? NamingScheme.Default).TrySerializeNonPrimitiveObject(value, out result);
            return result;
        }

        public static T Deserialize<T>(object value, INamingScheme namingScheme = null)
        {
            return (T) new SerializerStrategy(namingScheme ?? NamingScheme.Default).DeserializeObject(value, typeof (T));
        }

        public static IEnumerable<IDictionary<string, object>> AsDictionaries(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var generic = obj as IEnumerable<IDictionary<string, object>>;
            if (generic != null)
            {
                return generic;
            }

            var nonGeneric = obj as IEnumerable;
            if (nonGeneric != null &&
                obj.GetType()
                    .GetInterfaces()
                    .Any(item => item.GetTypeInfo().IsGenericType &&
                                 item.GetGenericTypeDefinition() == typeof (IEnumerable<>) &&
                                 typeof (IDictionary<string, object>).IsAssignableFrom(item.GetGenericArguments()[0])))
            {
                generic = nonGeneric.Cast<IDictionary<string, object>>();
            }

            var items = AsCollection(obj);
            if (items != null)
            {
                var dictionaries = items.Select(AsDictionary).ToList();
                if (dictionaries.Count > 0 && dictionaries.All(item => item != null))
                {
                    generic = dictionaries;
                }
            }

            if (generic != null)
            {
                var prot = obj as ISDataProtocolObject;
                if (prot != null)
                {
                    generic = new SDataCollection<IDictionary<string, object>>(generic);
                    ((ISDataProtocolObject) generic).Info = prot.Info;
                }

                return generic;
            }

            return null;
        }

        public static IDictionary<string, object> AsDictionary(object obj)
        {
            if (obj != null)
            {
                var generic = obj as IDictionary<string, object>;
                if (generic != null)
                {
                    return generic;
                }

                var nonGeneric = obj as IDictionary;
                if (nonGeneric != null)
                {
                    generic = nonGeneric.Cast<DictionaryEntry>()
                        .ToDictionary(entry => entry.Key.ToString(), entry => entry.Value);
                }
                else
                {
                    var entries = obj as IEnumerable;
                    if (entries != null)
                    {
                        var iface = obj.GetType()
                            .GetInterfaces()
                            .FirstOrDefault(item => item.GetTypeInfo().IsGenericType && item.GetGenericTypeDefinition() == typeof (IDictionary<,>));
                        if (iface != null)
                        {
                            var keyValueType = iface.GetInterfaces()
                                .First(a => a.GetTypeInfo().IsGenericType && a.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                                .GetGenericArguments()[0];
                            var keyProp = keyValueType.GetProperty("Key");
                            var valueProp = keyValueType.GetProperty("Value");
                            generic = entries.Cast<object>()
                                .ToDictionary(item => keyProp.GetValue(item, null).ToString(),
                                    item => valueProp.GetValue(item, null));
                        }
                    }
                }

                if (generic != null)
                {
                    var prot = obj as ISDataProtocolObject;
                    if (prot != null)
                    {
                        generic = new SDataResource(generic);
                        ((ISDataProtocolObject) generic).Info = prot.Info;
                    }

                    return generic;
                }
            }

            return null;
        }

        public static IEnumerable<object> AsCollection(object obj)
        {
            if (obj != null && !(obj is string) && !IsDictionary(obj))
            {
                var generic = obj as IEnumerable<object>;
                if (generic != null)
                {
                    return generic;
                }

                var nonGeneric = obj as IEnumerable;
                if (nonGeneric != null)
                {
                    generic = nonGeneric.Cast<object>();

                    var prot = obj as ISDataProtocolObject;
                    if (prot != null)
                    {
                        generic = new SDataCollection<object>(generic);
                        ((ISDataProtocolObject) generic).Info = prot.Info;
                    }
                    else
                    {
                        generic = generic.ToList();
                    }

                    return generic;
                }
            }

            return null;
        }

        public static object ToTypedCollection(IEnumerable<object> items)
        {
            var list = items.ToList();
            var itemTypes = list.Select(result => result.GetType()).Distinct().ToList();
            if (itemTypes.Count == 1)
            {
                var itemType = itemTypes[0];
                var type = typeof (SDataCollection<>).MakeGenericType(itemType);
                var ctor = type.GetConstructor(new[] {typeof (IEnumerable<>).MakeGenericType(itemType)});
                var array = Array.CreateInstance(itemType, list.Count);
                list.ToArray().CopyTo(array, 0);
                return ctor.Invoke(new object[] {array});
            }

            return new SDataCollection<object>(list);
        }

        public static bool IsDictionary(object obj)
        {
            return obj != null &&
                   (obj is IDictionary ||
                    obj is IDictionary<string, object> ||
                    obj.GetType().GetInterfaces().Any(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IDictionary<,>)));
        }

        public static bool IsCollection(object obj)
        {
            return obj != null &&
                   !(obj is string) &&
                   (obj is IEnumerable ||
                    obj.GetType().GetInterfaces().Any(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IEnumerable<>)));
        }

        public static bool IsObject(object obj)
        {
            return obj != null && IsObject(obj.GetType());
        }

        public static bool IsObject(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return !type.GetTypeInfo().IsPrimitive &&
                   !type.GetTypeInfo().IsEnum &&
                   !new[]
                       {
                           typeof (decimal),
                           typeof (string),
                           typeof (DateTime),
                           typeof (DateTimeOffset),
                           typeof (TimeSpan),
                           typeof (Guid),
                           typeof (Version),
                           typeof (Uri)
                       }.Contains(type);
        }

        public static T GetProtocolValue<T>(object obj, SDataProtocolProperty prop)
        {
            Guard.ArgumentNotNull(obj, "obj");

            var prot = obj as ISDataProtocolObject;
            if (prot != null)
            {
                return (T) prot.Info.GetValue(prop);
            }

            var getters = _getProtocolValueCache[obj.GetType()];
            ReflectionUtils.GetDelegate getter;
            return getters.TryGetValue(prop, out getter) ? (T) getter(obj) : default(T);
        }

        public static bool SetProtocolValue(object obj, SDataProtocolProperty prop, object value)
        {
            Guard.ArgumentNotNull(obj, "obj");

            var prot = obj as ISDataProtocolObject;
            if (prot != null)
            {
                prot.Info.SetValue(prop, value);
                return true;
            }

            var setters = _setProtocolValueCache[obj.GetType()];
            ReflectionUtils.SetDelegate setter;
            if (setters.TryGetValue(prop, out setter))
            {
                setter(obj, value);
                return true;
            }

            return false;
        }

        private static IDictionary<SDataProtocolProperty, ReflectionUtils.GetDelegate> ProtocolValueGetterFactory(Type type)
        {
            var result = new Dictionary<SDataProtocolProperty, ReflectionUtils.GetDelegate>();
            foreach (var propertyInfo in ReflectionUtils.GetProperties(type).Where(info => info.CanRead))
            {
                var getMethod = ReflectionUtils.GetGetterMethodInfo(propertyInfo);
                if (getMethod.IsStatic || !getMethod.IsPublic)
                {
                    continue;
                }
                var prop = GetProtocolProperty(propertyInfo);
                if (prop != null)
                {
                    result[prop.Value] = ReflectionUtils.GetGetMethod(propertyInfo);
                }
            }
            foreach (var fieldInfo in ReflectionUtils.GetFields(type).Where(info => !info.IsStatic && info.IsPublic))
            {
                var prop = GetProtocolProperty(fieldInfo);
                if (prop != null)
                {
                    result[prop.Value] = ReflectionUtils.GetGetMethod(fieldInfo);
                }
            }
            return result;
        }

        private static IDictionary<SDataProtocolProperty, ReflectionUtils.SetDelegate> ProtocolValueSetterFactory(Type type)
        {
            var result = new Dictionary<SDataProtocolProperty, ReflectionUtils.SetDelegate>();
            foreach (var propertyInfo in ReflectionUtils.GetProperties(type).Where(info => info.CanWrite))
            {
                var getMethod = ReflectionUtils.GetGetterMethodInfo(propertyInfo);
                if (getMethod.IsStatic || !getMethod.IsPublic)
                {
                    continue;
                }
                var prop = GetProtocolProperty(propertyInfo);
                if (prop != null)
                {
                    result[prop.Value] = ReflectionUtils.GetSetMethod(propertyInfo);
                }
            }
            foreach (var fieldInfo in ReflectionUtils.GetFields(type).Where(info => !info.IsInitOnly && !info.IsStatic && info.IsPublic))
            {
                var prop = GetProtocolProperty(fieldInfo);
                if (prop != null)
                {
                    result[prop.Value] = ReflectionUtils.GetSetMethod(fieldInfo);
                }
            }
            return result;
        }

        private static SDataProtocolProperty? GetProtocolProperty(MemberInfo memberInfo)
        {
            var attr = memberInfo.GetCustomAttribute<SDataProtocolPropertyAttribute>();
            if (attr == null)
            {
                return null;
            }
            if (attr.Value != null)
            {
                return attr.Value.Value;
            }

#if NET_2_0 || NET_3_5
            try
            {
                return (SDataProtocolProperty) Enum.Parse(typeof (SDataProtocolProperty), memberInfo.Name);
            }
            catch (ArgumentException)
            {
                return null;
            }
#else
            SDataProtocolProperty prop;
            return Enum.TryParse(memberInfo.Name, out prop) ? prop : (SDataProtocolProperty?) null;
#endif
        }

        #region Nested type: SerializerStrategy

        private class SerializerStrategy : PocoJsonSerializerStrategy
        {
            private readonly INamingScheme _namingScheme;
            private readonly IDictionary<Type, KeyValuePair<ParameterInfo[], ReflectionUtils.ConstructorDelegate>> _ctorCache;

            public SerializerStrategy(INamingScheme namingScheme)
            {
                _namingScheme = namingScheme;
                _ctorCache = new ReflectionUtils.ThreadSafeDictionary<Type, KeyValuePair<ParameterInfo[], ReflectionUtils.ConstructorDelegate>>(ConstructorFactory);
            }

            public override bool TrySerializeNonPrimitiveObject(object input, out object output)
            {
                if (!IsObject(input))
                {
                    output = input;
                    return true;
                }

                var items = AsCollection(input);
                if (items != null)
                {
                    return TrySerializeCollection(items, out output);
                }

                var dict = AsDictionary(input);
                if (dict == null)
                {
                    if (!base.TrySerializeNonPrimitiveObject(input, out output))
                    {
                        output = null;
                        return false;
                    }
                    dict = (IDictionary<string, object>) output;
                }

                var prot = input as ISDataProtocolObject;
                var info = prot != null ? prot.Info : null;
                var infoCreated = info == null;
                if (infoCreated)
                {
                    info = new SDataProtocolInfo();
                    var type = input.GetType();
#if !NET_2_0
                    var dataAttr = type.GetTypeInfo().GetCustomAttribute<DataContractAttribute>();
                    if (dataAttr != null)
                    {
                        info.XmlLocalName = dataAttr.Name;
                        info.XmlNamespace = dataAttr.Namespace;
                    }
                    else
#endif
                    {
                        var xmlAttr = type.GetTypeInfo().GetCustomAttribute<XmlTypeAttribute>();
                        if (xmlAttr != null)
                        {
                            info.XmlLocalName = xmlAttr.TypeName;
                            info.XmlNamespace = xmlAttr.Namespace;
                        }
                    }
                    if (string.IsNullOrEmpty(info.XmlLocalName))
                    {
                        info.XmlLocalName = type.Name;
                    }
                }

                foreach (var item in dict.ToList())
                {
                    var value = item.Value;
                    var surrogate = value as MetadataSurrogate;
                    if (surrogate != null)
                    {
                        value = surrogate.Value;
                    }

                    if (item.Key.StartsWith("$", StringComparison.Ordinal))
                    {
                        if (infoCreated)
                        {
                            var name = item.Key;
                            var prop = (SDataProtocolProperty) Enum.Parse(typeof (SDataProtocolProperty), char.ToUpperInvariant(name[1]) + name.Substring(2), false);
                            info.SetValue(prop, value);
                        }
                        dict.Remove(item.Key);
                        continue;
                    }

                    object result;
                    if (!TrySerializeNonPrimitiveObject(value, out result))
                    {
                        output = null;
                        return false;
                    }

                    if (surrogate != null && result != null)
                    {
                        var itemInfo = ((ISDataProtocolObject) result).Info;
                        if (surrogate.XmlLocalName != null)
                        {
                            itemInfo.XmlLocalName = surrogate.XmlLocalName;
                        }
                        if (surrogate.XmlNamespace != null)
                        {
                            itemInfo.XmlNamespace = surrogate.XmlNamespace;
                        }
                        if (surrogate.XmlIsFlat)
                        {
                            itemInfo.XmlIsFlat = surrogate.XmlIsFlat;
                        }
                        if (surrogate.JsonIsSimpleArray)
                        {
                            itemInfo.JsonIsSimpleArray = surrogate.JsonIsSimpleArray;
                        }
                    }

                    dict[item.Key] = result;
                }

                var resource = new SDataResource(dict);
                ((ISDataProtocolObject) resource).Info = info;
                output = resource;
                return true;
            }

            private bool TrySerializeCollection(IEnumerable<object> input, out object output)
            {
                var results = new List<object>();
                foreach (var item in input)
                {
                    object result;
                    if (!TrySerializeNonPrimitiveObject(item, out result))
                    {
                        output = null;
                        return false;
                    }
                    results.Add(result);
                }

                if (results.Count == 0)
                {
                    var itemType = input.GetType().GetInterfaces()
                                       .Where(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                                       .Select(iface => iface.GetGenericArguments()[0])
                                       .FirstOrDefault() ?? typeof (object);
                    if (itemType != typeof (object) && IsObject(itemType))
                    {
                        itemType = typeof (SDataResource);
                    }
                    output = Activator.CreateInstance(typeof (SDataCollection<>).MakeGenericType(itemType));
                }
                else
                {
                    output = ToTypedCollection(results);
                }

                var prot = input as ISDataProtocolObject;
                if (prot != null)
                {
                    ((ISDataProtocolObject) output).Info = prot.Info;
                }

                return true;
            }

            public override object DeserializeObject(object value, Type type)
            {
                if (value == null)
                {
                    return type.GetTypeInfo().IsValueType && !ReflectionUtils.IsNullableType(type)
                        ? Activator.CreateInstance(type)
                        : null;
                }

                type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.IsInstanceOfType(value))
                {
                    return value;
                }

                if (!IsObject(value))
                {
                    if (type == typeof (DateTime) || type == typeof (DateTimeOffset))
                    {
                        if (value is DateTime)
                        {
                            return (DateTimeOffset) (DateTime) value;
                        }
                        if (value is DateTimeOffset)
                        {
                            return ((DateTimeOffset) value).DateTime;
                        }
                    }
                    else if (!IsObject(type))
                    {
                        var str = value as string;
                        if (str != null)
                        {
                            if (type.GetTypeInfo().IsEnum)
                            {
                                return EnumEx.Parse(type, str);
                            }
                            if (type == typeof (TimeSpan))
                            {
#if NET_2_0 || NET_3_5
                                return TimeSpan.Parse(str);
#else
                                return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
#endif
                            }
                            if (type == typeof (Guid))
                            {
                                return new Guid(str);
                            }
                            if (type == typeof (Version))
                            {
                                return new Version(str);
                            }
                            if (type == typeof (Uri))
                            {
                                return new Uri(str);
                            }
                        }
                        else if (type.GetTypeInfo().IsEnum)
                        {
                            return Enum.ToObject(type, value);
                        }

                        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                    }

                    return base.DeserializeObject(value, type);
                }

                var dict = AsDictionary(value);

                // workaround: This can happen when type inference gets it wrong
                if (dict != null &&
                    (typeof (IEnumerable).IsAssignableFrom(type) ||
                     type.GetInterfaces().Any(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IEnumerable<>))))
                {
                    value = new object[0];
                    dict = null;
                }

                var prot = value as ISDataProtocolObject;

                if (prot != null && dict != null)
                {
                    foreach (SDataProtocolProperty protocolProp in Enum.GetValues(typeof (SDataProtocolProperty)))
                    {
                        var name = protocolProp.ToString();
                        dict[string.Format("${0}{1}", char.ToLowerInvariant(name[0]), name.Substring(1))] = prot.Info.GetValue(protocolProp);
                    }
                }

                // workaround: SimpleJson only recognizes IList<object>
                var items = AsCollection(value);
                if (items != null && !(value is IList<object>))
                {
                    value = items.ToList();
                }

                if (dict != null)
                {
                    var ctor = _ctorCache[type];
                    if (ctor.Key != null)
                    {
                        object arg;
                        return ctor.Value(ctor.Key.Select(param => DeserializeObject(dict.TryGetValue(param.Name, out arg) ? arg : null, param.ParameterType)).ToArray());
                    }
                }

                var result = base.DeserializeObject(value, type);

                if (prot != null)
                {
                    var resultProt = result as ISDataProtocolObject;
                    if (resultProt == null && items != null)
                    {
                        var itemType = type.GetTypeInfo().IsGenericType ? type.GetGenericArguments()[0] : typeof (object);
                        var colType = typeof (SDataCollection<>).MakeGenericType(itemType);
                        if (type.IsAssignableFrom(colType))
                        {
                            result = Activator.CreateInstance(colType, new[] {result});
                            resultProt = (ISDataProtocolObject) result;
                        }
                    }
                    if (resultProt != null)
                    {
                        resultProt.Info = prot.Info;
                    }
                }

                return result;
            }

            internal override ReflectionUtils.ConstructorDelegate ContructorDelegateFactory(Type key)
            {
                if (!key.IsArray && !key.GetTypeInfo().IsInterface && ReflectionUtils.IsAssignableFrom(typeof (IList), key))
                {
                    ReflectionUtils.ConstructorDelegate ctor = ReflectionUtils.GetContructor(key, ArrayConstructorParameterTypes);
                    if (ctor != null)
                    {
                        return ctor;
                    }
#if NET_2_0
                    ctor = base.ContructorDelegateFactory(key);
                    return args => ctor(new object[0]);
#endif
                }
                return base.ContructorDelegateFactory(key);
            }

            internal override IDictionary<string, ReflectionUtils.GetDelegate> GetterValueFactory(Type type)
            {
                var result = new Dictionary<string, ReflectionUtils.GetDelegate>();
                foreach (var propertyInfo in GetProperties(type, prop => prop.CanRead))
                {
                    result[_namingScheme.GetName(propertyInfo)] = GetGetter(ReflectionUtils.GetGetMethod(propertyInfo), propertyInfo, propertyInfo.PropertyType);
                }
                foreach (var fieldInfo in GetFields(type))
                {
                    result[_namingScheme.GetName(fieldInfo)] = GetGetter(ReflectionUtils.GetGetMethod(fieldInfo), fieldInfo, fieldInfo.FieldType);
                }

                return result;
            }

            private static ReflectionUtils.GetDelegate GetGetter(ReflectionUtils.GetDelegate baseGetter, MemberInfo memberInfo, Type memberType)
            {
                if (IsObject(memberType))
                {
                    string xmlLocalName = null;
                    string xmlNamespace = null;
                    var xmlIsFlat = false;

                    var elementAttr = memberInfo.GetCustomAttribute<XmlElementAttribute>();
                    if (elementAttr != null)
                    {
                        xmlLocalName = elementAttr.ElementName;
                        xmlNamespace = elementAttr.Namespace;
                        xmlIsFlat = memberType != typeof (string) &&
                                    (typeof (IEnumerable).IsAssignableFrom(memberType) ||
                                     memberType.GetInterfaces().Any(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IEnumerable<>)));
                    }

                    var itemAttr = memberInfo.GetCustomAttribute<XmlArrayItemAttribute>();
                    if (itemAttr != null)
                    {
                        xmlLocalName = itemAttr.ElementName;
                        xmlNamespace = itemAttr.Namespace;
                    }

                    if (xmlLocalName == null)
                    {
                        var itemType = memberType.IsArray
                            ? memberType.GetElementType()
                            : memberType.GetInterfaces()
                                .Where(iface => iface.GetTypeInfo().IsGenericType && iface.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                                .Select(iface => iface.GetGenericArguments()[0])
                                .FirstOrDefault();
                        xmlLocalName = itemType != null && itemType != typeof (object) ? itemType.Name : null;
                        xmlNamespace = null;
                    }

                    var jsonIsSimpleArray = memberInfo.GetCustomAttribute<JsonSimpleArrayAttribute>() != null;

                    if (xmlLocalName != null || xmlNamespace != null || xmlIsFlat || jsonIsSimpleArray)
                    {
                        return source => new MetadataSurrogate
                            {
                                Value = baseGetter(source),
                                XmlLocalName = xmlLocalName,
                                XmlNamespace = xmlNamespace,
                                XmlIsFlat = xmlIsFlat,
                                JsonIsSimpleArray = jsonIsSimpleArray
                            };
                    }
                }

                return baseGetter;
            }

            internal override IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> SetterValueFactory(Type type)
            {
                var result = new Dictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>();
                foreach (var propertyInfo in GetProperties(type, prop => prop.CanWrite))
                {
                    var setter = ReflectionUtils.GetSetMethod(propertyInfo);
                    result[_namingScheme.GetName(propertyInfo)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(propertyInfo.PropertyType, setter);
                }
                foreach (var fieldInfo in GetFields(type, field => !field.IsInitOnly))
                {
                    var setter = ReflectionUtils.GetSetMethod(fieldInfo);
                    result[_namingScheme.GetName(fieldInfo)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(fieldInfo.FieldType, setter);
                }

                return result;
            }

            private static IEnumerable<PropertyInfo> GetProperties(Type type, Func<PropertyInfo, bool> predicate)
            {
                return ReflectionUtils.GetProperties(type)
                    .Where(item => predicate(item) &&
                                   item.GetIndexParameters().Length == 0 &&
#if !NET_2_0
                        !item.IsDefined(typeof (IgnoreDataMemberAttribute)) &&
#endif
                        !item.IsDefined(typeof (XmlIgnoreAttribute)))
                    .Where(item =>
                    {
                        var getMethod = ReflectionUtils.GetGetterMethodInfo(item);
                        return !getMethod.IsStatic && getMethod.IsPublic;
                    });
            }

            private static IEnumerable<FieldInfo> GetFields(Type type, Func<FieldInfo, bool> predicate = null)
            {
                return ReflectionUtils.GetFields(type)
                    .Where(item => (predicate == null || predicate(item)) &&
                                   !item.IsStatic && item.IsPublic &&
#if !NET_2_0
                        !item.IsDefined(typeof (IgnoreDataMemberAttribute)) &&
#endif
                        !item.IsDefined(typeof (XmlIgnoreAttribute)));
            }

            private static KeyValuePair<ParameterInfo[], ReflectionUtils.ConstructorDelegate> ConstructorFactory(Type key)
            {
                var ctors = key.GetConstructors();
                if (ctors.Length == 1)
                {
                    var ctor = ctors[0];
                    var parms = ctor.GetParameters();
                    if (parms.Length > 0)
                    {
                        return new KeyValuePair<ParameterInfo[], ReflectionUtils.ConstructorDelegate>(parms, ReflectionUtils.GetContructor(ctor));
                    }
                }
                return default(KeyValuePair<ParameterInfo[], ReflectionUtils.ConstructorDelegate>);
            }
        }

        #endregion

        #region Nested type: MetadataSurrogate

        private class MetadataSurrogate
        {
            public object Value { get; set; }
            public string XmlLocalName { get; set; }
            public string XmlNamespace { get; set; }
            public bool XmlIsFlat { get; set; }
            public bool JsonIsSimpleArray { get; set; }
        }

        #endregion
    }
}