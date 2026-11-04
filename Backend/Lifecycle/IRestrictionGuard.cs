namespace Backend.Lifecycle;

public interface IRestrictionGuard
{
    string Name { get; }

    bool IsSatisfied(object data);
}

public interface IRestrictionGuard<T> : IRestrictionGuard
{
    bool IsSatisfied(T data);

    bool IRestrictionGuard.IsSatisfied(object data)
    {
        if (data is T specificData)
        {
            return IsSatisfied(specificData);
        }
        return false;
    }
}
