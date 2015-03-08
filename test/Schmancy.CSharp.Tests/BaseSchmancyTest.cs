using System;
using NUnit.Framework;
using RestSharp;

namespace Schmancy.CSharp.Tests
{
    public abstract class BaseSchmancyTest
    {
        public const string URL = "http://localhost:9988";

        protected RestClient client;
        protected SchmancyBuilder builder;

        [SetUp]
        public void Before_each()
        {
            client = new RestClient(URL);
            builder = new SchmancyBuilder(URL);
        }

        protected Customer ACustomer()
        {
            return new Customer
            {
                Name = "George Marsupalis",
                Address = "980 South St., NY, NY"
            };
        }
    }

    public class Customer : IEquatable<Customer>
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public bool Equals(Customer other)
        {
            return other != null && Equals(Name, other.Name) && Equals(Address, other.Address);
        }
    }


}