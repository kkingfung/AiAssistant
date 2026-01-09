using System;

namespace AiAssistant
{
    /// <summary>
    /// じゃんけんの手
    /// </summary>
    public enum RpsChoice
    {
        Rock,     // グー
        Paper,    // パー
        Scissors  // チョキ
    }

    /// <summary>
    /// じゃんけんの結果
    /// </summary>
    public enum RpsResult
    {
        Win,   // プレイヤーの勝ち
        Lose,  // プレイヤーの負け
        Draw   // 引き分け
    }

    /// <summary>
    /// ミニゲームサービス
    /// ペットとのじゃんけんゲームを提供します
    /// </summary>
    public sealed class MiniGameService
    {
        private readonly Random _random = new();

        // 戦績
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }
        public int TotalGames => Wins + Losses + Draws;

        /// <summary>
        /// じゃんけんをプレイします
        /// </summary>
        /// <param name="playerChoice">プレイヤーの選択</param>
        /// <returns>ゲーム結果</returns>
        public RpsGameResult PlayRockPaperScissors(RpsChoice playerChoice)
        {
            // ペットの選択をランダムに決定
            var petChoice = (RpsChoice)_random.Next(3);

            // 勝敗判定
            var result = DetermineWinner(playerChoice, petChoice);

            // 戦績を更新
            switch (result)
            {
                case RpsResult.Win:
                    Wins++;
                    break;
                case RpsResult.Lose:
                    Losses++;
                    break;
                case RpsResult.Draw:
                    Draws++;
                    break;
            }

            return new RpsGameResult(playerChoice, petChoice, result);
        }

        /// <summary>
        /// 勝敗を判定します
        /// </summary>
        private RpsResult DetermineWinner(RpsChoice player, RpsChoice pet)
        {
            if (player == pet)
                return RpsResult.Draw;

            // グー > チョキ > パー > グー
            return (player, pet) switch
            {
                (RpsChoice.Rock, RpsChoice.Scissors) => RpsResult.Win,
                (RpsChoice.Scissors, RpsChoice.Paper) => RpsResult.Win,
                (RpsChoice.Paper, RpsChoice.Rock) => RpsResult.Win,
                _ => RpsResult.Lose
            };
        }

        /// <summary>
        /// 戦績をリセットします
        /// </summary>
        public void ResetStats()
        {
            Wins = 0;
            Losses = 0;
            Draws = 0;
        }

        /// <summary>
        /// 戦績の文字列を取得します
        /// </summary>
        public string GetStatsText()
        {
            if (TotalGames == 0)
                return "まだ対戦していません";

            var winRate = TotalGames > 0 ? (double)Wins / TotalGames * 100 : 0;
            return $"🏆 {Wins}勝 {Losses}敗 {Draws}分 (勝率: {winRate:F1}%)";
        }

        /// <summary>
        /// 選択の絵文字を取得します
        /// </summary>
        public static string GetChoiceEmoji(RpsChoice choice)
        {
            return choice switch
            {
                RpsChoice.Rock => "✊",
                RpsChoice.Paper => "✋",
                RpsChoice.Scissors => "✌️",
                _ => "❓"
            };
        }

        /// <summary>
        /// 選択の日本語名を取得します
        /// </summary>
        public static string GetChoiceName(RpsChoice choice)
        {
            return choice switch
            {
                RpsChoice.Rock => "グー",
                RpsChoice.Paper => "パー",
                RpsChoice.Scissors => "チョキ",
                _ => "不明"
            };
        }

        /// <summary>
        /// 結果の絵文字を取得します
        /// </summary>
        public static string GetResultEmoji(RpsResult result)
        {
            return result switch
            {
                RpsResult.Win => "🎉",
                RpsResult.Lose => "😢",
                RpsResult.Draw => "🤝",
                _ => "❓"
            };
        }

        /// <summary>
        /// 結果の日本語名を取得します
        /// </summary>
        public static string GetResultName(RpsResult result)
        {
            return result switch
            {
                RpsResult.Win => "あなたの勝ち！",
                RpsResult.Lose => "ペットの勝ち！",
                RpsResult.Draw => "あいこ！",
                _ => "不明"
            };
        }

        /// <summary>
        /// ペットの反応メッセージを取得します
        /// </summary>
        public string GetPetReaction(RpsResult result)
        {
            var reactions = result switch
            {
                RpsResult.Win => new[]
                {
                    "やられた〜！次は負けないぞ！",
                    "うぅ...強いね！",
                    "くっ...もう一回！",
                    "参りました〜"
                },
                RpsResult.Lose => new[]
                {
                    "やった〜！勝った！",
                    "へへっ、僕の勝ち♪",
                    "ふふん、まだまだだね！",
                    "読み勝ち〜！"
                },
                RpsResult.Draw => new[]
                {
                    "あいこだ！もう一回！",
                    "同じこと考えてた？",
                    "息ぴったりだね！",
                    "引き分け〜、次で決着だ！"
                },
                _ => new[] { "..." }
            };

            return reactions[_random.Next(reactions.Length)];
        }
    }

    /// <summary>
    /// じゃんけんゲーム結果
    /// </summary>
    public class RpsGameResult
    {
        public RpsChoice PlayerChoice { get; }
        public RpsChoice PetChoice { get; }
        public RpsResult Result { get; }

        public RpsGameResult(RpsChoice playerChoice, RpsChoice petChoice, RpsResult result)
        {
            PlayerChoice = playerChoice;
            PetChoice = petChoice;
            Result = result;
        }

        /// <summary>
        /// 結果の詳細テキストを取得します
        /// </summary>
        public string GetDetailText()
        {
            var playerEmoji = MiniGameService.GetChoiceEmoji(PlayerChoice);
            var petEmoji = MiniGameService.GetChoiceEmoji(PetChoice);
            var resultEmoji = MiniGameService.GetResultEmoji(Result);
            var resultName = MiniGameService.GetResultName(Result);

            return $"あなた {playerEmoji} vs {petEmoji} ペット\n{resultEmoji} {resultName}";
        }
    }
}
