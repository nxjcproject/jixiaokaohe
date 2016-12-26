﻿var organizationID = '';
var datetime = '';
$(function () {
    //var m_UserName = $('#HiddenField_UserName').val();
    //loadGridData('first');
    InitializeGrid('');
});

function loadGridData(myLoadType, organizationId, datetime) {
    //parent.$.messager.progress({ text: '数据加载中....' });
    var m_MsgData;
    var win = $.messager.progress({
        title: '请稍后',
        msg: '数据载入中...'
    });
    $.ajax({
        type: "POST",
        url: "JobEvaluationMonthlyReport.aspx/GetJobEvaluationData",
        data: '{organizationId: "' + organizationId + '", date: "' + datetime + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $.messager.progress('close');
            if (myLoadType == 'first') {
                m_MsgData = jQuery.parseJSON(msg.d);
                InitializeGrid(m_MsgData);
            }
            else if (myLoadType == 'last') {
                m_MsgData = jQuery.parseJSON(msg.d);
                $('#gridMain_ReportTemplate').datagrid('loadData', m_MsgData['rows']);
            }
        },
        beforeSend: function (XMLHttpRequest) {
            win;
        },
        error: function () {
            $.messager.progress('close');
            handleError
        }
    });
}

function handleError() {
    $('#gridMain_ReportTemplate').datagrid('loadData', []);
    $.messager.alert('失败', '获取数据失败');
}

function InitializeGrid(myData) {

    $('#gridMain_ReportTemplate').datagrid({
        title: '',
        data: myData,
        dataType: "json",
        striped: true,
        idField: "field",
        //frozenColumns: [[m_IdAndNameColumn[1]]],
        columns: [[
                    { field: 'PerformanceName', title: '项目指标', width: 80 },
                    { field: 'PlanValue', title: '计划值', width: 60 },
                    { field: 'CompleteValue', title: '完成值', width: 60 },
                    { field: 'GradeValueI', title: '及格值', width: 60 },
                    { field: 'GradeValueII', title: '良好值', width: 60 },
                    { field: 'GradeValueIII', title: '优秀值', width: 60 },
                    { field: 'ActualScore', title: '得分', width: 40 }
                 ]],
        //loadMsg: '',   //设置本身的提示消息为空 则就不会提示了的。这个设置很关键的
        rownumbers: true,
        //pagination: true,
        singleSelect: true,
        //onClickCell: onClickCell,
        //idField: m_IdAndNameColumn[0].field,
        //pageSize: 20,
        //pageList: [20, 50, 100, 500],

        toolbar: '#toolbar_ReportTemplate'
    });

    //for(
}

function ExportFileFun() {
    var m_FunctionName = "ExcelStream";
    var m_Parameter1 = "Parameter1";
    var m_Parameter2 = "Parameter2";

    var form = $("<form id = 'ExportFile'>");   //定义一个form表单
    form.attr('style', 'display:none');   //在form表单中添加查询参数
    form.attr('target', '');
    form.attr('method', 'post');
    form.attr('action', "report_CementMilMonthlyEnergyConsumption.aspx");

    var input_Method = $('<input>');
    input_Method.attr('type', 'hidden');
    input_Method.attr('name', 'myFunctionName');
    input_Method.attr('value', m_FunctionName);
    var input_Data1 = $('<input>');
    input_Data1.attr('type', 'hidden');
    input_Data1.attr('name', 'myParameter1');
    input_Data1.attr('value', m_Parameter1);
    var input_Data2 = $('<input>');
    input_Data2.attr('type', 'hidden');
    input_Data2.attr('name', 'myParameter2');
    input_Data2.attr('value', m_Parameter2);

    $('body').append(form);  //将表单放置在web中 
    form.append(input_Method);   //将查询参数控件提交到表单上
    form.append(input_Data1);   //将查询参数控件提交到表单上
    form.append(input_Data2);   //将查询参数控件提交到表单上
    form.submit();
    //释放生成的资源
    form.remove();

    /*
    var m_Parmaters = { "myFunctionName": m_FunctionName, "myParameter1": m_Parameter1, "myParameter2": m_Parameter2 };
    $.ajax({
        type: "POST",
        url: "Report_Example.aspx",
        data: m_Parmater,                       //'myFunctionName=' + m_FunctionName + '&myParameter1=' + m_Parameter1 + '&myParameter2=' + m_Parameter2,
        //contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d == "1") {
                alert("导出成功!");
            }
            else{
                alert(msg.d);
            }
        }
    });
    */
}
function RefreshFun() {
    loadGridData('last', organizationID,datetime);
}
function PrintFileFun() {
    $.ajax({
        type: "POST",
        url: "report_CementMilMonthlyEnergyConsumption.aspx/PrintFile",
        data: "",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            PrintHtml(msg.d);
        }
    });


}

function QueryReportFun() {
    organizationID = $('#organizationId').val();
    datetime = $('#datetime').datetimespinner('getValue');
    var nowDate = new Date();

    var nowStr = nowDate.getFullYear().toString() + "-" + nowDate.getMonth().toString();
    
    if (organizationID == "" || datetime == "") {
        $.messager.alert('警告', '请选择生产线和时间');
        return;
    }
    if (datetime >= nowStr) {
        $.messager.alert('警告', '请选择当前时间之前的月份');
        return;
    }

    loadGridData('first', organizationID, datetime);
}

function onOrganisationTreeClick(node) {
    $('#productLineName').textbox('setText', node.text);
    $('#organizationId').val(node.OrganizationId);
}



function formatter2(date) {
    if (!date) { return ''; }
    var y = date.getFullYear();
    var m = date.getMonth() + 1;
    return y + '-' + (m < 10 ? ('0' + m) : m);
}
function parser2(s) {
    if (!s) { return null; }
    var ss = s.split('-');
    var y = parseInt(ss[0], 10);
    var m = parseInt(ss[1], 10);
    if (!isNaN(y) && !isNaN(m)) {
        return new Date(y, m - 1, 1);
    } else {
        return new Date();
    }
}