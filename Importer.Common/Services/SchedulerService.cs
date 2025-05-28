using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Services
{
    public class SchedulerService
    {
        public IScheduler Scheduler;

        public Task ScheduleJob<T>(string jobName, TimeSpan interval) where T : IJob
        {
            var job = JobBuilder.Create<T>()
                .WithIdentity(jobName)
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobName}_Trigger")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(interval)
                    .RepeatForever())
                .Build();
            return Scheduler.ScheduleJob(job, trigger);
        }

        // Overloaded method to schedule a job with a Cron expression
        public Task ScheduleJob<T>(string jobName, string cronExpression) where T : IJob
        {
            var job = JobBuilder.Create<T>()
                .WithIdentity(jobName)
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobName}_Trigger")
                .WithCronSchedule(cronExpression)
                .Build();
            return Scheduler.ScheduleJob(job, trigger);
        }

        public async Task StartSchedulerAsync()
        {
            if (Scheduler == null)
            {
                Scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            }
            if (!Scheduler.IsStarted)
            {
                await Scheduler.Start();
            }
        }

        public async Task StopSchedulerAsync()
        {
            if (Scheduler != null && !Scheduler.IsShutdown)
            {
                await Scheduler.Shutdown();
            }
        }
    }
}
