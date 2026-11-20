using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using BusinessObjects;
using ServiceLayer;

namespace HospitalUI
{
    public class MainWindow : Window
    {
        private readonly IHospitalService _service = new HospitalService();

        private ComboBox cmbPatients, cmbDepartments, cmbDoctors, cmbPrescType, cmbStaff;
        private TextBox txtName, txtAge, txtDiagnosis, txtPrimaryExam, txtPrescTitle, txtParam1, txtParam2, txtEmrView;
        private ListBox lstPrescriptions;
        private TextBlock lblStatus;

        public MainWindow()
        {
            Title = "Информационная система «Больница»";
            Width = 900;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            BuildInterface();
            LoadInitialData();
        }

        private void ShowAlert(string text, bool isError = false)
        {
            lblStatus.Text = text;
            lblStatus.Foreground = isError ? Brushes.Crimson : Brushes.DarkGreen;
        }

        private void BuildInterface()
        {
            var root = new DockPanel { Margin = new Thickness(12) };

            lblStatus = new TextBlock { Text = "Готово к работе", Margin = new Thickness(0, 8, 0, 0), FontWeight = FontWeight.Bold };
            DockPanel.SetDock(lblStatus, Dock.Bottom);
            root.Children.Add(lblStatus);

            var topBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 10) };
            topBar.Children.Add(new TextBlock { Text = "Текущий пациент:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.Bold });
            cmbPatients = new ComboBox { Width = 350 };
            cmbPatients.SelectionChanged += (s, e) => RefreshPatientData();
            topBar.Children.Add(cmbPatients);
            DockPanel.SetDock(topBar, Dock.Top);
            root.Children.Add(topBar);

            var tabs = new TabControl();

            var pnlAdmit = new StackPanel { Spacing = 8, Margin = new Thickness(10), MaxWidth = 450, HorizontalAlignment = HorizontalAlignment.Left };
            txtName = new TextBox { Watermark = "ФИО пациента" };
            txtAge = new TextBox { Watermark = "Возраст (число)" };
            txtDiagnosis = new TextBox { Watermark = "Диагноз при поступлении" };
            cmbDepartments = new ComboBox { Width = 450 };
            txtPrimaryExam = new TextBox { Watermark = "Заметки первичного осмотра", Height = 80, AcceptsReturn = true };
            var btnAdmit = new Button { Content = "Создать ЭМК и госпитализировать" };
            btnAdmit.Click += BtnAdmit_Click;

            pnlAdmit.Children.Add(new TextBlock { Text = "ФИО пациента:" });
            pnlAdmit.Children.Add(txtName);
            pnlAdmit.Children.Add(new TextBlock { Text = "Возраст:" });
            pnlAdmit.Children.Add(txtAge);
            pnlAdmit.Children.Add(new TextBlock { Text = "Диагноз при поступлении:" });
            pnlAdmit.Children.Add(txtDiagnosis);
            pnlAdmit.Children.Add(new TextBlock { Text = "Отделение:" });
            pnlAdmit.Children.Add(cmbDepartments);
            pnlAdmit.Children.Add(new TextBlock { Text = "Первичный осмотр:" });
            pnlAdmit.Children.Add(txtPrimaryExam);
            pnlAdmit.Children.Add(btnAdmit);
            tabs.Items.Add(new TabItem { Header = "1. Госпитализация", Content = pnlAdmit });

            var pnlDoc = new StackPanel { Spacing = 8, Margin = new Thickness(10), MaxWidth = 500, HorizontalAlignment = HorizontalAlignment.Left };
            var docRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            cmbDoctors = new ComboBox { Width = 300 };
            var btnAssignDoc = new Button { Content = "Закрепить врача" };
            btnAssignDoc.Click += BtnAssignDoc_Click;
            docRow.Children.Add(cmbDoctors);
            docRow.Children.Add(btnAssignDoc);

            cmbPrescType = new ComboBox
            {
                Width = 300,
                ItemsSource = new string[] { "Инъекции", "Таблетки", "Диагностика", "Профилактика" },
                SelectedIndex = 0
            };
            txtPrescTitle = new TextBox { Watermark = "Название препарата / процедуры" };
            txtParam1 = new TextBox { Watermark = "Кол-во инъекций ИЛИ дозировка таблеток" };
            txtParam2 = new TextBox { Watermark = "Кол-во дней (только для таблеток)" };
            var btnAddPresc = new Button { Content = "Добавить назначение в ЭМК" };
            btnAddPresc.Click += BtnAddPresc_Click;

            pnlDoc.Children.Add(new TextBlock { Text = "Лечащий врач отделения:" });
            pnlDoc.Children.Add(docRow);
            pnlDoc.Children.Add(new Separator { Margin = new Thickness(0, 10) });
            pnlDoc.Children.Add(new TextBlock { Text = "Тип назначения:" });
            pnlDoc.Children.Add(cmbPrescType);
            pnlDoc.Children.Add(new TextBlock { Text = "Название / Препарат:" });
            pnlDoc.Children.Add(txtPrescTitle);
            pnlDoc.Children.Add(new TextBlock { Text = "Кол-во / Дозировка:" });
            pnlDoc.Children.Add(txtParam1);
            pnlDoc.Children.Add(new TextBlock { Text = "Дней (для таблеток):" });
            pnlDoc.Children.Add(txtParam2);
            pnlDoc.Children.Add(btnAddPresc);
            tabs.Items.Add(new TabItem { Header = "2. Назначения врача", Content = pnlDoc });

            var pnlExec = new StackPanel { Spacing = 10, Margin = new Thickness(10) };
            lstPrescriptions = new ListBox { Height = 280 };
            var execRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            cmbStaff = new ComboBox { Width = 320 };
            var btnExec = new Button { Content = "Отметить выполненным" };
            btnExec.Click += BtnExec_Click;
            execRow.Children.Add(new TextBlock { Text = "Исполнитель:", VerticalAlignment = VerticalAlignment.Center });
            execRow.Children.Add(cmbStaff);
            execRow.Children.Add(btnExec);

            pnlExec.Children.Add(lstPrescriptions);
            pnlExec.Children.Add(execRow);
            tabs.Items.Add(new TabItem { Header = "3. Выполнение назначений", Content = pnlExec });

            var pnlEmr = new StackPanel { Spacing = 10, Margin = new Thickness(10) };
            txtEmrView = new TextBox { Height = 360, IsReadOnly = true, AcceptsReturn = true, FontFamily = new FontFamily("Monospace") };
            var btnDischarge = new Button { Content = "Выписать пациента и сформировать эпикриз" };
            btnDischarge.Click += BtnDischarge_Click;
            pnlEmr.Children.Add(txtEmrView);
            pnlEmr.Children.Add(btnDischarge);
            tabs.Items.Add(new TabItem { Header = "4. ЭМК и Выписка", Content = pnlEmr });

            root.Children.Add(tabs);
            Content = root;
        }

        private void LoadInitialData()
        {
            cmbDepartments.ItemsSource = _service.GetDepartments();
            cmbDepartments.SelectedIndex = 0;
            cmbStaff.ItemsSource = _service.GetAllStaff();
            cmbStaff.SelectedIndex = 0;
        }

        private void RefreshPatientList(Patient selectPatient = null)
        {
            var list = _service.GetPatients().ToList();
            cmbPatients.ItemsSource = list;
            if (selectPatient != null)
                cmbPatients.SelectedItem = selectPatient;
        }

        private void RefreshPatientData()
        {
            if (!(cmbPatients.SelectedItem is Patient p)) return;

            var docs = _service.GetDoctorsByDepartment(p.DepartmentId);
            cmbDoctors.ItemsSource = docs;
            cmbDoctors.SelectedItem = p.AttendingDoctorId.HasValue
                ? docs.FirstOrDefault(d => d.Id == p.AttendingDoctorId)
                : docs.FirstOrDefault();

            lstPrescriptions.ItemsSource = p.Record.Prescriptions
                .Select(pr => $"{(pr.IsCompleted ? "[ВЫПОЛНЕНО]" : "[ОЖИДАЕТ]")} {pr.GetDetails()}")
                .ToList();

            txtEmrView.Text = p.GenerateDischargeSummary();
        }

        private void BtnAdmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.TryParse(txtAge.Text, out int age);
                if (!(cmbDepartments.SelectedItem is Department dept)) return;

                var patient = new Patient
                {
                    FullName = txtName.Text,
                    Age = age,
                    AdmissionDiagnosis = txtDiagnosis.Text,
                    DepartmentId = dept.Id
                };

                _service.AdmitPatient(patient, txtPrimaryExam.Text);
                RefreshPatientList(patient);
                ShowAlert($"Пациент {patient.FullName} госпитализирован, ЭМК создана.");
            }
            catch (Exception ex)
            {
                ShowAlert($"Ошибка: {ex.Message}", true);
            }
        }

        private void BtnAssignDoc_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbPatients.SelectedItem is Patient p) || !(cmbDoctors.SelectedItem is Doctor d)) return;
            _service.AssignAttendingDoctor(p.Id, d.Id);
            ShowAlert($"Лечащий врач {d.FullName} закреплен за пациентом.");
        }

        private void BtnAddPresc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(cmbPatients.SelectedItem is Patient p))
                {
                    ShowAlert("Выберите пациента!", true);
                    return;
                }
                Prescription pr;
                switch (cmbPrescType.SelectedItem?.ToString())
                {
                    case "Инъекции":
                        int.TryParse(txtParam1.Text, out int count);
                        pr = new InjectionPrescription { Title = txtPrescTitle.Text, Count = count };
                        break;
                    case "Таблетки":
                        int.TryParse(txtParam2.Text, out int days);
                        pr = new PillPrescription { Title = txtPrescTitle.Text, Dosage = txtParam1.Text, Days = days };
                        break;
                    case "Диагностика":
                        pr = new DiagnosticPrescription { Title = txtPrescTitle.Text };
                        break;
                    default:
                        pr = new PreventivePrescription { Title = txtPrescTitle.Text };
                        break;
                }
                _service.AddPrescription(p.Id, pr);
                RefreshPatientData();
                ShowAlert("Назначение добавлено в ЭМК.");
            }
            catch (Exception ex)
            {
                ShowAlert($"Ошибка валидации: {ex.Message}", true);
            }
        }
        private void BtnExec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(cmbPatients.SelectedItem is Patient p) || lstPrescriptions.SelectedIndex < 0)
                {
                    ShowAlert("Выберите назначение из списка!", true);
                    return;
                }
                var pr = p.Record.Prescriptions[lstPrescriptions.SelectedIndex];
                var staff = (Staff)cmbStaff.SelectedItem;
                _service.ExecutePrescription(p.Id, pr.Id, staff.Id);
                RefreshPatientData();
                ShowAlert("Назначение успешно выполнено!");
            }
            catch (Exception ex)
            {
                ShowAlert($"Нарушение правила: {ex.Message}", true);
            }
        }
        private void BtnDischarge_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbPatients.SelectedItem is Patient p)) return;
            txtEmrView.Text = _service.DischargePatient(p.Id);
            RefreshPatientList(p);
            ShowAlert("Пациент выписан, выписной эпикриз сформирован.");
        }
    }
}