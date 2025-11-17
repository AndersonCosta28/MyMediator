using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Mert1s.MyMediator;
using Mert1s.MyMediator.Caching;
using System.Reflection;

var services = new ServiceCollection();

// register memory cache
services.AddMemoryCache();

// register our MemoryHandlerInvokerCache via AddMyMediator overload
services.AddMyMediator(sp => new MemoryHandlerInvokerCache(sp.GetRequiredService<IMemoryCache>()));

// register handlers by scanning the assembly containing Program
services.AddMyMediator(Assembly.GetExecutingAssembly());

var provider = services.BuildServiceProvider();

var mediator = provider.GetRequiredService<IMediator>();

// Example request and handler must be defined in this sample or referenced assembly.
Console.WriteLine("Sample configured. Use mediator.SendAsync(...) to send requests.");
