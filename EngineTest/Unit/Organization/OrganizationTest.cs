using FlagsmithEngine.Organization.Models;
using Xunit;
namespace EngineTest.Unit.Organization
{
    public class OrganizationTest
    {
        [Fact]
        public void TestUniqueSlugProperty()
        {
            var orgId = 1;
            var orgName = "test";
            var organization = new OrganizationModel { Id = orgId, Name = orgName };
            Assert.EndsWith($"{orgId}-{orgName}", organization.UniqueSlug);
        }
    }
}
