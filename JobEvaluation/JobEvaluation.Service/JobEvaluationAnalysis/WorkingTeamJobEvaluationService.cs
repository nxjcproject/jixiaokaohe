using EnergyConsumption;
using JobEvaluation.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace JobEvaluation.Service.JobEvaluationAnalysis
{
    public static class WorkingTeamJobEvaluationService
    {
        /// <summary>
        /// 获取排班记录
        /// </summary>
        /// <param name="organizationId">组织机构ID</param>
        /// <param name="date">时间，yyyy-MM</param>
        /// <returns></returns>
        public static DataTable GetShiftsSchedulingLogMonthly(string organizationId, string date)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            string sql = @" SELECT [TimeStamp],[FirstWorkingTeam],[SecondWorkingTeam],[ThirdWorkingTeam]
                              FROM [NXJC].[dbo].[tz_Balance]
                              WHERE SUBSTRING([TimeStamp],0,8) = @date AND
		                            LEN([TimeStamp]) = 10 AND
		                            [OrganizationID] = @organizationId
                              ORDER BY [TimeStamp]";

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("organizationId", organizationId),
                new SqlParameter("date", date)
            };

            return dataFactory.Query(sql, parameters); ;
        }


        private static DataTable GetProcessByOrganizationId(string organizationId, string consumptionType)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            string sql = @"SELECT	system_Organization.OrganizationID,
		                            'O' + SUBSTRING((CASE WHEN F.FormulaLevelCode IS NULL THEN system_Organization.LevelCode
		                             WHEN LEN(F.FormulaLevelCode) = 3 THEN system_Organization.LevelCode
		                             WHEN LEN(F.FormulaLevelCode) > 3 THEN system_Organization.LevelCode + SUBSTRING(F.FormulaLevelCode, 4, LEN(F.FormulaLevelCode) - 1) END), 4, LEN(
		                             (CASE WHEN F.FormulaLevelCode IS NULL THEN system_Organization.LevelCode
		                             WHEN LEN(F.FormulaLevelCode) = 3 THEN system_Organization.LevelCode
		                             WHEN LEN(F.FormulaLevelCode) > 3 THEN system_Organization.LevelCode + SUBSTRING(F.FormulaLevelCode, 4, LEN(F.FormulaLevelCode) - 1) END)
		                             )) AS LevelCode,
		                            (CASE WHEN F.ProcessName IS NULL THEN system_Organization.Name ELSE F.ProcessName END) AS Name,
		                            F.* 
                            FROM	system_Organization LEFT JOIN(
		                        SELECT	tz_Formula.Name + [balance_Energy_Template].VariableName AS [ProcessName], formula_FormulaDetail.LevelCode AS [FormulaLevelCode], formula_FormulaDetail.VariableId, [balance_Energy_Template].VariableName, [balance_Energy_Template].ValueFormula, tz_Formula.OrganizationID
		                            FROM	tz_Formula INNER JOIN
				                        formula_FormulaDetail ON tz_Formula.KeyID = formula_FormulaDetail.KeyID AND tz_Formula.[Type] <> 1 INNER JOIN
				                        [balance_Energy_Template] ON ([balance_Energy_Template].VariableId = formula_FormulaDetail.VariableId + '_' + @consumptionType)
		                            WHERE  (tz_Formula.OrganizationID LIKE @organizationId + '%')) AS F ON F.OrganizationID = system_Organization.OrganizationID
                           WHERE system_Organization.OrganizationID LIKE @organizationId + '%'
                        ORDER BY system_Organization.LevelCode, F.OrganizationID , F.[FormulaLevelCode]";

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("organizationId", organizationId),
                new SqlParameter("consumptionType", consumptionType)
            };

            return dataFactory.Query(sql, parameters);
        }

        private static DataTable GetProcessValueTableByOrganizationId(string organizationId, DateTime start, DateTime end)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            string sql = @" SELECT
		                            LEFT([A].[TimeStamp],7) AS [TimeStamp],
                                    [B].[OrganizationID],
		                            [B].[VariableId],
		                            SUM(CASE WHEN [A].[FirstWorkingTeam] = 'A班' THEN [B].[FirstB] WHEN [A].[SecondWorkingTeam] = 'A班' THEN [B].[SecondB] WHEN [A].[ThirdWorkingTeam] = 'A班' THEN [B].[ThirdB] ELSE 0 END) AS A班,
		                            SUM(CASE WHEN [A].[FirstWorkingTeam] = 'B班' THEN [B].[FirstB] WHEN [A].[SecondWorkingTeam] = 'B班' THEN [B].[SecondB] WHEN [A].[ThirdWorkingTeam] = 'B班' THEN [B].[ThirdB] ELSE 0 END) AS B班,
		                            SUM(CASE WHEN [A].[FirstWorkingTeam] = 'C班' THEN [B].[FirstB] WHEN [A].[SecondWorkingTeam] = 'C班' THEN [B].[SecondB] WHEN [A].[ThirdWorkingTeam] = 'C班' THEN [B].[ThirdB] ELSE 0 END) AS C班,
		                            SUM(CASE WHEN [A].[FirstWorkingTeam] = 'D班' THEN [B].[FirstB] WHEN [A].[SecondWorkingTeam] = 'D班' THEN [B].[SecondB] WHEN [A].[ThirdWorkingTeam] = 'D班' THEN [B].[ThirdB] ELSE 0 END) AS D班,
		                            SUM([B].[TotalPeakValleyFlatB]) AS [合计]
                            FROM	[tz_Balance] AS [A] INNER JOIN
		                            [balance_Energy] AS [B] ON [A].[BalanceId] = [B].[KeyId]
                            WHERE
		                            ([B].[OrganizationID] LIKE @organizationId + '%') AND 
		                            ([A].[StaticsCycle] = 'day') AND 
		                            ([A].[TimeStamp] >= @startTime) AND
		                            ([A].[TimeStamp] <= @endTime)
                            GROUP BY [B].[OrganizationID], [B].[VariableId], LEFT([A].[TimeStamp],7)";

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("organizationId", organizationId),
                new SqlParameter("startTime", start.ToString("yyyy-MM-dd")),
                new SqlParameter("endTime", end.ToString("yyyy-MM-dd"))
            };

            return dataFactory.Query(sql, parameters);
        }

        private static DataTable GetProcessValueTableMonthly(string organizationId, DateTime month)
        {
            string startDate = (month.ToString("yyyy-MM") + "-01");
            string endDate = (month.ToString("yyyy-MM") + "-" + month.AddMonths(1).AddDays(-(month.Day)).ToString("dd"));
            return GetProcessValueTableByOrganizationId(organizationId, DateTime.Parse(startDate),  DateTime.Parse(endDate));
        }

        public static DataTable GetTeamJobEvaluationMonthly(string organization, string consumptionType, DateTime date)
        {
            DataTable table = GetProcessValueTableMonthly(organization, date);
            DataTable templateTable = GetProcessByOrganizationId(organization, consumptionType);
            string[] calculateColumns = { "A班", "B班", "C班", "D班", "合计" };
            DataTable result = EnergyConsumptionCalculate.CalculateByOrganizationId(table, templateTable, "ValueFormula", calculateColumns);
            return result;
        }
    }
}
