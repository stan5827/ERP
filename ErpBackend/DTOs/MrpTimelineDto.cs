using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs
{
    public class MrpTimelineDto
    {
        public Guid SkuId { get; set; }
        public string SkuNumber { get; set; } = string.Empty;
        public string SkuName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal InitialStock { get; set; }
        public List<MrpTimelineEvent> Events { get; set; } = new();
    }

    public class MrpTimelineEvent
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // demand, supply, stock
        public string Source { get; set; } = string.Empty; // SO, WO, PO, Forecast
        public string Reference { get; set; } = string.Empty;
        public decimal ChangeQty { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
