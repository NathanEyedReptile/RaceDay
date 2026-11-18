using System.Net.Http.Json;
using RaceDay.Mvc.Models.Auth;

namespace RaceDay.Mvc.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool Success, string Error)> LoginAsync(
            LoginRequest request,
            ISession session)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            try
            {
                var response = await client.PostAsJsonAsync(
                    "/api/auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "Invalid email or password.");
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result == null || string.IsNullOrWhiteSpace(result.Token))
                {
                    return (false, "Invalid response from the server.");
                }

                session.SetString("AuthToken", result.Token);
                session.SetString("UserId", result.UserId.ToString());
                session.SetString("FirstName", result.FirstName);
                session.SetString("LastName", result.LastName);
                session.SetString("Email", result.Email);
                session.SetString("UserRole", result.Role);

                return (true, string.Empty);
            }
            catch (HttpRequestException)
            {
                return (false, "Unable to connect to the RaceDay server.");
            }
        }
    }
}