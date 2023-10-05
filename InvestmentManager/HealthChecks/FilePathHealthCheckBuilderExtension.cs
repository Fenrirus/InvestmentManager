using System;
using System.Collections.Generic;
using InvestmentManager.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FilePathHealthCheckBuilderExtension
    {
        public static IHealthChecksBuilder AddFilePathWriter(this IHealthChecksBuilder builder, string filePath, HealthStatus healthStatus, IEnumerable<string> tags = default)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return builder.Add(new HealthCheckRegistration("File Path Health Check", new FilePathWriterHealthCheck(filePath), healthStatus, tags));
        }
    }
}