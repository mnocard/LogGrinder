using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace LogGrinder.Tests.Services
{
    public class AutoDomainDataAttribute : AutoDataAttribute
    {
        public AutoDomainDataAttribute() : base(() => new Fixture().Customize(new AutoMoqCustomization())) { }
    }
}
