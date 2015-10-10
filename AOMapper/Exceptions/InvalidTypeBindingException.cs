using System;
using System.Runtime.Serialization;

namespace AOMapper.Exceptions
{
#if !PORTABLE
    [Serializable]
#endif
    public class InvalidTypeBindingException : Exception
    {
        public string Path { get; private set; }
        public Type SourceType { get; private set; }
        public Type TargetType { get; private set; }

        public InvalidTypeBindingException(string path, Type sourceType, Type targetType)
        {
            BuildException(path, sourceType, targetType);
        }

        public InvalidTypeBindingException(string message, string path, Type sourceType, Type targetType) : base(message)
        {
            BuildException(path, sourceType, targetType);
        }        

        public InvalidTypeBindingException(string message, Exception inner, string path, Type sourceType, Type targetType) : base(message, inner)
        {
            BuildException(path, sourceType, targetType);
        }

        private void BuildException(string path, Type sourceType, Type targetType)
        {
            Path = path;
            SourceType = sourceType;
            TargetType = targetType;
        }

#if !PORTABLE
        protected InvalidTypeBindingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {            
        }
#endif
    }
}