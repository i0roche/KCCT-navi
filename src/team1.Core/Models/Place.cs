namespace team1.Core.Models
{
    public class Place
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        /// <summary>名前の読み (ひらがな)。ひらがな・カタカナ・ローマ字・あいまい検索に使う。無くてもよい (scripts/generate_readings が作る)</summary>
        public string Reading { get; set; } = "";
        public string Type { get; set; } = "";
        public string Building { get; set; } = "";
        public int Floor { get; set; }
        public string Department { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();

        /// <summary>true の場所は検索・一覧に出さない (地図に載せられていない部屋など。データとしては残す)</summary>
        public bool Hidden { get; set; }

        /// <summary>同名の場所を見分けられる表示名 (例: "会議室 (本部棟,2F)")</summary>
        public string DisplayName
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(Name) ? Id : Name;
                var floor = Floor > 0 ? $"{Floor}F" : "";
                return $"{name} ({Building},{floor})";
            }
        }
    }
}
