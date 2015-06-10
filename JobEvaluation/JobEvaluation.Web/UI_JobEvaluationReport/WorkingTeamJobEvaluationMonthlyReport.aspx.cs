using JobEvaluation.Service.JobEvaluationAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace JobEvaluation.Web.UI_JobEvaluationReport
{
    public partial class WorkingTeamJobEvaluationMonthlyReport : WebStyleBaseForEnergy.webStyleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            base.InitComponts();

            ////////////////////调试用,自定义的数据授权
#if DEBUG
            List<string> m_DataValidIdItems = new List<string>() { "zc_nxjc_byc" };
            AddDataValidIdGroup("ProductionOrganization", m_DataValidIdItems);
#elif RELEASE
#endif
            this.OrganisationTree.Organizations = GetDataValidIdGroup("ProductionOrganization");              // 向web用户控件传递数据授权参数
            this.OrganisationTree.PageName = "WorkingTeamJobEvaluationMonthlyReport.aspx";                    // 向web用户控件传递当前调用的页面名称
            this.OrganisationTree.LeveDepth = 5;
        }

        [WebMethod]
        public static string GetShiftsSchedulingLog(string organizationId, string date)
        {
            DataTable table = WorkingTeamJobEvaluationService.GetShiftsSchedulingLogMonthly(organizationId, date);
            return EasyUIJsonParser.DataGridJsonParser.DataTableToJson(table);
        }

        [WebMethod]
        public static string GetTeamJobEvaluation(string organizationId, string consumptionType, string date)
        {
            DateTime datetime = DateTime.Parse(date);
            DataTable table = WorkingTeamJobEvaluationService.GetTeamJobEvaluationMonthly(organizationId, consumptionType, datetime);
            return EasyUIJsonParser.TreeGridJsonParser.DataTableToJsonByLevelCode(table, "LevelCode");
        }
    }
}