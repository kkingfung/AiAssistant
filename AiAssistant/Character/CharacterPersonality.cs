#nullable enable
using System;
using System.Collections.Generic;

namespace AiAssistant.Character
{
    /// <summary>
    /// キャラクターの性格タイプ
    /// </summary>
    public enum PersonalityType
    {
        /// <summary>元気で明るい</summary>
        Cheerful,

        /// <summary>落ち着いた優しい</summary>
        Gentle,

        /// <summary>クールでツンデレ</summary>
        Tsundere,

        /// <summary>おっとり天然</summary>
        Airhead,

        /// <summary>真面目で几帳面</summary>
        Diligent
    }

    /// <summary>
    /// キャラクターの性格定義
    /// セリフのトーンや反応パターンを決定する
    /// </summary>
    public sealed class CharacterPersonality
    {
        /// <summary>性格タイプ</summary>
        public PersonalityType Type { get; }

        /// <summary>キャラクター名</summary>
        public string Name { get; }

        /// <summary>一人称</summary>
        public string FirstPerson { get; }

        /// <summary>呼びかけ（ユーザーへの呼称）</summary>
        public string UserAddress { get; }

        /// <summary>語尾パターン</summary>
        public IReadOnlyList<string> SentenceEndings { get; }

        /// <summary>感嘆符の使用頻度（0.0-1.0）</summary>
        public double ExclamationFrequency { get; }

        /// <summary>絵文字の使用頻度（0.0-1.0）</summary>
        public double EmojiFrequency { get; }

        private CharacterPersonality(
            PersonalityType type,
            string name,
            string firstPerson,
            string userAddress,
            IReadOnlyList<string> sentenceEndings,
            double exclamationFrequency,
            double emojiFrequency)
        {
            Type = type;
            Name = name;
            FirstPerson = firstPerson;
            UserAddress = userAddress;
            SentenceEndings = sentenceEndings;
            ExclamationFrequency = exclamationFrequency;
            EmojiFrequency = emojiFrequency;
        }

        /// <summary>
        /// 元気キャラクターを作成
        /// </summary>
        public static CharacterPersonality CreateCheerful(string name = "ヒカリ")
        {
            return new CharacterPersonality(
                PersonalityType.Cheerful,
                name,
                firstPerson: "わたし",
                userAddress: "キミ",
                sentenceEndings: new[] { "だよ！", "だね！", "なの！", "よね！", "かな？" },
                exclamationFrequency: 0.8,
                emojiFrequency: 0.6
            );
        }

        /// <summary>
        /// 優しいキャラクターを作成
        /// </summary>
        public static CharacterPersonality CreateGentle(string name = "ユキ")
        {
            return new CharacterPersonality(
                PersonalityType.Gentle,
                name,
                firstPerson: "わたし",
                userAddress: "あなた",
                sentenceEndings: new[] { "ですね", "ですよ", "ですか？", "ましょう", "かしら" },
                exclamationFrequency: 0.2,
                emojiFrequency: 0.3
            );
        }

        /// <summary>
        /// ツンデレキャラクターを作成
        /// </summary>
        public static CharacterPersonality CreateTsundere(string name = "レイ")
        {
            return new CharacterPersonality(
                PersonalityType.Tsundere,
                name,
                firstPerson: "あたし",
                userAddress: "アンタ",
                sentenceEndings: new[] { "だし", "んだから", "わよ", "でしょ", "なんだからね" },
                exclamationFrequency: 0.5,
                emojiFrequency: 0.1
            );
        }

        /// <summary>
        /// 天然キャラクターを作成
        /// </summary>
        public static CharacterPersonality CreateAirhead(string name = "ココ")
        {
            return new CharacterPersonality(
                PersonalityType.Airhead,
                name,
                firstPerson: "ココ",
                userAddress: "ご主人さま",
                sentenceEndings: new[] { "なのです", "ですぅ", "かなぁ？", "だと思うの", "えへへ" },
                exclamationFrequency: 0.4,
                emojiFrequency: 0.5
            );
        }

        /// <summary>
        /// 真面目キャラクターを作成
        /// </summary>
        public static CharacterPersonality CreateDiligent(string name = "アヤ")
        {
            return new CharacterPersonality(
                PersonalityType.Diligent,
                name,
                firstPerson: "私",
                userAddress: "マスター",
                sentenceEndings: new[] { "です", "ですね", "でしょうか", "いたします", "ございます" },
                exclamationFrequency: 0.1,
                emojiFrequency: 0.1
            );
        }

        /// <summary>
        /// 性格タイプから作成
        /// </summary>
        public static CharacterPersonality FromType(PersonalityType type, string? customName = null)
        {
            return type switch
            {
                PersonalityType.Cheerful => CreateCheerful(customName ?? "ヒカリ"),
                PersonalityType.Gentle => CreateGentle(customName ?? "ユキ"),
                PersonalityType.Tsundere => CreateTsundere(customName ?? "レイ"),
                PersonalityType.Airhead => CreateAirhead(customName ?? "ココ"),
                PersonalityType.Diligent => CreateDiligent(customName ?? "アヤ"),
                _ => CreateGentle(customName ?? "ユキ")
            };
        }
    }
}
