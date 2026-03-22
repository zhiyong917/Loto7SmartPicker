using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;

namespace Loto7SmartPicker
{
    /// <summary>
    /// MainWindow の ViewModel。
    /// 選号条件・生成結果・历史分析結果・ステータスバー情報を管理する。
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // ============================================================
        //  INotifyPropertyChanged
        // ============================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        // ============================================================
        //  サービス（依存性注入の代わりにフィールドで保持）
        // ============================================================

        private readonly CsvImportService     _csvImportService     = new();
        private readonly HistoryAnalysisService _analysisService    = new();

        // ============================================================
        //  選号モード
        // ============================================================

        public List<string> PickModes { get; } = new()
        {
            "完全ランダム",
            "バランス分布",
            "分散カバー",
            "固定番号アシスト"
        };

        private string _selectedPickMode = "完全ランダム";
        public string SelectedPickMode
        {
            get => _selectedPickMode;
            set => Set(ref _selectedPickMode, value);
        }

        // ============================================================
        //  入力パラメータ
        // ============================================================

        private string _generateCount = "5";
        /// <summary>生成する注数（テキスト入力）</summary>
        public string GenerateCount
        {
            get => _generateCount;
            set => Set(ref _generateCount, value);
        }

        private string _fixedNumbers = string.Empty;
        /// <summary>固定番号（カンマ区切りテキスト入力）</summary>
        public string FixedNumbers
        {
            get => _fixedNumbers;
            set => Set(ref _fixedNumbers, value);
        }

        // ============================================================
        //  生成オプション
        // ============================================================

        private bool _avoidConsecutive = true;
        public bool AvoidConsecutive
        {
            get => _avoidConsecutive;
            set => Set(ref _avoidConsecutive, value);
        }

        private bool _keepOddEvenBalance = true;
        public bool KeepOddEvenBalance
        {
            get => _keepOddEvenBalance;
            set => Set(ref _keepOddEvenBalance, value);
        }

        private bool _coverDifferentRanges = true;
        public bool CoverDifferentRanges
        {
            get => _coverDifferentRanges;
            set => Set(ref _coverDifferentRanges, value);
        }

        // ============================================================
        //  結果リスト
        // ============================================================

        public ObservableCollection<TicketEntry> Tickets { get; } = new();

        // ============================================================
        //  ステータスバー
        // ============================================================

        private int _statusTicketCount;
        public int StatusTicketCount
        {
            get => _statusTicketCount;
            set => Set(ref _statusTicketCount, value);
        }

        private int _statusCost;
        /// <summary>予想金額（1注 300 円で計算）</summary>
        public int StatusCost
        {
            get => _statusCost;
            set => Set(ref _statusCost, value);
        }

        private string _statusMessage = "準備完了";
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        // ============================================================
        //  历史分析結果（表示用プロパティ）
        // ============================================================

        private bool _hasAnalysisData;
        /// <summary>CSV が一度でも読み込まれたかどうか（分析エリアの表示切替に使用）</summary>
        public bool HasAnalysisData
        {
            get => _hasAnalysisData;
            set => Set(ref _hasAnalysisData, value);
        }

        private string _analysisRecordCount = string.Empty;
        /// <summary>导入件数の表示文字列</summary>
        public string AnalysisRecordCount
        {
            get => _analysisRecordCount;
            set => Set(ref _analysisRecordCount, value);
        }

        private string _analysisHotNumbers = string.Empty;
        /// <summary>熱号 Top5 の表示文字列</summary>
        public string AnalysisHotNumbers
        {
            get => _analysisHotNumbers;
            set => Set(ref _analysisHotNumbers, value);
        }

        private string _analysisColdNumbers = string.Empty;
        /// <summary>冷号 Top5 の表示文字列</summary>
        public string AnalysisColdNumbers
        {
            get => _analysisColdNumbers;
            set => Set(ref _analysisColdNumbers, value);
        }

        private string _analysisTailDigits = string.Empty;
        /// <summary>尾数分布の表示文字列</summary>
        public string AnalysisTailDigits
        {
            get => _analysisTailDigits;
            set => Set(ref _analysisTailDigits, value);
        }

        private string _analysisRanges = string.Empty;
        /// <summary>区間分布の表示文字列</summary>
        public string AnalysisRanges
        {
            get => _analysisRanges;
            set => Set(ref _analysisRanges, value);
        }

        private string _analysisOddEven = string.Empty;
        /// <summary>奇偶総数の表示文字列</summary>
        public string AnalysisOddEven
        {
            get => _analysisOddEven;
            set => Set(ref _analysisOddEven, value);
        }

        // ============================================================
        //  コマンド
        // ============================================================

        public ICommand GenerateCommand         { get; }
        public ICommand ClearCommand            { get; }
        public ICommand SaveCommand             { get; }
        public ICommand ImportHistoryCsvCommand { get; }

        // ============================================================
        //  コンストラクタ
        // ============================================================

        public MainWindowViewModel()
        {
            GenerateCommand         = new RelayCommand(ExecuteGenerate);
            ClearCommand            = new RelayCommand(ExecuteClear);
            SaveCommand             = new RelayCommand(ExecuteSave, () => Tickets.Count > 0);
            ImportHistoryCsvCommand = new RelayCommand(ExecuteImportHistoryCsv);

            // UI 確認用のサンプルデータをロード
            LoadSampleData();
        }

        // ============================================================
        //  コマンド実装
        // ============================================================

        /// <summary>
        /// 号码生成コマンド。
        /// ※ 現時点はダミーのランダム生成。後で選号アルゴリズムに差し替える。
        /// </summary>
        private void ExecuteGenerate()
        {
            if (!int.TryParse(GenerateCount, out int count) || count <= 0 || count > 100)
            {
                StatusMessage = "注数は 1〜100 の整数を入力してください";
                return;
            }

            StatusMessage = "生成中...";
            Tickets.Clear();

            var rng = new Random();

            for (int i = 1; i <= count; i++)
            {
                // TODO: ここを実際の選号アルゴリズムに置き換える
                var nums = GenerateRandomNumbers(rng);

                Tickets.Add(new TicketEntry
                {
                    No              = i,
                    Numbers         = string.Join("  ", nums.Select(n => n.ToString("D2"))),
                    OddEvenRatio    = BuildOddEvenRatio(nums),
                    RangeSummary    = BuildRangeSummary(nums),
                    ConsecutiveInfo = HasConsecutive(nums) ? "あり" : "なし"
                });
            }

            RefreshStatus();
            StatusMessage = $"{count} 注を生成しました";
        }

        /// <summary>クリアコマンド。結果リストをすべて削除する。</summary>
        private void ExecuteClear()
        {
            Tickets.Clear();
            RefreshStatus();
            StatusMessage = "クリア済み";
        }

        /// <summary>
        /// 保存コマンド。
        /// TODO: CSV / テキストファイル等への保存処理を実装する。
        /// </summary>
        private void ExecuteSave()
        {
            StatusMessage = "保存機能は未実装です（TODO）";
        }

        /// <summary>
        /// 历史 CSV インポートコマンド。
        /// ファイル選択ダイアログを開き、読み込み→分析→表示更新を行う。
        /// </summary>
        private void ExecuteImportHistoryCsv()
        {
            var dialog = new OpenFileDialog
            {
                Title       = "历史開奨 CSV ファイルを選択",
                Filter      = "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                DefaultExt  = ".csv"
            };

            if (dialog.ShowDialog() != true) return;

            StatusMessage = "CSV 読み込み中...";

            var importResult = _csvImportService.ImportFromFile(dialog.FileName);

            // 読み込みエラー（ファイル I/O レベル）
            if (!importResult.Success)
            {
                StatusMessage = $"エラー: {importResult.ErrorMessage}";
                return;
            }

            // 有効レコードがない場合
            if (importResult.Records.Count == 0)
            {
                StatusMessage = importResult.ErrorMessage ?? "有効データが見つかりませんでした";
                return;
            }

            // 統計分析を実行
            var analysisResult = _analysisService.Analyze(importResult.Records);

            // 分析結果を表示用プロパティに反映
            ApplyAnalysisResult(analysisResult, importResult.SkippedLineCount);

            var skipInfo = importResult.SkippedLineCount > 0
                ? $"（{importResult.SkippedLineCount} 行スキップ）"
                : string.Empty;
            StatusMessage = $"CSV 読込完了: {importResult.Records.Count} 件 {skipInfo}";
        }

        // ============================================================
        //  ヘルパーメソッド
        // ============================================================

        /// <summary>分析結果を各表示用プロパティに書き込む</summary>
        private void ApplyAnalysisResult(LotteryHistoryAnalysisResult result, int skippedCount)
        {
            HasAnalysisData = true;

            // 件数表示
            var skipSuffix = skippedCount > 0 ? $"  ※ {skippedCount} 行スキップ" : string.Empty;
            AnalysisRecordCount = $"{result.TotalRecords} 件{skipSuffix}";

            // 熱号 / 冷号（2 桁固定で表示）
            AnalysisHotNumbers  = FormatNumberList(result.HotNumbersTop5);
            AnalysisColdNumbers = FormatNumberList(result.ColdNumbersTop5);

            // 区間分布（改行で見やすく）
            AnalysisRanges =
                $"[1-9] {result.RangeFrequency["1-9"]} 回    " +
                $"[10-19] {result.RangeFrequency["10-19"]} 回    " +
                $"[20-29] {result.RangeFrequency["20-29"]} 回    " +
                $"[30-37] {result.RangeFrequency["30-37"]} 回";

            // 奇偶
            int total = result.OddCountTotal + result.EvenCountTotal;
            string oddPct  = total > 0 ? $"({result.OddCountTotal  * 100 / total}%)" : string.Empty;
            string evenPct = total > 0 ? $"({result.EvenCountTotal * 100 / total}%)" : string.Empty;
            AnalysisOddEven = $"奇数: {result.OddCountTotal} 回 {oddPct}    偶数: {result.EvenCountTotal} 回 {evenPct}";

            // 尾数分布（0〜9）
            AnalysisTailDigits = string.Join("   ",
                result.TailDigitFrequency
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"末{kv.Key}：{kv.Value}"));
        }

        private static string FormatNumberList(List<int> nums)
            => string.Join("   ", nums.Select(n => n.ToString("D2")));

        /// <summary>1〜37 からランダムに 7 つ選んでソートして返す</summary>
        private static int[] GenerateRandomNumbers(Random rng)
        {
            var pool   = Enumerable.Range(1, 37).ToList();
            var result = new List<int>(7);

            for (int i = 0; i < 7; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            result.Sort();
            return result.ToArray();
        }

        private static string BuildOddEvenRatio(int[] nums)
        {
            int odd  = nums.Count(n => n % 2 != 0);
            int even = nums.Length - odd;
            return $"{odd}:{even}";
        }

        private static string BuildRangeSummary(int[] nums)
        {
            int[] zones = new int[4];
            foreach (var n in nums)
            {
                if      (n <=  9) zones[0]++;
                else if (n <= 19) zones[1]++;
                else if (n <= 29) zones[2]++;
                else              zones[3]++;
            }
            return string.Join("-", zones);
        }

        private static bool HasConsecutive(int[] nums)
        {
            for (int i = 0; i < nums.Length - 1; i++)
                if (nums[i + 1] - nums[i] == 1) return true;
            return false;
        }

        private void RefreshStatus()
        {
            StatusTicketCount = Tickets.Count;
            StatusCost        = Tickets.Count * 300; // LOTO7 は 1 注 300 円
        }

        private void LoadSampleData()
        {
            Tickets.Add(new TicketEntry { No = 1, Numbers = "03  08  15  22  29  33  37", OddEvenRatio = "4:3", RangeSummary = "2-2-2-1", ConsecutiveInfo = "なし" });
            Tickets.Add(new TicketEntry { No = 2, Numbers = "01  07  12  18  25  30  36", OddEvenRatio = "3:4", RangeSummary = "1-2-2-2", ConsecutiveInfo = "なし" });
            Tickets.Add(new TicketEntry { No = 3, Numbers = "05  09  14  20  23  31  35", OddEvenRatio = "5:2", RangeSummary = "2-1-2-2", ConsecutiveInfo = "なし" });
            Tickets.Add(new TicketEntry { No = 4, Numbers = "02  10  17  21  28  32  36", OddEvenRatio = "2:5", RangeSummary = "1-2-2-2", ConsecutiveInfo = "なし" });
            Tickets.Add(new TicketEntry { No = 5, Numbers = "06  11  16  24  27  30  37", OddEvenRatio = "3:4", RangeSummary = "1-2-2-2", ConsecutiveInfo = "なし" });

            RefreshStatus();
        }
    }
}
