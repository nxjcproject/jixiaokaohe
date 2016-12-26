
//var g_labelName;
var g_type = 0;
var g_labelList = [];//标签列表数组（参与对比的标签LevelCode）
var g_labelLengh = 0;
$(document).ready(function () {
    initDatagrid('', 'first');
    initOperatorEvaluationDatagrid('', 'first');
    radioOnclick();
})
//加载datagrid
function initDatagrid(myData, myType) {
    if (myType == 'first') {
        $('#labelListId').datagrid({
            data: myData,
            rownumbers: true,
            singleSelect: true,
            striped: true,
            columns: [[
                { field: 'Name', title: '名称', width: 250 },
                { field: 'LevelCode', title: '组织机构', width: 100, hidden: true }
            ]],
            onDblClickRow: function (index, field, value) {
                $('#labelListId').datagrid('deleteRow', index);
                g_labelList.splice(index, 1);
                //若标签数组为空，则置标签长度为0
                if (g_labelList.length == 0) {
                    g_labelLengh = 0;
                }
            }
        });
    }
    else {
        $('#labelListId').datagrid('reload', myData['rows']);
    }
}

function initOperatorEvaluationDatagrid(Data, myType) {
    if (myType == 'first') {
        $('#EvaluationDataGrid').datagrid({
            data: Data,
            rownumbers: true,
            singleSelect: true,
            striped: true,
            columns: [[
                { field: 'CompanyName', title: '公司名称', width: 100 },
                    { field: 'WorkingSectionName', title: '岗位', width: 100 },
                    { field: 'Name', title: '操作员', width: 100 },
                    { field: 'Numerator', title: '总电量', width: 100 },
                    { field: 'Denominator', title: '总产量', width: 100 },
                    { field: 'Value', title: '电耗', width: 100 }
            ]]
        });
    }
    else {
        $('#EvaluationDataGrid').datagrid('loadData', Data['rows']);
    }
}

function refresh() {
    query();
}
function query() {
    var date = $('#datetime').datetimespinner('getValue');
    var sendData = "{levelCodesStr:'" + g_labelList + "',date:'" + date + "',type:'" + g_type + "'}";
    var win = $.messager.progress({
        title: '请稍后',
        msg: '数据载入中...'
    });
    $.ajax({
        type: "POST",
        url: "OperatorEvaluation.aspx/GetData",
        data: sendData,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $.messager.progress('close');
            var data = JSON.parse(msg.d);
            initOperatorEvaluationDatagrid(data, 'last');
        },
        beforeSend: function (XMLHttpRequest) {
            win;
        }
    });
}
//目录树双击事件
function onOrganisationTreeClick(node) {

    //var myOrganizationId = node.OrganizationId;
    var myLevelCode = node.id;
    //获取标签的长度
    if (g_labelLengh == 0) {
        g_labelLengh = myLevelCode.length;
    }
    else {
        if (g_labelLengh != myLevelCode.length) {
            $.messager.alert('提示', '该标签与已添加的标签级别不同！');
            return;
        }
    }
    var myName = node.text;
    //var t_json = labelObj(myOrganizationId, myName);
    if (g_labelList.contains(myLevelCode) == true) {
        $.messager.alert('提示', '该标签已经存在！');
        return;
    }
    g_labelList.push(myLevelCode);
    datagridAppendRow(myLevelCode, myName);
}
//追加新行
function datagridAppendRow(levelCode, name) {
    $('#labelListId').datagrid('appendRow',
        {
            LevelCode: levelCode,
            Name: name
        });
}
//清空列表
function removeAll() {
    g_labelLengh = 0;
    var count = g_labelList.length;
    g_labelList = [];
    for (var i = 0; i < count; i++) {
        $('#labelListId').datagrid('deleteRow', 0);
    }
}

function radioOnclick() {
    $(":radio").click(function () {
        g_type = $(this).val();
        if (g_type == 0) {
            $('#EvaluationDataGrid').datagrid({               
                columns: [[
                    { field: 'CompanyName', title: '公司名称', width: 100 },
                    { field: 'WorkingSectionName', title: '岗位', width: 100 },
                    { field: 'Name', title: '操作员', width: 100 },
                    { field: 'Numerator', title: '总电量', width: 100 },
                    { field: 'Denominator', title: '总产量', width: 100 },
                    { field: 'Value', title: '电耗', width: 100 }
                ]]
            });
            $('#EvaluationDataGrid').datagrid('loadData', []);
        }
        if (g_type == 1) {
            $('#EvaluationDataGrid').datagrid({
                columns: [[
                    { field: 'CompanyName', title: '公司名称', width: 100 },
                    { field: 'WorkingSectionName', title: '岗位', width: 100 },
                    { field: 'Name', title: '操作员', width: 100 },
                    { field: 'Numerator', title: '耗煤量', width: 100 },
                    { field: 'Denominator', title: '熟料产量', width: 100 },
                    { field: 'Value', title: '煤耗', width: 100 }
                ]]
            });
            $('#EvaluationDataGrid').datagrid('loadData', []);
        }
    })
}
//判断数组内元素是否存在
Array.prototype.contains = function (arr) {
    for (var i = 0; i < this.length; i++) {//this指向真正调用这个方法的对象  
        if (this[i] == arr) {
            return true;
        }
    }
    return false;
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