using System;
using System.Collections.Generic;
using System.Linq;
using BusinessObjects;
using DataObjects;

namespace ServiceLayer
{
    public interface IHospitalService
    {
        List<Department> GetDepartments();
        List<Doctor> GetDoctorsByDepartment(int departmentId);
        List<Staff> GetAllStaff();
        List<Patient> GetPatients();
        void AdmitPatient(Patient patient, string primaryExamNotes);
        void AssignAttendingDoctor(Guid patientId, int doctorId);
        void AddPrescription(Guid patientId, Prescription prescription);
        void ExecutePrescription(Guid patientId, Guid prescriptionId, int staffId);
        string DischargePatient(Guid patientId);
    }

    public class HospitalService : IHospitalService
    {
        private readonly IPatientDao _patientDao = new PatientDao();
        private readonly IHospitalInfoDao _infoDao = new HospitalInfoDao();

        public List<Department> GetDepartments() => _infoDao.GetDepartments();
        public List<Doctor> GetDoctorsByDepartment(int departmentId) =>
            _infoDao.GetDoctors().Where(d => d.DepartmentId == departmentId).ToList();
        public List<Staff> GetAllStaff() => _infoDao.GetAllStaff();
        public List<Patient> GetPatients() => _patientDao.GetAll();

        public void AdmitPatient(Patient patient, string primaryExamNotes)
        {
            if (!patient.IsValid())
                throw new ArgumentException(string.Join(" ", patient.Errors));

            patient.Record = new MedicalRecord { PrimaryExamNotes = primaryExamNotes };
            _patientDao.Add(patient);
        }

        public void AssignAttendingDoctor(Guid patientId, int doctorId)
        {
            var patient = _patientDao.GetById(patientId) ?? throw new Exception("Пациент не найден.");
            patient.AttendingDoctorId = doctorId;
        }

        public void AddPrescription(Guid patientId, Prescription prescription)
        {
            if (!prescription.IsValid())
                throw new ArgumentException(string.Join(" ", prescription.Errors));

            var patient = _patientDao.GetById(patientId) ?? throw new Exception("Пациент не найден.");
            if (patient.IsDischarged) throw new InvalidOperationException("Пациент уже выписан.");
            if (patient.AttendingDoctorId == null) throw new InvalidOperationException("Сначала закрепите лечащего врача!");

            patient.Record.Prescriptions.Add(prescription);
        }

        public void ExecutePrescription(Guid patientId, Guid prescriptionId, int staffId)
        {
            var patient = _patientDao.GetById(patientId) ?? throw new Exception("Пациент не найден.");
            var prescription = patient.Record.Prescriptions.FirstOrDefault(p => p.Id == prescriptionId)
                               ?? throw new Exception("Назначение не найдено.");
            var staff = _infoDao.GetAllStaff().FirstOrDefault(s => s.Id == staffId)
                        ?? throw new Exception("Сотрудник не найден.");

            prescription.Execute(staff);
        }

        public string DischargePatient(Guid patientId)
        {
            var patient = _patientDao.GetById(patientId) ?? throw new Exception("Пациент не найден.");
            patient.DischargeDate = DateTime.Now;
            return patient.GenerateDischargeSummary();
        }
    }
}
