using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Loto7SmartPicker
{
    /// <summary>
    /// MainWindow の ViewModel。
    /// 選号条件・生成結果・ステータスバー情報を管理する。
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
        //  選号モード
        // ============================================================

        /// <summary>選号モードの選択肢リスト</summary>
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
        /// <summary>連番を避けるオプション</summary>
        public bool AvoidConsecutive
        {
            get => _avoidConsecutive;
            set => Set(ref _avoidConsecutive, value);
        }

        private bool _keepOddEvenBalance = true;
        /// <summary>奇偶バランスを保つオプション</summary>
        public bool KeepOddEvenBalance
        {
            get => _keepOddEvenBalance;
            set => Set(ref _keepOddEvenBalance, value);
        }

        private bool _coverDifferentRanges = true;
        /// <summary>異なる区間をカバーするオプション</summary>
        public bool CoverDifferentRanges
        {
            get => _coverDifferentRanges;
            set => Set(ref _coverDifferentRanges, value);
        }

        // ============================================================
        //  結果リスト
        // ============================================================

        /// <summary>生成された選号一覧（DataGrid バインド用）</summary>
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
        //  コマンド
        // ============================================================

        public ICommand GenerateCommand { get; }
        public ICommand ClearCommand    { get; }
        public ICommand SaveCommand     { get; }

        // ============================================================
        //  コンストラクタ
        // ============================================================

        public MainWindowViewModel()
        {
            GenerateCommand = new RelayCommand(ExecuteGenerate);
            ClearCommand    = new RelayCommand(ExecuteClear);
            SaveCommand     = new RelayCommand(ExecuteSave, () => Tickets.Count > 0);

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
                    No             = i,
                    Numbers        = string.Join("  ", nums.Select(n => n.ToString("D2"))),
                    OddEvenRatio   = BuildOddEvenRatio(nums),
                    RangeSummary   = BuildRangeSummary(nums),
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

        // ============================================================
        //  ヘルパーメソッド（後で本格実装に移行予定）
        // ============================================================

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

        /// <summary>奇偶比文字列を返す（例: "4:3"）</summary>
        private static string BuildOddEvenRatio(int[] nums)
        {
            int odd  = nums.Count(n => n % 2 != 0);
            int even = nums.Length - odd;
            return $"{odd}:{even}";
        }

        /// <summary>
        /// LOTO7 の区間（1-9 / 10-19 / 20-29 / 30-37）ごとの個数を
        /// "2-2-2-1" 形式で返す
        /// </summary>
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

        /// <summary>連続する番号が含まれているかを判定する</summary>
        private static bool HasConsecutive(int[] nums)
        {
            for (int i = 0; i < nums.Length - 1; i++)
                if (nums[i + 1] - nums[i] == 1) return true;
            return false;
        }

        /// <summary>ステータスバーの注数・金額表示を更新する</summary>
        private void RefreshStatus()
        {
            StatusTicketCount = Tickets.Count;
            StatusCost        = Tickets.Count * 300; // LOTO7 は 1 注 300 円
        }

        /// <summary>起動時に表示するサンプルデータを投入する</summary>
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
