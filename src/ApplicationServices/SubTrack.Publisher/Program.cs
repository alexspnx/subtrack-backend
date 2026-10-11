var builder = WebApplication.CreateBuilder(args);

// Orleans client: this host receives messages from the silo (e.g. via grain observers or Orleans streams)
// and forwards them to exactly one outlet: either a Kafka/RabbitMQ producer or SignalR clients — never both.
builder.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
});

var app = builder.Build();

app.Run();
