using System.Collections.Generic;
using System.Linq;

namespace Loto7SmartPicker
{
    /// <summary>
    /// 历史開奨レコードの統計分析を行うサービス。
    /// 分析対象は本数字（N1〜N7）のみ。ボーナス数字は今回対象外。
    /// </summary>
    public class HistoryAnalysisService
    {
        // LOTO7 区間定義
        private static readonly (string Label, int Min, int Max)[] Ranges =
        {
            ("1-9",   1,  9),
            ("10-19", 10, 19),
            ("20-29", 20, 29),
            ("30-37", 30, 37),
        };

        public LotteryHistoryAnalysisResult Analyze(List<LotteryHistoryRecord> records)
        {
            var result = new LotteryHistoryAnalysisResult
            {
                TotalRecords = records.Count
            };

            // 1〜37 の出現回数を 0 で初期化
            for (int n = 1; n <= 37; n++)
                result.NumberFrequency[n] = 0;

            // 尾数 0〜9 を 0 で初期化
            for (int d = 0; d <= 9; d++)
                result.TailDigitFrequency[d] = 0;

            // 区間を 0 で初期化
            foreach (var (label, _, _) in Ranges)
                result.RangeFrequency[label] = 0;

            // 全レコードを集計
            foreach (var record in records)
            {
                foreach (var n in record.MainNumbers)
                {
                    // 出現回数
                    result.NumberFrequency[n]++;

                    // 尾数（下 1 桁）
                    result.TailDigitFrequency[n % 10]++;

                    // 区間
                    foreach (var (label, min, max) in Ranges)
                    {
                        if (n >= min && n <= max)
                        {
                            result.RangeFrequency[label]++;
                            break;
                        }
                    }

                    // 奇偶
                    if (n % 2 != 0) result.OddCountTotal++;
                    else            result.EvenCountTotal++;
                }
            }

            // Hot Top5: 出現回数降順 → 同数なら番号昇順
            result.HotNumbersTop5 = result.NumberFrequency
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(5)
                .Select(kv => kv.Key)
                .ToList();

            // Cold Top5: 出現回数昇順 → 同数なら番号昇順
            result.ColdNumbersTop5 = result.NumberFrequency
                .OrderBy(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(5)
                .Select(kv => kv.Key)
                .ToList();

            return result;
        }
    }
}
