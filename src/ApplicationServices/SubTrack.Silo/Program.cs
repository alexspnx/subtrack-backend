using SubTrack.Silo.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
    // Local development clustering. Replace with the real clustering/persistence providers
    // (e.g. Redis, ADO.NET, Kubernetes) per environment.
    silo.UseLocalhostClustering();
});

// Registered after UseOrleans, so it starts once the silo is up and IGrainFactory is usable.
builder.Services.AddHostedService<GrainInitializationHostedService>();

var host = builder.Build();
host.Run();
