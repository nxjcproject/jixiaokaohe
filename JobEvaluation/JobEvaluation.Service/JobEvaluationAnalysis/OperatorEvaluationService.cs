using JobEvaluation.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace JobEvaluation.Service.JobEvaluationAnalysis
{
    public class OperatorEvaluationService
    {
        /// <summary>
        /// 服务
        /// </summary>
        /// <param name="type">1：电耗，2：煤耗</param>
        /// <param name="levelCodes"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DataTable DataService(int type, string[] levelCodes, string date)
        {
            if (type == 0)//电耗
            {
                return GetElectricityConsumption(levelCodes, date);
            }
            else//煤耗
            {
                return GetCoalConsumption(levelCodes, date);
            }
        }

        private static DataTable GetCoalConsumption(string[] levelCodes, string date)
        {
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);

            string baseSQL = @"SELECT H.VariableId,K.Name AS CompanyName,G.WorkingSectionName,G.Name,SUM(H.Value) AS Value
                                FROM 
	                                (select convert(varchar(10),A.ShiftDate,20) as ShiftDate,B.OrganizationID, D.Name as WorkingSectionName,C.Name,A.WorkingTeam
	                                from shift_WorkingTeamShiftLog A,shift_OperatorsLog B,system_StaffInfo C,system_WorkingSection D,system_Organization E
	                                where A.WorkingTeamShiftLogID=B.WorkingTeamShiftLogID
	                                and B.StaffID=C.StaffInfoID
	                                and B.WorkingSectionName=D.WorkingSectionID
	                                and B.OrganizationID=E.OrganizationID
                                    and ({3}) --{3}岗位条件
	                                and ({0})--{0}产线条件--操作员上班记录
                                    and convert(varchar(7),A.ShiftDate,20)='{2}') AS G   --{2}时间条件
                                INNER JOIN
	                                (SELECT (CASE WHEN C.LevelType='Company' THEN C.Name ELSE (D.Name) END) AS Name,C.OrganizationID,C.LevelCode
                                    FROM
                                    (select a.OrganizationID,a.LevelCode,a.Name,a.LevelType from system_Organization a) C
                                    LEFT JOIN
                                    (select b.OrganizationID,b.LevelCode,b.Name,b.LevelType from system_Organization b where b.LevelType='Company') D
                                    ON CHARINDEX(D.LevelCode,C.LevelCode)>0) AS K --获取公司名称
                                ON K.OrganizationID=G.OrganizationID
                                LEFT JOIN
	                                (select B.VariableId,B.OrganizationID,A.TimeStamp,A.FirstWorkingTeam as Team,isnull(B.FirstB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}' 
	                                and ({1})
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.SecondWorkingTeam as Team,isnull(B.SecondB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}'
	                                and ({1})
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.ThirdWorkingTeam as Team,isnull(B.ThirdB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}'
	                                and ({1}))AS H  --值
                                ON G.ShiftDate=H.TimeStamp
                                AND G.WorkingTeam=H.Team
                                AND G.OrganizationID=H.OrganizationID
                                GROUP BY H.VariableId,K.Name,G.WorkingSectionName,G.Name
                                ORDER BY K.Name,G.WorkingSectionName,G.Name";
            StringBuilder levelBuilder = new StringBuilder();
            foreach (string item in levelCodes)
            {
                levelBuilder.Append("CHARINDEX('");
                levelBuilder.Append(item.Trim());
                levelBuilder.Append("',E.LevelCode)>0");
                levelBuilder.Append(" or ");
            }
            levelBuilder.Remove(levelBuilder.Length - 4, 4);
            //---------------------岗位条件----------------
            //string workingSection01 = "D.Name='水泥磨操' or D.Name='窑操'";
            //string workingSection02 = "D.Name='生料磨操'";
            //string workingSection03 = "D.Name='水泥磨操' or D.Name='窑操' or D.Name='生料磨操'";
            string workingSection04 = "D.Name='窑操'";
            //---------------------电量部分----------------
            //耗煤量
            string coal = "B.VariableId='clinkerBurning_ElectricityQuantity' or B.VariableId='cementPreparation_ElectricityQuantity'";
            DataTable coalTable = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), coal, date, workingSection04));
            //--------------------产量部分---------------
            //产量（熟料产量）
            string output = "B.VariableId='clinker_ClinkerOutput'";
            DataTable outputTable = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), output, date, workingSection04));
            DataTable result = CalculateConsumpution(coalTable, outputTable);
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="levelCodes"></param>
        /// <param name="date"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static DataTable GetElectricityConsumption(string[] levelCodes, string date)
        {
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);
            
            string baseSQL = @"SELECT H.VariableId,K.Name AS CompanyName,G.WorkingSectionName,G.Name,SUM(H.Value) AS Value
                                FROM 
	                                (select convert(varchar(10),A.ShiftDate,20) as ShiftDate,B.OrganizationID, D.Name as WorkingSectionName,C.Name,A.WorkingTeam
	                                from shift_WorkingTeamShiftLog A,shift_OperatorsLog B,system_StaffInfo C,system_WorkingSection D,system_Organization E
	                                where A.WorkingTeamShiftLogID=B.WorkingTeamShiftLogID
	                                and B.StaffID=C.StaffInfoID
	                                and B.WorkingSectionName=D.WorkingSectionID
	                                and B.OrganizationID=E.OrganizationID
                                    and ({3}) --{3}岗位条件
	                                and ({0})--{0}产线条件--操作员上班记录
                                    and convert(varchar(7),A.ShiftDate,20)='{2}') AS G   --{2}时间条件
                                INNER JOIN
	                                (SELECT (CASE WHEN C.LevelType='Company' THEN C.Name ELSE (D.Name) END) AS Name,C.OrganizationID,C.LevelCode
                                    FROM
                                    (select a.OrganizationID,a.LevelCode,a.Name,a.LevelType from system_Organization a) C
                                    LEFT JOIN
                                    (select b.OrganizationID,b.LevelCode,b.Name,b.LevelType from system_Organization b where b.LevelType='Company') D
                                    ON CHARINDEX(D.LevelCode,C.LevelCode)>0) AS K --获取公司名称
                                ON K.OrganizationID=G.OrganizationID
                                LEFT JOIN
	                                (select B.VariableId,B.OrganizationID,A.TimeStamp,A.FirstWorkingTeam as Team,isnull(B.FirstB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}' 
	                                and ({1})
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.SecondWorkingTeam as Team,isnull(B.SecondB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}'
	                                and ({1})
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.ThirdWorkingTeam as Team,isnull(B.ThirdB,0) AS Value
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and convert(varchar(7),A.TimeStamp,20)='{2}'
	                                and ({1}))AS H  --值
                                ON G.ShiftDate=H.TimeStamp
                                AND G.WorkingTeam=H.Team
                                AND G.OrganizationID=H.OrganizationID
                                GROUP BY H.VariableId,K.Name,G.WorkingSectionName,G.Name
                                ORDER BY K.Name,G.WorkingSectionName,G.Name";
            StringBuilder levelBuilder = new StringBuilder();
            foreach (string item in levelCodes)
            {
                levelBuilder.Append("CHARINDEX('");
                levelBuilder.Append(item.Trim());
                levelBuilder.Append("',E.LevelCode)>0");
                levelBuilder.Append(" or ");
            }
            levelBuilder.Remove(levelBuilder.Length - 4, 4);
            //---------------------岗位条件----------------
            string workingSection01 = "D.Name='水泥磨操' or D.Name='窑操'";
            string workingSection02 = "D.Name='生料磨操'";
            string workingSection03 = "D.Name='水泥磨操' or D.Name='窑操' or D.Name='生料磨操'";
            string workingSection04 = "D.Name='窑操'";
            //---------------------电量部分----------------
            //电量（不包括生料磨操） //TODO：生料磨操的电量为生料制备电量
            string elec = "B.VariableId='clinkerBurning_ElectricityQuantity' or B.VariableId='cementPreparation_ElectricityQuantity'";
            //电量（生料磨操）
            string elecRaw = "B.VariableId='rawMaterialsPreparation_ElectricityQuantity'";
            DataTable elecTable = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), elec,date,workingSection01));
            DataTable rawElecTable = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), elecRaw, date, workingSection02));
            elecTable.Merge(rawElecTable);
            //--------------------产量部分---------------
            //产量（包括水泥产量、熟料产量、生料产量）
            string output = "B.VariableId='cement_CementOutput'	or B.VariableId='clinker_ClinkerOutput'";
            string outputRaw = "B.VariableId='clinker_MixtureMaterialsOutput'";
            DataTable outputTable = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), output, date,workingSection01));
            DataTable rawOutput = dataFactory.Query(string.Format(baseSQL, levelBuilder.ToString(), outputRaw, date, workingSection02));
            outputTable.Merge(rawOutput);
            DataTable result = CalculateConsumpution(elecTable,outputTable);
            return result;
        }

        private static DataTable CalculateConsumpution(DataTable elecTable,DataTable outputTable)
        {
            DataTable resultTable = outputTable.Copy();
            DataColumn numeratorColumn = new DataColumn("Numerator", typeof(decimal));//分子
            DataColumn denominatorColumn = new DataColumn("Denominator", typeof(decimal));//分母
            resultTable.Columns.Add(numeratorColumn);
            resultTable.Columns.Add(denominatorColumn);
            string criteria = "VariableId='{0}' and CompanyName='{1}' and WorkingSectionName='{2}' and Name='{3}'";
            int count = elecTable.Rows.Count;
            for (int i = 0; i < count;i++ )
            {
                DataRow currentRow = elecTable.Rows[i];
                //水泥磨操电耗
                if (currentRow["VariableId"].ToString().Trim() == "cementPreparation_ElectricityQuantity")
                {
                    string outputVariableId = "cement_CementOutput";
                    DataRow[] rows = outputTable.Select(string.Format(criteria,outputVariableId, currentRow["CompanyName"].ToString().Trim(),
                        currentRow["WorkingSectionName"].ToString().Trim(), currentRow["Name"].ToString().Trim()));
                    decimal numeratorValue = Convert.ToDecimal(currentRow["Value"]);//分子值
                    decimal denominatorValue = 0;//分母值
                    resultTable.Rows[i]["Numerator"] = numeratorValue;
                    if (rows.Count() == 1)
                    {
                        denominatorValue = Convert.ToDecimal(rows[0]["Value"]);
                        resultTable.Rows[i]["Value"]=denominatorValue==0?0 : numeratorValue / denominatorValue;
                        resultTable.Rows[i]["Denominator"] = denominatorValue;
                    }
                    else if (rows.Count() == 0)
                    {
                        resultTable.Rows[i]["Value"] = 0;
                        resultTable.Rows[i]["Denominator"] = 0;
                    }
                    else
                    {
                        throw new Exception("数据错误");
                    }
                }
                //窑操电耗
                if (currentRow["VariableId"].ToString().Trim() == "clinkerBurning_ElectricityQuantity")
                {
                    string outputVariableId = "clinker_ClinkerOutput";
                    DataRow[] rows = outputTable.Select(string.Format(criteria, outputVariableId, currentRow["CompanyName"].ToString().Trim(),
                        currentRow["WorkingSectionName"].ToString().Trim(), currentRow["Name"].ToString().Trim()));
                    decimal numeratorValue = Convert.ToDecimal(currentRow["Value"]);//分子值
                    decimal denominatorValue = 0;//分母值
                    resultTable.Rows[i]["Numerator"] = numeratorValue;
                    if (rows.Count() == 1)
                    {
                        denominatorValue = Convert.ToDecimal(rows[0]["Value"]);
                        resultTable.Rows[i]["Value"] = denominatorValue == 0 ? 0 : numeratorValue / denominatorValue;
                        resultTable.Rows[i]["Denominator"] = denominatorValue;
                    }
                    else if (rows.Count() == 0)
                    {
                        resultTable.Rows[i]["Value"] = 0;
                        resultTable.Rows[i]["Denominator"] = 0;
                    }
                    else
                    {
                        throw new Exception("数据错误");
                    }
                }
                //生料操电耗
                if (currentRow["VariableId"].ToString().Trim() == "rawMaterialsPreparation_ElectricityQuantity")
                {
                    string outputVariableId = "clinker_MixtureMaterialsOutput";
                    DataRow[] rows = outputTable.Select(string.Format(criteria, outputVariableId, currentRow["CompanyName"].ToString().Trim(),
                        currentRow["WorkingSectionName"].ToString().Trim(), currentRow["Name"].ToString().Trim()));
                    decimal numeratorValue = Convert.ToDecimal(currentRow["Value"]);//分子值
                    decimal denominatorValue = 0;//分母值
                    resultTable.Rows[i]["Numerator"] = numeratorValue;
                    if (rows.Count() == 1)
                    {
                        denominatorValue = Convert.ToDecimal(rows[0]["Value"]);
                        resultTable.Rows[i]["Value"] = denominatorValue == 0 ? 0 : numeratorValue / denominatorValue;
                        resultTable.Rows[i]["Denominator"] = denominatorValue;
                    }
                    else if (rows.Count() == 0)
                    {
                        resultTable.Rows[i]["Value"] = 0;
                        resultTable.Rows[i]["Denominator"] = 0;
                    }
                    else
                    {
                        throw new Exception("数据错误");
                    }
                }
                //煤耗
                if (currentRow["VariableId"].ToString().Trim() == "clinker_CoalConsumption")
                {
                    string outputVariableId = "clinker_ClinkerOutput";
                    DataRow[] rows = outputTable.Select(string.Format(criteria, outputVariableId, currentRow["CompanyName"].ToString().Trim(),
                        currentRow["WorkingSectionName"].ToString().Trim(), currentRow["Name"].ToString().Trim()));
                    decimal numeratorValue = Convert.ToDecimal(currentRow["Value"]);//分子值
                    decimal denominatorValue = 0;//分母值
                    resultTable.Rows[i]["Numerator"] = numeratorValue;
                    if (rows.Count() == 1)
                    {
                        denominatorValue = Convert.ToDecimal(rows[0]["Value"]);
                        resultTable.Rows[i]["Value"] = denominatorValue == 0 ? 0 : numeratorValue / denominatorValue;
                        resultTable.Rows[i]["Denominator"] = denominatorValue;
                    }
                    else if (rows.Count() == 0)
                    {
                        resultTable.Rows[i]["Value"] = 0;
                        resultTable.Rows[i]["Denominator"] = 0;
                    }
                    else
                    {
                        throw new Exception("数据错误");
                    }
                }
            }
            return resultTable;
        }
    }
}
