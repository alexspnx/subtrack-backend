var builder = Host.CreateApplicationBuilder(args);

// Orleans client, so message handlers (directly or through domain services) can call grains via IGrainFactory.
builder.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
});

// Register the Kafka/RabbitMQ consumer HostedService and the <Message>Handler classes here.

var host = builder.Build();
host.Run();
