using SentimentService;
using Shared;

var b = Host.CreateApplicationBuilder(args);

b.Services.AddSingleton<SentimentAnalyzer>();
b.Services.AddHostedService<Worker>();

b.Build().Run();