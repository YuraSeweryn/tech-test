using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Order.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetOrdersAsync();
            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return order;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(statusName);
            return orders;
        }

        public async Task<OrderSummary> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                throw new HttpRequestException("Status is required.", new Exception(), HttpStatusCode.BadRequest);
            }

            var statusExists = await _orderRepository.StatusExistsAsync(statusName);
            if (!statusExists)
            {
                throw new HttpRequestException("Status not found.", new Exception(), HttpStatusCode.BadRequest);
            }

            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                throw new HttpRequestException("Order not found.", new Exception(), HttpStatusCode.NotFound);
            }

            var result = await _orderRepository.UpdateOrderStatusAsync(orderId, statusName);

            return result;
        }

        public async Task<OrderDetail> CreateOrderAsync(CreateOrderRequest request)
        {
            var items = request.Items.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                var serviceExists = await _orderRepository.ServiceExistsAsync(item.ServiceId.Value);
                if (!serviceExists)
                {
                    throw new HttpRequestException($"Item {i + 1}: Service with ID {item.ServiceId} does not exist.", new Exception(), HttpStatusCode.BadRequest);
                }

                var productExists = await _orderRepository.ProductExistsAsync(item.ProductId.Value);
                if (!productExists)
                {
                    throw new HttpRequestException($"Item {i + 1}: Product with ID {item.ProductId} does not exist.", new Exception(), HttpStatusCode.BadRequest);
                }
            }

            var duplicateItems = items
                .GroupBy(x => new { x.ServiceId, x.ProductId })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateItems.Any())
            {
                throw new HttpRequestException("Order contains duplicate items with the same ServiceId and ProductId combination.", new Exception(), HttpStatusCode.BadRequest);
            }

            var createdOrder = await _orderRepository.CreateOrderAsync(request);

            return createdOrder;
        }

        public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync()
        {
            var monthlyProfit = await _orderRepository.GetMonthlyProfitAsync();
            return monthlyProfit;
        }
    }
}
