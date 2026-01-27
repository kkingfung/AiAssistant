#nullable enable
using System.Threading.Tasks;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// コンパニオンデータ永続化サービスのインターフェース
    /// </summary>
    public interface ICompanionshipDataService
    {
        /// <summary>
        /// コンテキストを読み込む
        /// </summary>
        /// <returns>読み込んだコンテキスト、存在しない場合はデフォルト値</returns>
        CompanionContext Load();

        /// <summary>
        /// コンテキストを非同期で読み込む
        /// </summary>
        /// <returns>読み込んだコンテキスト、存在しない場合はデフォルト値</returns>
        Task<CompanionContext> LoadAsync();

        /// <summary>
        /// コンテキストを保存する
        /// </summary>
        /// <param name="context">保存するコンテキスト</param>
        void Save(CompanionContext context);

        /// <summary>
        /// コンテキストを非同期で保存する
        /// </summary>
        /// <param name="context">保存するコンテキスト</param>
        Task SaveAsync(CompanionContext context);

        /// <summary>
        /// データファイルが存在するかどうか
        /// </summary>
        bool DataExists { get; }

        /// <summary>
        /// データをリセットする
        /// </summary>
        void Reset();
    }
}
