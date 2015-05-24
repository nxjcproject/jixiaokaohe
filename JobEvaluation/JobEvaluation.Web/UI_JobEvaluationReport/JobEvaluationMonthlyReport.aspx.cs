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
    public partial class JobEvaluationMonthlyReport : WebStyleBaseForEnergy.webStyleBase
    {
        private const string REPORT_TEMPLATE_PATH = "\\ReportHeaderTemplate\\report_CoalMilMonthlyPeakerValleyFlatElectricityConsumption.xml";
        

        protected void Page_Load(object sender, EventArgs e)
        {
            base.InitComponts();

            ////////////////////调试用,自定义的数据授权
#if DEBUG
            List<string> m_DataValidIdItems = new List<string>() { "zc_nxjc_byc" };
            AddDataValidIdGroup("ProductionOrganization", m_DataValidIdItems);
#elif RELEASE
#endif
            this.OrganisationTree.Organizations = GetDataValidIdGroup("ProductionOrganization");                         //向web用户控件传递数据授权参数
            this.OrganisationTree.PageName = "JobEvaluationMonthlyReport.aspx";                                     //向web用户控件传递当前调用的页面名称
            this.OrganisationTree.LeveDepth = 5;

            if (!IsPostBack)
            {

            }


        }
        [WebMethod]
        public static string GetJobEvaluationData(string date,string organizationId)
        {
            DataTable table = JobEvaluationService.GetJobEvaluationTable(date, organizationId);
            string json = EasyUIJsonParser.DataGridJsonParser.DataTableToJson(table);
            return json;
        }
    }
}