using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SFA.DAS.DurableFunction.Logging.POC
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json")
                .AddEnvironmentVariables();

            var builtConfig = configBuilder.Build();

            builder.Services.Replace(new ServiceDescriptor(typeof(IConfiguration), builtConfig));
            builder.Services.AddTransient<ITracingLogger, TracingLogger>();
        }
    }
}
