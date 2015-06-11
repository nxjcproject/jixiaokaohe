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
    public partial class OperatorEvaluation : WebStyleBaseForEnergy.webStyleBase
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
            this.OrganisationTree.Organizations = GetDataValidIdGroup("ProductionOrganization");                         //向web用户控件传递数据授权参数
            this.OrganisationTree.PageName = "OperatorEvaluation.aspx";                                     //向web用户控件传递当前调用的页面名称
            this.OrganisationTree.LeveDepth = 5;

            if (!IsPostBack)
            {

            }
        }
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="levelCodesStr">层次码拼起来的字符串</param>
        /// <param name="date">时间</param>
        /// <param name="type">考核类型：1.电耗，2.煤耗</param>
        /// <returns></returns>
        [WebMethod]
        public static string GetData(string levelCodesStr, string date,int type)
        {
            string[] levelCodes = levelCodesStr.Split(',');
            DataTable table= OperatorEvaluationService.DataService(type,levelCodes, date);
            string json = EasyUIJsonParser.DataGridJsonParser.DataTableToJson(table);
            return json;
        }
    }
}