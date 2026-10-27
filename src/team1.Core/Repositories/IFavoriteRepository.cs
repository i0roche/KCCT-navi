namespace team1.Core.Repositories
{
    /// <summary>お気に入りに登録した場所 ID の永続化</summary>
    public interface IFavoriteRepository
    {
        IReadOnlySet<string> Ids { get; }

        /// <summary>追加して保存する。保存に失敗した場合は IOException 等を投げる</summary>
        void Add(string id);

        /// <summary>削除して保存する。保存に失敗した場合は IOException 等を投げる</summary>
        void Remove(string id);
    }
}
