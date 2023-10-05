using Quartz;
using Quartz.Impl;
using ShopifyApp.Models;
using ShopifyApp.Services.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Services.Jobs
{
    public static class JobScheduler
    {
        public static async void Start()
        {
            
            IScheduler sched = await StdSchedulerFactory.GetDefaultScheduler();
            await AddJobs(sched);
            await sched.Start();

        }
        private static async Task<IScheduler> AddJobs(IScheduler sched)
        {
            foreach (var _job in Settings.Jobs)
            {
                ITrigger trigger = _job.Trigger;
                var job = _job.Detail();
                var exists = await sched.CheckExists(job.Key); 
                if (!exists)
                {

                    await sched.ScheduleJob(job, trigger);
                    new Log(LogType.Information, $"{_job.Name} Job Added to run every {_job.Interval} {_job.IntervalType}", LogSection.Global).Create();
                }
                else
                {
                    new Log(LogType.Information, $"{_job.Name} Job exists", LogSection.Global).Create();
                }
            }
            
            return sched;
        }
        public static void RunJob(string name)
        {
            var allJobs = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => typeof(IJobExt).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
            var jobType = allJobs.FirstOrDefault(c => c.Name == name);
            IJobExt job = (IJobExt)Activator.CreateInstance(jobType);
            job.Run();
        }
    }
}