using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BusinessObjects.BusinessRules;

namespace BusinessObjects.BusinessRules
{
    public abstract class BusinessRule
    {
        public string Property { get; set; }
        public string Error { get; set; }

        protected BusinessRule(string property, string error)
        {
            Property = property;
            Error = error;
        }

        public abstract bool Validate(BusinessObject businessObject);

        protected object GetPropertyValue(BusinessObject businessObject)
        {
            PropertyInfo prop = businessObject.GetType().GetProperty(Property);
            return prop?.GetValue(businessObject, null);
        }
    }

    public class RequiredRule : BusinessRule
    {
        public RequiredRule(string property, string error) : base(property, error) { }

        public override bool Validate(BusinessObject businessObject)
        {
            var val = GetPropertyValue(businessObject)?.ToString();
            return !string.IsNullOrWhiteSpace(val);
        }
    }

    public class PositiveNumberRule : BusinessRule
    {
        public PositiveNumberRule(string property, string error) : base(property, error) { }

        public override bool Validate(BusinessObject businessObject)
        {
            var val = GetPropertyValue(businessObject);
            if (val == null) return false;
            return Convert.ToDouble(val) > 0;
        }
    }
}

namespace BusinessObjects
{
    public abstract class BusinessObject
    {
        private readonly List<BusinessRule> _rules = new List<BusinessRule>();
        private readonly List<string> _errors = new List<string>();

        public List<string> Errors => _errors;

        protected void AddRule(BusinessRule rule) => _rules.Add(rule);

        public bool IsValid()
        {
            _errors.Clear();
            bool isValid = true;
            foreach (var rule in _rules)
            {
                if (!rule.Validate(this))
                {
                    isValid = false;
                    _errors.Add(rule.Error);
                }
            }
            return isValid;
        }
    }

    public class Department : BusinessObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public abstract class Staff : BusinessObject
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int DepartmentId { get; set; }
        public abstract string Role { get; }
        public override string ToString() => $"{FullName} ({Role})";
    }

    public class Doctor : Staff
    {
        public string Specialty { get; set; }
        public override string Role => $"Врач - {Specialty}";
    }

    public class Nurse : Staff
    {
        public override string Role => "Медсестра";
    }

    public abstract class Prescription : BusinessObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public string CompletedBy { get; set; }
        public DateTime? CompletedDate { get; set; }

        protected Prescription()
        {
            AddRule(new RequiredRule(nameof(Title), "Укажите название назначения."));
        }

        public abstract bool RequiresNurse { get; }
        public abstract string GetDetails();

        public void Execute(Staff performer)
        {
            if (RequiresNurse && !(performer is Nurse))
                throw new InvalidOperationException("Медикаментозные назначения выполняет только медсестра!");
            if (!RequiresNurse && !(performer is Doctor))
                throw new InvalidOperationException("Диагностику и процедуры выполняет только врач!");

            IsCompleted = true;
            CompletedBy = performer.FullName;
            CompletedDate = DateTime.Now;
        }
    }

    public class InjectionPrescription : Prescription
    {
        public int Count { get; set; }
        public override bool RequiresNurse => true;

        public InjectionPrescription()
        {
            AddRule(new PositiveNumberRule(nameof(Count), "Количество инъекций должно быть больше 0."));
        }

        public override string GetDetails() => $"Инъекции: {Title}, кол-во: {Count}";
    }

    public class PillPrescription : Prescription
    {
        public string Dosage { get; set; }
        public int Days { get; set; }
        public override bool RequiresNurse => true;

        public PillPrescription()
        {
            AddRule(new RequiredRule(nameof(Dosage), "Укажите дозировку таблеток."));
            AddRule(new PositiveNumberRule(nameof(Days), "Количество дней приема должно быть больше 0."));
        }

        public override string GetDetails() => $"Таблетки: {Title}, дозировка: {Dosage}, дней: {Days}";
    }

    public class DiagnosticPrescription : Prescription
    {
        public override bool RequiresNurse => false;
        public override string GetDetails() => $"Диагностика: {Title}";
    }

    public class PreventivePrescription : Prescription
    {
        public override bool RequiresNurse => false;
        public override string GetDetails() => $"Профилактика: {Title}";
    }

    public class MedicalRecord : BusinessObject
    {
        public string PrimaryExamNotes { get; set; }
        public List<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    public class Patient : BusinessObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; }
        public int Age { get; set; }
        public string AdmissionDiagnosis { get; set; }
        public DateTime AdmissionDate { get; set; } = DateTime.Now;
        public DateTime? DischargeDate { get; set; }
        public bool IsDischarged => DischargeDate.HasValue;

        public int DepartmentId { get; set; }
        public int? AttendingDoctorId { get; set; }
        public MedicalRecord Record { get; set; } = new MedicalRecord();

        public Patient()
        {
            AddRule(new RequiredRule(nameof(FullName), "ФИО пациента обязательно."));
            AddRule(new PositiveNumberRule(nameof(Age), "Возраст должен быть больше 0."));
            AddRule(new RequiredRule(nameof(AdmissionDiagnosis), "Укажите диагноз при поступлении."));
        }

        public string GenerateDischargeSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ВЫПИСНОЙ ЭПИКРИЗ ===");
            sb.AppendLine($"Пациент: {FullName}, Возраст: {Age}");
            sb.AppendLine($"Период пребывания: {AdmissionDate:dd.MM.yyyy} — {(DischargeDate ?? DateTime.Now):dd.MM.yyyy}");
            sb.AppendLine($"Диагноз при поступлении: {AdmissionDiagnosis}");
            sb.AppendLine($"Первичный осмотр: {Record.PrimaryExamNotes}");
            sb.AppendLine("\nВыполненные назначения:");

            var completed = Record.Prescriptions.Where(p => p.IsCompleted).ToList();
            if (!completed.Any())
                sb.AppendLine("- Выполненных назначений нет.");
            else
                foreach (var p in completed)
                    sb.AppendLine($"- {p.GetDetails()} (Выполнил: {p.CompletedBy}, {p.CompletedDate:dd.MM HH:mm})");

            return sb.ToString();
        }

        public override string ToString() => $"{FullName} ({(IsDischarged ? "Выписан" : "На лечении")})";
    }
}
