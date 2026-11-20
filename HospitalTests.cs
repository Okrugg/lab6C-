using System;
using System.Linq;
using BusinessObjects;
using ServiceLayer;
using Xunit;

namespace HospitalTests
{
    public class HospitalSystemTests
    {
        private readonly IHospitalService _service = new HospitalService();

        private Patient CreateValidPatient(string name = "Тестовый Пациент") => new Patient
        {
            FullName = name,
            Age = 35,
            AdmissionDiagnosis = "Острый бронхит",
            DepartmentId = 1
        };


        [Fact]
        public void Patient_Validation_FailsWhenDataIsInvalid()
        {
            var invalidPatient = new Patient
            {
                FullName = "",       
                Age = -5,           
                AdmissionDiagnosis = "   " 
            };

            bool isValid = invalidPatient.IsValid();

            Assert.False(isValid);
            Assert.Equal(3, invalidPatient.Errors.Count);
        }

        [Fact]
        public void Prescriptions_Validation_ChecksSpecificFields()
        {
            var badInjection = new InjectionPrescription { Title = "Витамин B12", Count = 0 };
            var badPills = new PillPrescription { Title = "Аспирин", Dosage = "", Days = 0 };
            var validPills = new PillPrescription { Title = "Аспирин", Dosage = "500 мг", Days = 5 };

            Assert.False(badInjection.IsValid());
            Assert.False(badPills.IsValid());
            Assert.True(validPills.IsValid());
        }


        [Fact]
        public void ExecutePrescription_Medication_OnlyNurseCanExecute()
        {
            var doctor = new Doctor { Id = 10, FullName = "Доктор Хаус", Specialty = "Диагност" };
            var nurse = new Nurse { Id = 20, FullName = "Медсестра Рэтчед" };
            var injection = new InjectionPrescription { Title = "Анальгин", Count = 2 };

            Assert.Throws<InvalidOperationException>(() => injection.Execute(doctor));

            injection.Execute(nurse);
            Assert.True(injection.IsCompleted);
            Assert.Equal("Медсестра Рэтчед", injection.CompletedBy);
            Assert.NotNull(injection.CompletedDate);
        }

        [Fact]
        public void ExecutePrescription_DiagnosticAndPreventive_OnlyDoctorCanExecute()
        {
            var doctor = new Doctor { Id = 10, FullName = "Доктор Ватсон", Specialty = "Терапевт" };
            var nurse = new Nurse { Id = 20, FullName = "Медсестра Анна" };
            var diagnostic = new DiagnosticPrescription { Title = "УЗИ сердца" };

            Assert.Throws<InvalidOperationException>(() => diagnostic.Execute(nurse));

            diagnostic.Execute(doctor);
            Assert.True(diagnostic.IsCompleted);
            Assert.Equal("Доктор Ватсон", diagnostic.CompletedBy);
        }


        [Fact]
        public void HospitalService_GetInitialHospitalData_ReturnsDepartmentsAndStaff()
        {
            var departments = _service.GetDepartments();
            var doctors = _service.GetDoctorsByDepartment(1);
            var staff = _service.GetAllStaff();

            Assert.NotEmpty(departments);
            Assert.NotEmpty(doctors);
            Assert.True(staff.Count >= 5);
        }

        [Fact]
        public void AdmitPatient_ValidPatient_CreatesEmrAndAddsToList()
        {
            var patient = CreateValidPatient("Иванов Иван Иванович");

            _service.AdmitPatient(patient, "Температура 37.8, жалобы на кашель");

            var saved = _service.GetPatients().FirstOrDefault(p => p.Id == patient.Id);
            Assert.NotNull(saved);
            Assert.NotNull(saved.Record);
            Assert.Equal("Температура 37.8, жалобы на кашель", saved.Record.PrimaryExamNotes);
        }

        [Fact]
        public void AdmitPatient_InvalidPatient_ThrowsArgumentException()
        {
            var badPatient = new Patient { FullName = "", Age = 0, AdmissionDiagnosis = "" };

            Assert.Throws<ArgumentException>(() => _service.AdmitPatient(badPatient, "Осмотр"));
        }

        [Fact]
        public void AddPrescription_WithoutAttendingDoctor_ThrowsException()
        {
            var patient = CreateValidPatient("Петров Петр");
            _service.AdmitPatient(patient, "Первичный осмотр");

            var pills = new PillPrescription { Title = "Парацетамол", Dosage = "1 таб", Days = 3 };

            Assert.Throws<InvalidOperationException>(() => _service.AddPrescription(patient.Id, pills));
        }

        [Fact]
        public void FullPatientLifecycle_AdmitTreatAndDischarge_GeneratesCorrectSummary()
        {
            var patient = CreateValidPatient("Сидоров Алексей");
            _service.AdmitPatient(patient, "Давление 150/90");

            _service.AssignAttendingDoctor(patient.Id, 1);

            var pills = new PillPrescription { Title = "Эналаприл", Dosage = "10 мг", Days = 7 };
            var uzi = new DiagnosticPrescription { Title = "ЭКГ" };
            _service.AddPrescription(patient.Id, pills);
            _service.AddPrescription(patient.Id, uzi);

            _service.ExecutePrescription(patient.Id, pills.Id, 4);

            string summary = _service.DischargePatient(patient.Id);

            Assert.True(patient.IsDischarged);
            Assert.Contains("Сидоров Алексей", summary);
            Assert.Contains("Эналаприл", summary);       
            Assert.Contains("Иванова М.П.", summary);    
            Assert.DoesNotContain("ЭКГ", summary);       
        }

        [Fact]
        public void AddPrescription_AfterDischarge_ThrowsException()
        {
            var patient = CreateValidPatient("Выписанный Пациент");
            _service.AdmitPatient(patient, "Осмотр");
            _service.AssignAttendingDoctor(patient.Id, 1);
            _service.DischargePatient(patient.Id);

            var diagnostic = new PreventivePrescription { Title = "Массаж" };

            Assert.Throws<InvalidOperationException>(() => _service.AddPrescription(patient.Id, diagnostic));
        }
    }
}
