function enable() {
    var items = SP.ListOperation.Selection.getSelectedItems();
    var itemCount = CountDictionary(items);
    return (itemCount > 0);

}

function Bulkdownload() {

    var context = SP.ClientContext.get_current();
    this.site = context.get_site();
    this.web = context.get_web();
    context.load(this.site);
    context.load(this.web);
    context.executeQueryAsync(
        Function.createDelegate(this, this.onQuerySucceeded),
        Function.createDelegate(this, this.onQueryFailed)
    );
}

function onQuerySucceeded() {

    var lstGuid = SP.ListOperation.Selection.getSelectedList();
    var listname = "";
    if (lstGuid == null) {
        listname = $(".s4-titletext h2 a:first").html(); //requires JQuery to get list name from ribbon breadcrumb
    }

    //alert(lstGuid);
    //alert(listname);

    var items = SP.ListOperation.Selection.getSelectedItems();
    var itemCount = CountDictionary(items);

    if (itemCount == 0) return;

    var ids = "";
    for (var i = 0; i < itemCount; i++) {
        ids += items[i].id + ";";
    }

    var fullUrl = "";
    if (site.get_serverRelativeUrl() == "/") {
        fullUrl = site.get_url() + web.get_serverRelativeUrl();
    }
    else {
        fullUrl = site.get_url().replace(site.get_serverRelativeUrl(), web.get_serverRelativeUrl());
    }


    //alert(fullUrl);
    //send a request to the zip aspx page.
    var form = document.createElement("form");
    form.setAttribute("method", "post");
    form.setAttribute("action", fullUrl + "/_layouts/DCMA.BulkDownLoad/DCMA.BulkDownLoadPage.aspx");


    var hfSourceUrl = document.createElement("input");
    hfSourceUrl.setAttribute("type", "hidden");
    hfSourceUrl.setAttribute("name", "sourceUrl");
    hfSourceUrl.setAttribute("value", location.href);
    form.appendChild(hfSourceUrl);

    var hfItemIds = document.createElement("input")
    hfItemIds.setAttribute("type", "hidden");
    hfItemIds.setAttribute("name", "itemIDs");
    hfItemIds.setAttribute("value", ids);
    form.appendChild(hfItemIds);

    //ListGUID
    var hfListID = document.createElement("input")
    hfListID.setAttribute("type", "hidden");
    hfListID.setAttribute("name", "ListID");
    hfListID.setAttribute("value", lstGuid);
    form.appendChild(hfListID);

    //ListName
    var hfListName = document.createElement("input")
    hfListName.setAttribute("type", "hidden");
    hfListName.setAttribute("name", "ListName");
    hfListName.setAttribute("value", listname);
    form.appendChild(hfListName);

    document.body.appendChild(form);
    form.submit();
}

function onQueryFailed(sender, args) {

    alert("Downloading Failed.");

}
