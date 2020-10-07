using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.DurableFunction.Logging.POC
{
    public class TracingLoggerFactory
    {
        public static ITracingLogger GetTracingLogger(ILogger logger)
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                var configuration = (IConfiguration)serviceScope.ServiceProvider.GetService(typeof(IConfiguration));

                return new TracingLogger(logger, configuration);
            }
        }
    }
}
