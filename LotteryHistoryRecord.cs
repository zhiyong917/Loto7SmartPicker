namespace Loto7SmartPicker
{
    /// <summary>
    /// CSV から読み込んだ 1 回分の開奨記録を表すモデル。
    /// フォーマット: DrawNo,Date,N1,N2,N3,N4,N5,N6,N7,B1,B2
    /// </summary>
    public class LotteryHistoryRecord
    {
        /// <summary>回号（例: 615）</summary>
        public int DrawNo { get; set; }

        /// <summary>開奨日（文字列として保持）</summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>本数字 7 個（N1〜N7）</summary>
        public int[] MainNumbers { get; set; } = new int[7];

        /// <summary>ボーナス数字 2 個（B1〜B2）。今回は分析対象外。</summary>
        public int[] BonusNumbers { get; set; } = new int[2];
    }
}
