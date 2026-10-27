using team1.Core.Repositories;

namespace team1.Core.Tests
{
    public class PlaceRepositoryTests
    {
        [Fact]
        public void Hiddenの場所は一覧にも取得にも出さない()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "general.json"), """
                    [
                      { "Id": "shown", "Name": "表示", "Floor": 1 },
                      { "Id": "secret", "Name": "非表示", "Floor": 1, "Hidden": true }
                    ]
                    """);
                var repository = new JsonPlaceRepository(dir);

                Assert.Equal(new[] { "shown" }, repository.All.Select(p => p.Id));
                Assert.Null(repository.Get("secret"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
