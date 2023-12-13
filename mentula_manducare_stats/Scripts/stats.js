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

if (!Object.prototype.forEach) {
    Object.defineProperty(Object.prototype, 'forEach', {
        value: function (callback, thisArg) {
            if (this == null) {
                throw new TypeError('Not an object');
            }
            thisArg = thisArg || window;
            for (var key in this) {
                if (this.hasOwnProperty(key)) {
                    callback.call(thisArg, this[key], key, this);
                }
            }
        }
    });
}








var MentulaExtra = function Mentula() {
    this.mapStrings = {
        ascension: "Ascension",
        backwash: "Backwash",
        beavercreek: "Beaver Creek",
        burial_mounds: "Burial Mounds",
        coagulation: "Coagulation",
        colossus: "Colossus",
        containment: "Containment",
        cyclotron: "Ivory Tower",
        deltatap: "Sanctuary",
        dune: "Relic",
        elongation: "Elongation",
        foundation: "Foundation",
        gemini: "Gemini",
        headlong: "Headlong",
        lockout: "Lockout",
        midship: "Midship",
        street_sweeper: "District",
        terminal: "Terminal",
        triplicate: "Uplift",
        needle: "Uplift",
        turf: "Turf",
        warlock: "Warlock",
        waterworks: "Waterworks",
        zanzibar: "Zanzibar"
    };
    this.statusStrings = {
        Lobby: "In Lobby",
        Starting: "Game Starting",
        InGame: "Playing",
        PostGame: "Post Game",
        MatchMaking: "Finding Players",
        Unknown: "Captain There is a problem"
    };
}

MentulaExtra.prototype.localizeMapName = function (mapName) {
    //Custom Map supportish...
    if (mapName === "")
        return "N/A";
    var name = this.mapStrings[mapName];
    if (name === undefined)
        name = mapName.char(0).toUpperCase() + mapName.slice(1);
    return name;
}
MentulaExtra.prototype.localizeStatus = function (status) {
    return this.statusStrings[status];
}