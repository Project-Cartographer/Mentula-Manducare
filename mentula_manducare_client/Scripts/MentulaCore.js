var MentulaCore = function MentulaCore() {
    this.serverCount = 0;
}

MentulaCore.prototype.init = function() {
    this.loginDialog = document.querySelector("#loginDialog");
    this.loginServer = document.querySelector("#serverHost");
    this.loginPassword = document.querySelector("#serverPassword");
    this.loginConnect = document.querySelector("#serverConnect");
    var _this = this;
    this.loginConnect.addEventListener("click", function() {
        if (_this.loginPassword.value !== "" && _this.loginServer.value !== "") {
            if (_this.loginServer.value.indexOf(":") !== -1) {
                _this.endPoint = new MentulaEndpoint(
                    _this.loginServer.value.split(":")[0],
                    _this.loginServer.value.split(":")[1],
                    _this.loginPassword.value,
                    null);
            } else {
                _this.endPoint = new MentulaEndpoint(
                    _this.loginServer.value,
                    "9922",
                    _this.loginPassword.value,
                    null);
            }
        }
    });
}

