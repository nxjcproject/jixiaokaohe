<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WorkingTeamJobEvaluationMonthlyReport.aspx.cs" Inherits="JobEvaluation.Web.UI_JobEvaluationReport.WorkingTeamJobEvaluationMonthlyReport" %>
<%@ Register Src="~/UI_WebUserControls/OrganizationSelector/OrganisationTree.ascx" TagPrefix="uc1" TagName="OrganisationTree" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>班组考核</title>
    <link rel="stylesheet" type="text/css" href="/lib/ealib/themes/gray/easyui.css"/>
	<link rel="stylesheet" type="text/css" href="/lib/ealib/themes/icon.css"/>
    <link rel="stylesheet" type="text/css" href="/lib/extlib/themes/syExtIcon.css"/>
    <link rel="stylesheet" type="text/css" href="/lib/extlib/themes/syExtCss.css"/>

	<script type="text/javascript" src="/lib/ealib/jquery.min.js" charset="utf-8"></script>
	<script type="text/javascript" src="/lib/ealib/jquery.easyui.min.js" charset="utf-8"></script>
    <script type="text/javascript" src="/lib/ealib/easyui-lang-zh_CN.js" charset="utf-8"></script>

    <script type="text/javascript" src="/lib/ealib/extend/jquery.PrintArea.js" charset="utf-8"></script> 
    <script type="text/javascript" src="/lib/ealib/extend/jquery.jqprint.js" charset="utf-8"></script>

    <script type="text/javascript" src="/js/common/PrintFile.js" charset="utf-8"></script> 

    <script type="text/javascript" src="/UI_JobEvaluationReport/js/page/WorkingTeamJobEvaluationMonthlyReport.js"></script>
</head>
<body>
    <div class="easyui-layout" data-options="fit:true,border:false">
        <div data-options="region:'west',split:true" style="width:230px;">
            <uc1:OrganisationTree runat="server" id="OrganisationTree" />
        </div>
        <div data-options="region:'center',border:false">
            <div class="easyui-layout" data-options="fit:true,border:false">
               <div class="easyui-panel queryPanel" data-options="region:'north', border:true, collapsible:false, split:false" style="height: 50px;">
                    <table>
                        <tr><td style="height:5px;"></td></tr>
                        <tr>
                            <td>组织机构：</td>
                            <td>
                                <input id="txtOrganization" class="easyui-textbox" data-options="editable:false" style="width: 150px;" /><input id="organizationId" readonly="true" style="display: none;" /></td>
                            <td><div class="datagrid-btn-separator"></div></td>
                            <td>考核项目：</td>
                            <td>
                                <select id="cbbConsumptionType" class="easyui-combobox" data-options="panelHeight:'auto'" style="width:80px;">
                                    <option value="ElectricityConsumption">电耗</option>
                                    <option value="CoalConsumption">煤耗</option>
                                </select>
                            </td>
                            <td><div class="datagrid-btn-separator"></div></td>
                            <td>选择月份：</td>
                            <td>
                                <input id="datetime" class="easyui-datetimespinner" value="6/24/2014" data-options="formatter:formatter2,parser:parser2,selections:[[0,4],[5,7]]" style="width:180px;" />
                            </td>
                            <td><a href="javascript:void(0);" class="easyui-linkbutton" data-options="iconCls:'icon-search'" onclick="Query();">查询</a></td>
                        </tr>
                    </table>
                </div>
                <div data-options="region:'east',split:true" style="width:300px;" title="排班情况">
	                <table id="dgShiftsScheduling" class="easyui-datagrid" data-options="fill: true,singleSelect:true,fit:true">
		                <thead>
			                <tr>
				                <th data-options="field:'TimeStamp',width:120">日期</th>
                                <th data-options="field:'FirstWorkingTeam',width:50,styler:ShiftsSchedulingStyler">夜班</th>
				                <th data-options="field:'SecondWorkingTeam',width:50,styler:ShiftsSchedulingStyler">白班</th>
                                <th data-options="field:'ThirdWorkingTeam',width:50,styler:ShiftsSchedulingStyler">中班</th>
			                </tr>
		                </thead>
	                </table>
                </div>
                <div data-options="region:'center'" title="班组考核">
	                <table id="tgTeamJobEvaluation" class="easyui-treegrid" data-options="idField:'id',treeField:'Name',rownumbers:true,singleSelect:true,fit:true" style="width:100%">
		                <thead>
			                <tr>
				                <th data-options="field:'Name',width:250">工序</th>
                                <th data-options="field:'A班',width:60,formatter:ValueFormatter,styler:JobEvaluationStyler">A班</th>
				                <th data-options="field:'B班',width:60,formatter:ValueFormatter,styler:JobEvaluationStyler">B班</th>
                                <th data-options="field:'C班',width:60,formatter:ValueFormatter,styler:JobEvaluationStyler">C班</th>
                                <th data-options="field:'D班',width:60,formatter:ValueFormatter,styler:JobEvaluationStyler">D班</th>
                                <th data-options="field:'合计',width:60,formatter:ValueFormatter,styler:JobEvaluationStyler">平均</th>
			                </tr>
		                </thead>
	                </table>
                </div>
            </div>
        </div>
    </div>
    <form id="form1" runat="server"></form>
</body>
</html>
