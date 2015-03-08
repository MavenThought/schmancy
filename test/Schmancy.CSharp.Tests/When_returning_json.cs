using System;
using System.Net;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace Schmancy.CSharp.Tests
{
    public class When_returning_json: BaseSchmancyTest
    {
        [Test]
        public void It_responds_with_the_expected_json()
        {
            var customer = ACustomer();

            var actual = builder
                .WhenRequesting("/customers", RequestType.Get)
                .Return(HttpStatusCode.OK)
                .RespondWithJson(JsonConvert.SerializeObject(customer))
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