using Grpc.HealthCheck;
using GrpcService1.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<ErrorTracker>();

builder.Services.AddSingleton<HealthServiceImpl>();
builder.Services.AddHostedService<HealthStatusManager>();

var app = builder.Build();

app.MapGrpcService<WorkerService>();
app.MapGrpcService<HealthServiceImpl>();

app.Run();