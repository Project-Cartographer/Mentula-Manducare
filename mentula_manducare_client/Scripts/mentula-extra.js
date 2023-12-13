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
MentulaExtra.prototype.localizeStatus = function(status) {
    return this.statusStrings[status];
}