using System.Text.Json;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Reads one mapped record of a stock or catalog API. Used by the pull of those API types and by
    /// the live stock lookup, so both read a record the same way. A record that lacks a required
    /// value is reported as <see cref="InvalidOperationException"/>; the caller decides what that
    /// means for the run.
    /// </summary>
    internal static class IntegrationStockWorkflow
    {
        /// <summary>The buyer's stock in hand of one material.</summary>
        public static LiveStockItemDto ReadStock(IReadOnlyList<ApiFieldMapping> mappings, JsonElement record)
        {
            string? Get(string target) => IntegrationRecordReader.Read(mappings, target, record);
            string? materialCode = Get("Stock.MaterialCode");
            decimal? quantity = IntegrationRecordReader.ParseDecimal(Get("Stock.Quantity"));
            if (string.IsNullOrWhiteSpace(materialCode) || quantity == null)
            {
                throw new InvalidOperationException("Material code and quantity are required.");
            }

            return new LiveStockItemDto
            {
                MaterialCode = materialCode,
                Quantity = quantity.Value,
                Plant = Get("Stock.Plant"),
                StorageLocation = Get("Stock.StorageLocation"),
                Uom = Get("Stock.Uom")
            };
        }

        /// <summary>A supplier's product, or the stock of one of its products.</summary>
        public static CatalogSyncItemDto ReadCatalogItem(IntegrationProcessType processType, IReadOnlyList<ApiFieldMapping> mappings, JsonElement record)
        {
            string? Get(string target) => IntegrationRecordReader.Read(mappings, target, record);
            if (processType == IntegrationProcessType.GET_CATALOG_STOCK)
            {
                string? stockSku = Get("CatalogStock.Sku");
                decimal? stock = IntegrationRecordReader.ParseDecimal(Get("CatalogStock.AvailableStock"));
                if (string.IsNullOrWhiteSpace(stockSku) || stock == null || stock < 0)
                {
                    throw new InvalidOperationException("SKU and a stock of zero or more are required.");
                }

                return new CatalogSyncItemDto
                {
                    Sku = stockSku,
                    AvailableStock = stock,
                    DiscountPercent = IntegrationRecordReader.ParseDecimal(Get("CatalogStock.DiscountPercent"))
                };
            }

            string? sku = Get("Catalog.Sku");
            string? name = Get("Catalog.Name");
            decimal? price = IntegrationRecordReader.ParseDecimal(Get("Catalog.Price"));
            string? currency = Get("Catalog.Currency");
            string? unitOfMeasure = Get("Catalog.UnitOfMeasure");
            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name) || price == null || price < 0
                || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(unitOfMeasure))
            {
                throw new InvalidOperationException("SKU, name, price, currency and unit of measure are required.");
            }

            return new CatalogSyncItemDto
            {
                Sku = sku,
                Name = name,
                Description = Get("Catalog.Description"),
                Price = price,
                Currency = currency,
                UnitOfMeasure = unitOfMeasure,
                AvailableStock = IntegrationRecordReader.ParseDecimal(Get("Catalog.AvailableStock")),
                DiscountPercent = IntegrationRecordReader.ParseDecimal(Get("Catalog.DiscountPercent"))
            };
        }
    }
}
