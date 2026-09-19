using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    [Serializable]
    public class HrmDataSnapshot
    {
        public List<Employee> Employees { get; set; }
        public List<AttendanceRecord> Attendance { get; set; }
        public List<LeaveRequest> LeaveRequests { get; set; }
        public List<SalaryRecord> Salaries { get; set; }
        public List<AttendanceAdjustmentRequest> AttendanceAdjustments { get; set; }
        public List<SalaryHistory> SalaryHistories { get; set; }
        public List<EmploymentPeriod> EmploymentPeriods { get; set; }
        public List<WorkShift> WorkShifts { get; set; }
        public List<Holiday> Holidays { get; set; }
        public List<UserCredential> Credentials { get; set; }
        public List<AuditLog> AuditLogs { get; set; }
    }

    public static class DataPersistenceService
    {
        private static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HRMDesktop");
        private static readonly string DataFile = Path.Combine(DataDirectory, "hrm-data.xml");
        public static string LastError { get; private set; }

        public static HrmDataSnapshot Load()
        {
            try
            {
                if (!File.Exists(DataFile)) return null;
                var serializer = new XmlSerializer(typeof(HrmDataSnapshot));
                using (var stream = File.OpenRead(DataFile)) return serializer.Deserialize(stream) as HrmDataSnapshot;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        public static bool Save(HrmDataSnapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                string temporaryFile = DataFile + ".tmp";
                var serializer = new XmlSerializer(typeof(HrmDataSnapshot));
                using (var stream = File.Create(temporaryFile)) serializer.Serialize(stream, snapshot);
                if (File.Exists(DataFile)) File.Replace(temporaryFile, DataFile, DataFile + ".bak", true);
                else File.Move(temporaryFile, DataFile);
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
    }
}
