using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HRMDesktop.Data;
using HRMDesktop.Models;
using DbAdjustment = HRMDesktop.Data.AttendanceAdjustmentRequest;
using DbAttendance = HRMDesktop.Data.AttendanceRecord;
using DbAuditLog = HRMDesktop.Data.AuditLog;
using DbEmployee = HRMDesktop.Data.Employee;
using DbEmploymentPeriod = HRMDesktop.Data.EmploymentPeriod;
using DbHoliday = HRMDesktop.Data.Holiday;
using DbLeaveRequest = HRMDesktop.Data.LeaveRequest;
using DbSalaryHistory = HRMDesktop.Data.SalaryHistory;
using DbSalaryRecord = HRMDesktop.Data.SalaryRecord;
using DbUserCredential = HRMDesktop.Data.UserCredential;
using DbWorkShift = HRMDesktop.Data.WorkShift;

namespace HRMDesktop.Services
{
    public static class DatabaseDataService
    {
        private const string DatabaseName = "HRMDatabase";
        private const string ServerName = @"(LocalDB)\MSSQLLocalDB";
        private const string SchemaResourceName = "HRMDesktop.Database.HRMDatabase.sql";

        public static string LastError { get; private set; }

        public static HrmDataSnapshot Load()
        {
            try
            {
                EnsureDatabase();
                using (var db = new HRMDatabaseEntities())
                {
                    db.Configuration.LazyLoadingEnabled = false;
                    var employeeRows = db.Employees.AsNoTracking().ToList();
                    if (employeeRows.Count == 0) return null;

                    var employees = employeeRows.Select(ToModel).ToList();
                    var employeeMap = employees.ToDictionary(x => x.Id);

                    return new HrmDataSnapshot
                    {
                        Employees = employees,
                        Attendance = db.AttendanceRecords.AsNoTracking().ToList().Select(x => ToModel(x, employeeMap)).ToList(),
                        LeaveRequests = db.LeaveRequests.AsNoTracking().ToList().Select(x => ToModel(x, employeeMap)).ToList(),
                        Salaries = db.SalaryRecords.AsNoTracking().ToList().Select(x => ToModel(x, employeeMap)).ToList(),
                        AttendanceAdjustments = db.AttendanceAdjustmentRequests.AsNoTracking().ToList().Select(x => ToModel(x, employeeMap)).ToList(),
                        SalaryHistories = db.SalaryHistories.AsNoTracking().ToList().Select(x => new Models.SalaryHistory
                        {
                            Id = x.SalaryHistoryId,
                            EmployeeId = x.EmployeeId,
                            EffectiveFrom = x.EffectiveFrom,
                            BaseSalary = x.BaseSalary
                        }).ToList(),
                        EmploymentPeriods = db.EmploymentPeriods.AsNoTracking().ToList().Select(x => new Models.EmploymentPeriod
                        {
                            Id = x.EmploymentPeriodId,
                            EmployeeId = x.EmployeeId,
                            StartDate = x.StartDate,
                            EndDate = x.EndDate
                        }).ToList(),
                        WorkShifts = db.WorkShifts.AsNoTracking().ToList().Select(x => new Models.WorkShift
                        {
                            Id = x.WorkShiftId,
                            Name = x.ShiftName,
                            StartTime = x.StartTime,
                            EndTime = x.EndTime,
                            GraceMinutes = x.GraceMinutes,
                            IsOvernight = x.IsOvernight
                        }).ToList(),
                        Holidays = db.Holidays.AsNoTracking().ToList().Select(x => new Models.Holiday
                        {
                            Date = x.HolidayDate,
                            Name = x.HolidayName
                        }).ToList(),
                        Credentials = db.UserCredentials.AsNoTracking().ToList().Select(x => new Models.UserCredential
                        {
                            EmployeeId = x.EmployeeId ?? 0,
                            Username = x.Username,
                            PasswordSalt = x.PasswordSalt,
                            PasswordHash = x.PasswordHash,
                            Role = x.UserRole,
                            IsLocked = x.IsLocked,
                            AttendanceNotificationEnabled = x.AttendanceNotificationEnabled,
                            LeaveNotificationEnabled = x.LeaveNotificationEnabled,
                            SalaryNotificationEnabled = x.SalaryNotificationEnabled
                        }).ToList(),
                        AuditLogs = db.AuditLogs.AsNoTracking().ToList().Select(x => new Models.AuditLog
                        {
                            Id = x.AuditLogId,
                            CreatedAt = x.CreatedAt,
                            Actor = x.Actor,
                            Action = x.ActionName,
                            Details = x.Details
                        }).ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                LastError = GetUsefulMessage(ex);
                return null;
            }
        }

        public static bool Save(HrmDataSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");

            try
            {
                EnsureDatabase();
                using (var db = new HRMDatabaseEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    db.Configuration.AutoDetectChangesEnabled = false;
                    SaveEmployees(db, snapshot.Employees ?? new List<Models.Employee>());
                    db.ChangeTracker.DetectChanges();
                    db.SaveChanges();

                    SaveCredentials(db, snapshot.Credentials ?? new List<Models.UserCredential>());
                    SaveAttendance(db, snapshot.Attendance ?? new List<Models.AttendanceRecord>());
                    SaveAdjustments(db, snapshot.AttendanceAdjustments ?? new List<Models.AttendanceAdjustmentRequest>());
                    SaveLeaveRequests(db, snapshot.LeaveRequests ?? new List<Models.LeaveRequest>());
                    SaveSalaryRecords(db, snapshot.Salaries ?? new List<Models.SalaryRecord>());
                    SaveSalaryHistories(db, snapshot.SalaryHistories ?? new List<Models.SalaryHistory>());
                    SaveEmploymentPeriods(db, snapshot.EmploymentPeriods ?? new List<Models.EmploymentPeriod>());
                    SaveWorkShifts(db, snapshot.WorkShifts ?? new List<Models.WorkShift>());
                    SaveHolidays(db, snapshot.Holidays ?? new List<Models.Holiday>());
                    SaveAuditLogs(db, snapshot.AuditLogs ?? new List<Models.AuditLog>());

                    db.ChangeTracker.DetectChanges();
                    db.SaveChanges();
                    transaction.Commit();
                }

                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = GetUsefulMessage(ex);
                return false;
            }
        }

        private static void EnsureDatabase()
        {
            var masterBuilder = new SqlConnectionStringBuilder
            {
                DataSource = ServerName,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ConnectTimeout = 30
            };
            using (var master = new SqlConnection(masterBuilder.ConnectionString))
            {
                master.Open();
                using (var command = master.CreateCommand())
                {
                    command.CommandText = "IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE [" + DatabaseName + "]')";
                    command.Parameters.AddWithValue("@name", DatabaseName);
                    command.ExecuteNonQuery();
                }
            }

            string script;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResourceName))
            {
                if (stream == null) throw new InvalidOperationException("Không tìm thấy script khởi tạo HRMDatabase.");
                using (var reader = new StreamReader(stream)) script = reader.ReadToEnd();
            }

            var databaseBuilder = new SqlConnectionStringBuilder(masterBuilder.ConnectionString) { InitialCatalog = DatabaseName };
            using (var connection = new SqlConnection(databaseBuilder.ConnectionString))
            {
                connection.Open();
                foreach (string batch in Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = batch;
                        command.CommandTimeout = 60;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void SaveEmployees(HRMDatabaseEntities db, IEnumerable<Models.Employee> source)
        {
            var existing = db.Employees.ToDictionary(x => x.EmployeeId);
            foreach (var item in source)
            {
                DbEmployee row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbEmployee { EmployeeId = item.Id };
                    db.Employees.Add(row);
                }
                row.EmployeeCode = item.Code;
                row.FullName = item.FullName;
                row.Email = item.Email;
                row.Phone = item.Phone;
                row.Department = item.Department;
                row.Position = item.Position;
                row.DateOfBirth = item.DateOfBirth.Date;
                row.HireDate = item.HireDate.Date;
                row.TerminationDate = item.TerminationDate.HasValue ? item.TerminationDate.Value.Date : (DateTime?)null;
                row.AnnualLeaveAllowance = item.AnnualLeaveAllowance;
                row.BaseSalary = item.BaseSalary;
                row.EmploymentStatus = item.Status;
            }
        }

        private static void SaveCredentials(HRMDatabaseEntities db, IEnumerable<Models.UserCredential> source)
        {
            var existing = db.UserCredentials.ToDictionary(x => x.Username, StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                DbUserCredential row;
                if (!existing.TryGetValue(item.Username, out row))
                {
                    row = new DbUserCredential { Username = item.Username };
                    db.UserCredentials.Add(row);
                }
                row.EmployeeId = item.EmployeeId <= 0 ? (int?)null : item.EmployeeId;
                row.PasswordSalt = item.PasswordSalt;
                row.PasswordHash = item.PasswordHash;
                row.UserRole = item.Role;
                row.IsLocked = item.IsLocked;
                row.AttendanceNotificationEnabled = item.AttendanceNotificationEnabled;
                row.LeaveNotificationEnabled = item.LeaveNotificationEnabled;
                row.SalaryNotificationEnabled = item.SalaryNotificationEnabled;
            }
        }

        private static void SaveAttendance(HRMDatabaseEntities db, IEnumerable<Models.AttendanceRecord> source)
        {
            var existing = db.AttendanceRecords.ToDictionary(x => x.AttendanceRecordId);
            foreach (var item in source)
            {
                DbAttendance row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbAttendance { AttendanceRecordId = item.Id };
                    db.AttendanceRecords.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.WorkDate = item.WorkDate.Date;
                row.CheckInAt = item.CheckInAt;
                row.CheckOutAt = item.CheckOutAt;
                row.ScheduledStart = item.ScheduledStart;
                row.ScheduledEnd = item.ScheduledEnd;
                row.GraceMinutes = item.GraceMinutes;
                row.IsOvernightShift = item.IsOvernightShift;
                row.AttendanceStatus = item.Status;
            }
        }

        private static void SaveAdjustments(HRMDatabaseEntities db, IEnumerable<Models.AttendanceAdjustmentRequest> source)
        {
            var existing = db.AttendanceAdjustmentRequests.ToDictionary(x => x.AttendanceAdjustmentRequestId);
            foreach (var item in source)
            {
                DbAdjustment row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbAdjustment { AttendanceAdjustmentRequestId = item.Id };
                    db.AttendanceAdjustmentRequests.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.WorkDate = item.WorkDate.Date;
                row.RequestedCheckIn = item.RequestedCheckIn;
                row.RequestedCheckOut = item.RequestedCheckOut;
                row.Reason = item.Reason;
                row.SubmittedAt = item.SubmittedAt;
                row.ReviewedAt = item.ReviewedAt;
                row.ReviewedBy = item.ReviewedBy;
                row.RequestStatus = item.Status;
            }
        }

        private static void SaveLeaveRequests(HRMDatabaseEntities db, IEnumerable<Models.LeaveRequest> source)
        {
            var existing = db.LeaveRequests.ToDictionary(x => x.LeaveRequestId);
            foreach (var item in source)
            {
                DbLeaveRequest row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbLeaveRequest { LeaveRequestId = item.Id };
                    db.LeaveRequests.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.LeaveType = item.LeaveType;
                row.FromDate = item.FromDate.Date;
                row.ToDate = item.ToDate.Date;
                row.SubmittedAt = item.SubmittedAt;
                row.ReviewedAt = item.ReviewedAt;
                row.CancelledAt = item.CancelledAt;
                row.ReviewedBy = item.ReviewedBy;
                row.Reason = item.Reason;
                row.RequestStatus = item.Status;
            }
        }

        private static void SaveSalaryRecords(HRMDatabaseEntities db, IEnumerable<Models.SalaryRecord> source)
        {
            var existing = db.SalaryRecords.ToDictionary(x => x.SalaryRecordId);
            foreach (var item in source)
            {
                DbSalaryRecord row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbSalaryRecord { SalaryRecordId = item.Id };
                    db.SalaryRecords.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.PeriodStart = item.PeriodStart.Date;
                row.BaseSalary = item.BaseSalary;
                row.Bonus = item.Bonus;
                row.Deduction = item.Deduction;
                row.PaidAt = item.PaidAt;
                row.PaidBy = item.PaidBy;
                row.PaymentMethod = item.PaymentMethod;
                row.TransactionReference = item.TransactionReference;
                row.PaymentStatus = item.Status;
            }
        }

        private static void SaveSalaryHistories(HRMDatabaseEntities db, IEnumerable<Models.SalaryHistory> source)
        {
            var existing = db.SalaryHistories.ToDictionary(x => x.SalaryHistoryId);
            foreach (var item in source)
            {
                DbSalaryHistory row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbSalaryHistory { SalaryHistoryId = item.Id };
                    db.SalaryHistories.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.EffectiveFrom = item.EffectiveFrom.Date;
                row.BaseSalary = item.BaseSalary;
            }
        }

        private static void SaveEmploymentPeriods(HRMDatabaseEntities db, IEnumerable<Models.EmploymentPeriod> source)
        {
            var existing = db.EmploymentPeriods.ToDictionary(x => x.EmploymentPeriodId);
            foreach (var item in source)
            {
                DbEmploymentPeriod row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbEmploymentPeriod { EmploymentPeriodId = item.Id };
                    db.EmploymentPeriods.Add(row);
                }
                row.EmployeeId = item.EmployeeId;
                row.StartDate = item.StartDate.Date;
                row.EndDate = item.EndDate.HasValue ? item.EndDate.Value.Date : (DateTime?)null;
            }
        }

        private static void SaveWorkShifts(HRMDatabaseEntities db, IEnumerable<Models.WorkShift> source)
        {
            var existing = db.WorkShifts.ToDictionary(x => x.WorkShiftId);
            foreach (var item in source)
            {
                DbWorkShift row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbWorkShift { WorkShiftId = item.Id };
                    db.WorkShifts.Add(row);
                }
                row.ShiftName = item.Name;
                row.StartTime = item.StartTime;
                row.EndTime = item.EndTime;
                row.GraceMinutes = item.GraceMinutes;
                row.IsOvernight = item.IsOvernight;
            }
        }

        private static void SaveHolidays(HRMDatabaseEntities db, IEnumerable<Models.Holiday> source)
        {
            var existing = db.Holidays.ToDictionary(x => x.HolidayDate.Date);
            foreach (var item in source)
            {
                DbHoliday row;
                if (!existing.TryGetValue(item.Date.Date, out row))
                {
                    row = new DbHoliday { HolidayDate = item.Date.Date };
                    db.Holidays.Add(row);
                }
                row.HolidayName = item.Name;
            }
        }

        private static void SaveAuditLogs(HRMDatabaseEntities db, IEnumerable<Models.AuditLog> source)
        {
            var existing = db.AuditLogs.ToDictionary(x => x.AuditLogId);
            foreach (var item in source)
            {
                DbAuditLog row;
                if (!existing.TryGetValue(item.Id, out row))
                {
                    row = new DbAuditLog { AuditLogId = item.Id };
                    db.AuditLogs.Add(row);
                }
                row.CreatedAt = item.CreatedAt;
                row.Actor = item.Actor;
                row.ActionName = item.Action;
                row.Details = item.Details;
            }
        }

        private static Models.Employee ToModel(DbEmployee row)
        {
            return new Models.Employee
            {
                Id = row.EmployeeId,
                Code = row.EmployeeCode,
                FullName = row.FullName,
                Email = row.Email,
                Phone = row.Phone,
                Department = row.Department,
                Position = row.Position,
                DateOfBirth = row.DateOfBirth,
                HireDate = row.HireDate,
                TerminationDate = row.TerminationDate,
                AnnualLeaveAllowance = row.AnnualLeaveAllowance,
                BaseSalary = row.BaseSalary,
                Status = row.EmploymentStatus
            };
        }

        private static Models.AttendanceRecord ToModel(DbAttendance row, IDictionary<int, Models.Employee> employees)
        {
            Models.Employee employee;
            employees.TryGetValue(row.EmployeeId, out employee);
            return new Models.AttendanceRecord
            {
                Id = row.AttendanceRecordId,
                EmployeeId = row.EmployeeId,
                EmployeeName = employee == null ? string.Empty : employee.FullName,
                EmployeeCode = employee == null ? string.Empty : employee.Code,
                Department = employee == null ? string.Empty : employee.Department,
                WorkDate = row.WorkDate,
                CheckInAt = row.CheckInAt,
                CheckOutAt = row.CheckOutAt,
                ScheduledStart = row.ScheduledStart,
                ScheduledEnd = row.ScheduledEnd,
                GraceMinutes = row.GraceMinutes,
                IsOvernightShift = row.IsOvernightShift,
                Status = row.AttendanceStatus
            };
        }

        private static Models.AttendanceAdjustmentRequest ToModel(DbAdjustment row, IDictionary<int, Models.Employee> employees)
        {
            Models.Employee employee;
            employees.TryGetValue(row.EmployeeId, out employee);
            return new Models.AttendanceAdjustmentRequest
            {
                Id = row.AttendanceAdjustmentRequestId,
                EmployeeId = row.EmployeeId,
                EmployeeName = employee == null ? string.Empty : employee.FullName,
                EmployeeCode = employee == null ? string.Empty : employee.Code,
                Department = employee == null ? string.Empty : employee.Department,
                WorkDate = row.WorkDate,
                RequestedCheckIn = row.RequestedCheckIn,
                RequestedCheckOut = row.RequestedCheckOut,
                Reason = row.Reason,
                SubmittedAt = row.SubmittedAt,
                ReviewedAt = row.ReviewedAt,
                ReviewedBy = row.ReviewedBy,
                Status = row.RequestStatus
            };
        }

        private static Models.LeaveRequest ToModel(DbLeaveRequest row, IDictionary<int, Models.Employee> employees)
        {
            Models.Employee employee;
            employees.TryGetValue(row.EmployeeId, out employee);
            return new Models.LeaveRequest
            {
                Id = row.LeaveRequestId,
                EmployeeId = row.EmployeeId,
                EmployeeName = employee == null ? string.Empty : employee.FullName,
                EmployeeCode = employee == null ? string.Empty : employee.Code,
                LeaveType = row.LeaveType,
                FromDate = row.FromDate,
                ToDate = row.ToDate,
                SubmittedAt = row.SubmittedAt,
                ReviewedAt = row.ReviewedAt,
                CancelledAt = row.CancelledAt,
                ReviewedBy = row.ReviewedBy,
                Reason = row.Reason,
                Status = row.RequestStatus
            };
        }

        private static Models.SalaryRecord ToModel(DbSalaryRecord row, IDictionary<int, Models.Employee> employees)
        {
            Models.Employee employee;
            employees.TryGetValue(row.EmployeeId, out employee);
            return new Models.SalaryRecord
            {
                Id = row.SalaryRecordId,
                EmployeeId = row.EmployeeId,
                EmployeeName = employee == null ? string.Empty : employee.FullName,
                EmployeeCode = employee == null ? string.Empty : employee.Code,
                PeriodStart = row.PeriodStart,
                BaseSalary = row.BaseSalary,
                Bonus = row.Bonus,
                Deduction = row.Deduction,
                PaidAt = row.PaidAt,
                PaidBy = row.PaidBy,
                PaymentMethod = row.PaymentMethod,
                TransactionReference = row.TransactionReference,
                Status = row.PaymentStatus
            };
        }

        private static string GetUsefulMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null) current = current.InnerException;
            return current.Message;
        }
    }
}
