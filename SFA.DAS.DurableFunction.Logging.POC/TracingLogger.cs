using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SFA.DAS.DurableFunction.Logging.POC
{
    public class TracingLogger : ITracingLogger
    {
        private readonly ILogger<TracingLogger> _logger;
        private readonly IConfiguration _configuration;

        public TracingLogger(ILogger<TracingLogger> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // Expand optional params to include identifiable data for logs
        public void LogInformation(string message, string uln = null, string legalEntityId = null)
        {
            _logger.LogInformation(message);

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = _configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
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
