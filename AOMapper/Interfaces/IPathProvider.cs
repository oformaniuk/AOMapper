using System;
using System.Linq.Expressions;
using AOMapper.Data;

namespace AOMapper.Interfaces
{
    public interface IPathProvider
    {
        string GetSourcePath(string destination);
        string GetDestinationPath(string source);

        string GetSourcePath<T, R>(Expression<Func<T, R>> destination);
        string GetDestinationPath<T, R>(Expression<Func<T, R>> source);
    }    
}