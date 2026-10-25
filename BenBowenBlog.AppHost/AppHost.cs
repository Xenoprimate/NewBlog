var builder = DistributedApplication.CreateBuilder(args);

var blogDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("blogdb");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite.WithDataVolume())
    .AddBlobs("blobs");

var adminKey = builder.AddParameter("admin-key", secret: true);

builder.AddProject<Projects.BenBowenBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(blogDb)
    .WaitFor(blogDb)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithEnvironment("Admin__Key", adminKey);

builder.Build().Run();
