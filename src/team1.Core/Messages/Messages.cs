using team1.Core.Models;

namespace team1.Core.Messages
{
    /// <summary>出発地・目的地・表示中の経路のいずれかが変わった</summary>
    public sealed record RouteStateChangedMessage;

    /// <summary>表示する階が変わった</summary>
    public sealed record FloorChangedMessage(int Floor);

    /// <summary>地図をこの場所へ移動させたい</summary>
    public sealed record FocusPlaceRequestMessage(Place Place);

    /// <summary>お気に入りが変わった</summary>
    public sealed record FavoritesChangedMessage;

    /// <summary>利用者に知らせたいこと (エラーや案内)</summary>
    public sealed record NotificationMessage(string Text);
}
