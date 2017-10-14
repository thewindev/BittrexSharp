using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BittrexSharp.Domain;
using System.Linq;

namespace BittrexSharp.BittrexOrderSimulation
{
    /// <summary>
    /// Behaves exactly like Bittrex, except that buy and sell orders are not put through to Bittrex, and are simulated instead
    /// </summary>
    public class BittrexOrderSimulation : Bittrex
    {
        private readonly List<OpenOrder> _simulatedOpenOrders = new List<OpenOrder>();
        private readonly List<Order> _simulatedFinishedOrders = new List<Order>();
        private readonly List<CurrencyBalance> _simulatedBalances = new List<CurrencyBalance>();

        public BittrexOrderSimulation(string apiKey, string apiSecret) : base(apiKey, apiSecret)
        {
        }

        private void AddBalance(string currency, decimal quantity)
        {
            var existingBalance = _simulatedBalances.SingleOrDefault(b => b.Currency == currency);
            if (existingBalance != null)
                existingBalance.Balance += quantity;
            else
                _simulatedBalances.Add(new CurrencyBalance
                {
                    Balance = quantity,
                    Currency = currency
                });
        }

        private void RemoveBalance(string currency, decimal quantity)
        {
            var existingBalance = _simulatedBalances.Single(b => b.Currency == currency);
            existingBalance.Balance -= quantity;
        }

        public override async Task<AcceptedOrder> BuyLimit(string marketName, decimal quantity, decimal rate)
        {
            var currentRate = (await GetTicker(marketName)).Last;

            var acceptedOrderId = Guid.NewGuid().ToString();
            if (currentRate <= rate)
            {
                var order = new Order
                {
                    Closed = DateTime.Now,
                    Exchange = marketName,
                    IsOpen = false,
                    Limit = rate,
                    Opened = DateTime.Now,
                    OrderUuid = acceptedOrderId,
                    Price = quantity * rate,
                    PricePerUnit = rate,
                    Quantity = quantity
                };
                _simulatedFinishedOrders.Add(order);

                var currency = Helper.GetTargetCurrencyFromMarketName(marketName);
                AddBalance(currency, quantity);
            }
            else
            {
                var order = new OpenOrder
                {
                    Closed = DateTime.Now,
                    Exchange = marketName,
                    Limit = rate,
                    Opened = DateTime.Now,
                    OrderUuid = acceptedOrderId,
                    Price = quantity * rate,
                    PricePerUnit = rate,
                    Quantity = quantity
                };
                _simulatedOpenOrders.Add(order);
            }

            return new AcceptedOrder
            {
                Uuid = acceptedOrderId
            };
        }

        public override async Task<AcceptedOrder> SellLimit(string marketName, decimal quantity, decimal rate)
        {
            var currentRate = (await GetTicker(marketName)).Last;

            var acceptedOrderId = Guid.NewGuid().ToString();
            if (currentRate >= rate)
            {
                var order = new Order
                {
                    Closed = DateTime.Now,
                    Exchange = marketName,
                    IsOpen = false,
                    Limit = rate,
                    Opened = DateTime.Now,
                    OrderUuid = acceptedOrderId,
                    Price = -quantity * rate,
                    PricePerUnit = rate,
                    Quantity = -quantity
                };
                _simulatedFinishedOrders.Add(order);

                var currency = Helper.GetTargetCurrencyFromMarketName(marketName);
                RemoveBalance(currency, quantity);
            }
            else
            {
                var order = new OpenOrder
                {
                    Closed = DateTime.Now,
                    Exchange = marketName,
                    Limit = rate,
                    Opened = DateTime.Now,
                    OrderUuid = acceptedOrderId,
                    Price = -quantity * rate,
                    PricePerUnit = rate,
                    Quantity = -quantity
                };
                _simulatedOpenOrders.Add(order);
            }

            return new AcceptedOrder
            {
                Uuid = acceptedOrderId
            };
        }

        public override async Task CancelOrder(string orderId)
        {
            var order = _simulatedOpenOrders.Single(o => o.OrderUuid == orderId);
            _simulatedOpenOrders.Remove(order);
        }

        public override async Task<IEnumerable<OpenOrder>> GetOpenOrders(string marketName = null)
        {
            if (marketName == null) return _simulatedOpenOrders;
            return _simulatedOpenOrders.Where(o => o.Exchange == marketName).ToList();
        }

        public override async Task<IEnumerable<CurrencyBalance>> GetBalances()
        {
            return _simulatedBalances;
        }

        public override async Task<CurrencyBalance> GetBalance(string currency)
        {
            return _simulatedBalances.SingleOrDefault(b => b.Currency == currency) ?? new CurrencyBalance
            {
                Balance = 0,
                Currency = currency
            };
        }

        public override async Task<Order> GetOrder(string orderId)
        {
            var openOrder = _simulatedOpenOrders.SingleOrDefault(o => o.OrderUuid == orderId);
            if (openOrder == null) return _simulatedFinishedOrders.SingleOrDefault(o => o.OrderUuid == orderId);

            return new Order
            {
                Closed = openOrder.Closed,
                Exchange = openOrder.Exchange,
                Limit = openOrder.Limit,
                Opened = openOrder.Opened,
                OrderUuid = openOrder.OrderUuid,
                Price = openOrder.Price,
                PricePerUnit = openOrder.PricePerUnit,
                Quantity = openOrder.Quantity
            };
        }

        public override async Task<IEnumerable<HistoricOrder>> GetOrderHistory(string marketName = null)
        {
            return _simulatedFinishedOrders.Where(o => o.Exchange == marketName).Select(o => new HistoricOrder
            {
                Exchange = o.Exchange,
                Limit = o.Limit,
                OrderUuid = o.OrderUuid,
                Price = o.Price,
                PricePerUnit = o.PricePerUnit,
                Quantity = o.Quantity,
                Timestamp = o.Closed.Value
            }).ToList();
        }
    }
}
