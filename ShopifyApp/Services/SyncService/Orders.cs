using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Data;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;

namespace ShopifyApp.Services
{
    public partial class SyncService
    {
        private static ShopifyDAL _shopDAL;
        public bool CreateOrder(Models.Webhook hook)
        {
            var tenantConfig = new TenantConfiguration().Get(hook.TenantConfigId);
            _shopDAL = new ShopifyDAL(tenantConfig);
            try
            {
                if (hook != null)
                {
                    var order = _shopDAL.GetOrderFromWebhook(hook);
                    var success = SyncNewOrder(tenantConfig, order);
                    if (success)
                    {
                        new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Processed);
                        new Log(LogType.Success, $"Webhook Processed {hook.WebhookId} Topic {hook.Type} ", LogSection.Webhooks, hook.WebhookId).Create();
                    }
                    return true;
                }
                else
                {
                    new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                    new Log(LogType.Error, $"Attempt to process Order webhookId {hook.WebhookId} failed, record not found", LogSection.Orders, hook.WebhookId).Create();
                    return false;
                }

            }
            catch (Exception exception)
            {
                new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                new Log(LogType.Error, $"Order creation failed webhook: {hook.WebhookId} message: {exception.Message}", LogSection.Orders, hook.WebhookId).Create();
                return false;
            }
        }
        public bool CancelOrder(Models.Webhook hook)
        {
            var tenantConfig = new TenantConfiguration().Get(hook.TenantConfigId);
            _shopDAL = new ShopifyDAL(tenantConfig);
            try
            {
                if (hook != null)
                {
                    var order = _shopDAL.GetOrderFromWebhook(hook);
                    var success = ProcessRefundOrder(tenantConfig, order);
                    success = SyncCancelledOrder(tenantConfig, order.ShopOrderId, order);
                    if (success)
                    {
                        new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Processed);
                        new Log(LogType.Success, $"Webhook Processed Id: {hook.WebhookId} Topic: {hook.Type}", LogSection.Orders, hook.WebhookId).Create();
                    }
                    return true;
                }
                else
                {
                    new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                    new Log(LogType.Error, $"Attempt to process Cancel Order webhookId {hook.WebhookId} failed for {tenantConfig.ShopUrl}, record not found", LogSection.Orders).Create();
                    return false;
                }
            }
            catch (Exception ex)
            {
                new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                new Log(LogType.Error, $"Order cancellation failed webhook: {hook.WebhookId} on {tenantConfig.ShopUrl} message: {ex.Message}", LogSection.Orders).Create();
                return false;
            }
        }
        public bool UpdateOrder(Models.Webhook hook)
        {
            var tenantConfig = new TenantConfiguration().Get(hook.TenantConfigId);
            _shopDAL = new ShopifyDAL(tenantConfig);
            try
            {
                if (hook != null)
                {
                    var order = _shopDAL.GetOrderFromWebhook(hook);
                    var success = true;
                    if (order.OrderRefunds.Any())
                        success = ProcessRefundOrder(tenantConfig, order);
                    if (order.IsShipped)
                        success = ProcessFulfillment(tenantConfig, order);
                    if (success)
                    {
                        new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Processed);
                        new Log(LogType.Success, $"Webhook Processed Id: {hook.WebhookId} Topic: {hook.Type}", LogSection.Orders, hook.WebhookId).Create();
                    }
                    return true;
                }
                else
                {
                    new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                    new Log(LogType.Error, $"Attempt to process Cancel Order webhookId {hook.WebhookId} failed for {tenantConfig.ShopUrl}, record not found", LogSection.Orders).Create();
                    return false;
                }

            }
            catch (Exception exception)
            {
                new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                new Log(LogType.Error, $"OrderRefund creation failed webhook: {hook.WebhookId} message: {exception.Message}", LogSection.Orders).Create();
                return false;
            }
        }
        public static bool SyncCancelledOrder(TenantConfiguration tenantConfig, int shopOrderId, SyncOrderObject order)
        {
            var orderId = CheckIfOrderExist(tenantConfig, shopOrderId);
            var cancelOrderRequest = new ChangeOrderStatusRequest
            {
                OrderID = orderId,
                OrderStatus = OrderStatusType.Canceled
            };
            var response = Exigo.WebService(tenantConfig).ChangeOrderStatus(cancelOrderRequest);
            if (response.Result.Status == ResultStatus.Success)
            {
                new OrderLog($"Order cancelled", order.WebhookId, tenantConfig.Id).CreateByExigoOrderId(orderId);
            }
            else
            {
                new OrderLog($"Order cancellation failed, Error: {response.Result.Errors[0].ToString()}", order.WebhookId, tenantConfig.Id).CreateByExigoOrderId(orderId);
            }
            return true;
        }
        public static bool SyncNewOrder(TenantConfiguration tenantConfig, SyncOrderObject order)
        {
            if(order.IsFlaggedFraud)
            {
                new Log(LogType.Success, $"Order sync stopped on OrderID: {order.ShopOrderId} from: {tenantConfig.ShopUrl}. Order flagged as fraud. WebhookID : {order.WebhookId}", LogSection.Orders, order.WebhookId).Create();
                return true;
            }
            var tenant = new Tenant();
            var orderConfiguration = tenant.OrderConfigurations.First(c => c.DefaultCountryCode == order.ShippingAddress.Country);
            var customerId = SyncCustomerWithOrder(tenantConfig, order, orderConfiguration.WarehouseID);
            var exigoOrderId = CheckIfOrderExist(tenantConfig, order.ShopOrderId);
            if(exigoOrderId == 0)
            {
                var orderRequest = CreateOrder(tenantConfig, orderConfiguration, order, order.ShippingAddress, customerId);
                var response = Exigo.WebService(tenantConfig).CreateOrder(orderRequest);
                if(response.Result.Status == ResultStatus.Success)
                {
                    var total = order.OrderTotal;
                    var newOrder = new Order(tenantConfig, order.ShopOrderId, response.OrderID, order.ShopOrderReference, customerId, total, order.WebhookId);
                    newOrder.Create();
                    new OrderLog(newOrder.Id, $"Order: {newOrder.ExigoOrderId} created in exigo from Shoporder: {newOrder.ShopOrderId} for customer: {newOrder.ExigoCustomerId}", order.WebhookId, tenantConfig.Id).Create();
                    UpdateOrderTaxInfo(response.OrderID, order, tenantConfig);
                    new OrderLog(newOrder.Id, $"Updated Taxinfo: {response.OrderID}  ShopId: {order.ShopOrderId}", order.WebhookId, tenantConfig.Id).Create();
                    if(order.IsPaid)
                    {
                        CreatePaymentRequest paymentReq = new CreatePaymentRequest
                        {
                            OrderID = response.OrderID,
                            PaymentDate = DateTime.Now,
                            PaymentType = PaymentType.Other3,
                            Amount = order.OrderTotal
                        };
                        var paymentRes = Exigo.WebService(tenantConfig).CreatePayment(paymentReq);
                        new OrderLog(newOrder.Id, $"Order Payment syncronized", order.WebhookId, tenantConfig.Id).Create();
                    }
                    new UnfulfilledOrder(order.ShopOrderReference, response.OrderID, tenantConfig.Id).Create();
                    new OrderLog(newOrder.Id, $"Order marked as unfulfilled", order.WebhookId, tenantConfig.Id).Create();
                }
                else
                {
                    new Log(LogType.Error, $"Order creation failed: {customerId} from: {tenantConfig.ShopUrl} ShopId: {order.ShopOrderId}, at newOrderSync Errors: {response.Result.Errors}", LogSection.Orders, order.WebhookId).Create();

                }
            }
            return true;
        }
        public static bool ProcessRefundOrder(TenantConfiguration tenantConfig, SyncOrderObject order)
        {
            var tenant = new Tenant();
            var _order = new Order().GetByShopifyId(order.ShopOrderId, tenantConfig.Id);
            if(_order != null)
            {
                var orderConfiguration = tenant.OrderConfigurations.First(c => c.DefaultCountryCode == order.ShippingAddress.Country);

                var exigoCustomerId = CheckIfCustomerExist(order.ShopCustomerId);
                if (exigoCustomerId == 0)
                {
                    exigoCustomerId = CheckIfCustomerExistByEmail(order.Customer.Email);
                }
                order.ShipMethodId = orderConfiguration.DefaultShipMethodID;
                foreach(var refund in order.OrderRefunds)
                {
                    if (!CheckIfRefundExists(refund.ShopRefundId) && (refund.Amount < 0 || refund.RefundedShipping < 0))
                    {
                        var orderRequest = CreateRefundOrder(tenantConfig, orderConfiguration, order, order.ShippingAddress, exigoCustomerId, refund);
                        orderRequest.ShippingAmountOverride = refund.RefundedShipping;
                        orderRequest.ReturnOrderID = _order.ExigoOrderId;
                        var response = Exigo.WebService(tenantConfig).CreateOrder(orderRequest);
                        if(response.Result.Status == ResultStatus.Success)
                        {
                            var total = order.OrderTotal;
                            var newOrder = new Order(tenantConfig, order.ShopOrderId, response.OrderID, order.ShopOrderReference, exigoCustomerId, total, order.WebhookId);
                            newOrder.Create();
                            new OrderLog(_order.Id, $"RefundOrder: {newOrder.ExigoOrderId} created in Exigo from Shoporder: {newOrder.ShopOrderId} for customer: {newOrder.ExigoCustomerId}", order.WebhookId, tenantConfig.Id).Create();
                            UpdateRefundOrderTaxInfo(response.OrderID, tenantConfig, refund, order);
                            new OrderLog(_order.Id, $"Updated Taxinfo for refund order: {response.OrderID}", order.WebhookId, tenantConfig.Id).Create();
                            new Refunds(tenantConfig, refund.ShopRefundId, order.ShopOrderId.ToString(), response.OrderID, total, order.WebhookId, refund.RefundedTax, refund.RefundedShipping).Create();
                            new OrderLog(_order.Id, $"RefundOrder registered in system", order.WebhookId, tenantConfig.Id).Create();
                        }
                        if(refund.Amount == _order.OrderTotal)
                        {
                            try
                            {
                                new UnfulfilledOrder().GetByShopifyId(_order.ShopOrderReference).Delete();
                            }
                            catch { }
                        }
                    }
                }
                return true;
            }
            else
            {

                new Log(LogType.Error, $"Refund cancelled, order not in exigo: {order.ShopOrderId}", LogSection.Orders, order.WebhookId).Create();
                return true;
            }
        }
        public static bool ProcessFulfillment(TenantConfiguration tenantConfig, SyncOrderObject order)
        {
            if(order.IsShipped && Settings.SyncFulfillmentFromShopify)
            {
                try
                {
                    var _order = new Order().GetByShopifyId(order.ShopOrderId, tenantConfig.Id);
                    var exigoOrderId = new Order().GetByShopifyId(order.ShopOrderId, tenantConfig.Id).ExigoOrderId;
                    var req = new ChangeOrderStatusRequest()
                    {
                        OrderID = exigoOrderId,
                        OrderStatus = OrderStatusType.Shipped
                    };
                    var response = Exigo.WebService(tenantConfig).ChangeOrderStatus(req);
                    var reqtracking = new UpdateOrderRequest
                    {
                        OrderID = exigoOrderId,
                        TrackingNumber1 = order.TrackingNumber
                    };
                    var response2 = Exigo.WebService(tenantConfig).UpdateOrder(reqtracking);
                    var orderId = new Order().GetOrderIDByExigoOrderId(exigoOrderId, tenantConfig.Id);
                    new OrderLog(orderId, $"Order updated to shipped: {order.ShopOrderId}, trackingnumber: {order.TrackingNumber}", order.WebhookId, tenantConfig.Id).Create();
                }
                catch
                {
                    new Log(LogType.Error, $"Fulfillment cancelled, order not in exigo: {order.ShopOrderId}", LogSection.Orders, order.WebhookId).Create();
                    return true;
                }
            }
            return true;
        }
        public static void UpdateOrderTaxInfo(int orderId, SyncOrderObject order, TenantConfiguration config)
        {
            try
            {
                UpdateOrderRequest updateOrderReq = new UpdateOrderRequest
                {
                    OrderID = orderId,
                    Total = order.OrderTotal,
                    TotalTax = order.TaxTotal,
                    TrackingNumber4 = order.ShopOrderReference
                };
                //TrackingNumber5 = shOrder.id.ToString() };
                UpdateOrderResponse orderRes = Exigo.WebService(config).UpdateOrder(updateOrderReq);
                if(orderRes.Result.Status != ResultStatus.Success)
                {
                    new Log(LogType.Error, $"Order update tax info failed: {orderId}  ShopId: {order.ShopOrderId}, at newOrderSync Errors: {orderRes.Result.Errors[0].ToString()}", LogSection.Orders, order.WebhookId).Create();
                }
            }
            catch (Exception e)
            {
                new Log(LogType.Error, $"Order update tax info failed: {orderId}  ShopId: {order.ShopOrderId}, at newOrderSync Errors: {e.Message}", LogSection.Orders, order.WebhookId).Create();
            }
        }
        public static void UpdateRefundOrderTaxInfo(int orderId, TenantConfiguration config, Refunds refund, SyncOrderObject order)
        {
            try
            { 
                UpdateOrderRequest updateOrderReq = new UpdateOrderRequest
                {
                    OrderID = orderId,
                    Total = refund.Amount,
                    TotalTax = refund.RefundedTax,
                    TrackingNumber4 = order.ShopOrderReference
                };
                UpdateOrderResponse orderRes = Exigo.WebService(config).UpdateOrder(updateOrderReq);
                if (orderRes.Result.Status != ResultStatus.Success)
                {
                    new Log(LogType.Error, $"RefundOrder update tax info failed: {orderId}  ShopId: {order.ShopOrderReference}, at newOrderSync Errors: {orderRes.Result.Errors[0].ToString()}", LogSection.Orders, order.WebhookId).Create();
                }
            }
            catch (Exception e)
            {
                new Log(LogType.Error, $"RefundOrder update tax info failed: {orderId}  ShopId: {order.ShopOrderId}, at newOrderSync Errors: {e.Message}", LogSection.Orders, order.WebhookId).Create();
            }
        }
        private static int CheckIfOrderExist(TenantConfiguration tenantConfig, int shopId)
        {
            var order = new Order().GetByOrderIdAndConfig(shopId, tenantConfig.Id);
            if (order != null)
                return order.ExigoOrderId;
            else
                return 0;
        }
        private static bool CheckIfRefundExists(string refundId)
        {
            var refund = new Refunds().GetByShopRefundId(refundId);
            if (refund.Any())
                return true;
            else
                return false;
        }
        private static CreateOrderRequest CreateOrder(TenantConfiguration config, TenantOrderConfiguration configuration, SyncOrderObject order, SyncShippingAddress address, int customerId)
        {
            CreateOrderRequest req = new CreateOrderRequest();
            req.ShippingAmountOverride = order.ShippingTotal; 
            if (order.ShipMethodId == 0)
                order.ShipMethodId = configuration.DefaultShipMethodID;
            if(order.DiscountCodes.Any())
            {
                var discCodes = "";
                foreach(var code in order.DiscountCodes)
                {
                    discCodes += code.Code + " ";
                }
                req.Other17 = discCodes;
            }
            if (order.ShipMethodId == 0)
                order.ShipMethodId = configuration.DefaultShipMethodID;
            req.ShipMethodID = order.ShipMethodId;
            req.OrderType = (order.IsReplacementOrder) ? OrderType.ReplacementOrder : OrderType.APIOrder;
            req.CustomerID = customerId;
            req.OrderStatus = OrderStatusType.Accepted;
            req.Notes = $"Order imported with app from {config.ShopUrl} Order#: {order.ShopOrderId}";
            req.WarehouseID = configuration.WarehouseID;
            req.PriceType = CustomService.SetPricetype(order, configuration);
            req.CurrencyCode = configuration.CurrencyCode;
            req.OrderDate = DateTime.Now;
            req.Notes = address.Notes;
            req.FirstName = address.FirstName;
            req.MiddleName = address.MiddleName;
            req.LastName = address.LastName;
            req.Email = address.Email;
            req.Phone = address.Phone;
            req.Address1 = address.Address1;
            req.Address2 = address.Address2;
            req.City = address.City;
            req.State = address.State;
            req.Zip = address.Zip;
            req.Country = address.Country;
            req = GetOrderDetails(req, order, configuration);
            return req;
        }
        private static CreateOrderRequest CreateRefundOrder(TenantConfiguration config, TenantOrderConfiguration configuration, SyncOrderObject order, SyncShippingAddress address, int customerId, Refunds refund)
        {
            CreateOrderRequest req = new CreateOrderRequest();
            req.ShippingAmountOverride = refund.RefundedShipping;
            if (order.ShipMethodId == 0)
                order.ShipMethodId = configuration.DefaultShipMethodID;
            req.ShipMethodID = order.ShipMethodId;
            req.OrderType = OrderType.ReturnOrder;
            req.CustomerID = customerId;
            req.OrderStatus = OrderStatusType.Printed;
            req.Notes = $"OrderRefund imported with app from {config.ShopUrl} Order#: {order.ShopOrderId}";
            req.WarehouseID = configuration.WarehouseID;
            req.PriceType = CustomService.SetPricetype(order, configuration);
            req.CurrencyCode = configuration.CurrencyCode;
            req.OrderDate = DateTime.Now;
            req.Notes = address.Notes;
            req.FirstName = address.FirstName;
            req.MiddleName = address.MiddleName;
            req.LastName = address.LastName;
            req.Email = address.Email;
            req.Phone = address.Phone;
            req.Address1 = address.Address1;
            req.Address2 = address.Address2;
            req.City = address.City;
            req.State = address.State;
            req.Zip = address.Zip;
            req.Country = address.Country;
            req = GetRefundOrderDetails(req, refund.RefundLineItems, configuration, order);
            return req;
        }
        private static CreateOrderRequest GetRefundOrderDetails(CreateOrderRequest req, List<ShopifySharp.RefundLineItem> lineitems, TenantOrderConfiguration config, SyncOrderObject order )
        {


            List<OrderDetailRequest> details = new List<OrderDetailRequest>();
            if(!lineitems.Any())
            {
                OrderDetailRequest det = new OrderDetailRequest();
                det.Quantity = 1;
                det.ItemCode = Settings.DefaultExigoDiscountItemCode;
                det.PriceEachOverride = 0;
                details.Add(det);
            }
            foreach (var line_item in lineitems)
            {

                OrderDetailRequest det = new OrderDetailRequest();
                det.Quantity = line_item.LineItem.Quantity.Value * -1;
                det.ItemCode = line_item.LineItem.SKU;
                det.PriceEachOverride = line_item.LineItem.Price;
                det.TaxableEachOverride = order.SubTotal;
                details.Add(det);
            }
            req.Details = details.ToArray();
            return req;
        }
        private static CreateOrderRequest GetOrderDetails(CreateOrderRequest req, SyncOrderObject shOrder, TenantOrderConfiguration config)
        {


            List<OrderDetailRequest> details = new List<OrderDetailRequest>();
            if (shOrder.TotalDiscount > 0)
            {
                OrderDetailRequest det = new OrderDetailRequest();
                det.Quantity = -1;
                det.ItemCode = Settings.DefaultExigoDiscountItemCode;
                det.PriceEachOverride = shOrder.TotalDiscount;
                det.TaxableEachOverride = shOrder.SubTotal;
                details.Add(det);
            }
            foreach (var line_item in shOrder.LineItems)
            {

                OrderDetailRequest det = new OrderDetailRequest();
                det.Quantity = line_item.Quantity.Value;
                det.ItemCode = line_item.SKU;
                det.PriceEachOverride = line_item.Price;
                det.TaxableEachOverride = shOrder.SubTotal;
                if (shOrder.DiscountApplications.Any())
                {
                    var discountedItem = CustomService.CalculatevolumeDiscount(line_item.SKU, shOrder.DiscountApplications.First(), config);
                    det.BusinessVolumeEachOverride = discountedItem.BV;
                    det.CommissionableVolumeEachOverride = discountedItem.CV;
                }
                details.Add(det);
            }
            req.Details = details.ToArray();
            return req;
        }
    }
}
