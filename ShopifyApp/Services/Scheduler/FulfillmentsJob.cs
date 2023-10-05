using Quartz;
using Quartz.Impl;
using ShopifyApp.Models;
using ShopifyApp.Services.Scheduler;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Services.Jobs
{
    [DisallowConcurrentExecution]
    public class FulfillmentsJob : IJobExt
    {
        public string Name => "FulfillmentsJob";
        public string Group => "Fulfillments";
        public string IntervalType => "Minutes";
        public int Interval => Settings.SyncFulfillmentInterval;
        public ITrigger Trigger => TriggerBuilder.Create()
              .WithIdentity("Syncfulfullments", "Continuous")
              .StartNow()
              .WithSimpleSchedule(x => x
                  .WithIntervalInMinutes(Interval)
                  .RepeatForever())
              .Build();
        public Task Execute(IJobExecutionContext context)
        {
            var result = Run(context).Result;
            return Task.FromResult(result);
        }
        public bool CheckJobStatus()
        {
            IScheduler sched = StdSchedulerFactory.GetDefaultScheduler().Result;
            IJobDetail job = JobBuilder.Create<FulfillmentsJob>()
                   .WithIdentity(Name, Group)
                   .Build();
            var result = sched.CheckExists(job.Key).Result;
            return result;
        }
        public async Task<bool> Run(IJobExecutionContext context = null)
        {
            var updateCount = 0;
            var updatedFulfillments = "";
            var shippedOrders = new ShipmentModel().GetOrderList();
            //For each unfulfilled order
            foreach (var order in shippedOrders)
            {
                //Get the onfulfilled order record
                var unfulfilledOrder = new UnfulfilledOrder().GetByShopifyId(order.ShopifyOrderId);

                //get tenentconfig to set the correct context
                var tenantConfig = new TenantConfiguration().Get(unfulfilledOrder.TenantConfigId);
                
                //Set shopify api credentials
                var shopifyDAL = new ShopifyDAL(tenantConfig);
                //Convert Orderid to Long
                long orderId = (long)Convert.ToDecimal(order.ShopifyOrderId);
                //If no tracking details and empty fulfillment not created
                if (unfulfilledOrder.Fulfillments == 0)
                {
                    try
                    {
                        if (tenantConfig.UseSandbox)
                        {
                            //Set fulfilment status on nfulfilled object
                            unfulfilledOrder.Fulfillments = 1;
                            unfulfilledOrder.UpdateFulfillments();
                            new OrderLog($"Order set to Fulfilled in Shopify", "SyncFulfillments", tenantConfig.Id).CreateByShopifyOrderReference(orderId.ToString());
                        }
                        else if(order.OrderStatusId > (int)OrderStatuses.Accepted)
                        {
                            var shOrder = await shopifyDAL.GetOrder(orderId.ToString());
                            if (shOrder.Fulfillments.Count() == 0)
                            {
                                //create empty fulfillment in shopify
                                var response = await shopifyDAL.CreateOrderFulfillment(orderId, new ShopifySharp.Fulfillment
                                {
                                    OrderId = orderId,
                                    LocationId = tenantConfig.LocationId,
                                    NotifyCustomer = false
                                });
                            }
                            updatedFulfillments += orderId.ToString() + ", ";
                            updateCount++;
                            //Set fulfilment status on nfulfilled object
                            unfulfilledOrder.Fulfillments = 1;
                            unfulfilledOrder.UpdateFulfillments();
                            new OrderLog($"Order set to Fulfilled in Shopify", "SyncFulfillments", tenantConfig.Id).CreateByShopifyOrderReference(orderId.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        new Log(LogType.Error, $"Fulfillment failed Shopify OrderId: {order.ShopifyOrderId} from: {tenantConfig.ShopUrl} , at Syncfulfillments Errors: {ex.Message}", LogSection.Orders).Create();
                    }
                }
                //If trackingnumber exists and empty fulfilment in shopify exists
                else if (unfulfilledOrder.Fulfillments != 0)
                {
                    try
                    {
                        if(tenantConfig.UseSandbox)
                        {
                            updatedFulfillments += orderId.ToString() + ", ";
                            updateCount++;
                            // We are finsihed with fulfillment, delete unfulfilledOrder object
                            new UnfulfilledOrder().GetByShopifyId(order.ShopifyOrderId).Delete();
                            new OrderLog($"Marked as shipped and Trackingnumber added to Shopify {order.TrackingNo}", "SyncFulfillments", tenantConfig.Id).CreateByShopifyOrderReference(orderId.ToString());
                        }
                        else if (order.OrderStatusId > (int)OrderStatuses.Accepted && order.TrackingNo.IsNotNullOrEmpty())
                        {
                            //Get shopify order
                            var shopifyOrder = await shopifyDAL.GetShopifyOrder(order.ShopifyOrderId);
                            var fulfillments = shopifyOrder.Fulfillments.ToList();
                            //Set tracking url
                            var trackingUrls = new List<string>();
                            trackingUrls.Add(Settings.TrackingUrl + order.TrackingNo);
                            //Double check that he fulfilment exists in shopify
                            if (fulfillments.Count() > 0)
                            {
                                //Update fulfillment with tracking info
                                var response = await shopifyDAL.UpdateOrderFulfillment(orderId, fulfillments[0].Id.Value, new ShopifySharp.Fulfillment
                                {
                                    OrderId = orderId,
                                    LocationId = tenantConfig.LocationId,
                                    TrackingNumber = order.TrackingNo,
                                    TrackingUrls = trackingUrls.ToArray(),
                                    NotifyCustomer = true
                                });
                                updatedFulfillments += orderId.ToString() + ", ";
                                updateCount++;
                                // We are finsihed with fulfillment, delete unfulfilledOrder object
                                new UnfulfilledOrder().GetByShopifyId(order.ShopifyOrderId).Delete();
                                new OrderLog($"Marked as shipped and Trackingnumber: {order.TrackingNo} added to Shopify ", "SyncFulfillments", tenantConfig.Id).CreateByShopifyOrderReference(orderId.ToString());
                            }
                            //If not, reset fulfillment status
                            else
                            {
                                unfulfilledOrder.Fulfillments = 0;
                                unfulfilledOrder.UpdateFulfillments();
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        new Log(LogType.Error, $"Fulfillment failed Shopify OrderId: {order.ShopifyOrderId} from: {tenantConfig.ShopUrl} , at Syncfulfillments Errors: {ex.Message}", LogSection.Orders).Create();
                    }

                }
            }
            foreach (var order in shippedOrders.Where(c => c.OrderStatusId == (int)OrderStatuses.Cancelled))
            {

                //Get the onfulfilled order record
                var unfulfilledOrder = new UnfulfilledOrder().GetByShopifyId(order.ShopifyOrderId);
                unfulfilledOrder.Delete();
                updatedFulfillments += unfulfilledOrder.ShopifyOrderId.ToString() + ", ";
                updateCount++;
            }
            //Log
            if (context == null)
                new Log(LogType.Information, $"Fulfillment sync completed: " + DateTime.Now.ToString() + ", Count: " + updateCount + ", Updated orders: " + updatedFulfillments, LogSection.Global).Create();
            else
            {
                var id = context.JobDetail;
                new Log(LogType.Information, $"{id.Key} completed, Count: {updateCount}, Updated orders: {updatedFulfillments}", LogSection.Global).Create();
            }
            return true;
        }
        public IJobDetail Detail()
        {
            return JobBuilder.Create<FulfillmentsJob>()
                   .WithIdentity(Name, Group)
                   .Build();
        }
    }
}