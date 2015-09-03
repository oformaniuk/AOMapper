using System;
using System.Linq.Expressions;
using AOMapper.Data;

namespace AOMapper.Interfaces
{
    /// <summary>
    /// Provides mapping functionality
    /// </summary>    
    public interface IMap
    {
        /// <summary>
        /// Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        IMap Auto();

        /// <summary>
        /// Compiles the map
        /// </summary>
        /// <returns></returns>
        IMap Compile();

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        IMap Remap(string source, string destination);

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        IMap Remap<TR>(string source, string destination);        

        /// <summary>
        /// Registers a new method for the destination type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        IMap RegisterDestinationMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new global method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        IMap RegisterGlobalMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for the source type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        IMap RegisterSourceMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for all available targets
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        IMap RegisterMethod<T>(string name, T method);

        /// <summary>
        /// Executes mapping from target to destination object        
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        TDestination Do<TSource, TDestination>(TSource obj);

        /// <summary>
        /// Executes mapping from target to destination object      
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do<TSource, TDestination>(TSource obj, TDestination dest);

        object Do(object source, object destination);
        object Do(object source);

        /// <summary>        
        /// Generates mapping proxy object, that allows to perform real-time mapping using parent gettes and setters.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        MappingObject GenerateProxy(object obj);

        /// <summary>
        /// Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IMap ConfigMap(Action<Mapper.Config> config);        
    }

    /// <summary>
    /// Provides mapping functionality
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    public interface IMap<TDestination> : IMap
    {
        /// <summary>
        /// Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        new IMap<TDestination> Auto();

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TDestination> Remap(string source, string destination);

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TDestination> Remap<TR>(string source, string destination);

        /// <summary>
        /// Registers a new method for the destination type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TDestination> RegisterDestinationMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new global method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TDestination> RegisterGlobalMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for the source type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TDestination> RegisterSourceMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for all available targets
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TDestination> RegisterMethod<T>(string name, T method);

        /// <summary>
        /// Executes mapping from target to destination object        
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        new TDestination Do(object obj);

        /// <summary>
        /// Executes mapping from target to destination object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do(object obj, TDestination dest);

        /// <summary>
        /// Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        new IMap<TDestination> ConfigMap(Action<Mapper.Config> config);        
    }

    /// <summary>
    /// Provides mapping functionality
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    public interface IMap<TSource, TDestination> : IMap<TDestination>
    {
        /// <summary>
        /// Tries to automatically configure the map using name comparation
        /// </summary>
        /// <returns></returns>
        new IMap<TSource, TDestination> Auto();

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> Remap(string source, string destination);

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>        
        IMap Remap<TS, TR>(Expression<Func<TSource, TS>> source, Expression<Func<TDestination, TR>> destination);

        /// <summary>
        /// Remaps the specified property according to specified path
        /// </summary>
        /// <typeparam name="TR">Last value type</typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> Remap<TR>(string source, string destination);

        /// <summary>
        /// Registers a new method for the destination type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> RegisterDestinationMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new global method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> RegisterGlobalMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for the source type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> RegisterSourceMethod<T>(string name, T method);

        /// <summary>
        /// Registers a new method for all available targets
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> RegisterMethod<T>(string name, T method);

        /// <summary>
        /// Executes mapping from target to destination object        
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <returns></returns>
        TDestination Do(TSource sourceObject);

        /// <summary>
        /// Executes mapping from target to destination object
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        TDestination Do(TSource sourceObject, TDestination dest);

        /// <summary>
        /// Provides access to map configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        new IMap<TSource, TDestination> ConfigMap(Action<Mapper.Config> config);        

        /// <summary>        
        /// Generates mapping proxy object, that allows to perform real-time mapping using parent gettes and setters.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        MappingObject<TSource, TDestination> GenerateProxy(TSource obj);        
    }    
}