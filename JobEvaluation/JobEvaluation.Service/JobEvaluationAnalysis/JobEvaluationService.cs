using JobEvaluation.Infrastructure.Configuration;
using SqlServerDataAdapter;
using StatisticalReport.Infrastructure.Report;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace JobEvaluation.Service.JobEvaluationAnalysis
{
    public static class JobEvaluationService
    {
        private static ISqlServerDataFactory _dataFactory;
        static JobEvaluationService()
        {
            _dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);
        }
        public static DataTable GetJobEvaluationTable(string date,string organizationId)
        {
            ///获得目的表结构及基本数据
            DataTable destination = new DataTable();
            string destinationSql = "SELECT * FROM [dbo].[performance_EnergyRatingTemplate] WHERE [PerformanceType]='Filiale' ORDER BY DisplayIndex";
            destination = _dataFactory.Query(destinationSql);
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
            DataTable productLineNum = GetCompanyProductLineNumTable(organizationId);
            int clinkerNum = (int)productLineNum.Select("Type='熟料'")[0]["Count"];
            int cementNum = (int)productLineNum.Select("Type='水泥磨'")[0]["Count"];
            //foreach (DataRow dr in planSourceTable.Rows)
            //{
            //    switch (dr["QuotasID"].ToString().Trim())
            //    {
            //        case "吨熟料发电量":
            //            dr
            //            break;
            //    }
            //}
            ///获取实际完成情况基本数据
            DataTable completeSourceTable = new DataTable();
            DateTime dateTime=DateTime.Parse(date);
            string startDate = (dateTime.AddDays(-dateTime.Day).ToString("yyyy-MM") + "-01");
            string endDate = (dateTime.ToString("yyyy-MM") + "-" + dateTime.AddMonths(1).AddDays(-(dateTime.Day)).ToString("dd"));
            string completeSql = @"SELECT SUM([B].[TotalPeakValleyFlatB]) AS Value,[VariableId]
		                                FROM [dbo].[balance_Energy] AS B INNER JOIN [dbo].[tz_Balance] AS A
		                                ON [A].[BalanceId]=[A].[BalanceId]
		                                INNER JOIN [dbo].[system_Organization] AS C
		                                ON [B].[OrganizationID]=[C].[OrganizationID]
		                                WHERE [C].[LevelCode] LIKE 
		                                (
				                                SELECT [C].[LevelCode] FROM [dbo].[system_Organization] AS C WHERE [C].[OrganizationID]=@organizationId
		                                )+'%'
		                                AND     
		                                [A].StaticsCycle='day' AND
		                                [A].[TimeStamp]>=@startDate AND
		                                [A].[TimeStamp]<=@endDate AND
		                                (
		                                [B].[VariableId]='coalPreparation_ElectricityQuantity' OR [B].[VariableId]='clinker_PulverizedCoalOutput' OR [B].[VariableId]='rawMaterialsPreparation_ElectricityQuantity'
		                                OR [B].[VariableId]='clinker_MixtureMaterialsOutput' OR [B].[VariableId]='clinkerPreparation_ElectricityQuantity'
		                                OR [B].[VariableId]='clinker_ClinkerOutput' OR [B].[VariableId]='clinker_PulverizedCoalInput' 
		                                OR [B].[VariableId]='cement_CementOutput' OR [B].[VariableId]='cementmill_ElectricityQuantity' 
		                                OR [B].[VariableId]='cementGrind_ElectricityQuantity'
		                                )
		                                GROUP BY [VariableId]
                                ";
            SqlParameter[] completeParameters = { new SqlParameter("startDate", startDate), new SqlParameter("endDate", endDate),
                                                    new SqlParameter("organizationId", organizationId) };
            completeSourceTable = _dataFactory.Query(completeSql, completeParameters);
            ///向目的表中填充数据
            string month = InitMonthDictionary()[Int16.Parse(dateTime.ToString("MM"))];
            decimal finalScore = 0;
            foreach (DataRow dr in destination.Rows)
            {
                ///填充目的表的PlanValue值
                foreach (DataRow planRow in planSourceTable.Rows)//
                {
                    if (dr["PerformanceName"].ToString().Trim() == planRow["QuotasID"].ToString().Trim())
                    {
                        if (dr["ProductionLineType"].ToString().Trim() == "熟料")
                        {
                            decimal value = clinkerNum == 0 ? 0 : (decimal)planRow[month] / clinkerNum;
                            dr["PlanValue"] = decimal.Parse((value).ToString("#0.00"));
                        }
                        else
                        {
                            decimal value = cementNum == 0 ? 0 : (decimal)planRow[month] / cementNum;
                            dr["PlanValue"] = decimal.Parse((value).ToString("#0.00"));
                        }
                    }
                }
                ///计算实际完成
                decimal currentValue = 0;
                string elecField = dr["ElectricityQuantityField"].ToString().Trim();
                string outputField = dr["OutputQuantityField"].ToString().Trim();
                if (dr["PerformanceName"].ToString().Trim() == "熟料煤耗")//熟料煤耗转换单位kg/t
                {
                    currentValue = (decimal)completeSourceTable.Select("VariableId='" + outputField + "'")[0]["Value"] == 0 ? 0 : (decimal)completeSourceTable.Select("VariableId='" + elecField + "'")[0]["Value"]*1000 /
                                         (decimal)completeSourceTable.Select("VariableId='" + outputField + "'")[0]["Value"];
                }
                else
                {
                    currentValue = (decimal)completeSourceTable.Select("VariableId='" + outputField + "'")[0]["Value"] == 0 ? 0 : (decimal)completeSourceTable.Select("VariableId='" + elecField + "'")[0]["Value"] /
                                         (decimal)completeSourceTable.Select("VariableId='" + outputField + "'")[0]["Value"];
                }
                dr["CompleteValue"] =decimal.Parse(currentValue.ToString("#0.00"));


                if (dr["CalculateMethod"].ToString().Trim() == "GT")//如果为大于
                {
                    if (currentValue>= (decimal)dr["GradeValueIII"])
                    {
                        dr["ActualScore"] = dr["ScoreIII"];//优秀
                    }
                    else
                    {
                        if (currentValue>= (decimal)dr["GradeValueII"])
                        {
                            dr["ActualScore"] = dr["ScoreII"];//良好
                        }
                        else
                        {
                            if (currentValue>= (decimal)dr["GradeValueI"])
                            {
                                dr["ActualScore"] = dr["ScoreI"];//及格
                            }
                            else
                            {
                                dr["ActualScore"] = 0;//不及格
                            }
                        }
                    }
                }
                else//如果为小于
                {
                    if (currentValue<= (decimal)dr["GradeValueIII"])
                    {
                        dr["ActualScore"] = dr["ScoreIII"];//优秀
                    }
                    else
                    {
                        if (currentValue <= (decimal)dr["GradeValueII"])
                        {
                            dr["ActualScore"] = dr["ScoreII"];//良好
                        }
                        else
                        {
                            if (currentValue<= (decimal)dr["GradeValueI"])
                            {
                                dr["ActualScore"] = dr["ScoreI"];//及格
                            }
                            else
                            {
                                dr["ActualScore"] = 0;//不及格
                            }
                        }
                    }
                }
                finalScore += (int)dr["ActualScore"];
            }
            finalScore = finalScore / (destination.Rows.Count * 10) * 100;
            DataRow finalRow = destination.NewRow();
            finalRow["PerformanceName"] = "综合得分";
            finalRow["ActualScore"] = finalScore;
            finalRow["PerformanceType"] = "Filiale";
            destination.Rows.Add(finalRow);
            return destination;
        }
        /// <summary>
        /// 获得生产线条数
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        private static DataTable GetCompanyProductLineNumTable(string organizationId)
        {
            string sql = @"SELECT COUNT(*) AS Count,Type 
                                   FROM [dbo].[system_Organization] 
                                   WHERE LevelCode LIKE 
                                   (SELECT [C].[LevelCode] FROM [dbo].[system_Organization] AS C WHERE [C].[OrganizationID]=@organizationId)+'%' 
                                   GROUP BY Type
                                  ";
            SqlParameter parameter = new SqlParameter("organizationId", organizationId);
            return _dataFactory.Query(sql, parameter);
        }
        /// <summary>
        /// 返回月份对照字典
        /// </summary>
        /// <returns></returns>
        private static IDictionary<int, string> InitMonthDictionary()
        {
            IDictionary<int, string> result = new Dictionary<int, string>();
            result.Add(1, "January");
            result.Add(2, "February");
            result.Add(3, "March");
            result.Add(4, "April");
            result.Add(5, "May");
            result.Add(6, "June");
            result.Add(7, "July");
            result.Add(8, "August");
            result.Add(9, "September");
            result.Add(10, "October");
            result.Add(11, "November");
            result.Add(12, "December");
            return result;
        }
    }
}
