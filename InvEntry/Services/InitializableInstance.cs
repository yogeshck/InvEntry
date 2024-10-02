using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IInitializable
{
    void Init();
}

public class InitializableInstance<T>  : IInitializable where T : IInitializable
{
    private readonly T _instance;

    public InitializableInstance(T instance) { _instance = instance; }

    public void Init() => _instance.Init();
}


public interface IInitializableAsync
{
    Task InitAsync();
}

public class InitializableInstanceAsync<T> : IInitializableAsync where T : IInitializableAsync
{
    private readonly T _instance;

    public InitializableInstanceAsync(T instance) { _instance = instance; }

    public async Task InitAsync() => await _instance.InitAsync();
}