using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapper.Resolvers;
using AOMapperTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AOMapperTests
{
    [TestClass]
    public partial class MapperTests
    {
        private static readonly int PerformanceCount = 100000;

        private readonly List<Customer> _customers = new List<Customer>();

        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

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
            var customerViewManual = new CustomerSimpleViewItem
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth
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
            var customerViewManual = new CustomerSimpleViewItem
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void SimpleMapLocalResolverTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerSimpleViewItem3>()
                .Remap("NumberOfOrders", "NumberOfOrders", new Resolver<int, string>(s => s.ToString()))
                .Auto();

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem3
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders.ToString()
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        //[TestMethod]
        //public void SimpleMapResolverExceptionTest()
        //{
        //    Mapper.Clear();

        //    IMap<Customer, CustomerSimpleViewItem3> map;
        //    try
        //    {
        //        map = Mapper.Create<Customer, CustomerSimpleViewItem3>()                
        //            .Auto()
        //            .Remap("NumberOfOrders", "NumberOfOrders", new Resolver<int, string>(s => s.ToString()));
        //    }
        //    catch (InvalidTypeBindingException e)
        //    {
        //        return;
        //    }


        //    Assert.Fail("Exception was not thrown");
        //}

        [TestMethod]
        public void SimpleMapGlobalResolverTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer6, CustomerSimpleViewItem6>()
                .ConfigMap(o => o.RegisterResolver<int, string>(r => r.ToString()))
                .Auto();

            var customer = GetCustomer6FromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem6
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders.ToString(),
                Color = (int) ConsoleColor.Blue,
                Cast = 42
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void SimpleMapGlobalDictResolverTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer4, CustomerSimpleViewItem4>()
                .ConfigMap(o => o.RegisterResolver<Dictionary<int, DateTime>, Dictionary<string, string>>
                    (times =>
                        times.ToDictionary(dateTime => dateTime.Key.ToString(), dateTime => dateTime.Value.ToString())))
                .Auto();

            var customer = GetCustomer4FromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem4
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                DateTimes =
                    customer.DateTimes.ToDictionary(dateTime => dateTime.Key.ToString(),
                        dateTime => dateTime.Value.ToString())
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
            Assert.IsTrue(customerViewMapper.DateTimes.SequenceEqual(customerViewManual.DateTimes));
        }

        [TestMethod]
        public void SimpleMapSameTypeCollectionResolverTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer5, CustomerSimpleViewItem5>()
                .Auto()
                .Remap(o => o.DateTimes, o => o.DateTimes)
                .RemapFrom(o => o.SubViewItem2.DateTimes2, o => o.DateTimes);
            //.Remap(o => o.SubClass2.DateTimes2, o => o.SubViewItem2.DateTimes2);

            var customer = GetCustomer5FromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem5
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                DateTimes = customer.DateTimes.ToArray()
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
            Assert.IsTrue(customerViewMapper.DateTimes.SequenceEqual(customerViewManual.DateTimes));
        }

        [TestMethod]
        public void SimpleMapPerformanceTest()
        {
            Mapper.Clear();
            var map = RunTimedFunction(Mapper.Create<Customer, CustomerSimpleViewItem>, "Map initialization: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapperSimple(map),
                    string.Format("Mapper with {0} elements: ", x));

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
            map = RunTimedFunction(() => map.Compile(), "Map compilation: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapperSimple(map),
                    string.Format("Mapper with {0} elements: ", x));

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
                .Apply(o => o.FirstName = (string) o.FirstName.GetType().GetDefault());
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapTest()
        {
            Mapper.Clear();

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "SubSubItem/Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem
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

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "SubSubItem/Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name
            };

            Mapper.Clear();

            var map2 = Mapper.Create<Customer, CustomerViewItem>();
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "SubSubItem/Name");

            Assert.IsFalse(ReferenceEquals(map, map2));
        }

        [TestMethod]
        public void MapGetDestinationPathTest()
        {
            Mapper.Clear();

            var map = Mapper.Create<Customer, CustomerViewItem>();
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

            var map = Mapper.Create<Customer, CustomerViewItem>();
            map.Remap<string>("Sub/Name", "SubName");
            map.Remap<string>("Sub/Name", "SubSubItem/Name");

            try
            {
                map.As<IPathProvider>().GetDestinationPath("Sub/Name");
                Assert.Fail("Exception was not thrown");
            }
            catch (AmbiguousMatchException)
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
            map.Remap<string>("Sub.Name", "SubName");
            map.Remap<string>("Sub.Name", "SubSubItem.Name");

            //var result = map.As<IPathProvider>().GetSourcePath((CustomerViewItem o) => n(o).SubSubItem.Name);

            //Assert.AreEqual(result, "Sub.Name");
        }

        [TestMethod]
        public void MapAutoTest()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerViewItem>()
                .Auto();

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem
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
                .Auto();

            var customer = GetCustomer2FromDB();
            var customerViewMapper = map.Do(customer);

            var customerViewManual = new CustomerViewItem2
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name,
                SubSubItem = new CustomerSubViewItem {Name = customer.Sub.Name},
                ViewItems = new SimpleObjectViewItem[5]
            }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObjectViewItem
            {
                Date = customer.ViewItems[i].Date,
                Name = customer.ViewItems[i].Name,
                Inners = new List<SimpleObjectViewItemInner>(2)
                {
                    new SimpleObjectViewItemInner {Inner = "123"},
                    new SimpleObjectViewItemInner {Inner = "543"}
                }
            }, i)));

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapComplexObjectIgnoreTest()
        {
            Mapper.Clear();

            var map = Mapper.Create<Customer2, CustomerViewItem2>()
                .ConfigMap(o => o.IgnoreDefaultValues = true)
                .Auto()
                .Compile();

            var customer = GetCustomer2FromDB().Apply(o => o.LastName = null);
            var customerViewMapper = map.Do(customer);

            var customerViewManual = new CustomerViewItem2
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders,
                SubName = customer.Sub.Name,
                SubSubItem = new CustomerSubViewItem {Name = customer.Sub.Name},
                ViewItems = new SimpleObjectViewItem[5]
            }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObjectViewItem
            {
                Date = customer.ViewItems[i].Date,
                Name = customer.ViewItems[i].Name,
                Inners = new List<SimpleObjectViewItemInner>(2)
                {
                    new SimpleObjectViewItemInner {Inner = "123"},
                    new SimpleObjectViewItemInner {Inner = "543"}
                }
            }, i)));

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }

        [TestMethod]
        public void MapAutoPerformanceTest()
        {
            Mapper.Clear();
            var map = RunTimedFunction(() => Mapper.Create<Customer, CustomerViewItem>().Auto(),
                "Map (Auto) initialization: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
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
            var map = RunTimedFunction(() => Mapper.Create<Customer, CustomerViewItem>().Auto(),
                "Map (Auto) initialization: ");
            map = RunTimedFunction(() => map.Compile(), "Map compilation: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
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
            //Func<CustomerSubClass, string> func = @class => @class.Name;
            //Func<CustomerViewItem, CustomerViewItem> n = item =>
            //    item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerViewItem>();
                o.Remap<string>("Sub/Name", "SubName");
                //o.Remap<string>("Sub/Name", "SubSubItem/Name");
                //o.Remap(x => x.Sub.SubClass.Name, x => x.SubSubItem.Item.Name);

                return o;
            }, "Map initialization: ");

            RunTimedFunction(() =>
            {
                AutoMapper.Mapper.CreateMap<Customer, CustomerViewItem>()
                    .ForMember(o => o.SubName, o => o.MapFrom(x => x.Sub.Name));
                //.ForMember(o => o.SubSubItem.Name, o => o.MapFrom(x => x.Sub.Name))
                //.ForMember(o => o.SubSubItem.Item.Name, o => o.MapFrom(x => x.Sub.SubClass.Name));

                return 1;
            }, "Automapper initialization: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));
                var autoMapperResult = RunTimedFunction(() =>
                {
                    var customers = new List<CustomerViewItem>();

                    foreach (var customer in _customers)
                    {
                        var customerViewItem = AutoMapper.Mapper.Map<Customer, CustomerViewItem>(customer);
                        customers.Add(customerViewItem);
                    }
                    return customers;
                }, string.Format("AutoMapper with {0} elements: ", x));
                //var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                //CollectionAssert.AreEqual(mapperResult, manualResult);
                CollectionAssert.AreEqual(mapperResult, autoMapperResult);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void MapCompiledPerformanceTest()
        {
            Mapper.Clear();
            //Func<CustomerSubClass, string> func = @class => @class.Name;
            //Func<CustomerViewItem, CustomerViewItem> n = item =>
            //    item.Apply(o => o.SubSubItem = new CustomerSubViewItem());

            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerViewItem>();
                //o.RegisterGlobalMethod("f", func);
                //o.RegisterGlobalMethod("n", n);
                o.Remap<string>("Sub/Name", "SubName");
                o.Remap<string>("Sub/Name", "SubSubItem/Name");

                return o;
            }, "Map initialization: ");

            map = RunTimedFunction(() =>
                map.Compile(), "Map compilation: ");

            for (var x = 1; x <= PerformanceCount; x *= 10)
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
        public void MapWithNonDefaultSeparatorTest()
        {
            Mapper.Clear();

            var map = Mapper.Create<Customer, CustomerViewItem>()
                .ConfigMap(config => config.Separator = '.');

            map.Remap<string>("Sub.Name", "SubName");
            //map.Remap<string>("Sub.Name", "n.SubSubItem.Name");

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerViewItem
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
        public void ThisObjectResolver()
        {
            Mapper.Clear();
            var map = Mapper.Create<Customer, CustomerSimpleViewItem3>()
                //.ConfigMap(o => o.RegisterResolver<int, string>(r => r.ToString()))
                //.ConfigMap(o => o.InitialyzeNullValues = false)
                //.ConfigMap(o => o.IgnoreDefaultValues = true)
                .Auto()
                .Remap(o => o.Sub.Name, o => o.SubName)
                .Remap(o => o.Sub.Name, o => o.SubDescription)
                .Remap(o => o.Sub, o => o.SubSubItem,
                    new Resolver<CustomerSubClass, CustomerSubViewItem>(c => new CustomerSubViewItem()))
                //.Remap(o => o.Sub.Name, o => o.SubSubItem.Name)
                .Remap(o => o.Sub.Name, o => o.SubSubItem.Description)
                .RemapFrom(o => o.FirstName, c => c.FirstName + 1)
                .Compile();

            var customer = GetCustomerFromDB();
            var customerViewMapper = map.Do(customer);
            var customerViewManual = new CustomerSimpleViewItem3
            {
                FirstName = customer.FirstName + 1,
                LastName = customer.LastName,
                DateOfBirth = customer.DateOfBirth,
                NumberOfOrders = customer.NumberOfOrders.ToString()
            };

            Assert.AreEqual(customerViewMapper, customerViewManual);
        }
    }
}