using System.Text.Json;

namespace team1.Core.Repositories
{
    /// <summary>
    /// お気に入りを JSON ファイルに保存する。変更のたびに UI スレッド上で同期的に書き込む
    /// (ファイルは小さく、別スレッドで書くとコレクションの同時変更や書き込みの競合が起きるため)
    /// </summary>
    public class JsonFavoriteRepository : IFavoriteRepository
    {
        private readonly string _filePath;
        private readonly HashSet<string> _ids;

        /// <param name="filePath">保存先</param>
        /// <param name="legacyFilePath">旧版の保存先。保存先にファイルが無ければここから引き継ぐ</param>
        public JsonFavoriteRepository(string filePath, string? legacyFilePath = null)
        {
            _filePath = filePath;

            string? source = File.Exists(filePath) ? filePath
                           : legacyFilePath != null && File.Exists(legacyFilePath) ? legacyFilePath
                           : null;
            _ids = source == null ? new HashSet<string>() : TryLoad(source);
        }

        public IReadOnlySet<string> Ids => _ids;

        public void Add(string id)
        {
            if (_ids.Add(id)) Save();
        }

        public void Remove(string id)
        {
            if (_ids.Remove(id)) Save();
        }

        private static HashSet<string> TryLoad(string path)
        {
            try
            {
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(path)) ?? new HashSet<string>();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // 壊れたファイルで起動できなくなるより、空から始める方を選ぶ
                return new HashSet<string>();
            }
        }

        private void Save()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_ids));
        }
    }

    /// <summary>展示モード用。来場者同士で共有されないよう、保存も表示もしない</summary>
    public class DisabledFavoriteRepository : IFavoriteRepository
    {
        public IReadOnlySet<string> Ids { get; } = new HashSet<string>();

        public void Add(string id) { }

        public void Remove(string id) { }
    }
}
