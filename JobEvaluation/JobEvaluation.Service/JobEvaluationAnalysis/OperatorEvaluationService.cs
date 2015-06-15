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
    public class OperatorEvaluationService
    {
        public static DataTable DataService(int type, string[] levelCodes, string date)
        {
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);
            string workongSectionSql = @"select A.WorkingSectionName,A.Type,A.OrganizationID,B.LevelCode,A.ElectricityQuantityId,A.OutputId,A.CoalWeightId
                                            from system_WorkingSection A,system_Organization B
                                            where A.OrganizationID=B.OrganizationID
                                            and ({0})
                                            order by B.LevelCode";
            StringBuilder levelBuilder = new StringBuilder();
            foreach (string item in levelCodes)
            {
                levelBuilder.Append("CHARINDEX('");
                levelBuilder.Append(item.Trim());
                levelBuilder.Append("',LevelCode)>0");
                levelBuilder.Append(" or ");
            }
            levelBuilder.Remove(levelBuilder.Length - 4, 4);
            //获取岗位信息
            DataTable workingSectionTable = dataFactory.Query(string.Format(workongSectionSql,levelBuilder.ToString()));
            string numeratorColumn = "";//分子
            string denominatorColumn = "";//分母
            if (type == 0)//电耗
            {
                numeratorColumn = "ElectricityQuantityId";
                denominatorColumn = "OutputId";
            }
            if (type == 1)//煤耗
            {
                numeratorColumn = "CoalWeightId";
                denominatorColumn = "OutputId";
            }
            DataTable resultTable = new DataTable();
            foreach (DataRow dr in workingSectionTable.Rows)
            {
                resultTable.Merge(GetConsumptionData(dr, numeratorColumn, denominatorColumn,date));
            }
            return resultTable;
        }

        private static DataTable GetConsumptionData(DataRow row, string numeratorColumn, string denominatorColumn,string date)
        {
            DataTable resultTable=new DataTable();
            //-----------参数列表----------
            string levelCode = row["LevelCode"].ToString().Trim();
            string numeratorId = row[numeratorColumn].ToString().Trim();//分子的variableId
            string denominatorId = row[denominatorColumn].ToString().Trim();//分母的variableId
            string sectionName = row["WorkingSectionName"].ToString();//岗位名称
            //-----------------------------
            if (numeratorId == "" || denominatorId == "")
            {
                return resultTable;
            }
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(ConnectionStringFactory.NXJCConnectionString);
            string baseSQL = @"SELECT H.VariableId,K.Name AS CompanyName,G.WorkingSectionName,G.Name,SUM(H.Value) AS Value,H.Type
                                FROM 
	                                (select convert(varchar(10),A.ShiftDate,20) as ShiftDate,B.OrganizationID, D.WorkingSectionName,C.Name,A.WorkingTeam
	                                from shift_WorkingTeamShiftLog A,shift_OperatorsLog B,system_StaffInfo C,system_WorkingSection D,system_Organization E
	                                where A.WorkingTeamShiftLogID=B.WorkingTeamShiftLogID
	                                and B.StaffID=C.StaffInfoID
	                                and B.WorkingSectionID=D.WorkingSectionID
	                                and B.OrganizationID=E.OrganizationID
                                    and D.WorkingSectionName=@WorkingSectionName  --岗位条件
	                                and CHARINDEX(@levelCode,E.LevelCode)>0               --产线条件--操作员上班记录
                                    and convert(varchar(7),A.ShiftDate,20)=@date) AS G   --时间条件
                                INNER JOIN
	                                (SELECT (CASE WHEN C.LevelType='Company' THEN C.Name ELSE (D.Name) END) AS Name,C.OrganizationID,C.LevelCode
                                    FROM
                                    (select a.OrganizationID,a.LevelCode,a.Name,a.LevelType from system_Organization a) C
                                    LEFT JOIN
                                    (select b.OrganizationID,b.LevelCode,b.Name,b.LevelType from system_Organization b where b.LevelType='Company') D
                                    ON CHARINDEX(D.LevelCode,C.LevelCode)>0) AS K --获取公司名称
                                ON K.OrganizationID=G.OrganizationID
                                LEFT JOIN
	                                (select B.VariableId,B.OrganizationID,A.TimeStamp,A.FirstWorkingTeam as Team,isnull(B.FirstB,0) AS Value,
                                            (case when B.VariableId='{0}' then 'numerator' when B.VariableId='{1}' then 'denominator' end) as Type
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and (B.VariableId='{0}' or B.VariableId='{1}')  --{0}分子{1}分母
                                    and convert(varchar(7),A.TimeStamp,20)=@date
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.SecondWorkingTeam as Team,isnull(B.SecondB,0) AS Value,
                                            (case when B.VariableId='{0}' then 'numerator' when B.VariableId='{1}' then 'denominator' end) as Type
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and (B.VariableId='{0}' or B.VariableId='{1}')  --{0}分子{1}分母
                                    and convert(varchar(7),A.TimeStamp,20)=@date
	                                union
	                                select B.VariableId,B.OrganizationID,A.TimeStamp,A.ThirdWorkingTeam as Team,isnull(B.ThirdB,0) AS Value,
                                            (case when B.VariableId='{0}' then 'numerator' when B.VariableId='{1}' then 'denominator' end) as Type
	                                from tz_Balance A,balance_Energy B
	                                where A.BalanceId=B.KeyId
	                                and A.StaticsCycle='day'
                                    and (B.VariableId='{0}' or B.VariableId='{1}')  --{0}分子{1}分母
                                    and convert(varchar(7),A.TimeStamp,20)=@date
                                    )AS H  --值
                                ON G.ShiftDate=H.TimeStamp
                                AND G.WorkingTeam=H.Team
                                AND G.OrganizationID=H.OrganizationID
                                GROUP BY H.VariableId,K.Name,G.WorkingSectionName,G.Name,H.Type
                                ORDER BY K.Name,G.WorkingSectionName,G.Name";
            SqlParameter[] parameters ={new SqlParameter("WorkingSectionName",sectionName),new SqlParameter("levelCode",levelCode)
                                      ,new SqlParameter("date",date)};
            DataTable table = dataFactory.Query(string.Format(baseSQL,numeratorId,denominatorId),parameters);

            DataTable result = CalculateConsumpution(table);
            return result;
        }

        private static DataTable CalculateConsumpution(DataTable sourceTable)
        {
            //Numerator字段为分子值，Denominator为分母值，Value为结果值
            DataColumn numeratorColumn = new DataColumn("Numerator", typeof(decimal));//分子
            DataColumn denominatorColumn = new DataColumn("Denominator", typeof(decimal));//分母
            sourceTable.Columns.Add(numeratorColumn);
            sourceTable.Columns.Add(denominatorColumn);
            //找出分子的行
            DataRow[] numeratorRows = sourceTable.Select("Type='numerator'");
            foreach (DataRow itemRow in numeratorRows)
            {
                string companyName = itemRow["CompanyName"].ToString().Trim();//公司名
                string workingSectionName = itemRow["WorkingSectionName"].ToString().Trim();//岗位名
                string name = itemRow["Name"].ToString().Trim();//岗位名
                DataRow[] denominatorRows = sourceTable.Select("CompanyName='" + companyName
                    + "' and WorkingSectionName='" + workingSectionName + "' and Name='" + name + "' and Type='denominator'");
                if (denominatorRows.Count() == 1)
                {
                    decimal numeratorValue = 0;//分子值
                    decimal denominatorValue = 0;//分母值
                    decimal.TryParse(denominatorRows[0]["Value"].ToString().Trim(),out denominatorValue);
                    decimal.TryParse(itemRow["Value"].ToString().Trim(), out numeratorValue);
                    itemRow["Numerator"] = numeratorValue;
                    itemRow["Denominator"] = denominatorValue;
                    itemRow["Value"] = denominatorValue == 0 ? 0 : numeratorValue / denominatorValue;
                }
                else
                {
                    itemRow["Value"] = 0;
                }
            }
            DataTable result = sourceTable.Clone();//克隆表结构
            foreach (DataRow dr in numeratorRows)
            {
                result.Rows.Add(dr.ItemArray);
            }
            return result;
        }
    }
}
