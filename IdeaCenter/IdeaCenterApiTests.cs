using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenter.Models;

namespace IdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI1NTBkYTIwZi1iY2ZlLTQ3NmUtYTZlYS1hYjE4YWZkZDljZDEiLCJpYXQiOiIwNC8xNi8yMDI2IDExOjMyOjUyIiwiVXNlcklkIjoiYjE2MWE3ZWUtNjU4Ni00N2QxLTUzYjMtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJ1c2VyMDFAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6InVzZXIwMSIsImV4cCI6MTc3NjM2MDc3MiwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.ZvmqoiaIMYYY24NItvHqu2S1p8MeUaBoHmzODk-mVPM";

        private const string LoginEmail = "user01@example.com";
        private const string LoginPassword = "user01";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if(response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }

        }

        [Order(1)]
        [Test]
        public void CreateIdeaWithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea One",
                Description = "This is the first test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);

            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditLastCreatedIdea_ShouldReturnSuccess()
        {
            var editIdeaData = new IdeaDTO
            {
                Title = "Edited Test Idea One",
                Description = "This is an edited idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editIdeaData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));

        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
        }

        [Order(5)]
        [Test]
        public void CreateIdeaWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "This is the description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonEexistingIdea_ShouldReturnBadRequest()
        {
            string nonExistingId = "123456";
            var editIdeaData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an edited idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingId);
            request.AddJsonBody(editIdeaData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(7)]
        [Test]
        public void DeleteNonEexistingIdea_ShouldReturnBadRequest()
        {
            string nonExistingId = "9999999";

            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }


        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            this.client?.Dispose();
        }
    }
}