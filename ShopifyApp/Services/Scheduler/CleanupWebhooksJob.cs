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
    public class CleanupWebhooksJob : IJobExt
    {
        public string Name => "CleanupWebhooksJob";
        public string Group => "Webhooks";
        public string IntervalType => "Hours";
        public int Interval => Settings.WebhookCleanupIntervalHours;
        public ITrigger Trigger => TriggerBuilder.Create()
              .WithIdentity("WebhookCleanup", "Continuous")
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
            IJobDetail job = JobBuilder.Create<CleanupWebhooksJob>()
                   .WithIdentity(Name, Group)
                   .Build();
            var result = sched.CheckExists(job.Key).Result;
            return result;
        }
        public async Task<bool> Run(IJobExecutionContext context = null)
        {
            new Webhook().CleanUpWebhookLog();
            new Log(LogType.Information, $"Webhooks older then {Settings.WebhookLogTimeDays} days removed", LogSection.Global).Create();
            return true;
        }
        public IJobDetail Detail()
        {
            return JobBuilder.Create<CleanupWebhooksJob>()
                   .WithIdentity(Name, Group)
                   .Build();
        }
    }
}