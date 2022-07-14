<%@ Control Language="c#" AutoEventWireup="true" CodeBehind="View.ascx.cs" Inherits="Upendo.Modules.YafToDnnForumMigration.View" %>
<%@ Import Namespace="DotNetNuke.Services.Localization" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/labelcontrol.ascx" %>

<div class="dnnClear">
    <div>
        <asp:Label id="lblBoardName" runat="server" controlname="BoardID">Yaf Board: </asp:Label>
        <asp:DropDownList runat="server" ID="BoardID"></asp:DropDownList>
    </div>
    <asp:PlaceHolder runat="server" ID="ActiveForumsPlaceHolder">
        <div>
            <asp:Label id="lblActiveForumsImport" runat="server" controlname="ActiveForums">Active Forum:</asp:Label>
            <asp:DropDownList id="ActiveForums" runat="server"
                datavaluefield="ModuleID" datatextfield="ModuleTitle">
            </asp:DropDownList>
        </div>
    </asp:PlaceHolder>

	<asp:LinkButton runat="server" ID="lnkImport" CssClass="btn btn-primary" OnClick="lnkImport_Click">Start Import</asp:LinkButton>
    <div id="importProgress"></div>
    <pre id="importLog"></pre>
</div>


<script type="text/javascript">

    var importProgress = function () {
        $.ajax({
            type: "POST",
            url: '/DesktopModules/YafToDnnForumMigration/ImportProgress.ashx',
            data: {
                method: "GetImportProgress",
            },
            dataType: "json",
            success: function (data) {
                $("#importProgress").text(data.Progress + "%");
                var $logpane = $("#importLog");
                $logpane.text(data.Log);
                $logpane[0].scrollTop = $logpane[0].scrollHeight;

                if (data.Progress < 100) {
                    setTimeout(importProgress, 500);
                }
            },
            error: function (data) {
                if (console && console.log)
                    console.log(data.responseText);
                $("#importProgress").text(data.statusText);
            }
        });
    };
</script>