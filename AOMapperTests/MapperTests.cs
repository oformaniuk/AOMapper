using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace jetMapperTests
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

        [TestMethod]
        public void CreateMapTest()
        {
            var first = Mapper.Create<Customer, CustomerSimpleViewItem>();
            var second = Mapper.Create<CustomerSimpleViewItem, Customer>();
            var third = Mapper.Create<Customer, CustomerSimpleViewItem>();

            Assert.AreEqual(first, third);
            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void SimpleMapTest()
        {
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

        private List<Customer> _customers = new List<Customer>();

        [TestMethod]
        public void SimpleMapPerformanceTest()
        {                        
            var map = RunTimedFunction(Mapper.Create<Customer, CustomerSimpleViewItem>, "Map initialization: ");

            for (int x = 1; x <= 100000; x *= 10)
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
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "n/SubSubItem/SubNameView");

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
        public void MapPerformanceTest()
        {
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerViewItem>();
                o.RegisterGlobalMethod("f", func);
                o.RegisterGlobalMethod("n", n);
                o.Remap<string>("Sub/Name", "SubName");
                o.Remap<string>("Sub/Name", "n/SubSubItem/SubNameView");

                return o;
            }, "Map initialization: ");

            for (int x = 1; x <= 100000; x *= 10)
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
            Func<CustomerSubClass, string> func = @class => @class.Name;
            Func<CustomerViewItem, CustomerViewItem> n = item =>
                item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = Mapper.Create<Customer, CustomerViewItem>()
                .ConfigMap(config => config.Separator = '.');

            map.RegisterGlobalMethod("f", func);
            map.RegisterGlobalMethod("n", n);
            map.Remap<string>("Sub.Name", "SubName");
            map.Remap<string>("Sub.Name", "n.SubSubItem.SubNameView");

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

        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        private int RandomInt(int min, int max)
        {            
            Random random = new Random();
            return random.Next(min, max);
        }

        private DateTime RandomDay()
        {
            DateTime start = new DateTime(1995, 1, 1);
            Random gen = new Random();

            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }
    }
}
