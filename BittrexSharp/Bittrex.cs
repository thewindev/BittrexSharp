using BittrexSharp.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BittrexSharp
{
    public class Bittrex
    {
        public const string Version = "v1.1";
        public const string BaseUrl = "https://bittrex.com/api/" + Version + "/";
        public const string SignHeaderName = "apisign";

        private readonly Encoding _encoding = Encoding.UTF8;

        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiSecret;
        private byte[] _apiSecretBytes;

        public Bittrex()
        {
            this._apiKey = null;
            this._apiSecret = null;
            this._apiSecretBytes = null;
            this._httpClient = new HttpClient();
        }

        public Bittrex(string apiKey, string apiSecret)
        {
            this._apiKey = apiKey;
            this._apiSecret = apiSecret;
            this._apiSecretBytes = _encoding.GetBytes(apiSecret);
            this._httpClient = new HttpClient();
        }

        #region Helper
        private string ByteToString(byte[] buff)
        {
            string sbinary = "";
            for (int i = 0; i < buff.Length; i++)
                sbinary += buff[i].ToString("X2"); /* hex format */
            return sbinary;
        }

        private string ConvertParameterListToString(IDictionary<string, string> parameters)
        {
            if (parameters.Count == 0) return "";
            return parameters.Select(param => WebUtility.UrlEncode(param.Key) + "=" + WebUtility.UrlEncode(param.Value)).Aggregate((l, r) => l + "&" + r);
        }

        private (string uri, string hash) CreateRequestAuthentication(string uri) => CreateRequestAuthentication(uri, new Dictionary<string, string>());
        private (string uri, string hash) CreateRequestAuthentication(string uri, IDictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>(parameters);

            var nonce = DateTime.Now.Ticks;
            parameters.Add("apikey", _apiKey);
            parameters.Add("nonce", nonce.ToString());

            var parameterString = ConvertParameterListToString(parameters);
            var completeUri = uri + "?" + parameterString;

            var uriBytes = _encoding.GetBytes(completeUri);
            using (var hmac = new HMACSHA512(_apiSecretBytes))
            {
                var hash = hmac.ComputeHash(uriBytes);
                var hashText = ByteToString(hash);
                return (completeUri, hashText);
            }
        }

        protected HttpRequestMessage CreateRequest(HttpMethod httpMethod, string uri, bool includeAuthentication = true) => CreateRequest(httpMethod, uri, new Dictionary<string, string>(), includeAuthentication);
        protected HttpRequestMessage CreateRequest(HttpMethod httpMethod, string uri, IDictionary<string, string> parameters, bool includeAuthentication)
        {
            if (includeAuthentication)
            {
                (var completeUri, var hash) = CreateRequestAuthentication(uri, parameters);
                var request = new HttpRequestMessage(httpMethod, completeUri);
                request.Headers.Add(SignHeaderName, hash);
                return request;
            }
            else
            {
                var parameterString = ConvertParameterListToString(parameters);
                var completeUri = uri + "?" + parameterString;
                var request = new HttpRequestMessage(httpMethod, completeUri);
                return request;
            }
        }

        protected async Task<JToken> Request(HttpMethod httpMethod, string uri, bool includeAuthentication = true) => await Request(httpMethod, uri, new Dictionary<string, string>(), includeAuthentication);
        protected async Task<JToken> Request(HttpMethod httpMethod, string uri, IDictionary<string, string> parameters, bool includeAuthentication = true)
        {
            var request = CreateRequest(HttpMethod.Get, uri, parameters, includeAuthentication);
            HttpResponseMessage response = null;
            while (response == null)
            {
                try
                {
                    response = await _httpClient.SendAsync(request);
                }
                catch (Exception)
                {
                    response = null;
                }
            }
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<BittrexResponse>(content);
            if (!result.Success) throw new Exception("Request failed: " + result.Message);
            return result.Result;
        }
        #endregion

        #region Public Api
        /// <summary>
        /// Get a list of all markets and associated metadata
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Market>> GetMarkets()
        {
            var uri = BaseUrl + "public/getmarkets";
            var jsonResponse = await Request(HttpMethod.Get, uri, false);
            var markets = jsonResponse.ToObject<IEnumerable<Market>>();
            return markets;
        }

        /// <summary>
        /// Get a list of all supported currencies and associated metadata
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<SupportedCurrency>> GetSupportedCurrencies()
        {
            var uri = BaseUrl + "public/getcurrencies";
            var jsonResponse = await Request(HttpMethod.Get, uri, false);
            var supportedCurrencies = jsonResponse.ToObject<IEnumerable<SupportedCurrency>>();
            return supportedCurrencies;
        }

        /// <summary>
        /// Get the current bid, ask and last prices for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        public virtual async Task<Ticker> GetTicker(string marketName)
        {
            var uri = BaseUrl + "public/getticker";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters, false);
            var ticker = jsonResponse.ToObject<Ticker>();
            if (ticker == null) return null;
            ticker.MarketName = marketName;
            return ticker;
        }

        /// <summary>
        /// Get summaries of the last 24 hours of all markets
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<MarketSummary>> GetMarketSummaries()
        {
            var uri = BaseUrl + "public/getmarketsummaries";
            var jsonResponse = await Request(HttpMethod.Get, uri, false);
            var marketSummaries = jsonResponse.ToObject<IEnumerable<MarketSummary>>();
            return marketSummaries;
        }

        /// <summary>
        /// Get the summary of the last 24 hours of the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        public virtual async Task<MarketSummary> GetMarketSummary(string marketName)
        {
            var uri = BaseUrl + "public/getmarketsummary";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters, false);
            var marketSummary = jsonResponse.ToObject<MarketSummary>();
            return marketSummary;
        }

        /// <summary>
        /// Get the order book for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="orderType">The types of orders you want to get, use the static properties of OrderType.</param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public virtual async Task<OrderBook> GetOrderBook(string marketName, string orderType, int depth)
        {
            var uri = BaseUrl + "public/getorderbook";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName },
                { "type", orderType },
                { "depth", depth.ToString() }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters, false);
            var orderBook = new OrderBook();

            if (orderType == OrderType.Both)
                orderBook = jsonResponse.ToObject<OrderBook>();
            else if (orderType == OrderType.Buy)
                orderBook.Buy = jsonResponse.ToObject<IEnumerable<OrderBookEntry>>();
            else if (orderType == OrderType.Sell)
                orderBook.Sell = jsonResponse.ToObject<IEnumerable<OrderBookEntry>>();

            orderBook.MarketName = marketName;
            return orderBook;
        }

        /// <summary>
        /// Get a list of recent orders for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Trade>> GetMarketHistory(string marketName)
        {
            var uri = BaseUrl + "public/getmarkethistory";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters, false);
            var orders = jsonResponse.ToObject<IEnumerable<Trade>>();
            return orders;
        }
        #endregion

        #region Market Api
        /// <summary>
        /// Place a buy order
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="quantity">How much of the currency you want to buy</param>
        /// <param name="rate">The price at which you want to buy</param>
        /// <returns></returns>
        public virtual async Task<AcceptedOrder> BuyLimit(string marketName, decimal quantity, decimal rate)
        {
            var uri = BaseUrl + "market/buylimit";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName },
                { "quantity", quantity.ToString() },
                { "rate", rate.ToString() }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var acceptedOrder = jsonResponse.ToObject<AcceptedOrder>();
            return acceptedOrder;
        }

        /// <summary>
        /// Place a sell order
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="quantity">How much of the currency you want to sell</param>
        /// <param name="rate">The price at which you want to sell</param>
        /// <returns></returns>
        public virtual async Task<AcceptedOrder> SellLimit(string marketName, decimal quantity, decimal rate)
        {
            var uri = BaseUrl + "market/selllimit";
            var parameters = new Dictionary<string, string>
            {
                { "market", marketName },
                { "quantity", quantity.ToString() },
                { "rate", rate.ToString() }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var acceptedOrder = jsonResponse.ToObject<AcceptedOrder>();
            return acceptedOrder;
        }

        /// <summary>
        /// Cancel the order with the given id
        /// </summary>
        /// <param name="orderId">The uuid of the order to cancel</param>
        /// <returns></returns>
        public virtual async Task CancelOrder(string orderId)
        {
            var uri = BaseUrl + "market/cancel";
            var parameters = new Dictionary<string, string>
            {
                { "uuid", orderId }
            };
            await Request(HttpMethod.Get, uri, parameters);
        }

        /// <summary>
        /// Get open orders
        /// </summary>
        /// <param name="marketName">If given, only get the open orders of the given market</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<OpenOrder>> GetOpenOrders(string marketName = null)
        {
            var uri = BaseUrl + "market/getopenorders";
            var parameters = new Dictionary<string, string>();
            if (marketName != null) parameters.Add("market", marketName);

            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var openOrders = jsonResponse.ToObject<IEnumerable<OpenOrder>>();
            return openOrders;
        }
        #endregion

        #region Account Api
        /// <summary>
        /// Get the balance of all cryptocurrencies
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<CurrencyBalance>> GetBalances()
        {
            var uri = BaseUrl + "account/getbalances";
            var jsonResponse = await Request(HttpMethod.Get, uri);
            var balances = jsonResponse.ToObject<IEnumerable<CurrencyBalance>>();
            return balances;
        }

        /// <summary>
        /// Get the balance of the given currency
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <returns></returns>
        public virtual async Task<CurrencyBalance> GetBalance(string currency)
        {
            var uri = BaseUrl + "account/getbalance";
            var parameters = new Dictionary<string, string>
            {
                { "currency", currency }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var balance = jsonResponse.ToObject<CurrencyBalance>();
            return balance;
        }

        /// <summary>
        /// Get the deposit address for the given currency
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <returns></returns>
        public virtual async Task<DepositAddress> GetDepositAddress(string currency)
        {
            var uri = BaseUrl + "account/getdepositaddress";
            var parameters = new Dictionary<string, string>
            {
                { "currency", currency }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var depositAddress = jsonResponse.ToObject<DepositAddress>();
            return depositAddress;
        }

        /// <summary>
        /// Send funds to another address
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <param name="quantity">How much of the currency should be withdrawn</param>
        /// <param name="address">The address to which the funds should be sent</param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public virtual async Task<AcceptedWithdrawal> Withdraw(string currency, decimal quantity, string address, string paymentId = null)
        {
            var uri = BaseUrl + "account/withdraw";
            var parameters = new Dictionary<string, string>
            {
                { "currency", currency },
                { "quantity", quantity.ToString() },
                { "address", address },
                { "paymentid", paymentId }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var acceptedWithdrawal = jsonResponse.ToObject<AcceptedWithdrawal>();
            return acceptedWithdrawal;
        }

        /// <summary>
        /// Get a specific order
        /// </summary>
        /// <param name="orderId">The uuid of the order</param>
        /// <returns></returns>
        public virtual async Task<Order> GetOrder(string orderId)
        {
            var uri = BaseUrl + "account/getorder";
            var parameters = new Dictionary<string, string>
            {
                { "uuid", orderId }
            };
            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var order = jsonResponse.ToObject<Order>();
            return order;
        }

        /// <summary>
        /// Get the order history of the account
        /// </summary>
        /// <param name="marketName">If given, restricts the history to the given market</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<HistoricOrder>> GetOrderHistory(string marketName = null)
        {
            var uri = BaseUrl + "account/getorderhistory";
            var parameters = new Dictionary<string, string>();
            if (marketName != null) parameters.Add("market", marketName);

            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var orderHistory = jsonResponse.ToObject<IEnumerable<HistoricOrder>>();
            return orderHistory;
        }

        /// <summary>
        /// Get the withdrawal history
        /// </summary>
        /// <param name="currency">If given, restricts the history to the given currency</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<HistoricWithdrawal>> GetWithdrawalHistory(string currency = null)
        {
            var uri = BaseUrl + "account/getwithdrawalhistory";
            var parameters = new Dictionary<string, string>();
            if (currency != null) parameters.Add("currency", currency);

            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var withdrawalHistory = jsonResponse.ToObject<IEnumerable<HistoricWithdrawal>>();
            return withdrawalHistory;
        }

        /// <summary>
        /// Get the deposit history
        /// </summary>
        /// <param name="currency">If given, restricts the history to the given currency</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<HistoricDeposit>> GetDepositHistory(string currency = null)
        {
            var uri = BaseUrl + "account/getdeposithistory";
            var parameters = new Dictionary<string, string>();
            if (currency != null) parameters.Add("currency", currency);

            var jsonResponse = await Request(HttpMethod.Get, uri, parameters);
            var depositHistory = jsonResponse.ToObject<IEnumerable<HistoricDeposit>>();
            return depositHistory;
        }
        #endregion
    }
}
