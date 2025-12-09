using Order.Data.Entities;
using Order.Model;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data
{
    public interface IOrderRepository
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();

        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName);

        Task<OrderSummary> UpdateOrderStatusAsync(Guid orderId, string statusName);

        Task<bool> StatusExistsAsync(string statusName);

        Task<OrderDetail> CreateOrderAsync(CreateOrderRequest request);

        Task<bool> ProductExistsAsync(Guid productId);

        Task<bool> ServiceExistsAsync(Guid serviceId);

        Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync();
    }
}
