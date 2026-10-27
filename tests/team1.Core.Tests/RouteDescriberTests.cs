using team1.Core.Logic;
using team1.Core.Models;

namespace team1.Core.Tests
{
    public class RouteDescriberTests
    {
        private static MapNode Node(string id, int floor) => new() { Id = id, Floor = floor };

        [Fact]
        public void 連続する階段は1つの手順にまとめる()
        {
            var a = Node("room_1f_a", 1);
            var s1 = Node("stair_1f_x", 1);
            var s2 = Node("stair_2f_x", 2);
            var s3 = Node("stair_3f_x", 3);
            var b = Node("room_3f_b", 3);
            var route = new RouteResult(RouteStatus.Found,
                new Place { Name = "A", Building = "一般科棟", Floor = 1 },
                new Place { Name = "B", Building = "一般科棟", Floor = 3 },
                new[] { new Edge(a, s1, 1), new Edge(s1, s2, 1), new Edge(s2, s3, 1), new Edge(s3, b, 1) },
                4);

            var steps = RouteDescriber.Describe(route);

            Assert.Equal(new[] { "出発: 一般科棟 1F A", "階段で 3F へ上がる", "到着: 一般科棟 3F B" }, steps);
        }

        [Fact]
        public void 下りとエレベーターを区別する()
        {
            var e3 = Node("elv_3f_x", 3);
            var e1 = Node("elv_1f_x", 1);
            var route = new RouteResult(RouteStatus.Found,
                new Place { Name = "A", Building = "X", Floor = 3 },
                new Place { Name = "B", Building = "X", Floor = 1 },
                new[] { new Edge(e3, e1, 1) },
                1);

            Assert.Contains("エレベーターで 1F へ下りる", RouteDescriber.Describe(route));
        }

        [Fact]
        public void 見つからなかった経路は空()
        {
            var place = new Place();
            Assert.Empty(RouteDescriber.Describe(RouteResult.Failed(RouteStatus.Unreachable, place, place)));
        }
    }
}
