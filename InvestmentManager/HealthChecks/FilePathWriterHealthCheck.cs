using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InvestmentManager.HealthChecks
{
    public class FilePathWriterHealthCheck : IHealthCheck
    {
        private readonly string filePath;
        private readonly IReadOnlyDictionary<string, object> healCheckData;

        public FilePathWriterHealthCheck(string filePath)
        {
            this.filePath = filePath;
            healCheckData = new Dictionary<string, object>
            {
                {"filePath", filePath }
            };
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var testFile = $"{filePath}\\test.txt";
                var fs = File.Create(testFile);
                fs.Close();
                File.Delete(testFile);
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception e)
            {
                switch (context.Registration.FailureStatus)
                {
                    case HealthStatus.Degraded:
                        return Task.FromResult(HealthCheckResult.Degraded($"Issues writing to file path", e, healCheckData));

                    case HealthStatus.Healthy:
                        return Task.FromResult(HealthCheckResult.Healthy($"Issues writing to file path", healCheckData));

                    default:
                        return Task.FromResult(HealthCheckResult.Unhealthy($"Issues writing to file path", e, healCheckData));
                }
            }
        }
    }
}