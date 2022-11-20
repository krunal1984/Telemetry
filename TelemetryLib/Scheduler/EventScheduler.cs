using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using TelemetryLib.Utilities;

namespace TelemetryLib.Scheduler

{
    public class EventScheduler
    {
        public static async Task Start(TelemetryConfiguration config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            try
            {
                int timeInterval = 1;
                Int32.TryParse(config.ScheduleCachedhedEventsTime, out timeInterval);

                IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
                await scheduler.Start();
                
                IJobDetail job = JobBuilder.Create<EventJob>().Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithSimpleSchedule
                      (s =>
                         s.WithIntervalInMinutes(timeInterval)
                        .RepeatForever()
                      )
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
        }

        public static async Task Stop()
        {
            try
            {
                IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
        }
    }

    public class EventJob: IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
             await CollectionUtilities.sendEventBatch();           
        }
    }


}



