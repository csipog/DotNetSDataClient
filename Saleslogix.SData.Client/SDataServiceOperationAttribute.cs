// Copyright (c) 1997-2014, SalesLogix NA, LLC. All rights reserved.

using System;

namespace Saleslogix.SData.Client
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SDataServiceOperationAttribute : Attribute
    {
        public string Name { get; set; }
        public string XmlLocalName { get; set; }
        public string XmlNamespace { get; set; }
        public InstancePassingConvention PassInstanceBy { get; set; }
        public string InstancePropertyName { get; set; }
        public string ResultPropertyName { get; set; }
    }

    public enum InstancePassingConvention
    {
        Default,
        Selector,
        KeyProperty,
        ObjectProperty
    }
}