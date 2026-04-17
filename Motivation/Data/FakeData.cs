namespace Motivation.Data
{
    public static class FakeData
    {
        public static int DepartmentsCount { get; set; } = 3;
        public static int PositionsCount { get; set; } = 5;
        public static int QualificationsCount { get; set; } = 3;
        public static int RanksCount { get; set; } = 11;
        public static int PenaltiesCount { get; set; } = 5;
        public static int EmployeePenaltiesCount { get; set; } = 15;
        public static int EmployeesCount { get; set; } = 10;
        public static int EmployeeTasksCount { get; set; } = 20;
        public static int PointsOfInterestCount { get; set; } = 30;
        public static int MinDepartmentBudget { get; set; } = 50_000;
        public static int MaxDepartmentBudget { get; set; } = 1000_000;
        public static int MinDepartmentExpenses { get; set; } = 50_000;
        public static int MaxDepartmentExpenses { get; set; } = 1000_000;
        public static int ManagerId { get; set; } = 1;
    }
}
