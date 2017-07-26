﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using presenter.data.types;

namespace presenter.predictions
{
    public class PredictionService
    {
        private readonly string _key;
        private readonly string _uri;

        public PredictionService(string uri, string key)
        {
            _uri = uri;
            _key = key;
        }

        public async Task<ProductDemand[]> GetAmlPredictions(ProductDemand[] data)
        {
            var payload = data.Select(CreatePayload).ToList();
            var response = await GetPrediction(payload);
            var result = JsonConvert.DeserializeObject<MlPredictionResponse>(response);
            var estimations = ExtractEstimatedValue(result.Results.Output1);
            return estimations;
        }

        public Dictionary<DateTime, double> GetMovingAveragePredictions(ProductDemand[] data)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, string> CreatePayload(ProductDemand productDemand)
        {
            return new Dictionary<string, string>()
            {
                {"Locationid", productDemand.Locationid.ToString()},
                {"RecipeName", productDemand.RecipeName},
                {"PLU", productDemand.Plu.ToString()},
                {"Salesdate", productDemand.Salesdate.ToString("s")},
                {"Quantity", productDemand.Quantity.ToString()},
                {"NetSalesPrice", productDemand.NetSalesPrice.ToString()},
                {"CostPrice", productDemand.CostPrice.ToString()},
                {"Year", productDemand.Year.ToString()},
                {"Month", productDemand.Month.ToString()},
                {"Day", productDemand.Day.ToString()},
                {"WeekDay", productDemand.WeekDay.ToString()},
                {"YearDay", productDemand.YearDay.ToString()}
            };
        }

        private ProductDemand[] ExtractEstimatedValue(Dictionary<string, string>[] output)
        {
            var result = new List<ProductDemand>();
            foreach (var dic in output)
            {
                var pd = new ProductDemand();

                pd.Quantity = (int) Math.Round(float.Parse(dic["Scored Labels"]), 0);

                pd.Locationid = int.Parse(dic["Locationid"]);
                pd.RecipeName = dic["RecipeName"];
                pd.Plu = int.Parse(dic["PLU"]);
                pd.Salesdate = DateTime.Parse(dic["Salesdate"]);
                pd.NetSalesPrice = float.Parse(dic["NetSalesPrice"]);
                pd.CostPrice = float.Parse(dic["CostPrice"]);
                pd.Year = int.Parse(dic["Year"]);
                pd.Month = int.Parse(dic["Month"]);
                pd.Day = int.Parse(dic["Day"]);
                pd.WeekDay = int.Parse(dic["WeekDay"]);
                pd.YearDay = int.Parse(dic["YearDay"]);

                result.Add(pd);
            }
            return result.ToArray();
        }

        private async Task<string> GetPrediction(List<Dictionary<string, string>> payload)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>> { { "input1", payload } },
                    GlobalParameters = new Dictionary<string, string>() { }
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                client.BaseAddress = new Uri(_uri);

                var response = await client.PostAsJsonAsync(string.Empty, scoreRequest);

                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
        }
    }
}