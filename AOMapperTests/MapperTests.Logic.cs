using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapperTests.Helpers;

namespace AOMapperTests
{
    public partial class MapperTests
    {
        private static readonly Random random = new Random((int) DateTime.Now.Ticks);

        private Customer GetCustomerFromDB()
        {
            return new Customer
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass
                {
                    Name = RandomString(10)
                    //SubClass = new CustomerSubClass
                    //{
                    //    Name = RandomString(7)
                    //}
                }
            };
        }

        private Customer6 GetCustomer6FromDB()
        {
            return new Customer6
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Color = ConsoleColor.Blue,
                Cast = 42,
                Sub = new CustomerSubClass
                {
                    Name = RandomString(10)
                    //SubClass = new CustomerSubClass
                    //{
                    //    Name = RandomString(7)
                    //}
                }
            };
        }

        private Customer2 GetCustomer2FromDB()
        {
            return new Customer2
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass {Name = RandomString(10)},
                ViewItems = new SimpleObject[5]
            }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObject
            {
                Id = i,
                Date = RandomDay(),
                Name = RandomString(6),
                Inners = new List<SimpleObjectInner>(2)
                {
                    new SimpleObjectInner {Inner = RandomString(4)},
                    new SimpleObjectInner {Inner = RandomString(3)}
                }
            }, i)));
        }

        private Customer4 GetCustomer4FromDB()
        {
            return new Customer4
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass {Name = RandomString(10)},
                DateTimes =
                    new Dictionary<int, DateTime>
                    {
                        {RandomInt(0, 1000), RandomDay()},
                        {RandomInt(0, 1000), RandomDay()},
                        {RandomInt(0, 1000), RandomDay()}
                    }
            };
        }

        private Customer5 GetCustomer5FromDB()
        {
            return new Customer5
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass {Name = RandomString(10)},
                DateTimes = new List<DateTime> {RandomDay(), RandomDay(), RandomDay()},
                SubClass2 = new CustomerSubClass2
                {
                    DateTimes2 = new[] {RandomDay(), RandomDay()}
                }
            };
        }

        private Customer7 GetCustomer7FromDB()
        {
            return new Customer7
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass { Name = RandomString(10) },
                DateTimes = new List<DateTime> { RandomDay(), RandomDay(), RandomDay() }                
            };
        }

        private void PopulateCustomers(int count)
        {
            _customers.Clear();
            for (var x = 0; x < count; x++)
            {
                var customer = GetCustomerFromDB();
                _customers.Add(customer);
            }
        }

        private List<CustomerSimpleViewItem> RunMapperSimple(IMap<Customer, CustomerSimpleViewItem> map)
        {
            var customers = new List<CustomerSimpleViewItem>();

            foreach (var customer in _customers)
            {
                var customerViewItem = map.Do(customer);
                customers.Add(customerViewItem);
            }
            return customers;
        }

        private List<CustomerViewItem> RunMapper(IMap<Customer, CustomerViewItem> map)
        {
            var customers = new List<CustomerViewItem>();

            foreach (var customer in _customers)
            {
                var customerViewItem = map.Do(customer);
                customers.Add(customerViewItem);
            }
            return customers;
        }

        private List<CustomerSimpleViewItem> RunManualSimple()
        {
            var customers = new List<CustomerSimpleViewItem>();

            foreach (var customer in _customers)
            {
                var customerViewManual = new CustomerSimpleViewItem
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth
                };
                customers.Add(customerViewManual);
            }
            return customers;
        }

        private List<CustomerViewItem> RunManual()
        {
            var customers = new List<CustomerViewItem>();

            foreach (var customer in _customers)
            {
                var customerViewManual = new CustomerViewItem
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth,
                    NumberOfOrders = customer.NumberOfOrders,
                    SubName = customer.Sub.Name,
                    SubSubItem = new CustomerSubViewItem
                    {
                        Name = customer.Sub.Name
                        //Item = new CustomerSubViewItem
                        //{
                        //    Name = customer.Sub.SubClass.Name
                        //}
                    }
                };
                customers.Add(customerViewManual);
            }
            return customers;
        }

        private T RunTimedFunction<T>(Func<T> f, string text)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = f();
            stopwatch.Stop();
            if (TestContext != null)
                TestContext.WriteLine(text + stopwatch.ElapsedMilliseconds);

            return result;
        }

        private string RandomString(int size)
        {
            var builder = new StringBuilder();
            char ch;
            for (var i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*random.NextDouble() + 65)));
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
            var start = new DateTime(1995, 1, 1);

            var range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }
    }
}