var MentulaEndpoint = function MentulaEndpoint(host, port, password, successCallback) {
    this.host = host;
    this.port = port;
    this.baseURI = "http://" + host + ":" + port;
    this.connectionURI = this.baseURI + "/signalr";
    this.hubURI = this.baseURI + "/signalr/hubs";
    this.serverConnection = null;

    //The current server GUID that determines which callback set to use.
    this.currentGUID = null;
    this.callbackSet = {};

    //Static Events
    this.GetServerListCallback = null;


    //Pass the password to init so it is never stored.
    this.init(password, successCallback);
}
MentulaEndpoint.prototype.init = function (password, successCallback) {
    //Dynamicly load the hub script 
    var hubScript = document.createElement('script');
    hubScript.setAttribute("src", this.hubURI);
    //Assert that the onload event wont fire on browsers that async scripts by default
    hubScript.setAttribute("async", "false");
    var _this = this;
    hubScript.onLoad = function() {
        //Configure hub path
        $.connection.hub.url = _this.connectionURI;

        //Store the hub to the Endpoint
        _this.serverConnection = $.connection.ServerHub;

        //Define client event for login
        _this.serverConnection.client.LoginEvent = function (result, token) {
            successCallback(result, token);
        }

        //Attempt to connect to hub
        $.connection.hub.start().done(function () {
            _this.serverConnection.server.loginEvent(password);
        });
    }
}
MentulaEndpoint.prototype.registerCallback = function(serverGUID, callbackName, callback) {
    if (!this.callbackSet.hasOwnProperty(serverGUID)) {
        this.callbackSet[serverGUID] = {};
    }
    this.callbackSet[serverGUID][callbackName] = callback;
}

MentulaEndpoint.prototype.setCurrentSet = function(serverGUID) {
    if (this.callbackSet.hasOwnProperty(serverGUID))
        this.currentGUID = serverGUID;
    else
        throw("Server does not exist in Endpoint cache");
}
MentulaEndpoint.prototype.executeCurrentCallback = function(callbackKey, result) {
    if(this.callbackSet[this.currentGUID].hasOwnProperty(callbackKey))
        this.callbackSet[this.currentGUID][callbackKey](result);
}
MentulaEndpoint.prototype.resetCallbacks = function () {
    this.callbackSet = {};
    this.currentGUID = null;
}

MentulaEndpoint.prototype.initEvents = function() {
    var _this = this;
    this.serverConnection.client.GetServerListEvent = function(result) {
        if (_this.GetServerListCallback !== null && typeof(_this.GetServerListCallback) === "function")
            _this.GetServerListCallback(result);
    }
    for (var callbackKey in MentulaEndpoint.constCallbacks) {
        this.serverConnection.client[callbackKey] = (function(ckey) {
            return function(result) {
                _this.executeCurrentCallback(ckey, result);
            }
        })(callbackKey);
    }
}

MentulaEndpoint.constCallbacks = {
        GetServerListEvent: "GetServerListEvent",
        GetPlayersListEvent: "GetPlayersListEvent",
        KickPlayerEvent: "KickPlayerEvent",
        GetServerStatusEvent: "GetServerStatusEvent",
        SkipServerEvent: "SkipServerEvent",
        LoadBanListEvent: "LoadBanListEvent",
        BanPlayerEvent: "BanPlayerEvent",
        UnBanPlayerEvent: "UnBanPlayerEvent",
        LoadPlaylistEvent: "LoadPlaylistEvent",
        ChangePlaylistEvent: "ChangePlaylistEvent",
        LoadServerLogEvent: "LoadServerLogEvent",
        NotifyServerChangeEvent: "NotifyServerChangeEvent",
        StopServerEvent: "StopServerEvent",
        LoadVIPListEvent: "LoadVIPListEvent",
        AddVIPPlayerEvent: "AddVIPPlayerEvent",
        RemoveVIPPlayerEvent: "RemoveVIPPlayerEvent",
        LoadServerMessagesEvent: "LoadServerMessagesEvent",
        DeleteServerMessageEvent: "DeleteServerMessageEvent"
    };
