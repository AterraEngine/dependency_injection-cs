// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Helpers;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public enum LifetimeSwitch {
    Transient,
    Singleton,
    Scoped
}

public static class LifetimeSwitcher {
    public static void On(LifetimeSwitch lifetime, Action onTransient, Action onSingleton, Action onScoped) {
        switch (lifetime) {
            case LifetimeSwitch.Transient: onTransient(); break;
            case LifetimeSwitch.Singleton: onSingleton(); break;
            case LifetimeSwitch.Scoped: onScoped(); break;
            default: throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    public static Task OnAsync(LifetimeSwitch lifetime, Func<Task> onTransient, Func<Task> onSingleton, Func<Task> onScoped) {
        return lifetime switch {
            LifetimeSwitch.Transient => onTransient(),
            LifetimeSwitch.Singleton => onSingleton(),
            LifetimeSwitch.Scoped => onScoped(),
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
    }
}