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
        public static DataTable GetShiftsSchedulingLogMonthly(string organizationId, string startDate,string endDate)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            string sql = @" SELECT [TimeStamp],[FirstWorkingTeam],[SecondWorkingTeam],[ThirdWorkingTeam]
                              FROM [tz_Balance]
                              WHERE TimeStamp>=@startDate AND TimeStamp<=@endDate
		                            and StaticsCycle = 'day' AND
		                            [OrganizationID] = @organizationId
                              ORDER BY [TimeStamp]";

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("organizationId", organizationId),
                new SqlParameter("startDate", startDate),
                new SqlParameter("endDate",endDate)
            };

            return dataFactory.Query(sql, parameters); ;
        }

        private static DataTable GetProcessByOrganizationId(string organizationLevelCode, string consumptionType)
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
		                            FROM tz_Formula INNER JOIN
									    system_Organization ON system_Organization.OrganizationID=tz_Formula.OrganizationID INNER JOIN
				                        formula_FormulaDetail ON tz_Formula.KeyID = formula_FormulaDetail.KeyID AND tz_Formula.[Type] <> 1 INNER JOIN
				                        balance_Energy_Template ON (balance_Energy_Template.VariableId = formula_FormulaDetail.VariableId + '_' + @consumptionType) 
		                            WHERE system_Organization.LevelCode like @organizationLevelCode + '%' and system_Organization.Type=balance_Energy_Template.ProductionLineType) AS F ON F.OrganizationID = system_Organization.OrganizationID
                           WHERE system_Organization.LevelCode like @organizationLevelCode + '%' and system_Organization.Type <> '余热发电' 
                        ORDER BY system_Organization.LevelCode, F.OrganizationID, F.[FormulaLevelCode]";
            SqlParameter[] parameters = new SqlParameter[]{ new SqlParameter("organizationLevelCode", organizationLevelCode),
                                                            new SqlParameter("consumptionType", consumptionType) 
                                                          };
            DataTable table = dataFactory.Query(sql, parameters);
            return table;
        }

        private static DataTable GetProcessValueTableByOrganizationId(string organizationLevelCode, string start, string end)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            string sql = @" SELECT
                                    B.[OrganizationID],
		                            B.[VariableId],
		                            SUM(CASE WHEN A.[FirstWorkingTeam] = 'A班' THEN B.[FirstB] WHEN A.[SecondWorkingTeam] = 'A班' THEN B.[SecondB] WHEN A.[ThirdWorkingTeam] = 'A班' THEN B.[ThirdB] ELSE 0 END) AS A班,
		                            SUM(CASE WHEN A.[FirstWorkingTeam] = 'B班' THEN B.[FirstB] WHEN A.[SecondWorkingTeam] = 'B班' THEN B.[SecondB] WHEN A.[ThirdWorkingTeam] = 'B班' THEN B.[ThirdB] ELSE 0 END) AS B班,
		                            SUM(CASE WHEN A.[FirstWorkingTeam] = 'C班' THEN B.[FirstB] WHEN A.[SecondWorkingTeam] = 'C班' THEN B.[SecondB] WHEN A.[ThirdWorkingTeam] = 'C班' THEN B.[ThirdB] ELSE 0 END) AS C班,
		                            SUM(CASE WHEN A.[FirstWorkingTeam] = 'D班' THEN B.[FirstB] WHEN A.[SecondWorkingTeam] = 'D班' THEN B.[SecondB] WHEN A.[ThirdWorkingTeam] = 'D班' THEN B.[ThirdB] ELSE 0 END) AS D班,
		                            SUM(B.[TotalPeakValleyFlatB]) AS 合计
                            FROM	tz_Balance AS A,
		                            balance_Energy AS B,
                                    system_Organization AS C 
                            WHERE
		                            C.LevelCode=@organizationLevelCode AND
                                    A.OrganizationID=C.OrganizationID AND
                                    A.BalanceId = B.KeyId AND
		                            A.StaticsCycle = 'day' AND 
		                            A.TimeStamp >= @startTime AND
		                            A.TimeStamp <= @endTime
                            GROUP BY B.[OrganizationID], B.[VariableId]";

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("organizationLevelCode", organizationLevelCode),
                new SqlParameter("startTime", start),
                new SqlParameter("endTime", end)
            };
            return dataFactory.Query(sql, parameters);
        }

        public static DataTable GetTeamJobEvaluationMonthly(string organizationLevelCode, string consumptionType, string startDate, string endDate)
        {
            DataTable table = GetProcessValueTableByOrganizationId(organizationLevelCode, startDate, endDate);
            DataTable templateTable = GetProcessByOrganizationId(organizationLevelCode, consumptionType);
            string[] calculateColumns = { "A班", "B班", "C班", "D班", "合计" };
            DataTable result = EnergyConsumptionCalculate.CalculateByOrganizationId(table, templateTable, "ValueFormula", calculateColumns);
            return result;
        }
    }
}
