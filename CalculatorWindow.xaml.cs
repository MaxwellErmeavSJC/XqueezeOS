using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XqueezeOS
{
    public partial class CalculatorWindow : Window
    {
        private string currentExpr = "";
        private readonly List<HistoryItem> history = new();
        private readonly string historyPath;
        private const int MaxHistory = 5;

        private Timer memTimer;
        private long memStart = -1;
        private long memFinal = -1;

        public CalculatorWindow()
        {
            InitializeComponent();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = System.IO.Path.Combine(appData, "XqueezeOS");
            Directory.CreateDirectory(dir);

            historyPath = System.IO.Path.Combine(dir, "calc_history.json");

            LoadHistory();
            RenderHistory();
            UpdateDisplay();

            TakeStartSnapshot();
            StartLiveMemoryUpdates();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string key)
            {
                currentExpr += key;
                UpdateDisplay();
            }
        }

        private void Equals_Click(object sender, RoutedEventArgs e) => DoEquals();

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentExpr = "";
            UpdateDisplay();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (currentExpr.Length > 0)
                currentExpr = currentExpr[..^1];

            UpdateDisplay();
        }

        private void Parens_Click(object sender, RoutedEventArgs e)
        {
            int open = Regex.Matches(currentExpr, "\\(").Count;
            int close = Regex.Matches(currentExpr, "\\)").Count;

            currentExpr += (open > close) ? ")" : "(";

            UpdateDisplay();
        }

        private void DoEquals()
        {
            try
            {
                double val = SafeEvalExpression(currentExpr);
                SaveToHistory(currentExpr, val);
                currentExpr = val.ToString();
                UpdateDisplay();
                RenderHistory();
            }
            catch
            {
                ResultText.Text = "Error";
            }
        }

        private double SafeEvalExpression(string raw)
        {
            string s = Sanitize(raw);
            s = PercentToJS(s);

            var dt = new DataTable();
            object result = dt.Compute(s, "");
            return Convert.ToDouble(result);
        }

        private string Sanitize(string s)
        {
            s = s.Replace("×", "*").Replace("÷", "/").Replace("−", "-");
            return Regex.Replace(s, @"[^0-9+\-*/(). %]", "");
        }

        private string PercentToJS(string s)
        {
            return Regex.Replace(s, @"(\d+(\.\d+)?)%", "($1/100)");
        }

        private void UpdateDisplay()
        {
            ExprText.Text = string.IsNullOrWhiteSpace(currentExpr) ? "0" : currentExpr;

            try
            {
                double val = SafeEvalExpression(currentExpr);
                ResultText.Text = val.ToString();
            }
            catch
            {
                ResultText.Text = "Err";
            }
        }

        private void LoadHistory()
        {
            if (File.Exists(historyPath))
            {
                string json = File.ReadAllText(historyPath);
                var items = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                if (items != null) history.AddRange(items);
            }
        }

        private void SaveToHistory(string expr, double result)
        {
            history.Add(new HistoryItem { Expr = expr, Result = result.ToString(), Timestamp = DateTime.Now });

            if (history.Count > MaxHistory)
                history.RemoveAt(0);

            File.WriteAllText(historyPath, JsonSerializer.Serialize(history));
        }

        private void RenderHistory()
        {
            HistoryListBox.Items.Clear();

            foreach (var h in history)
                HistoryListBox.Items.Add($"{h.Expr} = {h.Result}");
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            history.Clear();
            File.WriteAllText(historyPath, "[]");
            RenderHistory();
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListBox.SelectedIndex >= 0 && HistoryListBox.SelectedIndex < history.Count)
            {
                currentExpr = history[HistoryListBox.SelectedIndex].Expr;
                UpdateDisplay();
            }
        }

        private void TakeStartSnapshot()
        {
            memStart = GC.GetTotalMemory(false);
            MemStartText.Text = FormatBytes(memStart);
        }

        private void StartLiveMemoryUpdates()
        {
            memTimer = new Timer(800);
            memTimer.Elapsed += (s, e) =>
            {
                long live = GC.GetTotalMemory(false);

                Dispatcher.Invoke(() =>
                {
                    MemLiveText.Text = FormatBytes(live);
                });
            };
            memTimer.Start();
        }

        private void SnapshotBtn_Click(object sender, RoutedEventArgs e)
        {
            memTimer.Stop();
            memFinal = GC.GetTotalMemory(true);

            MemFinalText.Text = FormatBytes(memFinal);

            long delta = memFinal - memStart;
            MemNoteText.Text = $"Delta: {FormatBytes(delta)}";
        }

        private string FormatBytes(long b)
        {
            if (b < 1024) return $"{b} B";
            if (b < 1024 * 1024) return $"{b / 1024.0:F2} KB";
            return $"{b / 1024.0 / 1024.0:F2} MB";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (char.IsDigit((char)KeyInterop.VirtualKeyFromKey(e.Key)))
            {
                currentExpr += (char)KeyInterop.VirtualKeyFromKey(e.Key);
                UpdateDisplay();
                return;
            }

            switch (e.Key)
            {
                case Key.Add: currentExpr += "+"; break;
                case Key.Subtract: currentExpr += "-"; break;
                case Key.Multiply: currentExpr += "*"; break;
                case Key.Divide: currentExpr += "/"; break;
                case Key.Decimal: currentExpr += "."; break;
                case Key.Enter: DoEquals(); return;
                case Key.Back: Backspace_Click(null, null); return;
            }

            UpdateDisplay();
        }
    }
}
