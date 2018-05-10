$(document).ready(function () {
    InitDate();
})
//初始化日期框
function InitDate() {
    var endDate = new Date();
    var startDate = new Date();
    startDate.setDate(endDate.getDate() - 30);
    var endString = endDate.getFullYear() + '-' + (endDate.getMonth() + 1) + '-' + endDate.getDate();
    var startString = startDate.getFullYear() + '-' + (startDate.getMonth() + 1) + '-' + startDate.getDate();
    //var beforeString = beforeDate.getFullYear() + '-' + (beforeDate.getMonth() + 1) + '-' + beforeDate.getDate();
    $('#startDate').datebox('setValue', startString);
    $('#endDate').datebox('setValue', endString);
}
function Query() {
    var organizationLevelCode = $('#organizationId').val();
    var organizationId = $('#organizationId').val();
    var startDate = $('#startDate').datebox('getValue');
    var endDate = $('#endDate').datebox('getValue');
    var consumptionType = $('#cbbConsumptionType').combobox('getValue');

    // 获取排班记录
    GetShiftsSchedulingLog(organizationId, startDate, endDate);
    // 获取考核记录
    GetTeamJobEvaluation(organizationLevelCode, consumptionType, startDate, endDate);
}

function GetShiftsSchedulingLog(organizationId, startDate, endDate) {
    var queryUrl = 'WorkingTeamJobEvaluationMonthlyReport.aspx/GetShiftsSchedulingLog';
    var dataToSend = '{organizationId: "' + organizationId + '", startDate:"' + startDate + '", endDate:"' + endDate+'"}';
    var win = $.messager.progress({
        title: '请稍后',
        msg: '数据载入中...'
    });
    $.ajax({
        type: "POST",
        url: queryUrl,
        data: dataToSend,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $.messager.progress('close');
            $('#dgShiftsScheduling').datagrid({
                data: jQuery.parseJSON(msg.d)
            });
        },
        beforeSend: function (XMLHttpRequest) {
            win;
        }
    });
}

function GetTeamJobEvaluation(organizationLevelCode, consumptionType, startDate, endDate) {
    var queryUrl = 'WorkingTeamJobEvaluationMonthlyReport.aspx/GetTeamJobEvaluation';
    var dataToSend = '{organizationLevelCode: "' + organizationLevelCode + '",consumptionType:"' + consumptionType + '", startDate:"' + startDate + '", endDate:"' + endDate + '"}';
    $.ajax({
        type: "POST",
        url: queryUrl,
        data: dataToSend,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $('#tgTeamJobEvaluation').treegrid({
                data: jQuery.parseJSON(msg.d)
            });
        },
    });
}

function ShiftsSchedulingStyler(value, row, index) {
    if (value == "A班") {
        return 'background-color:#FF0000;';
    } else if (value == "B班") {
        return 'background-color:#00CD00;';
    } else if (value == "C班") {
        return 'background-color:#FFFF00;';
    } else if (value == "D班") {
        return 'background-color:#8470FF;';
    }
}

function ValueFormatter(value, row, index) {
    return isNaN(parseFloat(value)) ? '' : parseFloat(value).toFixed(2);
}

function JobEvaluationStyler(value, row, index) {
    if (value == '')
        return;
    if (IsMax(value,row)) {
        return 'color:#CE0000;';
    } else if (IsMin(value, row)) {
        return 'color:#007500;';
    }
}

function IsMax(value, row) {
    return value >= parseFloat(row.A班) && value >= parseFloat(row.B班) && value >= parseFloat(row.C班) && value >= parseFloat(row.D班);
}

function IsMin(value, row) {
    return value <= parseFloat(row.A班) && value <= parseFloat(row.B班) && value <= parseFloat(row.C班) && value <= parseFloat(row.D班);
}

// 获取双击组织机构时的组织机构信息
function onOrganisationTreeClick(node) {

    // 设置组织机构ID
    // organizationId为其它任何函数提供当前选中的组织机构ID

    $('#organizationId').val(node.id);

    // 设置组织机构名称
    // 用于呈现，在界面上显示当前的组织机构名称

    $('#txtOrganization').textbox('setText', node.text);
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