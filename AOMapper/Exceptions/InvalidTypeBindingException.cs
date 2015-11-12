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
            : base(Format(path, sourceType, targetType))
        {
            BuildException(path, sourceType, targetType);
        }

        private static string Format(string path, Type sourceType, Type targetType)
        {
            return string.Format("Cannot bind path '{0}'. Types '{1}' and '{2}' are incompatible. " +
                                 "To fix this you can register global Resolver if you are generating map automatically " +
                                 "or use .Remap() overload with resolver registration for manual remapping", path, sourceType, targetType);
        }

        public InvalidTypeBindingException(string message, string path, Type sourceType, Type targetType) 
            : base(string.IsNullOrEmpty(message) ? Format(path, sourceType, targetType) : message)
        {
            BuildException(path, sourceType, targetType);
        }        

        public InvalidTypeBindingException(string message, Exception inner, string path, Type sourceType, Type targetType)
            : base(string.IsNullOrEmpty(message) ? Format(path, sourceType, targetType) : message, inner)
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