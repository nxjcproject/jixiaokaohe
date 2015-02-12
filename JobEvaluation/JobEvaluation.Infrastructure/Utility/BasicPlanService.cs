using JobEvaluation.Infrastructure.Configuration;
using SqlServerDataAdapter;
using StatisticalReport.Infrastructure.Report;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace JobEvaluation.Infrastructure.Utility
{
    public class BasicPlanService
    {
        private static ISqlServerDataFactory _dataFactory;
        static BasicPlanService()
        {
            _dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);
        }
        public static DataTable GetBasicPlanTable(string date, string organizationId)
        {
            ///获取计划基本数据
            string year = DateTime.Parse(date).Year.ToString();
            DataTable planSourceTable = new DataTable();
            string planSql = @"SELECT  B.* 
	                                FROM [dbo].[tz_Plan] AS A 
	                                INNER JOIN [dbo].[plan_EnergyConsumptionYearlyPlan] AS B 
	                                ON A.KeyID=B.KeyID 
	                                INNER JOIN [dbo].[system_Organization] AS C ON A.OrganizationID=C.OrganizationID
	                                WHERE A.Date=@year AND
	                                C.LevelCode LIKE (SELECT LevelCode FROM [dbo].[system_Organization] AS C WHERE C.OrganizationID=@organizationId)+'%'
                                    ORDER BY [B].[ProductionLineType],[B].[DisplayIndex]
                             ";
            SqlParameter[] planParameters = { new SqlParameter("organizationId", organizationId), new SqlParameter("year", year) };
            DataTable temp = _dataFactory.Query(planSql, planParameters);
            planSourceTable = ReportHelper.MyTotalOn(temp, "QuotasID", "January,February,March,April,May,June,July,August,September,October,November,December,Totals");
            //构造计划目的表
            DataTable destination = planSourceTable.Clone();
            DataRow clinkerOutput = destination.NewRow();
            //clinkerOutput.ItemArray = planSourceTable.Rows.Find("熟料产量").ItemArray;
            //destination.Rows.Add(clinkerOutput);
            DataRow cememtOutput = planSourceTable.NewRow();
            //cememtOutput.ItemArray = planSourceTable.Rows.Find("水泥产量").ItemArray;
            //destination.Rows.Add(cememtOutput);

            DataRow clinkerElec = destination.NewRow();
            DataRow coalOutput = destination.NewRow();
            DataRow coalElec = destination.NewRow();
            //clinkerElec["QuotasID"] = "熟料电量";
            for (int i = planSourceTable.Columns.IndexOf("January"); i <= 13; i++)
            {
                clinkerElec["QuotasID"] = "熟料电量";
                clinkerElec[i] = (decimal)planSourceTable.Rows[ReportHelper.GetNoRow(planSourceTable, "QuotasID", "熟料产量")][i] *
                    (decimal)planSourceTable.Rows[ReportHelper.GetNoRow(planSourceTable, "QuotasID", "熟料电耗")][i];
                coalOutput["QuotasID"] = "煤粉消耗量";
                coalOutput[i] = (decimal)planSourceTable.Rows[ReportHelper.GetNoRow(planSourceTable, "QuotasID", "熟料产量")][i] *
                    (decimal)planSourceTable.Rows[ReportHelper.GetNoRow(planSourceTable, "QuotasID", "熟料煤耗")][i]*1000;
                coalElec["QuotasID"] = "煤粉耗电量";
                coalElec[i] = (decimal)planSourceTable.Rows[ReportHelper.GetNoRow(planSourceTable, "QuotasID", "煤磨电耗")][i] *
                    (decimal)coalOutput[i];

            }
            return destination;
        }
    }
}
