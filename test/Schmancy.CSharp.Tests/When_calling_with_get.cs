using System;
using System.Net;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace Schmancy.CSharp.Tests
{
    public class BaseSchmancyTest
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
    }

    public class When_calling_with_get: BaseSchmancyTest
    {
        [Test]
        public void It_responds_with_the_expected_response()
        {
            var actual = builder
                            .WhenRequesting("/customers", RequestType.Get)
                            .Return(HttpStatusCode.OK)
                            .HostWith(() =>
                            {
                                var request = new RestRequest("/customers", Method.GET);
                                var response = client.Execute(request);
                                return response.StatusCode;
                            });

            Assert.That(actual, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    public class When_returning_json: BaseSchmancyTest
    {
        public class Customer: IEquatable<Customer>
        {
            public string Name { get; set; }
            
            public string Address { get; set; }

            public bool Equals(Customer other)
            {
                return other != null && Equals(Name, other.Name) && Equals(Address, other.Address);
            }
        }

        [Test]
        public void It_responds_with_the_expected_json()
        {
            var customer = new Customer
            {
                Name = "George Marsupalis",
                Address = "980 South St., NY, NY"
            };

            var json = JsonConvert.SerializeObject(customer);

            var actual = builder
                            .WhenRequesting("/customers", RequestType.Get)
                            .Return(HttpStatusCode.OK)
                            .RespondWithJson(json)
                            .HostWith(() =>
                            {
                                var request = new RestRequest("/customers", Method.GET)
                                {
                                    RequestFormat = DataFormat.Json
                                };
                                var response = client.Execute(request);
                                return Tuple.Create(response.StatusCode, JsonConvert.DeserializeObject<Customer>(response.Content));
                            });

            Assert.That(actual.Item1, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(actual.Item2, Is.EqualTo(customer));
        }
        
    }
}
