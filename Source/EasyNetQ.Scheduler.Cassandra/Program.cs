using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace EasyNetQ.Scheduler.Cassandra
{
    public class Program
    {
        private static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.EnableServiceRecovery(serviceRecoveryConfiguration =>
                {
                    serviceRecoveryConfiguration.RestartService(delayInMinutes: 1); // On the first service failure, reset service after a minute
                    serviceRecoveryConfiguration.SetResetPeriod(days: 0); // Reset failure count after every failure
                });
                hostConfiguration.RunAsLocalSystem();
                hostConfiguration.SetDescription("EasyNetQ.Scheduler");
                hostConfiguration.SetDisplayName("EasyNetQ.Scheduler");
                hostConfiguration.SetServiceName("EasyNetQ.Scheduler");

                hostConfiguration.Service<ISchedulerService>(ConfigureService);
            });
        }

        private static void ConfigureService(ServiceConfigurator<ISchedulerService> s)
        {
            s.ConstructUsing(_ => SchedulerServiceFactory.CreateScheduler());
            s.WhenStarted(service => service.Start());
            s.WhenStopped(service => service.Stop());
        }
    }
}
