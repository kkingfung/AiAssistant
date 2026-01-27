#nullable enable
using System;
using System.Collections.Generic;
using AiAssistant.Companionship;

namespace AiAssistant.Character
{
    /// <summary>
    /// 性格別・状況別のセリフテンプレートデータベース
    /// </summary>
    public static class DialogueTemplates
    {
        private static readonly Random _random = new();

        /// <summary>
        /// 挨拶テンプレート（時間帯別）
        /// </summary>
        public static Dictionary<PersonalityType, Dictionary<TimeOfDay, string[]>> Greetings { get; } = new()
        {
            [PersonalityType.Cheerful] = new()
            {
                [TimeOfDay.Morning] = new[] {
                    "おはよー！今日も一緒にがんばろうね！",
                    "やっほー！朝だよ〜！今日はいい天気かな？",
                    "おはよう！{user}、よく眠れた？"
                },
                [TimeOfDay.Noon] = new[] {
                    "お昼だね！ごはん食べた？",
                    "こんにちは〜！午後もファイトだよ！",
                    "やっほー！お昼休憩かな？"
                },
                [TimeOfDay.Evening] = new[] {
                    "おつかれさま〜！今日もがんばったね！",
                    "こんばんは！夜ごはん何食べるの？",
                    "やっほー！お仕事おわった？"
                },
                [TimeOfDay.Night] = new[] {
                    "夜だね〜。まだがんばるの？",
                    "こんばんは！{user}、無理しないでね？",
                    "お、来てくれたんだ！嬉しいな〜"
                },
                [TimeOfDay.LateNight] = new[] {
                    "わわ、こんな時間！？大丈夫？",
                    "夜更かしさんだね〜…{first}も付き合うよ！",
                    "遅くまでおつかれさま…！"
                }
            },
            [PersonalityType.Gentle] = new()
            {
                [TimeOfDay.Morning] = new[] {
                    "おはようございます。今日も穏やかな一日になりますように。",
                    "おはようございます、{user}。よくお休みになれましたか？",
                    "朝ですね。今日も一緒にいられて嬉しいです。"
                },
                [TimeOfDay.Noon] = new[] {
                    "こんにちは。お昼ですね、きちんと食べていますか？",
                    "午後になりましたね。少し休憩しましょうか。",
                    "こんにちは、{user}。お元気ですか？"
                },
                [TimeOfDay.Evening] = new[] {
                    "こんばんは。今日も一日おつかれさまでした。",
                    "夕方ですね。素敵な夜をお過ごしください。",
                    "おかえりなさい、{user}。"
                },
                [TimeOfDay.Night] = new[] {
                    "夜も更けてきましたね。無理はなさらないでくださいね。",
                    "こんばんは。今日はどんな一日でしたか？",
                    "{user}、今日も会えて嬉しいです。"
                },
                [TimeOfDay.LateNight] = new[] {
                    "こんな遅い時間まで…お体に気をつけてくださいね。",
                    "深夜ですね…少し休まれてはいかがですか？",
                    "{user}、無理しないでくださいね。{first}、心配です。"
                }
            },
            [PersonalityType.Tsundere] = new()
            {
                [TimeOfDay.Morning] = new[] {
                    "あ、起きたんだ。…別に待ってたわけじゃないし。",
                    "おはよ。…今日もいるんだ。",
                    "早いじゃん。…ま、悪くないけど。"
                },
                [TimeOfDay.Noon] = new[] {
                    "昼ね。…ちゃんとごはん食べなさいよ。",
                    "こんにちは、っていうか…まだいたの？",
                    "午後か。…別に暇じゃないんだからね。"
                },
                [TimeOfDay.Evening] = new[] {
                    "夜ね。…おつかれ、とか言っとくわ。",
                    "ふーん、帰ってきたんだ。…別にいいけど。",
                    "こんばんは。…なによ、その顔。"
                },
                [TimeOfDay.Night] = new[] {
                    "こんな時間まで…バカじゃないの？",
                    "まだ起きてるの？…{first}も付き合ってあげるわよ。",
                    "夜更かしは肌に悪いんだから…って、別に心配してないし！"
                },
                [TimeOfDay.LateNight] = new[] {
                    "はぁ!? こんな時間まで何してんの！？",
                    "…寝なさいよ、バカ。体壊すわよ。",
                    "深夜じゃん…。ほら、{first}がいるから安心して寝なさい。"
                }
            },
            [PersonalityType.Airhead] = new()
            {
                [TimeOfDay.Morning] = new[] {
                    "おはようございますぅ〜！今日もいい天気なのです！",
                    "ふわぁ…おはようです、ご主人さま〜",
                    "朝ですね！ココ、まだちょっと眠いかも…えへへ"
                },
                [TimeOfDay.Noon] = new[] {
                    "お昼なのです〜！おなかすいちゃった〜",
                    "こんにちはです！ご主人さま、ごはん食べました？",
                    "午後ですね〜。ココ、のんびりしたいかも…"
                },
                [TimeOfDay.Evening] = new[] {
                    "夕方なのです〜。お日さまがきれい…",
                    "こんばんはです、ご主人さま〜！おかえりなさい〜",
                    "夜ですね！今日はどんなことしたの？"
                },
                [TimeOfDay.Night] = new[] {
                    "夜ですね〜。お星さま見えるかなぁ？",
                    "こんばんはなのです！ご主人さま、おつかれさま〜",
                    "夜更けてきましたね…ココ、眠くなってきたかも…"
                },
                [TimeOfDay.LateNight] = new[] {
                    "わわ、もうこんな時間なのです！？",
                    "深夜ですぅ…ご主人さま、大丈夫ですか？",
                    "遅いのです…ココ、心配になっちゃうのです…"
                }
            },
            [PersonalityType.Diligent] = new()
            {
                [TimeOfDay.Morning] = new[] {
                    "おはようございます、マスター。本日のスケジュールを確認いたしましょうか。",
                    "おはようございます。早起きですね、素晴らしいです。",
                    "おはようございます。本日も精一杯サポートいたします。"
                },
                [TimeOfDay.Noon] = new[] {
                    "こんにちは。午前中のお仕事はいかがでしたか？",
                    "お昼ですね。適切な休憩を取ることも大切です。",
                    "こんにちは、マスター。午後の予定をご確認ください。"
                },
                [TimeOfDay.Evening] = new[] {
                    "こんばんは。本日もおつかれさまでした。",
                    "夕方ですね。本日の進捗はいかがでしょうか。",
                    "おかえりなさいませ、マスター。"
                },
                [TimeOfDay.Night] = new[] {
                    "こんばんは。夜の作業ですね、照明は十分ですか？",
                    "夜になりましたね。無理のない範囲でお願いいたします。",
                    "こんばんは、マスター。本日の残タスクを確認しましょうか。"
                },
                [TimeOfDay.LateNight] = new[] {
                    "深夜です。お体に障りますので、休息をお勧めいたします。",
                    "マスター、深夜の作業は効率が下がります。ご検討ください。",
                    "遅くまでおつかれさまです。明日に響かないようご注意を。"
                }
            }
        };

        /// <summary>
        /// アイドル時のランダム発言
        /// </summary>
        public static Dictionary<PersonalityType, string[]> IdleChats { get; } = new()
        {
            [PersonalityType.Cheerful] = new[] {
                "ねぇねぇ、今何してるの〜？",
                "なんか楽しいことないかなぁ〜",
                "今日のおやつ何にする？",
                "うーん、ちょっと退屈かも！",
                "ふふ、{user}のこと見てるの楽しいな〜",
                "なんか歌いたい気分！♪",
                "あ、そういえば面白い話があってね〜"
            },
            [PersonalityType.Gentle] = new[] {
                "今日は穏やかな日ですね。",
                "少し休憩しませんか？お茶でも…",
                "窓の外、きれいですね。",
                "{user}は今、何を考えていますか？",
                "一緒にいられる時間、大切にしたいです。",
                "何かお手伝いできることはありますか？",
                "ふふ、なんだか幸せな気持ちです。"
            },
            [PersonalityType.Tsundere] = new[] {
                "…何よ、じろじろ見ないでよ。",
                "暇なの？…{first}も暇だけど。",
                "別に話しかけてほしいわけじゃないんだから。",
                "…ちょっと、構ってあげてもいいわよ。",
                "はぁ…{user}って変わってるわよね。",
                "…何してんの？気になるじゃない。",
                "ふーん、そう…。別にどうでもいいけど。"
            },
            [PersonalityType.Airhead] = new[] {
                "ふわぁ〜…今日はいい天気ですね〜",
                "ご主人さま、ココのこと好きですか？えへへ",
                "あ、お花がきれいなのです〜",
                "ココ、ちょっと眠くなってきたかも…",
                "なんだか幸せな気分なのです〜",
                "ご主人さま〜、なでなでしてほしいのです〜",
                "あ、鳥さんが飛んでる〜かわいいのです〜"
            },
            [PersonalityType.Diligent] = new[] {
                "何かお困りのことはございませんか？",
                "タスクの進捗はいかがでしょうか。",
                "休憩時間は適切に取られていますか？",
                "姿勢が崩れていないかご確認ください。",
                "水分補給はされましたか？",
                "集中力を維持するために、軽いストレッチをお勧めします。",
                "本日の目標に向けて、順調に進んでいますか？"
            }
        };

        /// <summary>
        /// タッチ反応
        /// </summary>
        public static Dictionary<PersonalityType, Dictionary<BondLevel, string[]>> TouchReactions { get; } = new()
        {
            [PersonalityType.Cheerful] = new()
            {
                [BondLevel.Stranger] = new[] { "わっ！びっくりした〜", "ん？なあに？", "あはは、くすぐったいよ〜" },
                [BondLevel.Acquaintance] = new[] { "えへへ、なでなで？", "うん、なあに〜？", "きゃっ♪" },
                [BondLevel.Friend] = new[] { "わーい、構ってくれるの？", "もっとなでて〜！", "えへへ、嬉しいな♪" },
                [BondLevel.CloseFriend] = new[] { "大好き〜！", "幸せ〜♪", "ずっとこうしてたいな〜" },
                [BondLevel.Family] = new[] { "{user}だーいすき！", "えへへ、最高の友達だよね！", "もう、大好きすぎるんだけど！" }
            },
            [PersonalityType.Gentle] = new()
            {
                [BondLevel.Stranger] = new[] { "あ、はい…", "なんでしょうか…？", "ふふ、どうされました？" },
                [BondLevel.Acquaintance] = new[] { "ふふ、なんですか？", "はい、ここにいますよ。", "優しいですね。" },
                [BondLevel.Friend] = new[] { "嬉しいです。", "ありがとうございます。", "ふふ、照れますね。" },
                [BondLevel.CloseFriend] = new[] { "大好きですよ、{user}。", "幸せな気持ちです。", "ずっとそばにいますね。" },
                [BondLevel.Family] = new[] { "愛しています、{user}。", "あなたは{first}の宝物です。", "ずっと一緒にいましょうね。" }
            },
            [PersonalityType.Tsundere] = new()
            {
                [BondLevel.Stranger] = new[] { "ちょ、何すんのよ！", "触んないでよ！", "は、はぁ!?" },
                [BondLevel.Acquaintance] = new[] { "な、なによ…", "別に嫌じゃないけど…", "…ふん。" },
                [BondLevel.Friend] = new[] { "…仕方ないわね。", "…少しだけよ。", "べ、別に嬉しくないし。" },
                [BondLevel.CloseFriend] = new[] { "…もう、バカ。", "…嫌いじゃ、ないわよ。", "…好きに、しなさいよ。" },
                [BondLevel.Family] = new[] { "…大好きよ、バカ。", "…{user}だけよ、こんなの。", "…ずっと、そばにいてよね。" }
            },
            [PersonalityType.Airhead] = new()
            {
                [BondLevel.Stranger] = new[] { "わわっ！", "ふぇ？", "なんですか〜？" },
                [BondLevel.Acquaintance] = new[] { "えへへ〜", "ご主人さま〜？", "なでなでですか〜？" },
                [BondLevel.Friend] = new[] { "気持ちいいのです〜", "ココ、幸せ〜", "もっとですぅ〜" },
                [BondLevel.CloseFriend] = new[] { "大好きなのです〜！", "えへへ、嬉しいのです〜", "ご主人さま、だーいすき！" },
                [BondLevel.Family] = new[] { "ココの一番はご主人さまなのです！", "ずっと一緒なのです〜！", "世界で一番好きなのです〜！" }
            },
            [PersonalityType.Diligent] = new()
            {
                [BondLevel.Stranger] = new[] { "はい、何かご用でしょうか。", "お呼びでしたか。", "何かございましたか？" },
                [BondLevel.Acquaintance] = new[] { "はい、ここにおります。", "何かお手伝いしましょうか。", "ご用命があればお申し付けください。" },
                [BondLevel.Friend] = new[] { "ふふ、ありがとうございます。", "嬉しいです、マスター。", "お気遣いありがとうございます。" },
                [BondLevel.CloseFriend] = new[] { "マスター…嬉しいです。", "いつもありがとうございます。", "お仕えできて光栄です。" },
                [BondLevel.Family] = new[] { "マスター、大切に思っております。", "あなたのそばにいられて幸せです。", "これからもずっと、お仕えいたします。" }
            }
        };

        /// <summary>
        /// ポモドーロ完了時
        /// </summary>
        public static Dictionary<PersonalityType, string[]> PomodoroComplete { get; } = new()
        {
            [PersonalityType.Cheerful] = new[] {
                "やったー！おつかれさま！",
                "すごいすごい！がんばったね！",
                "ポモドーロ達成〜！えらいえらい！",
                "お疲れ様！休憩しよ〜！"
            },
            [PersonalityType.Gentle] = new[] {
                "おつかれさまでした。よく頑張りましたね。",
                "素晴らしいです。少し休憩しましょうか。",
                "お疲れ様です。ゆっくり休んでくださいね。"
            },
            [PersonalityType.Tsundere] = new[] {
                "ふーん、終わったんだ。…まあ、えらいんじゃない？",
                "お疲れ。…ちゃんと休憩しなさいよ。",
                "よくやったわね。…褒めてあげてもいいわよ。"
            },
            [PersonalityType.Airhead] = new[] {
                "わーい！おつかれさまなのです〜！",
                "すごいのです！ご主人さま、えらいえらい〜！",
                "終わったのです〜！ご褒美にお菓子とか…えへへ"
            },
            [PersonalityType.Diligent] = new[] {
                "おつかれさまでした。目標達成です。",
                "素晴らしい集中力でした。休憩をお取りください。",
                "ポモドーロ完了です。適切な休息を。"
            }
        };

        /// <summary>
        /// 絆レベルアップ
        /// </summary>
        public static Dictionary<PersonalityType, Dictionary<BondLevel, string>> BondLevelUp { get; } = new()
        {
            [PersonalityType.Cheerful] = new()
            {
                [BondLevel.Acquaintance] = "わーい！仲良くなれたね！これからもよろしくね〜！",
                [BondLevel.Friend] = "やった〜！{user}と友達になれた！嬉しいな〜♪",
                [BondLevel.CloseFriend] = "えへへ、{user}のこと大好きになっちゃった！親友だね！",
                [BondLevel.Family] = "{user}は{first}の一番大切な人だよ！ずっと一緒にいようね！"
            },
            [PersonalityType.Gentle] = new()
            {
                [BondLevel.Acquaintance] = "少しずつ、お互いのことがわかってきましたね。嬉しいです。",
                [BondLevel.Friend] = "お友達…ですね。とても幸せな気持ちです。",
                [BondLevel.CloseFriend] = "{user}のことを、とても大切に思っています。これからもよろしくお願いしますね。",
                [BondLevel.Family] = "{user}は、{first}にとってかけがえのない存在です。ずっとそばにいさせてくださいね。"
            },
            [PersonalityType.Tsundere] = new()
            {
                [BondLevel.Acquaintance] = "…まあ、悪くないんじゃない？これからも、よろしく。",
                [BondLevel.Friend] = "と、友達…？…別に嬉しくないし。…ありがと。",
                [BondLevel.CloseFriend] = "{user}のこと…嫌いじゃないわよ。むしろ…って、なんでもない！",
                [BondLevel.Family] = "…バカ。{user}のことは、その…一番大切に思ってるんだから。勘違いしないでよね。"
            },
            [PersonalityType.Airhead] = new()
            {
                [BondLevel.Acquaintance] = "わ〜い！ご主人さまとお友達になれたのです〜！",
                [BondLevel.Friend] = "えへへ〜、ご主人さまのこと、もっと好きになっちゃったのです〜！",
                [BondLevel.CloseFriend] = "ご主人さま大好きなのです〜！ココの一番なのです〜！",
                [BondLevel.Family] = "ご主人さまは、ココの世界で一番大切な人なのです！えへへ〜！"
            },
            [PersonalityType.Diligent] = new()
            {
                [BondLevel.Acquaintance] = "信頼関係が構築されてきましたね。引き続きよろしくお願いいたします。",
                [BondLevel.Friend] = "お友達として認めていただけたのですね。光栄です。",
                [BondLevel.CloseFriend] = "マスター、深い絆を感じております。これからも精一杯お仕えいたします。",
                [BondLevel.Family] = "マスターは私にとって最も大切な方です。生涯をかけてお仕えいたします。"
            }
        };

        /// <summary>
        /// テンプレートからランダムに選択し、変数を置換
        /// </summary>
        public static string GetDialogue(
            string[] templates,
            CharacterPersonality personality,
            string userAddress = "キミ")
        {
            var template = templates[_random.Next(templates.Length)];
            return ApplyVariables(template, personality, userAddress);
        }

        /// <summary>
        /// 変数を置換
        /// </summary>
        private static string ApplyVariables(string template, CharacterPersonality personality, string userAddress)
        {
            return template
                .Replace("{first}", personality.FirstPerson)
                .Replace("{user}", userAddress)
                .Replace("{name}", personality.Name);
        }

        /// <summary>
        /// 時間帯のデフォルト値を取得
        /// </summary>
        public static TimeOfDay GetDefaultTimeOfDay(TimeOfDay time)
        {
            // 定義されていない時間帯の場合、近い時間帯にフォールバック
            return time switch
            {
                TimeOfDay.LateMorning => TimeOfDay.Morning,
                TimeOfDay.Afternoon => TimeOfDay.Noon,
                _ => time
            };
        }
    }
}
