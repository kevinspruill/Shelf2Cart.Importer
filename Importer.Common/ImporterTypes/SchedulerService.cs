using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models.TypeSettings;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.ImporterTypes
{
    public class SchedulerService: IImporterType
    {

        public string Name { get; set; } = "SchedulerService";

        public SchedulerServiceSettings Settings = new SchedulerServiceSettings();

        public IScheduler Scheduler;

        IImporterModule ImporterModule;

        public SchedulerService(IImporterModule importerModule) 
        {
            ImporterModule = importerModule;
            Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;

            ApplySettings();
        }

        private void ApplySettings()
        {
            var typeSettings = ImporterModule.ImporterInstance.TypeSettings;
            Settings = typeSettings as SchedulerServiceSettings;
        }

        public Task ScheduleJob<T>(string jobName, TimeSpan interval, JobDataMap newJobDataMap) where T : IJob
        {
            var job = JobBuilder.Create<T>()
                .WithIdentity(jobName)
                .UsingJobData("ImporterModuleKey", ImporterModule.GetType().AssemblyQualifiedName)
                .UsingJobData(newJobDataMap)
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
        public Task ScheduleJob<T>(string jobName, string cronExpression, JobDataMap newJobDataMap) where T : IJob
        {
            var job = JobBuilder.Create<T>()
                .WithIdentity(jobName)
                .UsingJobData("ImporterModuleKey", ImporterModule.GetType().AssemblyQualifiedName)
                .UsingJobData(newJobDataMap)
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
