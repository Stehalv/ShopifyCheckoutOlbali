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
    public class PriceSyncJob : IJobExt
    {
        public string Name => "PriceSyncJob";
        public string Group => "Prices";
        public string IntervalType => "Minutes";
        public int Interval => Settings.SyncPricesInterval;
        public ITrigger Trigger => TriggerBuilder.Create()
              .WithIdentity("SyncPrices", "Continuous")
              .StartNow()
              .WithSimpleSchedule(x => x
                  .WithIntervalInHours(Interval)
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
            IJobDetail job = JobBuilder.Create<PriceSyncJob>()
                   .WithIdentity(Name, Group)
                   .Build();
            var result = sched.CheckExists(job.Key).Result;
            return result;
        }
        public async Task<bool> Run(IJobExecutionContext context = null)
        {
            var result = 0;
            var tenantConfigurations = new TenantConfiguration().GetAll();
            foreach(var config in tenantConfigurations)
            {
                result = result + await new SyncService().SyncAllProductPrices(config.Id);
            }
            return true;
        }
        public IJobDetail Detail()
        {
            return JobBuilder.Create<PriceSyncJob>()
                   .WithIdentity(Name, Group)
                   .Build();
        }
    }
}