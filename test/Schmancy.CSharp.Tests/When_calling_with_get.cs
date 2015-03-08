using System.Net;
using NUnit.Framework;
using RestSharp;

namespace Schmancy.CSharp.Tests
{
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
}
