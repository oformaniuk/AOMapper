﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapperTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AOMapperTests
{
    [TestClass]
    public class MapperTests
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }        

        static int PerformanceCount = 100000;

        [TestMethod]
        public void CreateMapTest()
        {
            Mapper.Clear();
            var first = Mapper.Create<Customer, CustomerSimpleViewItem>();
            var second = Mapper.Create<CustomerSimpleViewItem, Customer>();
            var third = Mapper.Create<Customer, CustomerSimpleViewItem>();

            Assert.AreEqual(first, third);
            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void CreateMapAutoTest()
        {
            Mapper.Clear();
            var first = Mapper.Create<Customer, CustomerViewItem>().Auto();
            var second = Mapper.Create<CustomerViewItem, Customer>().Auto();
            var third = Mapper.Create<Customer, CustomerViewItem>().Auto();

            Assert.AreEqual(first, third);
            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void SimpleMapTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerSimpleViewItem>();
            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,                
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void SimpleMapAutoTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .Auto();

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void SimpleDirectMapTest()
        {
            Mapper.Clear();
            var mapBack = Mapper.Create<CustomerSimpleViewItem, Customer>();
            var customerBlank = new CustomerSimpleViewItem();

            var proxy = mapBack.GenerateProxy(customerBlank);
            var customer = GetCustomerFromDB();
            
            proxy.SetValue(x => x.FirstName, customer.FirstName);
            proxy.SetValue(x => x.LastName, customer.LastName);
            proxy.SetValue(x => x.DateOfBirth, customer.DateOfBirth);

            var customerViewManual = new CustomerSimpleViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
            };

            Assert.AreEqual(customerBlank, customerViewManual);
        }        

        private List<Customer> _customers = new List<Customer>();

        [TestMethod]
        public void SimpleMapPerformanceTest()
        {
            Mapper.Clear();       
            var map = RunTimedFunction(Mapper.Create<Customer, CustomerSimpleViewItem>, "Map initialization: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {                
                PopulateCustomers(x);
                
                var mapperResult = RunTimedFunction(() => RunMapperSimple(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManualSimple, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);
                
                Console.WriteLine();
                Console.WriteLine();
            }            
        }

        [TestMethod]
        public void SimpleMapCompiledPerformanceTest()
        {
            Mapper.Clear();
            var map = RunTimedFunction(Mapper.Create<Customer, CustomerSimpleViewItem>, "Map initialization: ");
            map = RunTimedFunction(() => (IMap<Customer, CustomerSimpleViewItem>)map.Compile(), "Map compilation: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapperSimple(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManualSimple, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void SimpleMapDefaultIgnoreTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .ConfigMap(config => config.IgnoreDefaultValues = true);                

            var customer = GetCustomerFromDB()
                .Apply(o => o.FirstName = (string)o.FirstName.GetType().GetDefault());
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "n/SubSubItem/Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapClearTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "n/SubSubItem/Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name
            };

            Mapper.Clear();

            var map2 = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "n/SubSubItem/Name");

            Assert.AreNotEqual(map, map2);
        }

        [TestMethod]
        public void MapMethodMissingTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            try
            {
                Mapper.Clear();
                var map = Mapper.Create<Customer, CustomerViewItem>();
                map.RegisterGlobalMethod("f", func);
                //map.RegisterGlobalMethod("n", n);
                map.Remap<string>("Sub/Name", "SubName");
                map.Remap<string>("Sub/Name", "n/SubSubItem/Name");

                var customer = GetCustomerFromDB();
            
                var customerViewMapper = map.Do(customer);
                Assert.Fail("Exception was not thrown");
            }
            catch (InvalidOperationException)
            {    
                return;
            }
            
            Assert.Fail("Exception was not catch");
        }

        [TestMethod]
        public void MapGetDestinationPathTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            
            var result = map.As<IPathProvider>().GetDestinationPath("Sub/Name");
            var result1 = map.As<IPathProvider>().GetDestinationPath((Customer o) => o.Sub.Name);      

            Assert.AreEqual(result, "SubName");
            Assert.AreEqual(result1, "SubName");
        }

        [TestMethod]
        public void MapGetDestinationPathAmbiousTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "n/SubSubItem/Name");

            try
            {
                map.As<IPathProvider>().GetDestinationPath("Sub/Name");
                Assert.Fail("Exception was not thrown");
            }
            catch(AmbiguousMatchException)
            {
                return;
            }

            Assert.Fail("Exception was not catch");
        }

        [TestMethod]
        public void MapGetSourcePathTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.ConfigMap(o => o.Separator = '.');
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub.Name", "SubName");            

            var result = map.As<IPathProvider>().GetSourcePath("SubName");

            Assert.AreEqual(result, "Sub.Name");
        }

        [TestMethod]
        public void MapAutoTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerViewItem>()
                .Auto();            

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapComplexObjectTest()
        {
            Mapper.Clear();
            
            var map = Mapper.Create<Customer2, CustomerViewItem2>()
                .Auto().Compile();            

            var customer = GetCustomer2FromDB();
            var customerViewMapper = map.Do(customer);

            var customerViewManual = new CustomerViewItem2()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name,
                SubSubItem = new CustomerSubViewItem { Name = customer.Sub.Name },
                ViewItems = new SimpleObjectViewItem[5]
            }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObjectViewItem()
            {
                Date = customer.ViewItems[i].Date,
                Name = customer.ViewItems[i].Name,
                Inners = new List<SimpleObjectViewItemInner>(2)
                {
                    new SimpleObjectViewItemInner{Inner = "123"}, new SimpleObjectViewItemInner{Inner = "543"}
                }
            }, i)));
            
            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapAutoPerformanceTest()
        {
            Mapper.Clear();
            var map = RunTimedFunction(() => Mapper.Create<Customer, CustomerViewItem>().Auto(), "Map (Auto) initialization: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);

                Console.WriteLine();
                Console.WriteLine();
            }            
        }

        [TestMethod]
        public void MapAutoCompiledPerformanceTest()
        {
            Mapper.Clear();
            var map = RunTimedFunction(() => Mapper.Create<Customer, CustomerViewItem>().Auto(), "Map (Auto) initialization: ");
            map = RunTimedFunction(() => (IMap<Customer, CustomerViewItem>)map.Compile(), "Map compilation: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void MapPerformanceTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerViewItem>();
                o.RegisterGlobalMethod("f", func);
                o.RegisterGlobalMethod("n", n);
                o.Remap<string>("Sub/Name", "SubName");
                o.Remap<string>("Sub/Name", "n/SubSubItem/Name");

                return o;
            }, "Map initialization: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void MapCompiledPerformanceTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerViewItem>();
                o.RegisterGlobalMethod("f", func);
                o.RegisterGlobalMethod("n", n);
                o.Remap<string>("Sub/Name", "SubName");
                o.Remap<string>("Sub/Name", "n/SubSubItem/Name");

                return o;
            }, "Map initialization: ");

            map = RunTimedFunction(() => (IMap<Customer, CustomerViewItem>)map.Compile(), "Map compilation: ");

            for (int x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));

                var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                CollectionAssert.AreEqual(mapperResult, manualResult);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void MapWithNonDefaulrSeparatorTest()
        {
            Mapper.Clear();
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>()
                .ConfigMap(config => config.Separator = '.');

            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub.Name", "SubName");
            map.Remap<string>("Sub.Name", "n.SubSubItem.Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem()
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }    

        private Customer GetCustomerFromDB()
        {
            return new Customer()
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass { Name = RandomString(10)},               
            };
        }

        private Customer2 GetCustomer2FromDB()
        {
            return new Customer2()
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass { Name = RandomString(10) },
                ViewItems = new SimpleObject[5]
            }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObject
            {
                Id = i, Date = RandomDay(), Name = RandomString(6), Inners = new List<SimpleObjectInner>(2)
                {
                    new SimpleObjectInner{Inner = "123"}, new SimpleObjectInner{Inner = "543"}
                }
            }, i)));
        }

        private void PopulateCustomers(int count)
        {      
            _customers.Clear();
            for (int x = 0; x < count; x++)
            {               
                Customer customer = GetCustomerFromDB();
                this._customers.Add(customer);
            }
        }

        private List<CustomerSimpleViewItem> RunMapperSimple(IMap<Customer, CustomerSimpleViewItem> map)
        {
            List<CustomerSimpleViewItem> customers = new List<CustomerSimpleViewItem>();

            foreach (Customer customer in this._customers)
            {
                CustomerSimpleViewItem customerViewItem = map.Do(customer);
                customers.Add(customerViewItem);
            }
            return customers;
        }

        private List<CustomerViewItem> RunMapper(IMap<Customer, CustomerViewItem> map)
        {
            List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {
                CustomerViewItem customerViewItem = map.Do(customer);
                customers.Add(customerViewItem);
            }
            return customers;
        }

        private List<CustomerSimpleViewItem> RunManualSimple()
        {
            List<CustomerSimpleViewItem> customers = new List<CustomerSimpleViewItem>();

            foreach (Customer customer in  this._customers)
            {
                var customerViewManual = new CustomerSimpleViewItem()
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth,
                };
                customers.Add(customerViewManual);
            }
            return customers;
        }

        private List<CustomerViewItem> RunManual()
        {
            List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {
                var customerViewManual = new CustomerViewItem()
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth,
                    NumberOfOrders = customer.NumberOfOrders,
                    SubName = customer.Sub.Name
                };
                customers.Add(customerViewManual);
            }
            return customers;
        }

        private T RunTimedFunction<T>(Func<T> f, string text)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = f();
            stopwatch.Stop();
            TestContext.WriteLine(text + stopwatch.ElapsedMilliseconds);

            return result;
        }

        private static readonly Random random = new Random((int)DateTime.Now.Ticks);

        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();            
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
                builder.Append(RandomInt(0, 1000));
            }

            return builder.ToString();
        }

        private int RandomInt(int min, int max)
        {                        
            return random.Next(min, max);
        }

        private DateTime RandomDay()
        {
            DateTime start = new DateTime(1995, 1, 1);            

            int range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }        
    }

    public class Class1
    {
        public void UpdateMethod1(StaticDataEntities entities)
        {            
            //insert some items or make some changes in entity
        }
        public void UpdateMethod2(StaticDataEntities entities)
        {            
            //insert some items or make some changes in entity
        }        
    }

    public static class Usage
    {
        public static void Update()
        {
            var x = new Class1();
            UpdateHelper.Update<StaticDataEntities>(x.UpdateMethod1); // via a method group
            UpdateHelper.Update<StaticDataEntities>(o => x.UpdateMethod2(o)); // via classic lambda
        }
    }

    public class StaticDataEntities : IObjectContext
    {        
        public int SaveChanges()
        {
            throw new NotImplementedException();
        }
    }

    public interface IObjectContext
    {
        int SaveChanges();
    }

    public static class UpdateHelper
    {
        // where ObjectContext is the base class for 'StaticDataEntities' that contains ObjectContext.SaveChanges();
        public static int Update<T>(Action<T> action) where T : IObjectContext, new()
        {
            var entities = new T();
            action(entities);
            return entities.SaveChanges();
        }
    }

}
