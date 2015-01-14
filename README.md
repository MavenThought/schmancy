# Schmancy
Library to test HTTP requests in .NET

Inspired by [WebMock](https://github.com/bblimke/webmock) _Schmancy_ lets you specify requests for a particular URl indicating query parameters, HTTP method to use, encoding, etc and what to send as a result.

The idea is to be able to test locally request to validate that the call works as expected.

Under the covers uses [Nancy](https://github.com/NancyFx/Nancy) to host the _fake_ request logic.
