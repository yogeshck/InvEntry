using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Tally;

public static class IoCService
{
    public static IServiceCollection AddTallyService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ITallyConfig, TallyConfig>();
        serviceCollection.AddHttpClient("Tally")
            .ConfigureHttpClient((serviceProvider, configureClient) => 
            {
                var clientConfig = serviceProvider.GetRequiredService<ITallyConfig>();
                configureClient.BaseAddress = new (clientConfig?.BaseAddress ?? "http://localhost:9000/");
            //    configureClient.DefaultRequestHeaders.Add("Content-Type", "text/xml");
            });

        serviceCollection.AddSingleton<ITallyXMLService, TallyXMLService>();
        return serviceCollection;
    }
}
