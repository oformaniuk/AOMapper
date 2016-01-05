using System;
using System.Linq;
using System.Reflection;
using AOMapperTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet_Package
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var tester = new MapperTests();
                var methods = tester.GetType().GetMethods()
                    .Where(o => o.GetCustomAttribute<TestMethodAttribute>() != null)
                    .Select(o => new {Action = (Action) Delegate.CreateDelegate(typeof (Action), tester, o), Method = o});

                foreach (var method in methods)
                {
                    try
                    {
                        method.Action();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Method: " + method.Method.Name + " Error: " + e.Message);
                        throw;
                    }
                }
            }
            catch
            {
                Environment.Exit(2);
            }
        }
    }
}