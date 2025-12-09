using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Order.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderContext _orderContext;

        public OrderRepository(OrderContext orderContext)
        {
            _orderContext = orderContext;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Select(ConvertToOrderSummary)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .Select(x => new OrderDetail
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    CreatedDate = x.CreatedDate,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    Items = x.Items.Select(i => new Model.OrderItem
                    {
                        Id = new Guid(i.Id),
                        OrderId = new Guid(i.OrderId),
                        ServiceId = new Guid(i.ServiceId),
                        ServiceName = i.Service.Name,
                        ProductId = new Guid(i.ProductId),
                        ProductName = i.Product.Name,
                        UnitCost = i.Product.UnitCost,
                        UnitPrice = i.Product.UnitPrice,
                        TotalCost = i.Product.UnitCost * i.Quantity.Value,
                        TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                        Quantity = i.Quantity.Value
                    })
                }).SingleOrDefaultAsync();

            return order;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Where(x => x.Status.Name == statusName)
                .Select(ConvertToOrderSummary)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<OrderSummary> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            var orderIdBytes = orderId.ToByteArray();
            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .Include(x => x.Items)
                .Include(x => x.Status)
                .FirstOrDefaultAsync();
            var status = await _orderContext.OrderStatus
                .Where(x => x.Name == statusName)
                .FirstOrDefaultAsync();

            order.StatusId = status.Id;

            await _orderContext.SaveChangesAsync();

            return ConvertToOrderSummary.Compile()(order);
        }

        public async Task<bool> StatusExistsAsync(string statusName)
        {
            var status = await _orderContext.OrderStatus
                .Where(x => x.Name == statusName)
                .FirstOrDefaultAsync();

            return status != null;
        }

        public async Task<OrderDetail> CreateOrderAsync(CreateOrderRequest request)
        {
            var orderId = Guid.NewGuid();
            var orderIdBytes = orderId.ToByteArray();
            var createdDate = DateTime.UtcNow;

            var createdStatus = await _orderContext.OrderStatus
                .FirstOrDefaultAsync(x => x.Name == "Created");

            if (createdStatus == null)
            {
                createdStatus = await _orderContext.OrderStatus.FirstOrDefaultAsync();
            }

            var orderEntity = new Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = request.ResellerId.Value.ToByteArray(),
                CustomerId = request.CustomerId.Value.ToByteArray(),
                StatusId = createdStatus.Id,
                CreatedDate = createdDate
            };

            _orderContext.Order.Add(orderEntity);

            foreach (var item in request.Items)
            {
                var orderItem = new Entities.OrderItem
                {
                    Id = Guid.NewGuid().ToByteArray(),
                    OrderId = orderIdBytes,
                    ServiceId = item.ServiceId.Value.ToByteArray(),
                    ProductId = item.ProductId.Value.ToByteArray(),
                    Quantity = item.Quantity
                };

                _orderContext.OrderItem.Add(orderItem);
            }

            await _orderContext.SaveChangesAsync();

            var createdOrder = await GetOrderByIdAsync(orderId);
            return createdOrder;
        }

        public async Task<bool> ProductExistsAsync(Guid productId)
        {
            var productIdBytes = productId.ToByteArray();
            var product = await _orderContext.OrderProduct
                .Where(x => _orderContext.Database.IsInMemory()
                    ? x.Id.SequenceEqual(productIdBytes)
                    : x.Id == productIdBytes)
                .FirstOrDefaultAsync();

            return product != null;
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceId)
        {
            var serviceIdBytes = serviceId.ToByteArray();
            var service = await _orderContext.OrderService
                .Where(x => _orderContext.Database.IsInMemory()
                    ? x.Id.SequenceEqual(serviceIdBytes)
                    : x.Id == serviceIdBytes)
                .FirstOrDefaultAsync();

            return service != null;
        }

        public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync()
        {
            var completedOrders = await _orderContext.Order
                .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                .Include(x => x.Status)
                .Where(x => x.Status.Name == "Completed")
                .ToListAsync();

            var monthlyProfits = completedOrders
                .GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
                .Select(g => new MonthlyProfit
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    TotalCost = g.Sum(o => o.Items.Sum(i => i.Quantity.Value * i.Product.UnitCost)),
                    TotalPrice = g.Sum(o => o.Items.Sum(i => i.Quantity.Value * i.Product.UnitPrice)),
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month);

            return monthlyProfits;
        }

        private static Expression<Func<Entities.Order, OrderSummary>> ConvertToOrderSummary = x => new OrderSummary
        {
            Id = new Guid(x.Id),
            ResellerId = new Guid(x.ResellerId),
            CustomerId = new Guid(x.CustomerId),
            StatusId = new Guid(x.StatusId),
            StatusName = x.Status.Name,
            ItemCount = x.Items.Count,
            TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
            TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
            CreatedDate = x.CreatedDate
        };
    }
}
