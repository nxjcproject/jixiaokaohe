function Query() {
    var organizationId = $('#organizationId').val();
    var datetime = $('#datetime').datetimespinner('getValue');
    var consumptionType = $('#cbbConsumptionType').combobox('getValue');

    // 获取排班记录
    GetShiftsSchedulingLog(organizationId, datetime);
    // 获取考核记录
    GetTeamJobEvaluation(organizationId, consumptionType, datetime);
}

function GetShiftsSchedulingLog(organizationId, date) {
    var queryUrl = 'WorkingTeamJobEvaluationMonthlyReport.aspx/GetShiftsSchedulingLog';
    var dataToSend = '{organizationId: "' + organizationId + '", date:"' + date + '"}';

    $.ajax({
        type: "POST",
        url: queryUrl,
        data: dataToSend,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $('#dgShiftsScheduling').datagrid({
                data: jQuery.parseJSON(msg.d)
            });
        }
    });
}

function GetTeamJobEvaluation(organizationId, consumptionType, date) {
    var queryUrl = 'WorkingTeamJobEvaluationMonthlyReport.aspx/GetTeamJobEvaluation';
    var dataToSend = '{organizationId: "' + organizationId + '",consumptionType:"' + consumptionType + '", date:"' + date + '"}';

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
        }
    });
}

function ShiftsSchedulingStyler(value, row, index) {
    if (value == "A班") {
        return 'background-color:#00FFFF;';
    } else if (value == "B班") {
        return 'background-color:#ADFF2F;';
    } else if (value == "C班") {
        return 'background-color:#FFD700;';
    } else if (value == "D班") {
        return 'background-color:#FF7F50;';
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

    $('#organizationId').val(node.OrganizationId);

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