using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Data
{
    public class MappingObject<TSource, TDestination> : MappingObject
    {
        internal MappingObject(IEnumerable<FieldMetadata> metadatas) : base(metadatas) { }        

        /// <summary>
        /// Gets the underlying parent object.
        /// </summary>
        /// <value>
        /// The underlying object.
        /// </value>
        public new TSource UnderlyingObject { get; internal set; }

        /// <summary>
        /// Gets the value of the mapped parent object's property.
        /// </summary>       
        /// <returns></returns>
        public TResult GetValue<TResult>(Expression<Func<TDestination, TResult>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null) throw new MissingMemberException();

            var pair = FieldMetadatas[memberExpression.Member.Name];
            return pair.Value.GetGeneric<TSource, TResult>((TSource)pair.Key);
        }

        /// <summary>
        /// Sets the value to the mapped parent object's property.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="value"></param>
        public void SetValue<TResult>(Expression<Func<TDestination, TResult>> expression, TResult value)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null) throw new MissingMemberException();

            var pair = FieldMetadatas[memberExpression.Member.Name];
            pair.Value.SetGeneric(pair.Key, value);
        }
    }

    public class MappingObject
    {
        internal MappingObject(IEnumerable<FieldMetadata> metadatas)
        {
            FieldMetadatas = new Dictionary<string, EditableKeyValuePair<object, IAccessObject>>();
            foreach (var fieldMetadata in metadatas)
            {
                _accessorBuilderMethod.MakeGeneric(fieldMetadata.DeclareType, fieldMetadata.FieldType)
                    .Invoke(null, new object[] { FieldMetadatas, fieldMetadata.FieldName, fieldMetadata.Object, fieldMetadata.MappedPropertyGetter, fieldMetadata.MappedPropertySetter });
            }
        }        

        /// <summary>
        /// Gets the underlying parent object.
        /// </summary>
        /// <value>
        /// The underlying object.
        /// </value>
        public object UnderlyingObject { get; internal set; }

        /// <summary>
        /// Gets or sets the target property of the parent object
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name]
        {
            get { return GetValue(name); }
            set { SetValue(name, value); }
        }


        /// <summary>
        /// Gets the value of the mapped parent object's property.
        /// </summary>
        /// <param name="name">Target property name</param>
        /// <returns></returns>
        public object GetValue(string name)
        {
            var pair = FieldMetadatas[name];
            return pair.Value.Get(pair.Key);
        }

        /// <summary>
        /// Sets the value to the mapped parent object's property.
        /// </summary>
        /// <param name="name">Target property name</param>
        /// <param name="value"></param>
        public void SetValue(string name, object value)
        {
            var pair = FieldMetadatas[name];
            pair.Value.Set(pair.Key, value);
        }

        /// <summary>
        /// Gets the value of the mapped parent object's property.
        /// </summary>        
        /// <param name="expression"></param>
        /// <returns></returns>
        public TResult GetValue<TSource, TResult>(Expression<Func<TSource, TResult>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null) throw new MissingMemberException();

            var pair = FieldMetadatas[memberExpression.Member.Name];
            return pair.Value.GetGeneric<TSource, TResult>((TSource)pair.Key);
        }

        /// <summary>
        /// Sets the value to the mapped parent object's property.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="value"></param>
        public void SetValue<TSource, TResult>(Expression<Func<TSource, TResult>> expression, TResult value)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null) throw new MissingMemberException();

            var pair = FieldMetadatas[memberExpression.Member.Name];
            pair.Value.SetGeneric(pair.Key, value);
        }

        #region Fields

        private readonly MethodInfo _accessorBuilderMethod =
            typeof(MappingObject).GetMethod("BuildAccessors", BindingFlags.NonPublic | BindingFlags.Static);

        internal readonly Dictionary<string, EditableKeyValuePair<object, IAccessObject>> FieldMetadatas;
        #endregion

        #region Helpers

        private static Action<T, TR> _convertDelegateToAction<T, TR>(Delegate f)
        {
            return (arg1, r) => ((Action<T, TR>)f)(arg1, r);
        }

        private static Func<T, TR> _convertDelegateToFunc<T, TR>(Delegate f)
        {
            return arg => ((Func<T, TR>) f)(arg);
        }
        private static void BuildAccessors<T, TRet>(
            Dictionary<string, EditableKeyValuePair<object, IAccessObject>> dictionary, string name, object o,
            Delegate getter, Delegate setter)
        {
            var obj = new AccessObject<T, TRet>
            {
                Getter = _convertDelegateToFunc<T, TRet>(getter),
                Setter = _convertDelegateToAction<T, TRet>(setter),
            };

            dictionary[name] = new EditableKeyValuePair<object, IAccessObject>(o, obj);
        }

        #endregion
     
    }
}