// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using AterraEngine.DependencyInjection.Services;

namespace Tests.AterraEngine.DependencyInjection.Services;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class FactoryCreatedService(Guid id) : IFactoryCreatedService {
    public Guid Id { get; } = id;
}

public interface IFactoryCreatedService {
    public Guid Id { get; }
}

public class ExampleFactoryService : IExampleFactoryService {

    private int _index;
    public Guid[] SpecificIds { get; } = [
        "02b047b2-32dc-45b2-a8fc-edf72bde24c2".ToGuid(),
        "39bf1fde-bd98-4585-b335-de9802255f46".ToGuid(),
        "f52fb122-5404-41ab-a29e-3b56f445eef8".ToGuid(),
        "458f93e1-4b23-4820-a711-2cb8fb80eeb2".ToGuid(),
        "bf27877c-3976-4738-8fe6-0cb61902ad0c".ToGuid(),
        "37f9ce56-4264-453e-b798-016b47217872".ToGuid(),
        "7c154dc9-170d-4e20-a50d-827fab5a02cc".ToGuid(),
        "a05bf4ed-17fa-4bf0-8c1b-680ae496bc26".ToGuid(),
        "2b7cd190-a8e2-4e7a-86d2-bc11224d612d".ToGuid(),
        "c501072e-1c9a-4166-a538-b08bfe9c4933".ToGuid()
    ];

    public IFactoryCreatedService Create(IScopedProvider scopedProvider) {
        Guid id = SpecificIds[_index++ % SpecificIds.Length];
        return new FactoryCreatedService(id);
    }
}

public interface IExampleFactoryService : IFactoryService<IFactoryCreatedService> {
    public Guid[] SpecificIds { get; }
}
