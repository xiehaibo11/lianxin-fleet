using Lianxin.Sms.Api.Hosting;

var application = SmsApplication.Build(args);
application.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine("联信短信兼容服务：" + string.Join(", ", application.Urls)));
await application.RunAsync();
