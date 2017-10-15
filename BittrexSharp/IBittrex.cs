using System.Collections.Generic;
using System.Threading.Tasks;
using BittrexSharp.Domain;

namespace BittrexSharp
{
    public interface IBittrex
    {
        /// <summary>
        /// Get a list of all markets and associated metadata
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Market>> GetMarkets();

        /// <summary>
        /// Get a list of all supported currencies and associated metadata
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SupportedCurrency>> GetSupportedCurrencies();

        /// <summary>
        /// Get the current bid, ask and last prices for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        Task<Ticker> GetTicker(string marketName);

        /// <summary>
        /// Get summaries of the last 24 hours of all markets
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MarketSummary>> GetMarketSummaries();

        /// <summary>
        /// Get the summary of the last 24 hours of the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        Task<MarketSummary> GetMarketSummary(string marketName);

        /// <summary>
        /// Get the order book for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="orderType">The types of orders you want to get, use the static properties of OrderType.</param>
        /// <param name="depth"></param>
        /// <returns></returns>
        Task<OrderBook> GetOrderBook(string marketName, string orderType, int depth);

        /// <summary>
        /// Get a list of recent orders for the given market
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <returns></returns>
        Task<IEnumerable<Trade>> GetMarketHistory(string marketName);

        /// <summary>
        /// Place a buy order
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="quantity">How much of the currency you want to buy</param>
        /// <param name="rate">The price at which you want to buy</param>
        /// <returns></returns>
        Task<AcceptedOrder> BuyLimit(string marketName, decimal quantity, decimal rate);

        /// <summary>
        /// Place a sell order
        /// </summary>
        /// <param name="marketName">The name of the market, e.g. BTC-LTC</param>
        /// <param name="quantity">How much of the currency you want to sell</param>
        /// <param name="rate">The price at which you want to sell</param>
        /// <returns></returns>
        Task<AcceptedOrder> SellLimit(string marketName, decimal quantity, decimal rate);

        /// <summary>
        /// Cancel the order with the given id
        /// </summary>
        /// <param name="orderId">The uuid of the order to cancel</param>
        /// <returns></returns>
        Task CancelOrder(string orderId);

        /// <summary>
        /// Get open orders
        /// </summary>
        /// <param name="marketName">If given, only get the open orders of the given market</param>
        /// <returns></returns>
        Task<IEnumerable<OpenOrder>> GetOpenOrders(string marketName = null);

        /// <summary>
        /// Get the balance of all cryptocurrencies
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<CurrencyBalance>> GetBalances();

        /// <summary>
        /// Get the balance of the given currency
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <returns></returns>
        Task<CurrencyBalance> GetBalance(string currency);

        /// <summary>
        /// Get the deposit address for the given currency
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <returns></returns>
        Task<DepositAddress> GetDepositAddress(string currency);

        /// <summary>
        /// Send funds to another address
        /// </summary>
        /// <param name="currency">Currency symbol, e.g. BTC</param>
        /// <param name="quantity">How much of the currency should be withdrawn</param>
        /// <param name="address">The address to which the funds should be sent</param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        Task<AcceptedWithdrawal> Withdraw(string currency, decimal quantity, string address, string paymentId = null);

        /// <summary>
        /// Get a specific order
        /// </summary>
        /// <param name="orderId">The uuid of the order</param>
        /// <returns></returns>
        Task<Order> GetOrder(string orderId);

        /// <summary>
        /// Get the order history of the account
        /// </summary>
        /// <param name="marketName">If given, restricts the history to the given market</param>
        /// <returns></returns>
        Task<IEnumerable<HistoricOrder>> GetOrderHistory(string marketName = null);

        /// <summary>
        /// Get the withdrawal history
        /// </summary>
        /// <param name="currency">If given, restricts the history to the given currency</param>
        /// <returns></returns>
        Task<IEnumerable<HistoricWithdrawal>> GetWithdrawalHistory(string currency = null);

        /// <summary>
        /// Get the deposit history
        /// </summary>
        /// <param name="currency">If given, restricts the history to the given currency</param>
        /// <returns></returns>
        Task<IEnumerable<HistoricDeposit>> GetDepositHistory(string currency = null);
    }
}