var MentulaStats = function MentulaStats(callback) {
    var _this = this;

    //SET THE HOST TO THE SERVER IP.
    this.host = "66.85.77.238";
    this.port = "9922";
    this.baseURI = `http://${this.host}:${this.port}`;
    this.connectionURI = `${this.baseURI}/signalr`;
    this.hubURI = `${this.baseURI}/signalr/hubs`;
    this.resultsCallback = callback;
    var hubsScript = document.createElement('script');
    hubsScript.setAttribute("src", this.hubURI);
    hubsScript.setAttribute("async", "false");
    hubsScript.onload = function() {
        $.connection.hub.url = _this.connectionURI;
        _this.serverConnection = $.connection.ServerHub;
        _this.initEvents();

        $.connection.hub.start().done(function () {
        });
    }
    document.head.append(hubsScript);
}

MentulaStats.prototype.initEvents = function () {
    var _this = this;
    this.serverConnection.client.GetStats = function(result) {
        _this.GetStatsCallback(result);
    }
}

MentulaStats.prototype.GetStats = function() {
    this.serverConnection.server.getStats();
}
MentulaStats.prototype.GetStatsCallback = function(result) {
    if (this.resultsCallback != null && this.resultsCallback != undefined)
        this.resultsCallback(JSON.parse(result));
}