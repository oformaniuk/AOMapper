using System;
using System.Linq.Expressions;
using AOMapper.Resolvers;

namespace AOMapper.Interfaces
{
    /// <summary>
    ///     Provides mapping functionality
    /// </summary>
    public interface IMap
    {
        /// <summary>
        ///     Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        IMap Auto();

        /// <summary>
        ///     Ignores the specified source path during auto mapping.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        IMap IgnoreSource(string source);

        /// <summary>
        ///     Ignores the destination path during auto mapping.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
         IMap IgnoreDestination(string destination);

        /// <summary>
        ///     Compiles the map
        /// </summary>
        /// <returns></returns>
        IMap Compile();

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        IMap Remap(string source, string destination, Resolver resolver = null);

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        IMap Remap<TR>(string source, string destination, Resolver resolver = null);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        TDestination Do<TSource, TDestination>(TSource obj);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do<TSource, TDestination>(TSource obj, TDestination dest);


        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        object Do(object source, object destination);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        object Do(object source);

        /// <summary>
        ///     Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IMap ConfigMap(Action<Mapper.Config> config);

        T GetConfigurationParameter<T>(Func<Mapper.Config, T> selector);

        Expression CompileToExpression(out ParameterExpression sourceParameterExpression,
            out ParameterExpression destinationParameterExpression);
    }

    /// <summary>
    ///     Provides mapping functionality
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    public interface IMap<TDestination> : IMap
    {
        /// <summary>
        ///     Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        new IMap<TDestination> Auto();

        /// <summary>
        ///     Ignores the specified source path during auto mapping.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        new IMap<TDestination> IgnoreSource(string source);

        /// <summary>
        ///     Ignores the destination path during auto mapping.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TDestination> IgnoreDestination(string destination);

        /// <summary>
        ///     Compiles the map
        /// </summary>
        /// <returns></returns>
        new IMap<TDestination> Compile();

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TDestination> Remap(string source, string destination, Resolver resolver = null);

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TDestination> Remap<TR>(string source, string destination, Resolver resolver = null);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        new TDestination Do(object obj);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do(object obj, TDestination dest);

        /// <summary>
        ///     Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        new IMap<TDestination> ConfigMap(Action<Mapper.Config> config);
    }

    /// <summary>
    ///     Provides mapping functionality
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    public interface IMap<TSource, TDestination> : IMap<TDestination>
    {
        /// <summary>
        ///     Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        new IMap<TSource, TDestination> Auto();

        /// <summary>
        ///     Ignores the specified source path during auto mapping.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> IgnoreSource(string source);

        /// <summary>
        ///     Ignores the destination path during auto mapping.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> IgnoreDestination(string destination);

        /// <summary>
        ///     Compiles the map
        /// </summary>
        /// <returns></returns>
        new IMap<TSource, TDestination> Compile();

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> Remap(string source, string destination, Resolver resolver = null);

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        IMap<TSource, TDestination> Remap<TS>(Expression<Func<TSource, TS>> source,
            Expression<Func<TDestination, TS>> destination);

        IMap<TSource, TDestination> Remap(Expression<Func<TSource, object>> source,
            Expression<Func<TDestination, object>> destination);

        IMap<TSource, TDestination> Remap<TS, TR>(Expression<Func<TSource, TS>> source,
            Expression<Func<TDestination, TR>> destination, SimpleResolver<TS, TR> resolver);

        IMap<TSource, TDestination> Remap<TS, TR>(Expression<Func<TSource, TS>> source,
            Expression<Func<TDestination, TR>> destination, Expression<Func<TS, TR>> resolver);

         IMap<TSource, TDestination> RemapFrom<TR>(Expression<Func<TDestination, TR>> destination,
             Expression<Func<TSource, TR>> selector);

        /// <summary>
        ///     Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
         IMap<TSource, TDestination> Remap<TR>(string source, string destination);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <returns></returns>
        TDestination Do(TSource sourceObject);

        /// <summary>
        ///     Executes mapping from target to destination object
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do(TSource sourceObject, TDestination dest);

        /// <summary>
        ///     Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> ConfigMap(Action<Mapper.Config> config);
    }
}