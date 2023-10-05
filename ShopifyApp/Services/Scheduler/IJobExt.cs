using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyApp.Services.Scheduler
{
    public interface IJobExt : IJob
    {
        string Name { get; }
        string Group { get; }
        int Interval { get; }
        string IntervalType { get; }
        bool CheckJobStatus();
        Task<bool> Run(IJobExecutionContext context = null);
        IJobDetail Detail();
        ITrigger Trigger { get; }
    }
}
