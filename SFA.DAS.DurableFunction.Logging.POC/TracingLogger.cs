using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SFA.DAS.DurableFunction.Logging.POC
{
    public class TracingLogger : ITracingLogger
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TracingLogger(ILogger logger)
        {
            _logger = logger;
            //_configuration = configuration;
        }

        public void LogInformation(string message, string uln = null, string legalEntityId = null)
        {
            _logger.LogInformation(message);

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = "11111111-2222-3333-4444-555555555555"; //_configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            var customProperties = new Dictionary<string, string>
            {
                { "Uln", uln },
                { "LegalEntityId", legalEntityId }
            };
            telemetryClient.TrackTrace(message, customProperties); // works with TrackEvent too
            telemetryClient.Flush();
        }
    }

    public interface ITracingLogger
    {
        void LogInformation(string message, string uln = null, string legalEntityId = null);
    }

}
