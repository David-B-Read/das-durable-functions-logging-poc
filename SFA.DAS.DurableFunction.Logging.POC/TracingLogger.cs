using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
                        
            var customProperties = new Dictionary<string, string>
            {
                { "Uln", uln },
                { "LegalEntityId", legalEntityId }
            };
            var telemetryClient = GetTelemetryClient();
            telemetryClient.TrackTrace(message, customProperties); // works with TrackEvent too
            telemetryClient.Flush();
        }

        public void LogError(string message, Exception exception, string uln = null, string legalEntityId = null)
        {
            _logger.LogError(exception, message);

            var customProperties = new Dictionary<string, string>
            {
                { "Uln", uln },
                { "LegalEntityId", legalEntityId }
            };
            var telemetryClient = GetTelemetryClient();
            telemetryClient.TrackException(exception, customProperties); 
            telemetryClient.Flush();
        }

        private TelemetryClient GetTelemetryClient()
        {
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = _configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
            return new TelemetryClient(telemetryConfiguration);
        }
    }

    public interface ITracingLogger
    {
        void LogInformation(string message, string uln = null, string legalEntityId = null);
        void LogError(string message, Exception exception, string uln = null, string legalEntityId = null);
    }

}
