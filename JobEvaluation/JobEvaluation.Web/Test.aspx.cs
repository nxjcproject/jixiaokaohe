using JobEvaluation.Service.JobEvaluationAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace JobEvaluation.Web
{
    public partial class Test : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //DataTable temp= JobEvaluationService.GetJobEvaluationTable("2015-01-02", "zc_nxjc_qtx_efc");
            DataTable temp = OperatorEvaluationService.DataService(1,new string[] { "O03" }, "2015-05");
        }
    }
}