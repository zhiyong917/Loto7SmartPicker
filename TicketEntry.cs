namespace Loto7SmartPicker
{
    /// <summary>
    /// 一注分の選号情報を表すモデルクラス。
    /// DataGrid の行データとして使用する。
    /// </summary>
    public class TicketEntry
    {
        /// <summary>通し番号（1 始まり）</summary>
        public int No { get; set; }

        /// <summary>選択番号の表示文字列（例: "03  08  15  22  29  33  37"）</summary>
        public string Numbers { get; set; } = string.Empty;

        /// <summary>奇偶比（例: "4:3"）</summary>
        public string OddEvenRatio { get; set; } = string.Empty;

        /// <summary>区間ごとの個数サマリ（例: "2-2-2-1"）</summary>
        public string RangeSummary { get; set; } = string.Empty;

        /// <summary>連番の有無（例: "なし" / "あり"）</summary>
        public string ConsecutiveInfo { get; set; } = string.Empty;
    }
}
