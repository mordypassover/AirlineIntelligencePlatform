using Microsoft.EntityFrameworkCore;using PriceCollector;using Shared;
var b=Host.CreateApplicationBuilder(args);b.Services.AddDbContext<AirlineDbContext>(o=>o.UseNpgsql(b.Configuration["POSTGRES_CONNECTION_STRING"]));b.Services.AddHostedService<Worker>();b.Build().Run();
