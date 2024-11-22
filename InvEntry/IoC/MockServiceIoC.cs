using InvEntry.Services.Mock;
using InvEntry.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.IoC;

public static class MockServiceIoC
{
    public static IServiceCollection AddMockService(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IGrnService, MockGrnService>("MockGrnService");

        return services;
    }
}
