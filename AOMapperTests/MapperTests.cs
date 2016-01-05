using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Helpers;
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
        public void NonCompiledTest()
        {
            try
            {
                Mapper.Clear();
                var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>()
                    .Auto();

                var obj = GetCustomerFromDB();
                mapper.Do(obj);
                Assert.Fail("Exception was not thrown");
            }
            catch(InvalidOperationException)
            {
                return;
            }
        }

        [TestMethod]
        public void AutoMappingTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .Auto()
                .Remap(o => o.Sub.Name, o => o.SubDescription)                
                .Compile();

            var obj = GetCustomerFromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.Sub.Name,
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                }
            };

            Assert.AreEqual(autoMap, manual);
        }

        [TestMethod]
        public void RemapFromTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .Auto()
                .RemapFrom(o => o.SubDescription, o => o.Sub.Name + " Test")
                .Compile();

            var obj = GetCustomerFromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.Sub.Name + " Test",
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                }
            };

            Assert.AreEqual(autoMap, manual);
        }

        [TestMethod]
        public void NonStandartTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>(o => new CustomerSimpleViewItem(o.Sub.Name + " Test"))
                .Auto()                
                .Compile();

            var obj = GetCustomerFromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.Sub.Name + " Test",
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                }
            };

            Assert.AreEqual(autoMap, manual);
        }

        [TestMethod]
        public void LocalResolverTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription, time => time.ToString())
                .Compile();

            var obj = GetCustomerFromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                }
            };

            Assert.AreEqual(autoMap, manual);
        }

        [TestMethod]
        public void GeneralResolverTest()
        {
            Mapper.Clear();

            var simpleResolver = new SimpleResolver<DateTime, string>(time => time.ToString());

            var mapper = Mapper.Create<Customer, CustomerSimpleViewItem>()
                .ConfigMap(o => o.RegisterResolver(simpleResolver))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Compile();

            var obj = GetCustomerFromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                }
            };

            Assert.AreEqual(autoMap, manual);
        }

        [TestMethod]
        public void ListToListTest()
        {
            Mapper.Clear();

            var simpleResolver = new SimpleResolver<DateTime, string>(time => time.ToString());

            var mapper = Mapper.Create<Customer7, CustomerSimpleViewItem7>()
                .ConfigMap(o => o.RegisterResolver(simpleResolver))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Compile();

            var obj = GetCustomer7FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem7
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                DateTimes = new List<DateTime>(obj.DateTimes)
            };

            Assert.AreEqual(autoMap, manual);
            Assert.IsTrue(autoMap.DateTimes.SequenceEqual(manual.DateTimes));
        }

        [TestMethod]
        public void ListToListWithResolverTest()
        {
            Mapper.Clear();

            var simpleResolver = new SimpleResolver<DateTime, string>(time => time.ToString());

            var mapper = Mapper.Create<Customer7, CustomerSimpleViewItem8>()
                .ConfigMap(o => o.RegisterResolver(simpleResolver))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Compile();

            var obj = GetCustomer7FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem8
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                DateTimes = new List<string>(obj.DateTimes.Select(o => o.ToString()))
            };

            Assert.AreEqual(autoMap, manual);
            Assert.IsTrue(autoMap.DateTimes.SequenceEqual(manual.DateTimes));
        }

        [TestMethod]
        public void ListToArrayTest()
        {
            Mapper.Clear();            

            var mapper = Mapper.Create<Customer7, CustomerSimpleViewItem5>()                
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription, time => time.ToString())
                .Compile();

            var obj = GetCustomer7FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem5
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                DateTimes = new List<DateTime>(obj.DateTimes).ToArray()
            };

            Assert.AreEqual(autoMap, manual);
            Assert.IsTrue(autoMap.DateTimes.SequenceEqual(manual.DateTimes));
        }

        [TestMethod]
        public void ListToArrayWithResolverTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer7, CustomerSimpleViewItem9>()
                .ConfigMap(o => o.RegisterResolver<DateTime, string>(x => x.ToString()))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Compile();

            var obj = GetCustomer7FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem9
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                DateTimes = new List<string>(obj.DateTimes.Select(x => x.ToString())).ToArray()
            };

            Assert.AreEqual(autoMap, manual);
            Assert.IsTrue(autoMap.DateTimes.SequenceEqual(manual.DateTimes));
        }

        [TestMethod]
        public void CastTest()
        {
            Mapper.Clear();

            var mapper = Mapper.Create<Customer6, CustomerSimpleViewItem6>()
                .ConfigMap(o => o.RegisterResolver<DateTime, string>(x => x.ToString()))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Remap(o => o.NumberOfOrders, o => o.NumberOfOrders, i => i.ToString())
                .Compile();

            var obj = GetCustomer6FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem6
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                Cast = obj.Cast,
                Color = CastTo<int>.From(obj.Color),
                NumberOfOrders = obj.NumberOfOrders.ToString()
            };

            Assert.AreEqual(autoMap, manual);            
        }

        [TestMethod]
        public void ComplexSequenceTest()
        {
            Mapper.Clear();

            var simpleResolver = new SimpleResolver<DateTime, string>(time => time.ToString());

            var mapper = Mapper.Create<Customer2, CustomerSimpleViewItem10>()
                .ConfigMap(o => o.RegisterResolver(simpleResolver))
                .Auto()
                .Remap(o => o.DateOfBirth, o => o.SubDescription)
                .Compile();

            var obj = GetCustomer2FromDB();
            var autoMap = mapper.Do(obj);
            var manual = new CustomerSimpleViewItem10
            {
                DateOfBirth = obj.DateOfBirth,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                SubDescription = obj.DateOfBirth.ToString(),
                SubName = obj.Sub.Name,
                SubSubItem = new CustomerSubViewItem
                {
                    Name = obj.Sub.Name,
                    Description = obj.Sub.Name
                },
                ViewItems = obj.ViewItems.Select(x => new SimpleObjectViewItem
                {
                    Name = x.Name,
                    Date = x.Date,
                    Inners = x.Inners.Select(y => new SimpleObjectViewItemInner
                    {
                        Inner = y.Inner
                    }).ToList()
                }).ToArray()
            };

            Assert.AreEqual(autoMap, manual);
            Assert.IsTrue(autoMap.ViewItems.SequenceEqual(manual.ViewItems));
        }
    }
}