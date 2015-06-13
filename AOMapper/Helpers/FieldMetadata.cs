using System;

namespace AOMapper
{
    internal class FieldMetadata
    {
        public object Object { get; set; }
        public Type DeclareType { get; set; }
        public string FieldName { get; set; }
        public Type FieldType { get; set; }
        public Delegate MappedPropertyGetter { get; set; }
        public Delegate MappedPropertySetter { get; set; }
    }
}