using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace Utilities;

public static class Logging {
	public static void ConfigureSerilog(this IHostBuilder host) {
		host.UseSerilog(
			(context, sp, logger) => {
				var destructuringPolicies = sp.GetServices<IDestructuringPolicy>().ToArray();
				if (destructuringPolicies.Any()) {
					logger.Destructure.With(destructuringPolicies);
				}

				logger.MinimumLevel.Debug();
				logger.Enrich.FromLogContext();
				logger.WriteTo.Async(
					configuration => {
						configuration.Console(
							outputTemplate:
							"{Timestamp:HH:mm:ss} [{Level:u3}] {InstanceId} [{RequestId}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
							theme: AnsiConsoleTheme.Code
						);
						configuration.Elasticsearch(
							new(new Uri(context.Configuration.GetConnectionString("Logstash"))) {
								IndexFormat = "express-mobile-{Date}"
							}
						);
					}
				);
				logger.ReadFrom.Configuration(context.Configuration);
			}
		);
	}
}
