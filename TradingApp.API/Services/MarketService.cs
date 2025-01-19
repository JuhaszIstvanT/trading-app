using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using TradingApp.API.Models;

namespace TradingApp.API.Services
{
    public class MarketService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public MarketService()
        {
            _httpClient = new HttpClient();
            _apiUrl = "https://api.coinlore.net/api/";
        }

        public async Task<string> GetGlobalMarketDataAsync()
        {
            var fullUrl = $"{_apiUrl}global/";

            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
        }

        public async Task<string> GetTickerDataAsync()
        {
            var fullUrl = $"{_apiUrl}tickers/?start=0&limit=50/";

            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
        }

        public async Task<string> GetSpecificTickerDataAsync(string coinIds)
        {
            var fullUrl = $"{_apiUrl}ticker/?id={coinIds}";

            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
        }
    }
}
