using AutoFixture;
using AutoFixture.Xunit3;

namespace Backend.Tests;

public sealed class TestCustomizationsAttribute : AutoDataAttribute
{
    public TestCustomizationsAttribute()
        : base(() =>
        {
            return new Fixture().Customize(new TestCustomizations());
        })
    { }
}

public sealed class InlineTestCustomizationsAttribute : InlineAutoDataAttribute
{
    public InlineTestCustomizationsAttribute(params object[] args)
        : base(() => new Fixture().Customize(new TestCustomizations()), args)
    {}
}

public sealed class TestCustomizations : CompositeCustomization
{
}