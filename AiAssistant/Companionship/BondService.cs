#nullable enable
using System;
using System.Diagnostics;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 絆システムの実装
    /// ユーザーとペットの絆レベルを管理
    /// </summary>
    public sealed class BondService : IBondService
    {
        private readonly CompanionContext _context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">コンパニオンコンテキスト</param>
        public BondService(CompanionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public int CurrentPoints => _context.BondPoints;

        /// <inheritdoc/>
        public BondLevel CurrentLevel => _context.BondLevel;

        /// <inheritdoc/>
        public int PointsToNextLevel => _context.PointsToNextLevel;

        /// <inheritdoc/>
        public double LevelProgress
        {
            get
            {
                var currentLevelInfo = BondLevelInfo.GetInfo(CurrentLevel);
                var currentLevelStart = currentLevelInfo.RequiredPoints;

                if (CurrentLevel == BondLevel.Family)
                {
                    return 1.0; // 最大レベル
                }

                var nextLevelInfo = BondLevelInfo.AllLevels[(int)CurrentLevel];
                var nextLevelStart = nextLevelInfo.RequiredPoints;
                var levelRange = nextLevelStart - currentLevelStart;

                if (levelRange <= 0) return 1.0;

                var pointsInLevel = CurrentPoints - currentLevelStart;
                return Math.Clamp((double)pointsInLevel / levelRange, 0.0, 1.0);
            }
        }

        /// <inheritdoc/>
        public event EventHandler<BondLevelUpEventArgs>? LevelUp;

        /// <inheritdoc/>
        public event EventHandler<int>? PointsChanged;

        /// <inheritdoc/>
        public int AddPoints(BondPointSource source)
        {
            var points = GetPointsForSource(source);
            return AddPointsInternal(points, source.ToString());
        }

        /// <inheritdoc/>
        public int AddPoints(int points, string reason)
        {
            if (points <= 0) return 0;
            return AddPointsInternal(points, reason);
        }

        /// <summary>
        /// ポイント追加の内部処理
        /// </summary>
        private int AddPointsInternal(int points, string reason)
        {
            var previousLevel = CurrentLevel;
            var previousPoints = CurrentPoints;

            var leveledUp = _context.AddBondPoints(points);

            Debug.WriteLine($"[BondService] +{points} points ({reason}). Total: {CurrentPoints}, Level: {CurrentLevel}");

            // ポイント変更イベント発火
            PointsChanged?.Invoke(this, CurrentPoints);

            // レベルアップイベント発火
            if (leveledUp)
            {
                var args = new BondLevelUpEventArgs(previousLevel, CurrentLevel, CurrentPoints);
                LevelUp?.Invoke(this, args);
                Debug.WriteLine($"[BondService] Level Up! {previousLevel} -> {CurrentLevel}");
            }

            return points;
        }

        /// <summary>
        /// ソースごとのポイント数を取得
        /// </summary>
        private static int GetPointsForSource(BondPointSource source)
        {
            return source switch
            {
                BondPointSource.ChatMessage => 1,
                BondPointSource.LongConversationBonus => 5,
                BondPointSource.DailyLogin => 10,
                BondPointSource.PomodoroComplete => 3,
                BondPointSource.GoalAchieved => 5,
                BondPointSource.ReturnBonus => 3,
                _ => 0
            };
        }
    }
}
