var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca");

// A local container in development; Azure Database for PostgreSQL (passwordless, via the app's managed identity) when deployed.
var blogDb = builder.AddAzurePostgresFlexibleServer("postgres")
	.RunAsContainer(postgres => postgres.WithDataVolume())
	.AddDatabase("blogdb");

var blobs = builder.AddAzureStorage("storage")
	.RunAsEmulator(azurite => azurite.WithDataVolume())
	.AddBlobs("blobs");

var adminKey = builder.AddParameter("admin-key", secret: true);
var metricsKey = builder.AddParameter("metrics-key", secret: true);

// Custom domains and their managed certificates (created with `az containerapp hostname bind`); values are in
// appsettings.json. Declaring them here stops `aspire deploy` from removing them.
var apexDomain = builder.AddParameter("apex-domain");
var apexCertificate = builder.AddParameter("apex-certificate");
var wwwDomain = builder.AddParameter("www-domain");
var wwwCertificate = builder.AddParameter("www-certificate");

builder.AddProject<Projects.BenBowenBlog_Web>("webfrontend")
	.WithExternalHttpEndpoints()
	.WithHttpHealthCheck("/health")
	.WithReference(blogDb)
	.WaitFor(blogDb)
	.WithReference(blobs)
	.WaitFor(blobs)
	.WithEnvironment("Admin__Key", adminKey)
	.WithEnvironment("Metrics__Key", metricsKey)
	// Container Apps terminates TLS in a proxy; this makes the app see https and the real client IP.
	.WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
	// Exactly one instance: no cold starts, and admin pages keep their Blazor circuit on the same instance.
	.PublishAsAzureContainerApp((_, app) => {
		app.Template.Scale.MinReplicas = 1;
		app.Template.Scale.MaxReplicas = 1;
		app.ConfigureCustomDomain(apexDomain, apexCertificate);
		app.ConfigureCustomDomain(wwwDomain, wwwCertificate);
	});

builder.Build().Run();