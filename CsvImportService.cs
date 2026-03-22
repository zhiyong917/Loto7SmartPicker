using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Loto7SmartPicker
{
    /// <summary>CSV インポート処理の結果を表すクラス</summary>
    public class CsvImportResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<LotteryHistoryRecord> Records { get; set; } = new();

        /// <summary>パース失敗などでスキップされた行数</summary>
        public int SkippedLineCount { get; set; }
    }

    /// <summary>
    /// 历史開奨 CSV ファイルの読み込み・パースを行うサービス。
    ///
    /// 期待するフォーマット（1 行目はヘッダー行または 1 レコード目）:
    ///   DrawNo,Date,N1,N2,N3,N4,N5,N6,N7,B1,B2
    ///
    /// 文字コードは UTF-8（BOM あり/なし両対応）を優先する。
    /// 読み込み失敗時は例外を投げずに CsvImportResult.Success=false で返す。
    /// </summary>
    public class CsvImportService
    {
        private const int RequiredColumnCount = 11; // DrawNo + Date + N1-N7 + B1-B2

        public CsvImportResult ImportFromFile(string filePath)
        {
            var result = new CsvImportResult();

            try
            {
                // UTF-8 BOM 付き・なし両対応（true = BOM を自動検出）
                var lines = File.ReadAllLines(filePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                if (lines.Length == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "ファイルが空です。";
                    return result;
                }

                // 最初の行が数値（DrawNo）で始まらない場合はヘッダー行としてスキップ
                int startIndex = StartsWithNumeric(lines[0]) ? 0 : 1;

                for (int i = startIndex; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var record = TryParseLine(line);
                    if (record != null)
                        result.Records.Add(record);
                    else
                        result.SkippedLineCount++;
                }

                result.Success = true;

                if (result.Records.Count == 0)
                    result.ErrorMessage = "有効なデータ行が 1 件も見つかりませんでした。フォーマットを確認してください。";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"ファイル読み込みエラー: {ex.Message}";
            }

            return result;
        }

        // ============================================================
        //  内部ヘルパー
        // ============================================================

        /// <summary>行の先頭が数値（DrawNo）かどうかを判定する</summary>
        private static bool StartsWithNumeric(string line)
        {
            var first = line.Split(',')[0].Trim();
            return int.TryParse(first, out _);
        }

        /// <summary>
        /// CSV 1 行を LotteryHistoryRecord にパースする。
        /// 失敗した場合は null を返す（例外は投げない）。
        /// </summary>
        private static LotteryHistoryRecord? TryParseLine(string line)
        {
            try
            {
                var parts = line.Split(',');
                if (parts.Length < RequiredColumnCount) return null;

                var drawNo = int.Parse(parts[0].Trim());
                var date   = parts[1].Trim();

                var mainNums = new int[7];
                for (int i = 0; i < 7; i++)
                    mainNums[i] = int.Parse(parts[2 + i].Trim());

                var bonusNums = new int[2];
                for (int i = 0; i < 2; i++)
                    bonusNums[i] = int.Parse(parts[9 + i].Trim());

                // 本数字の範囲チェック（1〜37）
                if (mainNums.Any(n => n < 1 || n > 37)) return null;

                // 本数字の重複チェック
                if (mainNums.Distinct().Count() != 7) return null;

                return new LotteryHistoryRecord
                {
                    DrawNo       = drawNo,
                    Date         = date,
                    MainNumbers  = mainNums,
                    BonusNumbers = bonusNums
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
