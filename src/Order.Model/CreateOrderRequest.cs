using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Order.Model
{
    public class CreateOrderRequest
    {
        [Required]
        public Guid? ResellerId { get; set; }

        [Required]
        public Guid? CustomerId { get; set; }

        [Required]
        [MinLength(1)]
        public IEnumerable<CreateOrderItemRequest> Items { get; set; }
    }
    
    public class CreateOrderItemRequest
    {
        [Required]
        public Guid? ServiceId { get; set; }

        [Required]
        public Guid? ProductId { get; set; }

        [Required]
        [Range(1, 1000000000)]
        public int Quantity { get; set; }
    }
}
