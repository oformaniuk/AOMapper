using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapperTests;
using AOMapperTests.Helpers;

namespace ProfilerTarget
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run();
        }

        public void Run()
        {            
            var map = RunTimedFunction((s) =>
            {
                s.Start();
                var o = Mapper.Create<Customer, CustomerViewItem>().Auto();   
                s.Stop();
                return o;
            }, "Mapper initialization: ");

            var mapCompiled = RunTimedFunction((s) =>
            {
                Mapper.Clear();
                s.Start();
                var o = (IMap<Customer, CustomerViewItem>)Mapper.Create<Customer, CustomerViewItem>().Auto().Compile();
                s.Stop();
                return o;
            }, "Mapper compile: ");

            RunTimedFunction<object>((s) =>
            {
                s.Start();
                AutoMapper.Mapper.CreateMap<Customer, CustomerViewItem>();
                s.Start();

                return null;
            }, "AutoMapper initialization: ");                                                 

            Console.WriteLine();
            Console.WriteLine();

            for (int x = 1; x <= 1000000; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction((s) => RunMapper(map, s), string.Format("Mapper with {0} elements: ", x));
                var mapperCompiledResult = RunTimedFunction((s) => RunMapper(mapCompiled, s), string.Format("Mapper (Compiled) with {0} elements: ", x));
                var autoMapperResult = RunTimedFunction(RunAutoMapper<Customer, CustomerViewItem>, string.Format("AutoMapper with {0} elements: ", x));                
                var manualResult = RunTimedFunction(RunManual, string.Format("Manual with {0} elements: ", x));

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private List<Customer> _customers = new List<Customer>();

        private Customer GetCustomerFromDB()
        {
            return new Customer()
            {
                DateOfBirth = RandomDay(),
                FirstName = RandomString(7),
                LastName = RandomString(8),
                NumberOfOrders = RandomInt(1, 100),
                Sub = new CustomerSubClass { Name = RandomString(10) },
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
                Id = i,
                Date = RandomDay(),
                Name = RandomString(6),
                Inners = new List<SimpleObjectInner>(2)
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
                Customer2 customer = GetCustomer2FromDB();
                this._customers.Add(customer);
            }
        }

        private List<TR> RunMapper<T, TR>(IMap<T, TR> map, Stopwatch s)
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (var customer in this._customers)
            {
                s.Start();
                var customerViewItem = map.Do(customer);
                s.Stop();
                //customers.Add(customerViewItem);
            }
            //return customers;
            return null;
        }

        private List<CustomerViewItem> RunAutoMapper<T, TR>(Stopwatch s)
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (var customer in this._customers)
            {
                s.Start();
                var customerViewItem = AutoMapper.Mapper.Map<T, TR>((T)(object)customer);
                s.Stop();
                //customers.Add(customerViewItem);
            }
            //return customers;
            return null;
        }

        private List<CustomerViewItem> RunManual(Stopwatch s)
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {
                s.Start();
                var customer1 = customer;
                var customerViewManual = new CustomerViewItem()
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth,
                    NumberOfOrders = customer.NumberOfOrders,
                    SubName = customer.Sub.Name
                };
                //    ViewItems = new List<SimpleObjectViewItem>(5)
                //}.Apply(o => customer1.ViewItems.ForEach(simpleObject => o.ViewItems.Add(new SimpleObjectViewItem { Date = simpleObject.Date, Name = simpleObject.Name })));
                s.Stop();
                //customers.Add(customerViewManual);
            }
            //return customers;
            return null;
        }

        //private List<CustomerViewItem2> RunManual2(Stopwatch s)
        //{
        //    foreach (var customer in this._customers)
        //    {
        //        s.Start();
        //        var customerViewManual = new CustomerViewItem2()
        //        {
        //            FirstName = customer.FirstName,
        //            LastName = customer.LastName,
        //            DateOfBirth = customer.DateOfBirth,
        //            NumberOfOrders = customer.NumberOfOrders,
        //            SubName = customer.Sub.Name,
        //            SubSubItem = new CustomerSubViewItem { Name = customer.Sub.Name },
        //            ViewItems = new SimpleObjectViewItem[5]
        //        }.Apply(o => 5.For(i => o.ViewItems.SetValue(new SimpleObjectViewItem()
        //        {
        //            Date = customer.ViewItems[i].Date,
        //            Name = customer.ViewItems[i].Name,
        //            Inners = new List<SimpleObjectViewItemInner>(2)
        //        {
        //            new SimpleObjectViewItemInner{Inner = "123"}, new SimpleObjectViewItemInner{Inner = "543"}
        //        }
        //        }, i)));
        //        s.Stop();
        //    }

        //    return null;
        //}

        private T RunTimedFunction<T>(Func<Stopwatch, T> f, string text)
        {
            Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            var result = f(stopwatch);
            //stopwatch.Stop();            
            Console.WriteLine(string.Format(text + stopwatch.ElapsedMilliseconds));

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
