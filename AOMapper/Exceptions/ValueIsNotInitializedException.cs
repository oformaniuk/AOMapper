using System;
using System.Runtime.Serialization;

namespace AOMapper.Exceptions
{
#if !PORTABLE
    [Serializable]
#endif
    public class ValueIsNotInitializedException : Exception
    {
        public ValueIsNotInitializedException(string path, Type sourceType, Type targetType)
            : base(Format(path, sourceType, targetType))
        {
            BuildException(path, sourceType, targetType);
        }

        public ValueIsNotInitializedException(string message, string path, Type sourceType, Type targetType)
            : base(string.IsNullOrEmpty(message) ? Format(path, sourceType, targetType) : message)
        {
            BuildException(path, sourceType, targetType);
        }

        public ValueIsNotInitializedException(string message, Exception inner, string path, Type sourceType,
            Type targetType)
            : base(string.IsNullOrEmpty(message) ? Format(path, sourceType, targetType) : message, inner)
        {
            BuildException(path, sourceType, targetType);
        }

#if !PORTABLE
        protected ValueIsNotInitializedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
#endif
        public string Path { get; private set; }
        public Type SourceType { get; private set; }
        public Type TargetType { get; private set; }

        private static string Format(string path, Type sourceType, Type targetType)
        {
            return string.Format("Value '{0}' is not initialized." +
                                 "To fix this you can register global Resolver if you are generating map automatically " +
                                 "or use .Remap() overload with resolver registration for manual remapping. Also see 'InitialyzeNullValues' configuration property.",
                path);
        }

        private void BuildException(string path, Type sourceType, Type targetType)
        {
            Path = path;
            SourceType = sourceType;
            TargetType = targetType;
        }
    }
}