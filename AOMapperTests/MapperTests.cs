using System;
using System.Collections.Generic;
using AOMapper;
using AOMapper.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace jetMapperTests
{
    [TestClass]
    public class MapperTests
    {
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
                DateOfBirth = new DateTime(1987, 11, 2),
                FirstName = "Andriy",
                LastName = "Buday",
                NumberOfOrders = 7,
                Sub = new CustomerSubClass { Name = "SomeSubName"},               
            };
        }

    }
}
