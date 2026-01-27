#nullable enable
using System.Text.Json.Serialization;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 絆レベルの定義
    /// ユーザーとペットの関係性の深さを表します
    /// </summary>
    public enum BondLevel
    {
        /// <summary>出会い - 基本会話のみ</summary>
        Stranger = 1,

        /// <summary>知り合い - 挨拶バリエーション増加</summary>
        Acquaintance = 2,

        /// <summary>友達 - 励まし・心配の言葉</summary>
        Friend = 3,

        /// <summary>親友 - 特別な反応、秘密の話</summary>
        CloseFriend = 4,

        /// <summary>家族 - 最大限の親密さ、特別演出</summary>
        Family = 5
    }

    /// <summary>
    /// 絆レベルの詳細情報
    /// </summary>
    public sealed class BondLevelInfo
    {
        /// <summary>レベル</summary>
        public BondLevel Level { get; }

        /// <summary>日本語名</summary>
        public string Name { get; }

        /// <summary>このレベルに必要なポイント</summary>
        public int RequiredPoints { get; }

        /// <summary>解放される要素の説明</summary>
        public string UnlockedFeatures { get; }

        private BondLevelInfo(BondLevel level, string name, int requiredPoints, string unlockedFeatures)
        {
            Level = level;
            Name = name;
            RequiredPoints = requiredPoints;
            UnlockedFeatures = unlockedFeatures;
        }

        /// <summary>
        /// 全絆レベル情報
        /// </summary>
        public static readonly BondLevelInfo[] AllLevels = new[]
        {
            new BondLevelInfo(BondLevel.Stranger, "出会い", 0, "基本会話"),
            new BondLevelInfo(BondLevel.Acquaintance, "知り合い", 100, "挨拶バリエーション増加"),
            new BondLevelInfo(BondLevel.Friend, "友達", 300, "励まし・心配の言葉"),
            new BondLevelInfo(BondLevel.CloseFriend, "親友", 600, "特別な反応、秘密の話"),
            new BondLevelInfo(BondLevel.Family, "家族", 1000, "最大限の親密さ、特別演出")
        };

        /// <summary>
        /// 絆レベルから情報を取得
        /// </summary>
        public static BondLevelInfo GetInfo(BondLevel level)
        {
            return AllLevels[(int)level - 1];
        }

        /// <summary>
        /// ポイントから絆レベルを計算
        /// </summary>
        public static BondLevel CalculateLevel(int points)
        {
            if (points >= 1000) return BondLevel.Family;
            if (points >= 600) return BondLevel.CloseFriend;
            if (points >= 300) return BondLevel.Friend;
            if (points >= 100) return BondLevel.Acquaintance;
            return BondLevel.Stranger;
        }

        /// <summary>
        /// 次のレベルまでの必要ポイント（最大レベルの場合は0）
        /// </summary>
        public static int GetPointsToNextLevel(int currentPoints)
        {
            var currentLevel = CalculateLevel(currentPoints);
            if (currentLevel == BondLevel.Family) return 0;

            var nextLevelInfo = AllLevels[(int)currentLevel];
            return nextLevelInfo.RequiredPoints - currentPoints;
        }
    }
}
