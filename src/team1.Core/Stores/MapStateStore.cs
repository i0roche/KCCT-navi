using CommunityToolkit.Mvvm.Messaging;
using team1.Core.Messages;

namespace team1.Core.Stores
{
    /// <summary>地図の表示状態 (いま表示している階)</summary>
    public interface IMapStateStore
    {
        int CurrentFloor { get; }

        int MinFloor { get; }

        int MaxFloor { get; }

        void SetFloor(int floor);
    }

    public class MapStateStore(IMessenger messenger) : IMapStateStore
    {
        public int CurrentFloor { get; private set; } = 1;

        public int MinFloor => 1;

        public int MaxFloor => 6;

        public void SetFloor(int floor)
        {
            floor = Math.Clamp(floor, MinFloor, MaxFloor);
            if (floor == CurrentFloor) return;

            CurrentFloor = floor;
            messenger.Send(new FloorChangedMessage(floor));
        }
    }
}
