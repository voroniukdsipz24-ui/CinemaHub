using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace CinemaHub
{
    /// <summary>
    /// Універсальне вікно перегляду RDLC-звіту (MS Report, ReportViewer).
    /// Шаблон береться з папки Reports поруч з exe, дані передаються за Push-моделлю.
    /// </summary>
    public class ReportForm : Form
    {
        private readonly ReportViewer _viewer = new ReportViewer { Dock = DockStyle.Fill };
        private readonly string _title;

        public ReportForm(string title, string rdlcFileName, string dataSetName, DataTable data,
                          IDictionary<string, string> parameters)
        {
            _title = title;
            Text = title;
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;

            var btnPdf = new Button { Text = "Зберегти PDF", Dock = DockStyle.Top, Height = 32 };
            btnPdf.Click += (s, e) => ExportPdf();

            Controls.Add(_viewer);
            Controls.Add(btnPdf);

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", rdlcFileName);
            if (!File.Exists(path))
                throw new FileNotFoundException("Шаблон звіту не знайдено: " + path);

            _viewer.ProcessingMode = ProcessingMode.Local;
            _viewer.LocalReport.ReportPath = path;
            _viewer.LocalReport.DataSources.Clear();
            _viewer.LocalReport.DataSources.Add(new ReportDataSource(dataSetName, data));

            if (parameters != null && parameters.Count > 0)
            {
                var list = new List<ReportParameter>();
                foreach (var pair in parameters)
                    list.Add(new ReportParameter(pair.Key, pair.Value));
                _viewer.LocalReport.SetParameters(list);
            }

            Shown += (s, e) => _viewer.RefreshReport();
        }

        // Експорт без діалогу ReportViewer: Render("PDF") -> byte[] -> файл (Тема 4, розділ 4)
        private void ExportPdf()
        {
            using (var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = _title + ".pdf" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string mime, encoding, ext;
                    string[] streams;
                    Warning[] warnings;
                    byte[] bytes = _viewer.LocalReport.Render("PDF", null, out mime, out encoding, out ext, out streams, out warnings);
                    File.WriteAllBytes(dlg.FileName, bytes);
                    MessageBox.Show("Збережено: " + dlg.FileName, "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Помилка експорту", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
