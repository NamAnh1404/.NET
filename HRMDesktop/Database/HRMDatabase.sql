SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId INT NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        CreatedAt DATETIME2(0) NOT NULL,
        Actor NVARCHAR(100) NOT NULL,
        ActionName NVARCHAR(200) NOT NULL,
        Details NVARCHAR(1000) NULL
    );
END;

IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        EmployeeId INT NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode NVARCHAR(20) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Department NVARCHAR(100) NOT NULL,
        Position NVARCHAR(100) NOT NULL,
        DateOfBirth DATE NOT NULL,
        HireDate DATE NOT NULL,
        TerminationDate DATE NULL,
        AnnualLeaveAllowance INT NOT NULL CONSTRAINT DF_Employees_AnnualLeave DEFAULT (12),
        BaseSalary DECIMAL(18, 2) NOT NULL,
        EmploymentStatus NVARCHAR(30) NOT NULL,
        CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT UQ_Employees_Email UNIQUE (Email),
        CONSTRAINT CK_Employees_AnnualLeave CHECK (AnnualLeaveAllowance >= 0),
        CONSTRAINT CK_Employees_BaseSalary CHECK (BaseSalary >= 0),
        CONSTRAINT CK_Employees_Dates CHECK (TerminationDate IS NULL OR TerminationDate >= HireDate)
    );
END;

IF OBJECT_ID(N'dbo.UserCredentials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCredentials
    (
        Username NVARCHAR(50) NOT NULL CONSTRAINT PK_UserCredentials PRIMARY KEY,
        EmployeeId INT NULL,
        PasswordSalt NVARCHAR(200) NOT NULL,
        PasswordHash NVARCHAR(300) NOT NULL,
        UserRole NVARCHAR(20) NOT NULL,
        IsLocked BIT NOT NULL CONSTRAINT DF_UserCredentials_IsLocked DEFAULT (0),
        AttendanceNotificationEnabled BIT NOT NULL CONSTRAINT DF_UserCredentials_Attendance DEFAULT (1),
        LeaveNotificationEnabled BIT NOT NULL CONSTRAINT DF_UserCredentials_Leave DEFAULT (1),
        SalaryNotificationEnabled BIT NOT NULL CONSTRAINT DF_UserCredentials_Salary DEFAULT (1),
        CONSTRAINT FK_UserCredentials_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT CK_UserCredentials_Role CHECK (UserRole IN (N'Admin', N'Employee'))
    );
END;

IF OBJECT_ID(N'dbo.WorkShifts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkShifts
    (
        WorkShiftId INT NOT NULL CONSTRAINT PK_WorkShifts PRIMARY KEY,
        ShiftName NVARCHAR(100) NOT NULL,
        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,
        GraceMinutes INT NOT NULL,
        IsOvernight BIT NOT NULL,
        CONSTRAINT CK_WorkShifts_GraceMinutes CHECK (GraceMinutes >= 0)
    );
END;

IF OBJECT_ID(N'dbo.Holidays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Holidays
    (
        HolidayDate DATE NOT NULL CONSTRAINT PK_Holidays PRIMARY KEY,
        HolidayName NVARCHAR(150) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.AttendanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttendanceRecords
    (
        AttendanceRecordId INT NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY,
        EmployeeId INT NOT NULL,
        WorkDate DATE NOT NULL,
        CheckInAt DATETIME2(0) NULL,
        CheckOutAt DATETIME2(0) NULL,
        ScheduledStart TIME(0) NOT NULL,
        ScheduledEnd TIME(0) NOT NULL,
        GraceMinutes INT NOT NULL,
        IsOvernightShift BIT NOT NULL,
        AttendanceStatus NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_AttendanceRecords_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT UQ_AttendanceRecords_EmployeeDate UNIQUE (EmployeeId, WorkDate),
        CONSTRAINT CK_AttendanceRecords_Times CHECK (CheckOutAt IS NULL OR CheckInAt IS NULL OR CheckOutAt >= CheckInAt)
    );
END;

IF OBJECT_ID(N'dbo.AttendanceAdjustmentRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttendanceAdjustmentRequests
    (
        AttendanceAdjustmentRequestId INT NOT NULL CONSTRAINT PK_AttendanceAdjustmentRequests PRIMARY KEY,
        EmployeeId INT NOT NULL,
        WorkDate DATE NOT NULL,
        RequestedCheckIn NVARCHAR(5) NOT NULL,
        RequestedCheckOut NVARCHAR(5) NULL,
        Reason NVARCHAR(1000) NOT NULL,
        SubmittedAt DATETIME2(0) NOT NULL,
        ReviewedAt DATETIME2(0) NULL,
        ReviewedBy NVARCHAR(100) NULL,
        RequestStatus NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_AttendanceAdjustmentRequests_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId)
    );
END;

IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests
    (
        LeaveRequestId INT NOT NULL CONSTRAINT PK_LeaveRequests PRIMARY KEY,
        EmployeeId INT NOT NULL,
        LeaveType NVARCHAR(50) NOT NULL,
        FromDate DATE NOT NULL,
        ToDate DATE NOT NULL,
        SubmittedAt DATETIME2(0) NOT NULL,
        ReviewedAt DATETIME2(0) NULL,
        CancelledAt DATETIME2(0) NULL,
        ReviewedBy NVARCHAR(100) NULL,
        Reason NVARCHAR(1000) NOT NULL,
        RequestStatus NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT CK_LeaveRequests_Dates CHECK (ToDate >= FromDate)
    );
END;

IF OBJECT_ID(N'dbo.SalaryRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalaryRecords
    (
        SalaryRecordId INT NOT NULL CONSTRAINT PK_SalaryRecords PRIMARY KEY,
        EmployeeId INT NOT NULL,
        PeriodStart DATE NOT NULL,
        BaseSalary DECIMAL(18, 2) NOT NULL,
        Bonus DECIMAL(18, 2) NOT NULL,
        Deduction DECIMAL(18, 2) NOT NULL,
        PaidAt DATETIME2(0) NULL,
        PaidBy NVARCHAR(100) NULL,
        PaymentMethod NVARCHAR(100) NULL,
        TransactionReference NVARCHAR(100) NULL,
        PaymentStatus NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_SalaryRecords_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT UQ_SalaryRecords_EmployeePeriod UNIQUE (EmployeeId, PeriodStart),
        CONSTRAINT CK_SalaryRecords_Amounts CHECK (BaseSalary >= 0 AND Bonus >= 0 AND Deduction >= 0)
    );
END;

IF OBJECT_ID(N'dbo.SalaryHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalaryHistories
    (
        SalaryHistoryId INT NOT NULL CONSTRAINT PK_SalaryHistories PRIMARY KEY,
        EmployeeId INT NOT NULL,
        EffectiveFrom DATE NOT NULL,
        BaseSalary DECIMAL(18, 2) NOT NULL,
        CONSTRAINT FK_SalaryHistories_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT UQ_SalaryHistories_EmployeeEffective UNIQUE (EmployeeId, EffectiveFrom),
        CONSTRAINT CK_SalaryHistories_BaseSalary CHECK (BaseSalary >= 0)
    );
END;

IF OBJECT_ID(N'dbo.EmploymentPeriods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmploymentPeriods
    (
        EmploymentPeriodId INT NOT NULL CONSTRAINT PK_EmploymentPeriods PRIMARY KEY,
        EmployeeId INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        CONSTRAINT FK_EmploymentPeriods_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT CK_EmploymentPeriods_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate)
    );
END;

