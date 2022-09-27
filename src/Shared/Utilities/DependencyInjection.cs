namespace Utilities;

public enum ConfigurationServices {
	Accounting = 1,
	AcquiringProcessing,
	Billing,
	Communication,
	Customers,
	PublicContent
}

public static class DependencyInjectionExtensions {
	public static string GetPrivateAreaServiceGrpcAddress(this IConfiguration configuration, ConfigurationServices service) {
		var section = configuration.GetSection("PrivateArea");
		var serviceSection = section.GetSection(service.ToString());
		var address = serviceSection["GrpcAddress"];
		return address;
	}
}
