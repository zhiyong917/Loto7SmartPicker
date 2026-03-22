using System.Collections.Generic;

namespace Loto7SmartPicker
{
    /// <summary>
    /// 历史データの統計分析結果を格納するモデル。
    /// HistoryAnalysisService によって生成され、ViewModel 経由で表示に使われる。
    /// </summary>
    public class LotteryHistoryAnalysisResult
    {
        /// <summary>読み込んだ有効レコード数</summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 1〜37 各番号の出現回数。
        /// キー = 番号（1-37）、バリュー = 出現回数
        /// </summary>
        public Dictionary<int, int> NumberFrequency { get; set; } = new();

        /// <summary>
        /// 尾数（下 1 桁）の出現回数。
        /// キー = 0〜9、バリュー = 出現回数
        /// </summary>
        public Dictionary<int, int> TailDigitFrequency { get; set; } = new();

        /// <summary>
        /// 区間別の出現回数。
        /// キー = "1-9" / "10-19" / "20-29" / "30-37"
        /// </summary>
        public Dictionary<string, int> RangeFrequency { get; set; } = new();

        /// <summary>奇数番号の総出現回数</summary>
        public int OddCountTotal { get; set; }

        /// <summary>偶数番号の総出現回数</summary>
        public int EvenCountTotal { get; set; }

        /// <summary>出現回数 上位 5 番号（Hot Numbers）</summary>
        public List<int> HotNumbersTop5 { get; set; } = new();

        /// <summary>出現回数 下位 5 番号（Cold Numbers）</summary>
        public List<int> ColdNumbersTop5 { get; set; } = new();
    }
}
