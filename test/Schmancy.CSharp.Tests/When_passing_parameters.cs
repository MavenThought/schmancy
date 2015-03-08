using System;
using System.Net;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace Schmancy.CSharp.Tests
{
    public class Passing_parameters: BaseSchmancyTest
    {
        private Customer customer;
        private RestRequest request;

        [SetUp]
        public new void Before_each()
        {
            customer = ACustomer();

            request = new RestRequest("/customers", Method.GET)
            {
                RequestFormat = DataFormat.Json
            };

            builder = builder
                .WhenRequesting("/customers", RequestType.Get)
                .WithParameter("ids", "1,2,3,4")
                .Return(HttpStatusCode.OK)
                .RespondWithJson(JsonConvert.SerializeObject(customer));
        }

        public class When_matching_parameters : Passing_parameters
        {
            [Test]
            public void It_responds_with_the_expected_json()
            {
                var actual = builder
                    .HostWith(() =>
                    {
                        request.AddQueryParameter("ids", "1,2,3,4");
                        var response = client.Execute(request);
                        return Tuple.Create(response.StatusCode, JsonConvert.DeserializeObject<Customer>(response.Content));
                    });

                Assert.That(actual.Item1, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(actual.Item2, Is.EqualTo(customer));
            }
        }

        public class When_not_matching_parameters : Passing_parameters
        {
            [Test]
            public void It_responds_with_not_found()
            {
                var actual = builder
                    .HostWith(() =>
                    {
                        request.AddQueryParameter("ids", "5,6,7");
                        var response = client.Execute(request);
                        return response.StatusCode;
                    });
                
                Assert.That(actual, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        public class When_not_passing_parameters : Passing_parameters
        {
            [Test]
            public void It_responds_with_not_found()
            {
                var actual = builder
                    .HostWith(() =>
                    {
                        var response = client.Execute(request);
                        return response.StatusCode;
                    });

                Assert.That(actual, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

    }
}