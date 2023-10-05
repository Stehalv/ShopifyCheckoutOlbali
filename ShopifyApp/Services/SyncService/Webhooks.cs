using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Services
{
    public partial class SyncService
    {
        private static QueueProcessing queue = new QueueProcessing();
        public static bool ProcessWebhook(string webhookId)
        {
            return ProcessWebhook(new Webhook().GetWebhookById(webhookId));
        }
        public static void EnqueueWebhook(Webhook hook)
        {
            queue.Enqueue(hook.WebhookId);
        }
        public static bool ProcessWebhook(Webhook hook)
        {
            var success = false;
            var syncService = new SyncService();
            if (hook.Method == "CreateOrder")
                success = syncService.CreateOrder(hook);
            if (hook.Method == "CancelOrder")
                success = syncService.CancelOrder(hook);
            if (hook.Method == "UpdateOrder")
                success = syncService.UpdateOrder(hook);
            if (hook.Method == "CreateCustomer")
                success = syncService.CreateCustomer(hook);
            if (hook.Method == "UpdateCustomer")
                success = syncService.CreateCustomer(hook);
            if (success && Settings.DeleteProcessedWebhooks)
                hook.Delete();
            return success;
        }
        public static List<Webhook> GetWebhookQueueByConfig(string type, int tenantConfigId)
        {
            return new Webhook().GetWebhookByTypeAndConfig(type, tenantConfigId).ToList();
        }
        
    }
    public class QueueProcessing
    {
        private Queue<string> _jobs = new Queue<string>();
        private bool _delegateQueuedOrRunning = false;

        public void Enqueue(string webhookid)
        {
            lock (_jobs)
            {
                _jobs.Enqueue(webhookid);
                if (!_delegateQueuedOrRunning)
                {
                    _delegateQueuedOrRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                }
            }
        }

        private void ProcessQueuedItems(object ignored)
        {
            while (true)
            {
                string item;
                lock (_jobs)
                {
                    if (_jobs.Count == 0)
                    {
                        _delegateQueuedOrRunning = false;
                        break;
                    }

                    item = _jobs.Dequeue();
                }

                try
                {
                    SyncService.ProcessWebhook(item);
                }
                catch
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                    throw;
                }
            }

        }
    }
}