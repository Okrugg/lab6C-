using System;
using System.Collections.Generic;
using System.Linq;
using BusinessObjects;

namespace DataObjects
{
    public class Db
    {
        private static Db _instance;
        public static Db GetInstance() => _instance ?? (_instance = new Db());

        public List<Department> Departments { get; }
        public List<Staff> StaffMembers { get; }
        public List<Patient> Patients { get; }

        private Db()
        {
            Departments = new List<Department>
            {
                new Department { Id = 1, Name = "Кардиологическое отделение" },
                new Department { Id = 2, Name = "Хирургическое отделение" },
                new Department { Id = 3, Name = "Неврологическое отделение" }
            };

            StaffMembers = new List<Staff>
            {
                new Doctor { Id = 1, FullName = "Смирнов А.В.", Specialty = "Кардиолог", DepartmentId = 1 },
                new Doctor { Id = 2, FullName = "Козлова Е.Н.", Specialty = "Хирург", DepartmentId = 2 },
                new Doctor { Id = 3, FullName = "Волков Д.С.", Specialty = "Невролог", DepartmentId = 3 },
                new Nurse { Id = 4, FullName = "Иванова М.П.", DepartmentId = 1 },
                new Nurse { Id = 5, FullName = "Петрова О.И.", DepartmentId = 2 }
            };

            Patients = new List<Patient>();
        }
    }

    public interface IPatientDao
    {
        List<Patient> GetAll();
        Patient GetById(Guid id);
        void Add(Patient patient);
    }

    public interface IHospitalInfoDao
    {
        List<Department> GetDepartments();
        List<Doctor> GetDoctors();
        List<Nurse> GetNurses();
        List<Staff> GetAllStaff();
    }

    public class PatientDao : IPatientDao
    {
        private readonly Db _db = Db.GetInstance();
        public List<Patient> GetAll() => _db.Patients;
        public Patient GetById(Guid id) => _db.Patients.FirstOrDefault(p => p.Id == id);
        public void Add(Patient patient) => _db.Patients.Add(patient);
    }

    public class HospitalInfoDao : IHospitalInfoDao
    {
        private readonly Db _db = Db.GetInstance();
        public List<Department> GetDepartments() => _db.Departments;
        public List<Doctor> GetDoctors() => _db.StaffMembers.OfType<Doctor>().ToList();
        public List<Nurse> GetNurses() => _db.StaffMembers.OfType<Nurse>().ToList();
        public List<Staff> GetAllStaff() => _db.StaffMembers;
    }
}
