using System;

namespace AOMapper.Data
{
    internal struct FieldMetadata
    {
        public object Object { get; set; }
        public Type DeclareType { get; set; }
        public string FieldName { get; set; }
        public Type FieldType { get; set; }
        public Delegate MappedPropertyGetter { get; set; }
        public Delegate MappedPropertySetter { get; set; }
    }
}