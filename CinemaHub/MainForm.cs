using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CinemaHub
{
    /// <summary>
    /// Головне вікно CinemaHub. Інтерфейс створюється кодом (без .Designer.cs),
    /// щоб проєкт було легко читати й переносити.
    /// Вкладки: Завдання 1-5 (Тема 3), 10 самостійних варіантів, Звіти RDLC (Теми 4-5).
    /// </summary>
    public class MainForm : Form
    {
        private readonly TabControl _tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly StatusStrip _statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel _status = new ToolStripStatusLabel("Готово");

        // --- Завдання 1
        private readonly DataGridView _gridSchedule = NewGrid();
        private readonly CheckBox _chkOnlyRegular = new CheckBox { Text = "Лише звичайні зали (IsVIP = 0)", Checked = true, AutoSize = true, Margin = new Padding(3, 6, 12, 0) };
        // --- Завдання 2
        private readonly DataGridView _gridRevenue = NewGrid();
        private readonly NumericUpDown _numMinRevenue = new NumericUpDown { Maximum = 1000000, Value = 300, Width = 90 };
        // --- Завдання 3
        private readonly DataGridView _gridOccupancy = NewGrid();
        // --- Завдання 4
        private readonly DataGridView _gridSearch = NewGrid();
        private readonly TextBox _txtClient = new TextBox { Text = "Олександр", Width = 160 };
        private readonly DateTimePicker _dtSearchFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 9, 1), Width = 100 };
        private readonly DateTimePicker _dtSearchTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 9, 30), Width = 100 };
        // --- Завдання 5
        private readonly DataGridView _gridPrices = NewGrid();
        private readonly NumericUpDown _numPercent = new NumericUpDown { Minimum = -90, Maximum = 500, Value = 15, Width = 70 };
        private readonly DateTimePicker _dtEvening = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm", Value = new DateTime(2026, 10, 1, 18, 0, 0), Width = 150 };
        // --- Варіанти
        private readonly DataGridView _gridVariant = NewGrid();
        private readonly ComboBox _cmbVariant = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380 };
        private readonly TextBox _txtVariantSql = new TextBox { Multiline = true, Dock = DockStyle.Top, Height = 150, Font = new Font("Consolas", 10f), ScrollBars = ScrollBars.Vertical };
        private readonly NumericUpDown _numGenre = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Width = 60 };
        private readonly NumericUpDown _numMaxPrice = new NumericUpDown { Maximum = 100000, Value = 250, Width = 80 };
        // --- Звіти
        private readonly DateTimePicker _dtRepFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 9, 1), Width = 100 };
        private readonly DateTimePicker _dtRepTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 10, 31), Width = 100 };
        private readonly NumericUpDown _numTicketId = new NumericUpDown { Minimum = 1, Maximum = 1000000, Value = 1, Width = 70 };

        private sealed class Variant
        {
            public string Name;
            public string Sql;
            public override string ToString() => Name;
        }

        private static readonly Variant[] Variants =
        {
            new Variant { Name = "1. Заповненість VIP-залів (квитки та виручка)",
                Sql = "SELECT h.HallName AS [Зал], COUNT(t.TicketID) AS [Квитків], SUM(s.TicketPrice) AS [Виручка]\r\n" +
                      "FROM Halls h\r\nINNER JOIN Sessions s ON h.HallID = s.HallID\r\nINNER JOIN Tickets t ON s.SessionID = t.SessionID\r\n" +
                      "WHERE h.IsVIP = 1\r\nGROUP BY h.HallName" },
            new Variant { Name = "2. Клієнти, що купили більше 2 квитків",
                Sql = "SELECT CustomerName AS [Клієнт], COUNT(TicketID) AS [Квитків]\r\nFROM Tickets\r\nGROUP BY CustomerName\r\nHAVING COUNT(TicketID) > 2" },
            new Variant { Name = "3. ТОП-3 фільми за кількістю проданих квитків",
                Sql = "SELECT TOP 3 m.Title AS [Фільм], COUNT(t.TicketID) AS [Квитків]\r\nFROM Movies m\r\nINNER JOIN Sessions s ON m.MovieID = s.MovieID\r\nINNER JOIN Tickets t ON s.SessionID = t.SessionID\r\nGROUP BY m.Title\r\nORDER BY COUNT(t.TicketID) DESC" },
            new Variant { Name = "4. Видалення неоплачених квитків (DELETE)",
                Sql = "DELETE FROM Tickets WHERE IsPaid = 0" },
            new Variant { Name = "5. Середня ціна квитка та кількість сеансів за AgeRating",
                Sql = "SELECT m.AgeRating AS [Віковий рейтинг], AVG(s.TicketPrice) AS [Середня ціна], COUNT(s.SessionID) AS [Сеансів]\r\nFROM Movies m\r\nINNER JOIN Sessions s ON m.MovieID = s.MovieID\r\nGROUP BY m.AgeRating" },
            new Variant { Name = "6. Сеанси без жодного проданого квитка",
                Sql = "SELECT s.SessionID, m.Title AS [Фільм], s.StartTime AS [Початок], s.TicketPrice AS [Ціна]\r\nFROM Sessions s\r\nINNER JOIN Movies m ON s.MovieID = m.MovieID\r\nLEFT JOIN Tickets t ON s.SessionID = t.SessionID\r\nWHERE t.TicketID IS NULL" },
            new Variant { Name = "7. Параметризований пошук: @GenreID та ціна <= @MaxPrice",
                Sql = "SELECT s.SessionID, m.Title AS [Фільм], s.StartTime AS [Початок], s.TicketPrice AS [Ціна]\r\nFROM Sessions s\r\nINNER JOIN Movies m ON s.MovieID = m.MovieID\r\nWHERE m.GenreID = @GenreID AND s.TicketPrice <= @MaxPrice" },
            new Variant { Name = "8. Знижка 20% на жанр 'Анімація та Сімейні' (UPDATE)",
                Sql = "UPDATE s\r\nSET s.TicketPrice = s.TicketPrice * 0.8\r\nFROM Sessions s\r\nINNER JOIN Movies m ON s.MovieID = m.MovieID\r\nINNER JOIN Genres g ON m.GenreID = g.GenreID\r\nWHERE g.GenreName = N'Анімація та Сімейні'" },
            new Variant { Name = "9. Погодинна завантаженість (продажі за годинами)",
                Sql = "SELECT DATEPART(hour, s.StartTime) AS [Година], COUNT(t.TicketID) AS [Квитків], SUM(s.TicketPrice) AS [Виручка]\r\nFROM Sessions s\r\nINNER JOIN Tickets t ON s.SessionID = t.SessionID\r\nGROUP BY DATEPART(hour, s.StartTime)\r\nORDER BY [Година]" },
            new Variant { Name = "10. Прибутковість залів (від max до min)",
                Sql = "SELECT h.HallName AS [Зал], SUM(s.TicketPrice) AS [Дохід]\r\nFROM Halls h\r\nINNER JOIN Sessions s ON h.HallID = s.HallID\r\nINNER JOIN Tickets t ON s.SessionID = t.SessionID\r\nGROUP BY h.HallName\r\nORDER BY SUM(s.TicketPrice) DESC" },
        };

        public MainForm()
        {
            Text = "CinemaHub - Інформаційна система онлайн-кінотеатру";
            Width = 1100;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            _statusStrip.Items.Add(_status);
            Controls.Add(_tabs);
            Controls.Add(_statusStrip);

            BuildScheduleTab();
            BuildRevenueTab();
            BuildOccupancyTab();
            BuildSearchTab();
            BuildPriceTab();
            BuildVariantsTab();
            BuildReportsTab();

            Shown += MainForm_Shown;
        }

        // ============================ допоміжні методи ============================
        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            RowHeadersVisible = false
        };

        private static FlowLayoutPanel NewBar() => new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(6, 4, 6, 4),
            WrapContents = false
        };

        private static Label Lbl(string text) => new Label { Text = text, AutoSize = true, Margin = new Padding(6, 7, 3, 0) };

        private static Button Btn(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, AutoSize = true, Margin = new Padding(8, 1, 3, 0), Padding = new Padding(6, 1, 6, 1) };
            b.Click += onClick;
            return b;
        }

        private TabPage NewTab(string title)
        {
            var page = new TabPage(title);
            _tabs.TabPages.Add(page);
            return page;
        }

        private void RunQuery(DataGridView grid, string sql, params SqlParameter[] parameters)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                DataTable table = Db.Query(sql, parameters);
                grid.DataSource = table;
                _status.Text = "Отримано рядків: " + table.Rows.Count;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ShowError(Exception ex)
        {
            _status.Text = "Помилка";
            MessageBox.Show(ex.Message, "CinemaHub - помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                Db.Query("SELECT 1");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не вдалося підключитися до бази CinemaHub_DB.\n\n" + ex.Message +
                    "\n\nЩо перевірити:\n" +
                    "1) Чи виконано скрипт SQL\\01_CinemaHub_DB.sql (створює базу та дані).\n" +
                    "2) Чи правильний рядок підключення у файлі App.config (LocalDB / SQLEXPRESS / localhost).",
                    "CinemaHub - немає з'єднання", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            LoadSchedule();
            LoadRevenue();
            LoadOccupancy();
            LoadPrices();
        }

        // ============================ ЗАВДАННЯ 1 ============================
        private void BuildScheduleTab()
        {
            var page = NewTab("1. Розклад сеансів");
            var bar = NewBar();
            bar.Controls.Add(_chkOnlyRegular);
            bar.Controls.Add(Btn("Оновити", (s, e) => LoadSchedule()));
            page.Controls.Add(_gridSchedule);
            page.Controls.Add(bar);
        }

        private void LoadSchedule()
        {
            RunQuery(_gridSchedule,
                "SELECT m.Title AS [Назва Фільму], m.AgeRating AS [Віковий Рейтинг], g.GenreName AS [Жанр], " +
                "h.HallName AS [Кінозал], h.Capacity AS [Місткість], s.StartTime AS [Час Початку], s.TicketPrice AS [Ціна] " +
                "FROM Genres g " +
                "INNER JOIN Movies m ON g.GenreID = m.GenreID " +
                "INNER JOIN Sessions s ON m.MovieID = s.MovieID " +
                "INNER JOIN Halls h ON s.HallID = h.HallID " +
                "WHERE (@OnlyRegular = 0 OR h.IsVIP = 0) " +
                "ORDER BY s.StartTime",
                Db.P("@OnlyRegular", _chkOnlyRegular.Checked ? 1 : 0));
        }

        // ============================ ЗАВДАННЯ 2 ============================
        private void BuildRevenueTab()
        {
            var page = NewTab("2. Каса (GROUP BY / HAVING)");
            var bar = NewBar();
            bar.Controls.Add(Lbl("Показати фільми з виручкою більше, грн:"));
            bar.Controls.Add(_numMinRevenue);
            bar.Controls.Add(Btn("Розрахувати", (s, e) => LoadRevenue()));
            page.Controls.Add(_gridRevenue);
            page.Controls.Add(bar);
        }

        private void LoadRevenue()
        {
            RunQuery(_gridRevenue,
                "SELECT m.Title AS [Фільм], COUNT(t.TicketID) AS [Продано Квитків], SUM(s.TicketPrice) AS [Загальна Виручка] " +
                "FROM Movies m " +
                "INNER JOIN Sessions s ON m.MovieID = s.MovieID " +
                "INNER JOIN Tickets t ON s.SessionID = t.SessionID " +
                "GROUP BY m.Title " +
                "HAVING SUM(s.TicketPrice) > @MinRevenue " +
                "ORDER BY [Загальна Виручка] DESC",
                Db.P("@MinRevenue", _numMinRevenue.Value));
        }

        // ============================ ЗАВДАННЯ 3 ============================
        private void BuildOccupancyTab()
        {
            var page = NewTab("3. Заповненість залів");
            var bar = NewBar();
            bar.Controls.Add(Lbl("Відсоток = (квитки / місткість) x 100. LEFT JOIN показує і сеанси без квитків."));
            bar.Controls.Add(Btn("Оновити", (s, e) => LoadOccupancy()));
            page.Controls.Add(_gridOccupancy);
            page.Controls.Add(bar);
        }

        private void LoadOccupancy()
        {
            RunQuery(_gridOccupancy,
                "SELECT s.SessionID, s.StartTime AS [Дата та Час], h.HallName AS [Зал], h.Capacity AS [Всього місць], " +
                "COUNT(t.TicketID) AS [Куплено квитків], " +
                "ROUND((CAST(COUNT(t.TicketID) AS FLOAT) / h.Capacity) * 100, 2) AS [Заповненість %] " +
                "FROM Halls h " +
                "INNER JOIN Sessions s ON h.HallID = s.HallID " +
                "LEFT OUTER JOIN Tickets t ON s.SessionID = t.SessionID " +
                "GROUP BY s.SessionID, s.StartTime, h.HallName, h.Capacity " +
                "ORDER BY s.StartTime");
        }

        // ============================ ЗАВДАННЯ 4 ============================
        private void BuildSearchTab()
        {
            var page = NewTab("4. Пошук квитків (параметри)");
            var bar = NewBar();
            bar.Controls.Add(Lbl("Ім'я клієнта:"));
            bar.Controls.Add(_txtClient);
            bar.Controls.Add(Lbl("Купівля з:"));
            bar.Controls.Add(_dtSearchFrom);
            bar.Controls.Add(Lbl("по:"));
            bar.Controls.Add(_dtSearchTo);
            bar.Controls.Add(Btn("Знайти", (s, e) => LoadSearch()));
            page.Controls.Add(_gridSearch);
            page.Controls.Add(bar);
        }

        private void LoadSearch()
        {
            // Аналог методу TableAdapter GetDataByClientAndDate(@CustomerName, @StartDate, @EndDate).
            // Кінцева дата включається повністю (до наступної доби).
            RunQuery(_gridSearch,
                "SELECT TicketID, SessionID, CustomerName AS [Клієнт], CustomerPhone AS [Телефон], SeatNumber AS [Місце], " +
                "PurchaseDate AS [Дата купівлі], IsPaid AS [Оплачено] " +
                "FROM Tickets " +
                "WHERE CustomerName LIKE '%' + @CustomerName + '%' " +
                "AND PurchaseDate >= @StartDate AND PurchaseDate < DATEADD(day, 1, @EndDate)",
                Db.P("@CustomerName", _txtClient.Text.Trim()),
                Db.P("@StartDate", _dtSearchFrom.Value.Date),
                Db.P("@EndDate", _dtSearchTo.Value.Date));
        }

        // ============================ ЗАВДАННЯ 5 ============================
        private void BuildPriceTab()
        {
            var page = NewTab("5. Масова зміна цін (UPDATE)");
            var bar = NewBar();
            bar.Controls.Add(Lbl("Змінити ціну на, %:"));
            bar.Controls.Add(_numPercent);
            bar.Controls.Add(Lbl("для сеансів, що починаються від:"));
            bar.Controls.Add(_dtEvening);
            bar.Controls.Add(Btn("Застосувати UPDATE", (s, e) => ApplyPriceChange()));
            bar.Controls.Add(Btn("Оновити таблицю", (s, e) => LoadPrices()));
            page.Controls.Add(_gridPrices);
            page.Controls.Add(bar);
        }

        private void LoadPrices()
        {
            RunQuery(_gridPrices,
                "SELECT s.SessionID, m.Title AS [Фільм], s.StartTime AS [Початок], s.TicketPrice AS [Ціна] " +
                "FROM Sessions s INNER JOIN Movies m ON s.MovieID = m.MovieID ORDER BY s.StartTime");
        }

        private void ApplyPriceChange()
        {
            string question = string.Format(
                "Змінити ціни на {0}% для всіх сеансів від {1:dd.MM.yyyy HH:mm}?\n\n" +
                "Увага: кожне натискання змінює ціни ще раз (15% + 15% = 32,25%).\n" +
                "Щоб повернути початкові дані, заново виконайте SQL\\01_CinemaHub_DB.sql.",
                _numPercent.Value, _dtEvening.Value);
            if (MessageBox.Show(question, "Підтвердження UPDATE", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                int rows = Db.Execute(
                    "UPDATE Sessions SET TicketPrice = ROUND(TicketPrice * (1 + @Percent / 100.0), 2) WHERE StartTime >= @From",
                    Db.P("@Percent", _numPercent.Value), Db.P("@From", _dtEvening.Value));
                _status.Text = "Оновлено сеансів: " + rows;
                LoadPrices();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ============================ САМОСТІЙНІ ВАРІАНТИ ============================
        private void BuildVariantsTab()
        {
            var page = NewTab("Самостійні варіанти 1-10");
            var bar = NewBar();
            _cmbVariant.Items.AddRange(Variants);
            _cmbVariant.SelectedIndexChanged += (s, e) =>
            {
                var v = _cmbVariant.SelectedItem as Variant;
                if (v != null) _txtVariantSql.Text = v.Sql;
            };
            bar.Controls.Add(_cmbVariant);
            bar.Controls.Add(Lbl("@GenreID:"));
            bar.Controls.Add(_numGenre);
            bar.Controls.Add(Lbl("@MaxPrice:"));
            bar.Controls.Add(_numMaxPrice);
            bar.Controls.Add(Btn("Виконати", (s, e) => RunVariant()));

            // порядок додавання важливий для Dock: Fill -> Top (текст) -> Top (панель)
            page.Controls.Add(_gridVariant);
            page.Controls.Add(_txtVariantSql);
            page.Controls.Add(bar);
            _cmbVariant.SelectedIndex = 0;
        }

        private void RunVariant()
        {
            string sql = _txtVariantSql.Text.Trim();
            if (sql.Length == 0) return;

            var parameters = new List<SqlParameter>();
            if (sql.IndexOf("@GenreID", StringComparison.OrdinalIgnoreCase) >= 0)
                parameters.Add(Db.P("@GenreID", (int)_numGenre.Value));
            if (sql.IndexOf("@MaxPrice", StringComparison.OrdinalIgnoreCase) >= 0)
                parameters.Add(Db.P("@MaxPrice", _numMaxPrice.Value));

            bool isSelect = sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
            if (isSelect)
            {
                RunQuery(_gridVariant, sql, parameters.ToArray());
                return;
            }

            if (MessageBox.Show("Цей запит ЗМІНЮЄ дані:\n\n" + sql + "\n\nВиконати?", "Підтвердження DML-запиту",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                int rows = Db.Execute(sql, parameters.ToArray());
                _gridVariant.DataSource = null;
                _status.Text = "Запит виконано, змінено рядків: " + rows;
                MessageBox.Show("Змінено рядків: " + rows, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ============================ ЗВІТИ (RDLC) ============================
        private void BuildReportsTab()
        {
            var page = NewTab("Звіти (RDLC)");

            var info = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Text = "Звіти будуються за Push-моделлю: C#-код виконує SQL-запит і передає DataTable у шаблон .rdlc (папка Reports).\r\n\r\n" +
                       "1) Каса по жанрах - групування за жанром, підсумки по жанру та загальний підсумок (Тема 4, Модуль Б).\r\n" +
                       "2) Квиток покупця - друкована форма квитка з ПДВ 20% і статусом оплати (Тема 4, Модуль А).\r\n\r\n" +
                       "У вікні звіту є кнопки друку та експорту (PDF, Excel, Word) і кнопка \"Зберегти PDF\"."
            };

            var barTicket = NewBar();
            barTicket.Controls.Add(Lbl("Квиток №"));
            barTicket.Controls.Add(_numTicketId);
            barTicket.Controls.Add(Btn("Відкрити квиток покупця", (s, e) => OpenTicketReport()));

            var barRevenue = NewBar();
            barRevenue.Controls.Add(Lbl("Період з:"));
            barRevenue.Controls.Add(_dtRepFrom);
            barRevenue.Controls.Add(Lbl("по:"));
            barRevenue.Controls.Add(_dtRepTo);
            barRevenue.Controls.Add(Btn("Відкрити звіт 'Каса по жанрах'", (s, e) => OpenRevenueReport()));

            page.Controls.Add(info);
            page.Controls.Add(barTicket);
            page.Controls.Add(barRevenue);
        }

        private void OpenRevenueReport()
        {
            try
            {
                DataTable dt = Db.Query(
                    "SELECT g.GenreName, m.Title, COUNT(t.TicketID) AS TotalTicketsSold, SUM(s.TicketPrice) AS TotalRevenue " +
                    "FROM Genres g " +
                    "INNER JOIN Movies m ON g.GenreID = m.GenreID " +
                    "INNER JOIN Sessions s ON m.MovieID = s.MovieID " +
                    "INNER JOIN Tickets t ON s.SessionID = t.SessionID " +
                    "WHERE t.PurchaseDate >= @FromDate AND t.PurchaseDate < DATEADD(day, 1, @ToDate) " +
                    "GROUP BY g.GenreName, m.Title " +
                    "ORDER BY g.GenreName, m.Title",
                    Db.P("@FromDate", _dtRepFrom.Value.Date), Db.P("@ToDate", _dtRepTo.Value.Date));

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("За вибраний період продажів немає.", "Звіт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var pars = new Dictionary<string, string>
                {
                    { "PeriodFrom", _dtRepFrom.Value.ToString("dd.MM.yyyy") },
                    { "PeriodTo", _dtRepTo.Value.ToString("dd.MM.yyyy") }
                };
                new ReportForm("Звіт: каса по жанрах", "RevenueReport.rdlc", "RevenueDS", dt, pars).Show(this);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void OpenTicketReport()
        {
            try
            {
                DataTable dt = Db.Query("SELECT * FROM vw_TicketDetails WHERE TicketID = @TicketID",
                    Db.P("@TicketID", (int)_numTicketId.Value));
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Квиток з таким номером не знайдено.", "Звіт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var pars = new Dictionary<string, string> { { "OperatorName", "Касир #1 (Іванов)" } };
                new ReportForm("Квиток покупця", "TicketReport.rdlc", "TicketDS", dt, pars).Show(this);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
