using CommunityToolkit.Mvvm.Messaging;
using team1.Core.Messages;
using team1.Core.Models;

namespace team1.Core.Stores
{
    /// <summary>
    /// 出発地・目的地と表示中の経路。場所は名前ではなく Place そのものを持つので、
    /// 同名の場所 (会議室 ×6 など) を取り違えない
    /// </summary>
    public interface IRouteStateStore
    {
        Place? Start { get; }
        Place? Goal { get; }
        RouteResult? Route { get; }

        void SetStart(Place? place);
        void SetGoal(Place? place);
        void SetRoute(RouteResult? route);
        void Clear();
    }

    public class RouteStateStore(IMessenger messenger) : IRouteStateStore
    {
        public Place? Start { get; private set; }
        public Place? Goal { get; private set; }
        public RouteResult? Route { get; private set; }

        public void SetStart(Place? place)
        {
            if (Start == place) return;
            Start = place;
            Route = null; // 古い経路を出したままにしない
            Notify();
        }

        public void SetGoal(Place? place)
        {
            if (Goal == place) return;
            Goal = place;
            Route = null;
            Notify();
        }

        public void SetRoute(RouteResult? route)
        {
            Route = route;
            if (route != null)
            {
                // カテゴリ検索では目的地が探索結果で決まるので合わせる
                Start = route.Start;
                Goal = route.Goal;
            }
            Notify();
        }

        public void Clear()
        {
            Start = null;
            Goal = null;
            Route = null;
            Notify();
        }

        private void Notify() => messenger.Send(new RouteStateChangedMessage());
    }
}
