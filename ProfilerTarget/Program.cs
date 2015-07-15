using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AOMapper;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using jetMapperTests;

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
            var map = RunTimedFunction(() =>
            {
                var o = Mapper.Create<Customer, CustomerSimpleViewItem>().Auto();                
                return o;
            }, "Mapper initialization: ");

            RunTimedFunction<object>(() =>
            {
                AutoMapper.Mapper.CreateMap<Customer, CustomerSimpleViewItem>();

                return null;
            }, "AutoMapper initialization: ");                                                 

            Console.WriteLine();
            Console.WriteLine();

            for (int x = 1; x <= 1000000; x *= 10)
            {
                PopulateCustomers(x);

                var mapperResult = RunTimedFunction(() => RunMapper(map), string.Format("Mapper with {0} elements: ", x));
                var autoMapperResult = RunTimedFunction(() => RunAutoMapper(), string.Format("AutoMapper with {0} elements: ", x));                
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

        private void PopulateCustomers(int count)
        {
            _customers.Clear();
            for (int x = 0; x < count; x++)
            {
                Customer customer = GetCustomerFromDB();
                this._customers.Add(customer);
            }
        }

        private List<CustomerViewItem> RunMapper(IMap<Customer, CustomerSimpleViewItem> map)
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {
                var customerViewItem = map.Do(customer);
                //customers.Add(customerViewItem);
            }
            //return customers;
            return null;
        }

        private List<CustomerViewItem> RunAutoMapper()
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {

                var customerViewItem = AutoMapper.Mapper.Map<Customer, CustomerSimpleViewItem>(customer);
                //customers.Add(customerViewItem);
            }
            //return customers;
            return null;
        }        

        private List<CustomerViewItem> RunManual()
        {
            //List<CustomerViewItem> customers = new List<CustomerViewItem>();

            foreach (Customer customer in this._customers)
            {
                var customerViewManual = new CustomerSimpleViewItem()
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    DateOfBirth = customer.DateOfBirth,
                    //NumberOfOrders = customer.NumberOfOrders,
                    //SubName = customer.Sub.Name
                };
                //customers.Add(customerViewManual);
            }
            //return customers;
            return null;
        }

        private T RunTimedFunction<T>(Func<T> f, string text)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = f();
            stopwatch.Stop();            
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
