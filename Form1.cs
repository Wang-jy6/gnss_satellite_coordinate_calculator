using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GNSS卫星坐标计算.Core;
using GNSS卫星坐标计算.Data;
using GNSS卫星坐标计算.Models;

namespace GNSS卫星坐标计算
{
    public partial class Form1 : Form
    {
        private readonly List<GnssEphemeris> _ephData = new List<GnssEphemeris>();
        private readonly SatOrbitCalculator _calculator = new SatOrbitCalculator();
        private string _currentNavFile;

        private DataGridView dataGridView1;
        private TextBox txtPRN, txtGPST, txtX, txtY, txtZ;
        private Button btnOpenRinex, btnSingleCalc, btnBatchCalc;
        private Label lblStatus, lblFileName, lblRecordCount, lblSystemCount, lblEpochRange;

        private readonly Color _bg = Color.FromArgb(245, 247, 250);
        private readonly Color _panel = Color.White;
        private readonly Color _line = Color.FromArgb(224, 229, 236);
        private readonly Color _primary = Color.FromArgb(32, 78, 125);
        private readonly Color _primaryDark = Color.FromArgb(24, 52, 83);
        private readonly Color _accent = Color.FromArgb(19, 132, 150);
        private readonly Color _text = Color.FromArgb(35, 44, 58);
        private readonly Color _muted = Color.FromArgb(105, 118, 136);

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            SqliteHelper.InitDB();
        }

        private void BuildUi()
        {
            Text = "GNSS多系统卫星坐标计算";
            Size = new Size(1180, 760);
            MinimumSize = new Size(1050, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _bg;
            Font = new Font("Microsoft YaHei UI", 9F);

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = _primaryDark,
                Padding = new Padding(28, 0, 28, 0)
            };
            Controls.Add(header);

            Label title = new Label
            {
                Text = "GNSS多系统卫星坐标计算",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 430,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(title);

            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(210, 222, 235),
                Dock = DockStyle.Right,
                Width = 260,
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(lblStatus);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _bg,
                Padding = new Padding(22, 18, 22, 16),
                ColumnCount = 1,
                RowCount = 3
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            Controls.Add(main);
            main.BringToFront();

            TableLayoutPanel cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _bg,
                ColumnCount = 4,
                RowCount = 1
            };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            main.Controls.Add(cards, 0, 0);

            Panel fileCard = CreateCard();
            fileCard.Controls.Add(CreateCaption("当前文件", 18, 12));
            lblFileName = CreateValueLabel("尚未载入星历文件", 18, 38);
            fileCard.Controls.Add(lblFileName);
            cards.Controls.Add(fileCard, 0, 0);

            Panel countCard = CreateCard();
            countCard.Controls.Add(CreateCaption("星历记录", 18, 12));
            lblRecordCount = CreateValueLabel("0", 18, 32);
            lblRecordCount.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
            countCard.Controls.Add(lblRecordCount);
            cards.Controls.Add(countCard, 1, 0);

            Panel systemCard = CreateCard();
            systemCard.Controls.Add(CreateCaption("系统分布", 18, 12));
            lblSystemCount = CreateValueLabel("-", 18, 38);
            systemCard.Controls.Add(lblSystemCount);
            cards.Controls.Add(systemCard, 2, 0);

            Panel epochCard = CreateCard();
            epochCard.Controls.Add(CreateCaption("历元范围", 18, 12));
            lblEpochRange = CreateValueLabel("-", 18, 38);
            epochCard.Controls.Add(lblEpochRange);
            cards.Controls.Add(epochCard, 3, 0);

            Panel gridPanel = CreateCard();
            gridPanel.Padding = new Padding(18, 14, 18, 18);
            main.Controls.Add(gridPanel, 0, 1);

            Label gridTitle = new Label
            {
                Text = "星历数据",
                ForeColor = _text,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30
            };
            gridPanel.Controls.Add(gridTitle);

            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 248);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = _text;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 237, 247);
            dataGridView1.DefaultCellStyle.SelectionForeColor = _text;
            dataGridView1.GridColor = _line;
            gridPanel.Controls.Add(dataGridView1);
            dataGridView1.BringToFront();

            Panel operationPanel = CreateCard();
            operationPanel.Padding = new Padding(18, 14, 18, 14);
            main.Controls.Add(operationPanel, 0, 2);

            TableLayoutPanel ops = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 2,
                BackColor = _panel
            };
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            ops.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
            ops.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            ops.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            operationPanel.Controls.Add(ops);

            Label inputTitle = new Label
            {
                Text = "坐标计算",
                ForeColor = _text,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            ops.Controls.Add(inputTitle, 0, 0);
            ops.SetColumnSpan(inputTitle, 4);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            ops.Controls.Add(buttons, 7, 0);
            ops.SetRowSpan(buttons, 2);

            btnBatchCalc = CreateButton("导出TXT", _primary);
            btnSingleCalc = CreateButton("单次计算", _accent);
            btnOpenRinex = CreateButton("打开星历", Color.FromArgb(73, 91, 113));
            buttons.Controls.Add(btnBatchCalc);
            buttons.Controls.Add(btnSingleCalc);
            buttons.Controls.Add(btnOpenRinex);

            ops.Controls.Add(CreateInlineCaption("卫星号"), 0, 1);
            txtPRN = CreateInput();
            txtPRN.Text = "R01";
            ops.Controls.Add(txtPRN, 1, 1);

            ops.Controls.Add(CreateInlineCaption("tk 秒"), 2, 1);
            txtGPST = CreateInput();
            txtGPST.Text = "0";
            ops.Controls.Add(txtGPST, 3, 1);

            txtX = CreateOutput("X");
            txtY = CreateOutput("Y");
            txtZ = CreateOutput("Z");
            ops.Controls.Add(txtX, 4, 1);
            ops.Controls.Add(txtY, 5, 1);
            ops.Controls.Add(txtZ, 6, 1);

            btnOpenRinex.Click += btnOpenRinex_Click;
            btnSingleCalc.Click += btnSingleCalc_Click;
            btnBatchCalc.Click += btnBatchCalc_Click;
        }

        private Panel CreateCard()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 14, 14),
                BackColor = _panel,
                BorderStyle = BorderStyle.FixedSingle
            };
            return card;
        }

        private Label CreateCaption(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                ForeColor = _muted,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private Label CreateValueLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                ForeColor = _text,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                Location = new Point(x, y),
                Size = new Size(1000, 26),
                AutoEllipsis = true
            };
        }

        private Label CreateInlineCaption(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = _muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0)
            };
        }

        private TextBox CreateInput()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 9, 16, 0),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10F)
            };
        }

        private TextBox CreateOutput(string label)
        {
            TextBox box = CreateInput();
            box.Text = label;
            box.ReadOnly = true;
            box.BackColor = Color.FromArgb(249, 251, 253);
            return box;
        }

        private Button CreateButton(string text, Color color)
        {
            Button button = new Button
            {
                Text = text,
                Size = new Size(128, 34),
                Margin = new Padding(10, 14, 0, 0),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private async void btnOpenRinex_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "RINEX导航文件(*.nav;*.rnx;*.??n;*.??g;*.??l;*.??p;*.n;*.g;*.l;*.p;*.gz;*.zip)|*.nav;*.rnx;*.??n;*.??g;*.??l;*.??p;*.n;*.g;*.l;*.p;*.gz;*.zip|所有文件(*.*)|*.*";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                SetBusy(true, "正在读取星历文件...");
                string fileName = dlg.FileName;

                List<GnssEphemeris> loaded = await Task.Run(() =>
                {
                    var reader = new RinexNavReader();
                    List<GnssEphemeris> result = reader.ReadAnyNavFile(fileName);
                    SqliteHelper.SaveEphemerisBatch(result);
                    return result;
                });

                _ephData.Clear();
                _ephData.AddRange(loaded);
                _currentNavFile = fileName;
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = _ephData;
                UpdateSummary();

                MessageBox.Show($"解析完成，有效GNSS星历数量：{_ephData.Count} 条");
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取失败：" + ex.Message);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void btnSingleCalc_Click(object sender, EventArgs e)
        {
            if (_ephData.Count == 0)
            {
                MessageBox.Show("请先载入RINEX星历文件！");
                return;
            }

            try
            {
                string systemCode;
                int prn;
                if (!TryParseSatelliteInput(txtPRN.Text.Trim().ToUpperInvariant(), out systemCode, out prn))
                {
                    MessageBox.Show("卫星号请输入合法格式，例如 1、G01、R08、C19、E11");
                    txtPRN.Focus();
                    return;
                }

                double tk;
                if (!TryParseTk(out tk))
                    return;

                var eph = FindEphemeris(systemCode, prn);
                if (eph == null)
                {
                    MessageBox.Show("未找到该卫星星历");
                    return;
                }

                var pos = _calculator.CalcSatPosition(eph, tk);
                txtX.Text = pos.X.ToString("F4");
                txtY.Text = pos.Y.ToString("F4");
                txtZ.Text = pos.Z.ToString("F4");
                SqliteHelper.SaveResult(pos);
                lblStatus.Text = $"已计算 {pos.SatelliteId} @ tk={tk:0.###}s";
            }
            catch (Exception ex)
            {
                MessageBox.Show("计算出错：" + ex.Message);
            }
        }

        private async void btnBatchCalc_Click(object sender, EventArgs e)
        {
            if (_ephData.Count == 0)
            {
                MessageBox.Show("请先载入RINEX星历文件！");
                return;
            }

            double tk;
            if (!TryParseTk(out tk))
                return;

            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "结果文本(*.txt)|*.txt";
            saveDlg.FileName = BuildDefaultOutputFileName(tk);
            if (!string.IsNullOrWhiteSpace(_currentNavFile))
                saveDlg.InitialDirectory = Path.GetDirectoryName(_currentNavFile);
            if (saveDlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SetBusy(true, "正在计算并导出...");
                string outputPath = saveDlg.FileName;
                List<GnssEphemeris> ephSnapshot = new List<GnssEphemeris>(_ephData);

                int count = await Task.Run(() =>
                {
                    List<SatPositionResult> results = new List<SatPositionResult>();
                    foreach (var eph in ephSnapshot)
                        results.Add(_calculator.CalcSatPosition(eph, tk));

                    SqliteHelper.SaveResultBatch(results);
                    using (StreamWriter sw = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine("卫星编号,系统,PRN,计算时刻,X坐标(m),Y坐标(m),Z坐标(m),方法");
                        foreach (var r in results)
                            sw.WriteLine($"{r.SatelliteId},{r.SystemCode},{r.PRN},{r.CalcTime:yyyy-MM-dd HH:mm:ss},{r.X:F3},{r.Y:F3},{r.Z:F3},{r.Method}");
                    }
                    return results.Count;
                });

                MessageBox.Show($"导出完成！共计算 {count} 组数据。\n文件位置：{outputPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败：" + ex.Message);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private bool TryParseTk(out double tk)
        {
            tk = 0;
            string tkText = txtGPST.Text.Trim();
            if (string.IsNullOrWhiteSpace(tkText))
                return true;
            if (double.TryParse(tkText, out tk))
                return true;

            MessageBox.Show("tk请输入合法数字，单位为秒；留空或填写 0 表示星历历元时刻");
            txtGPST.Focus();
            return false;
        }

        private void SetBusy(bool busy, string text)
        {
            btnOpenRinex.Enabled = !busy;
            btnSingleCalc.Enabled = !busy;
            btnBatchCalc.Enabled = !busy;
            Text = busy && !string.IsNullOrWhiteSpace(text)
                ? "GNSS多系统卫星坐标计算 - " + text
                : "GNSS多系统卫星坐标计算";
            lblStatus.Text = busy && !string.IsNullOrWhiteSpace(text) ? text : "";
        }

        private void UpdateSummary()
        {
            lblFileName.Text = string.IsNullOrWhiteSpace(_currentNavFile) ? "尚未载入星历文件" : _currentNavFile;
            lblRecordCount.Text = _ephData.Count.ToString();

            var groups = _ephData
                .GroupBy(e => e.SystemCode)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}:{g.Count()}");
            lblSystemCount.Text = _ephData.Count == 0 ? "-" : string.Join("  ", groups);

            if (_ephData.Count == 0)
            {
                lblEpochRange.Text = "-";
                return;
            }

            DateTime min = _ephData.Min(e => e.EpochTime);
            DateTime max = _ephData.Max(e => e.EpochTime);
            lblEpochRange.Text = min.Date == max.Date
                ? min.ToString("MM-dd HH:mm") + " - " + max.ToString("HH:mm")
                : min.ToString("MM-dd HH:mm") + " - " + max.ToString("MM-dd HH:mm");
        }

        private string BuildDefaultOutputFileName(double tk)
        {
            string stem = "gnss_satpos";
            if (!string.IsNullOrWhiteSpace(_currentNavFile))
                stem = Path.GetFileNameWithoutExtension(_currentNavFile);

            string tkPart = tk.ToString("0.###").Replace("-", "m").Replace(".", "p");
            string timePart = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{stem}_satpos_tk{tkPart}_{timePart}.txt";
        }

        private bool TryParseSatelliteInput(string input, out string systemCode, out int prn)
        {
            systemCode = null;
            prn = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (char.IsLetter(input[0]))
            {
                systemCode = input.Substring(0, 1);
                return int.TryParse(input.Substring(1), out prn) && prn > 0;
            }

            return int.TryParse(input, out prn) && prn > 0;
        }

        private GnssEphemeris FindEphemeris(string systemCode, int prn)
        {
            if (!string.IsNullOrWhiteSpace(systemCode))
                return _ephData.Find(p => p.SystemCode == systemCode && p.PRN == prn);

            return _ephData.Find(p => p.PRN == prn);
        }
    }
}
