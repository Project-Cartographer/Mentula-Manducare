//This shit is so crude.

var Mentula = function Mentula() {
    var _this = this;
    this.host = "localhost";
    this.port = "9922";
    this.password = "";
    this.loginDialog = document.querySelector("#loginDialog");
    this.loginServer = document.querySelector("#serverHost");
    this.loginPassword = document.querySelector("#serverPassword");
    this.loginConnect = document.querySelector("#serverConnect");
    this.serverCount = 0;
    this.loginConnect.addEventListener('click', function () {
        if (_this.loginPassword.value !== "" && _this.loginServer.value !== "") {
            if (_this.loginServer.value.indexOf(':')) {
                _this.host = _this.loginServer.value.split(':')[0];
                _this.port = _this.loginServer.value.split(':')[1];
            } else
                _this.host = _this.loginServer.value;
            _this.password = _this.loginPassword.value;
            _this.loginDialog.close();
            _this.init();
        }
    });
    if (navigator.userAgent.toLowerCase().indexOf('firefox') > -1) {
        dialogPolyfill.registerDialog(this.loginDialog);
    }
    this.loginDialog.showModal();

    this.hexDialog = document.querySelector('#hexValuesDialog');
    this.hexDialog.querySelector('.close').addEventListener('click', function() {
        _this.hexDialog.close();
    });
}

/**************************
 *          INIT          *
 **************************/

Mentula.prototype.init = function () {
    this.baseURI = `http://${this.host}:${this.port}`;
    this.connectionURI = `${this.baseURI}/signalr`;
    this.hubURI = `${this.baseURI}/signalr/hubs`;
    this.mapImageBaseURI = "./Content/Images/";
    this.serverConnection = null;
    this.currentTab = null;
    this.extra = new MentulaExtra();

    var hubsScript = document.createElement('script');
    hubsScript.setAttribute("src", this.hubURI);
    hubsScript.setAttribute("async", "false");
    var _this = this;
    hubsScript.onload = function () {
        $.connection.hub.url = _this.connectionURI;
        _this.serverConnection = $.connection.ServerHub;
        _this.initEvents();
        $.connection.hub.start().done(function () {
            _this.serverConnection.server.loginEvent(_this.password);
        });
    }
    document.head.append(hubsScript);
}

Mentula.prototype.initEvents = function () {
    var _this = this;
    this.serverConnection.client.GetStats = function(result) {
        console.log(result);
    }
    this.serverConnection.client.LoginEvent = function (result, Token) {
        _this.LoginCallback(result, Token);
    }
    this.serverConnection.client.GetServerListEvent = function (result) {
        _this.GetServerListCallback(result);
    }
    this.serverConnection.client.GetPlayersListEvent = function (result) {
        _this.GetPlayerListCallback(result);
    }
    this.serverConnection.client.KickPlayerEvent = function (result) {
        _this.KickPlayerCallback(result);
    }
    this.serverConnection.client.GetServerStatusEvent = function (result) {
        _this.GetServerStatusCallback(result);
    }
    this.serverConnection.client.SkipServerEvent = function (result) {
        _this.SkipServerCallback(result);
    }
    this.serverConnection.client.LoadBanListEvent = function (result) {
        _this.LoadBanListCallback(result);
    }
    this.serverConnection.client.BanPlayerEvent = function (result) {
        _this.BanPlayerCallback(result);
    }
    this.serverConnection.client.UnBanPlayerEvent = function (result) {
        _this.UnBanPlayerCallback(result);
    }
    this.serverConnection.client.LoadPlaylistsEvent = function(result) {
        _this.LoadPlayListsCallback(result);
    }
    this.serverConnection.client.ChangePlaylistEvent = function(result) {
        _this.ChangePlayListCallback(result);
    }
    this.serverConnection.client.LoadServerLogEvent = function(result) {
        _this.LoadServerLogCallback(result);
    }
    this.serverConnection.client.NotifyServerChangeEvent = function() {
        _this.serverConnection.server.getServerListEvent();
    }
    this.serverConnection.client.StopServerEvent = function() {
        _this.StopServerCallback(result);
    }
    this.serverConnection.client.LoadVIPListEvent = function(result) {
        _this.LoadVIPListCallback(result);
    }
    this.serverConnection.client.AddVIPPlayerEvent = function(result) {
        _this.AddVIPPlayerCallback(result);
    }
    this.serverConnection.client.RemoveVIPPlayerEvent = function(result) {
        _this.RemoveVIPPlayerCallback(result);
    }
    this.serverConnection.client.LoadServerMessagesEvent = function(result) {
        _this.LoadServerMessagesCallback(result);
    }
    this.serverConnection.client.DeleteServerMessageEvent = function() {
        _this.LoadServerMessagesEvent();
    }
    window.setInterval(function () {
        _this.updateCurrentState();
    }, 5000);
};

Mentula.prototype.InitNewServerTab = function (server) {
    var _this = this;
    //Create new tab link element for header
    var newTabLink = $('<a></a>').text(server["Name"]);
    newTabLink.attr("href", `#${server["Name"].replace(/\s/g, '-')}`);
    newTabLink.attr("data-index", server["Index"]);
    newTabLink.addClass("mdl-layout__tab");
    $('.mdl-layout__tab-bar').append(newTabLink);

    newTabLink[0].addEventListener("click",
        function () {
            _this.tabChangedEvent(newTabLink[0]);
        });
    //Create new tab content element
    var newTab = $("<section></section>").append($("<div></div>").addClass("page-content"));
    newTab.addClass("mdl-layout__tab-panel");
    newTab.attr("id", server["Name"].replace(/\s/g, '-'));
    $('.mdl-layout__content').append(newTab);
    newTabLink[0].tabContent = newTab[0];

    //Clone the template for tab content and append it
    var tabContents = document.getElementById('ServerContentsTemplate').content.cloneNode(true);
    var newTabElm = document.getElementById(`${server["Name"].replace(/\s/g, '-')}`);
    newTabElm.append(tabContents);

    newTabElm.init = false;
    newTabElm.controls = {};
    //Hook the skip button in the new tab content
    newTabElm.controls.skipButton = newTabElm.querySelector('.mentula-skip');
    newTabElm.controls.skipButton.addEventListener('click', function () {
        _this.serverConnection.server.skipServerEvent(server["Index"]);
    });

    //Hook the stop button
    newTabElm.controls.stopButton = newTabElm.querySelector('.mentula-stop');
    newTabElm.controls.stopButton.addEventListener('click',
        function () {
            _this.StopServerEvent();
        });

    //Hook the restart button
    newTabElm.controls.restartButton = newTabElm.querySelector('.mentula-restart');
    newTabElm.controls.restartButton.addEventListener('click',
        function() {
            _this.RestartServerEvent();
        });

    //Hook banlist refresh button
    newTabElm.controls.banrefresh = newTabElm.querySelector('.banlist-container h4 i');
    newTabElm.controls.banrefresh.addEventListener('click',
        function() {
            _this.LoadBanListEvent();
        });
    //Hook banlist refresh button
    newTabElm.controls.viprefresh = newTabElm.querySelector('.vip-container h4 i');
    newTabElm.controls.viprefresh.addEventListener('click',
        function () {
            _this.LoadVIPListEvent();
        });
    //Hook server log refresh button
    newTabElm.controls.logrefresh = newTabElm.querySelector('.right-container .right-one .server-refresh');
    newTabElm.controls.logrefresh.addEventListener('click',
        function() {
            _this.LoadServerLogEvent();
        });

    //Hook the Banlist add button
    newTabElm.controls.banAdd = newTabElm.querySelector('.ban-add button');
    newTabElm.querySelector('.ban-add .mdl-textfield input').id = `${server.Index}-ban-add`;
    newTabElm.querySelector('.ban-add .mdl-textfield label').setAttribute('for', `${server.Index}-ban-add`);
    newTabElm.controls.banAdd.addEventListener('click',
        function () {
            if (newTabElm.init === true) {
                var player = newTabElm.querySelector('.ban-add .mdl-textfield input').value;
                if (player !== "") {
                    newTabElm.querySelector('.ban-add .mdl-textfield input').value = "";
                    _this.BanPlayerEvent(player);
                }
            }
        });

    //Hook the VIPlist add button
    newTabElm.controls.vipAdd = newTabElm.querySelector('.vip-add button');
    newTabElm.querySelector('.vip-add .mdl-textfield input').id = `${server.Index}-vip-add`;
    newTabElm.querySelector('.vip-add .mdl-textfield label').setAttribute('for', `${server.Index}-vip-add`);
    newTabElm.controls.vipAdd.addEventListener('click',
        function () {
            if (newTabElm.init === true) {
                var player = newTabElm.querySelector('.vip-add .mdl-textfield input').value;
                if (player !== "") {
                    newTabElm.querySelector('.ban-add .mdl-textfield input').value = "";
                    _this.AddVIPPlayerEvent(player);
                }
            }
        });

    //Hook the Server Privacy
    newTabElm.controls.serverPrivacy = newTabElm.querySelector('.server-privacy');
    newTabElm.controls.serverPrivacyInput = newTabElm.querySelector('.server-privacy input');
    newTabElm.controls.serverPrivacyInput.id = `${server.Index}-privacy`;
    newTabElm.querySelector('.server-privacy label').setAttribute('for', `${server.Index}-privacy`);
    newTabElm.controls.serverPrivacyInput.addEventListener('change', function () {
        if (newTabElm.init === true) {
            _this.SetPrivacyEvent(this.dataset["value"]);
        }
    });

    //Hook the Server forced biped
    newTabElm.controls.serverBiped = newTabElm.querySelector('.server-biped');
    newTabElm.controls.serverBipedInput = newTabElm.querySelector('.server-biped input');
    newTabElm.controls.serverBipedInput.id = `${server.Index}-biped`;
    newTabElm.querySelector('.server-biped label').setAttribute('for', `${server.Index}-biped`);
    newTabElm.controls.serverBipedInput.addEventListener('change', function() {
        if (newTabElm.init === true) {
            _this.SetForcedBipedEvent(this.dataset["value"]);
        }
    });

    //Hook the Server max Players
    newTabElm.controls.serverMaxPlayers = newTabElm.querySelector('.server-max-players');
    newTabElm.controls.serverMaxPlayersInput = newTabElm.querySelector('.server-max-players input');
    newTabElm.controls.serverMaxPlayersInput.id = `${server.Index}-max-players`;
    newTabElm.querySelector('.server-max-players label').setAttribute('for', `${server.Index}-max-players`);
    newTabElm.controls.serverMaxPlayersInput.addEventListener('change', function() {
        if (newTabElm.init === true) {
            _this.SetMaxPlayersEvent(this.dataset["value"]);
        }
    });

    //Hook the Xdelay Timer
    newTabElm.controls.serverXDelay = newTabElm.querySelector('.server-xdelay-timer');
    newTabElm.controls.serverXDelayInput = newTabElm.querySelector('.server-xdelay-timer input');
    newTabElm.controls.serverXDelayInput.id = `${server.Index}-xdelay-timer`;
    newTabElm.querySelector('.server-xdelay-timer label').setAttribute('for', `${server.Index}-xdelay-timer`);
    newTabElm.controls.serverXDelayInput.addEventListener('change', function () {
        if (newTabElm.init === true) {
            if (this.validity.valid) {
                _this.SetXDelayTimer(this.value);
            }
        }
    });

    newTabElm.controls.serverAFKTimer = newTabElm.querySelector('.server-afk-timer');
    newTabElm.controls.serverAFKTimerInput = newTabElm.querySelector('.server-afk-timer input');
    newTabElm.controls.serverAFKTimerInput.id = `${server.Index}-afk-timer`;
    newTabElm.querySelector('.server-afk-timer label').setAttribute('for', `${server.Index}-afk-timer`);
    newTabElm.controls.serverAFKTimerInput.addEventListener('change', function() {
        if (newTabElm.init === true) {
            if (this.validity.valid) {
                _this.SetAFKTimerEvent(this.value);
            }
        }
    });

    //Hook the gay ass battle rifle shit that actually does nothing.
    newTabElm.controls.serverBattleRifle = newTabElm.querySelector('.server-br-fix');
    newTabElm.controls.serverBattleRifleInput = newTabElm.querySelector('.server-br-fix input');
    newTabElm.controls.serverBattleRifleInput.id = `${server.Index}-br-fix`;
    newTabElm.querySelector('.server-br-fix label').setAttribute('for', `${server.Index}-br-fix`);
    newTabElm.controls.serverBattleRifleInput.addEventListener('change', function () {
        if (newTabElm.init === true) {
            if (this.validity.valid) {
                _this.SetBRFixEvent(this.value);
            }
        }
    });

    //Hook the freeze lobby
    newTabElm.controls.serverFreeze = newTabElm.querySelector('.server-freeze-lobby input');
    newTabElm.controls.serverFreeze.id = `${server.Index}-freeze-lobby`;
    newTabElm.querySelector('.server-freeze-lobby').setAttribute('for', `${server.Index}-freeze-lobby`);
    newTabElm.controls.serverFreeze.addEventListener('change', function () {
        if (newTabElm.init === true)
            _this.FreezeLobbyEvent(this.checked.toString());
    });

    //Hook the Force Start Game
    newTabElm.controls.serverForce = newTabElm.querySelector('.server-force-start');
    newTabElm.controls.serverForce.id = `${server.Index}-force-start`;
    newTabElm.controls.serverForce.addEventListener('click', function() {
        if (newTabElm.init === true)
            _this.ForceStartLobbyEvent();
    });

    newTabElm.controls.serverDescription = newTabElm.querySelector('.server-description');
    newTabElm.controls.serverDescriptionInput = newTabElm.querySelector('.server-description input');
    newTabElm.controls.serverDescriptionInput.id = `${server.Index}-description`;
    newTabElm.querySelector('.server-description label').setAttribute('for', `${server.Index}-description`);
    newTabElm.controls.serverDescriptionInput.addEventListener('change',
        function () {
            if (newTabElm.init === true) 
                _this.SetDescriptionEvent(this.value);
        });

    newTabElm.controls.serverPCR = newTabElm.querySelector('.server-pcr input');
    newTabElm.controls.serverPCR.id = `${server.Index}-pcr`;
    newTabElm.querySelector('.server-pcr').setAttribute('for', `${server.Index}-pcr`);
    newTabElm.controls.serverPCR.addEventListener('change',
        function() {
            if (newTabElm.init === true)
                _this.SetPCRStateEvent(this.checked.toString());
        });

    newTabElm.controls.serverSyncProj = newTabElm.querySelector('.server-sync-proj input');
    newTabElm.controls.serverSyncProj.id = `${server.Index}-sync-proj`;
    newTabElm.querySelector('.server-sync-proj').setAttribute('for', `${server.Index}-sync-proj`);
    newTabElm.controls.serverSyncProj.addEventListener('change', function() {
        if (newTabElm.init === true)
            _this.SetSyncProjEvent(this.checked.toString());
    });

    newTabElm.controls.serverMessage = newTabElm.querySelector('.messages-message');
    newTabElm.controls.serverMessageInput = newTabElm.querySelector('.messages-message input');
    newTabElm.controls.serverMessageInput.id = `${server.Index}-message`;
    newTabElm.querySelector('.messages-message label').setAttribute('for', `${server.Index}-message`);
    
    newTabElm.controls.serverMessageDuration = newTabElm.querySelector('.messages-duration');
    newTabElm.controls.serverMessageDurationInput = newTabElm.querySelector('.messages-duration input');
    newTabElm.controls.serverMessageDurationInput.id = `${server.Index}-message-duration`;
    newTabElm.querySelector('.messages-duration label').setAttribute('for', `${server.Index}-message-duration`);

    newTabElm.controls.serverMessageAll = newTabElm.querySelector('.messages-all input');
    newTabElm.controls.serverMessageAll.id = `${server.Index}-message-all`;
    newTabElm.querySelector('.messages-all').setAttribute('for',`${server.Index}-message-all`);

    newTabElm.controls.serverMessageAdd = newTabElm.querySelector('.messages-add');
    newTabElm.controls.serverMessageAdd.id = `${server.Index}-message-add`;
    newTabElm.controls.serverMessageAdd.addEventListener("click", function() {
        if (newTabElm.init === true) {
            if (newTabElm.controls.serverMessageInput.validity.valid &&
                newTabElm.controls.serverMessageDurationInput.validity.valid) {
                _this.AddNewServerMessageEvent(
                    newTabElm.controls.serverMessageInput.value,
                    newTabElm.controls.serverMessageDurationInput.value,
                    newTabElm.controls.serverMessageAll.checked.toString());
                newTabElm.controls.serverMessage.MaterialTextfield.change("");
                newTabElm.controls.serverMessageDuration.MaterialTextfield.change("");
            }
        }
    });


    newTabElm.querySelectorAll('.mentula-hex').forEach(function (button) {
        button.addEventListener('click',
            function () {
                _this.hexDialog.showModal();
            });
    });



    componentHandler.upgradeElements(newTabElm);

    //Yet again fixing MDL...
    newTabElm.querySelectorAll('.mdl-tabs').forEach(function(mdltab) {
        var tabs = mdltab.querySelectorAll('.mdl-tabs__panel');
        tabs.forEach(function(tab) {
            tab.classList.remove('is-active');
        });
        tabs[0].classList.add('is-active');
    });
    //document.querySelectorAll('.mdl-textfield').forEach(function (a) { componentHandler.upgradeElement(a) });
}

/**************************
 *       FUNCTIONS        *
 **************************/

Mentula.prototype.updateCurrentState = function(){
    if (this.currentTab !== undefined) {
        this.serverConnection.server.getCurrentPlayersEvent(this.currentServer);
        this.serverConnection.server.getServerStatusEvent(this.currentServer);
    }
}

Mentula.prototype.LoginEvent = function (Password) {
    this.serverConnection.server.loginEvent(Password);
}

Mentula.prototype.KickPlayerEvent = function (PlayerName) {
    this.serverConnection.server.kickPlayerEvent(this.currentServer, PlayerName);
}

Mentula.prototype.tabChangedEvent = function (tabElement, force) {

    //Move the player table updating to its own function
    var forced = force || false;
    if (this.currentTab !== tabElement || forced) {
        //Rewriting MDL Code cause they suck donkey balls.
        document.querySelectorAll('.mdl-layout__tab-bar a').forEach(function(button) {
            button.className = "mdl-layout__tab";
        });
        tabElement.classList.add('is-active');
        document.querySelectorAll('.mdl-layout__tab-panel').forEach(function(tab) {
            tab.className = "mdl-layout__tab-panel";
        });
        tabElement.tabContent.classList.add('is-active');
        this.currentTab = tabElement;
        this.currentServer = tabElement.dataset["index"];
        this.LoadPlaylistsEvent();
        this.LoadBanListEvent();
        this.LoadVIPListEvent();
        this.LoadServerLogEvent();
        this.LoadServerMessagesEvent();
        this.updateCurrentState();
    }
}

Mentula.prototype.LoadBanListEvent = function() {
    this.serverConnection.server.loadBanListEvent(this.currentServer);
}

Mentula.prototype.UnBanPlayerEvent = function(PlayerName) {
    this.serverConnection.server.unBanPlayerEvent(this.currentServer, PlayerName);
}

Mentula.prototype.BanPlayerEvent = function(PlayerName) {
    this.serverConnection.server.banPlayerEvent(this.currentServer, PlayerName);
}

Mentula.prototype.TimeoutPlayerEvent = function(PlayerName) {
    this.serverConnection.server.timeoutPlayerEvent(this.currentServer, PlayerName);
}

Mentula.prototype.LoadPlaylistsEvent = function() {
    this.serverConnection.server.loadPlaylistsEvent();
}

Mentula.prototype.ChangePlaylistEvent = function(PlayList) {
    this.serverConnection.server.changePlaylistEvent(this.currentServer, PlayList);
}

Mentula.prototype.LoadServerLogEvent = function() {
    this.serverConnection.server.loadServerLogEvent(this.currentServer);
}

Mentula.prototype.RestartServerEvent = function() {
    if (confirm(
        "ARE YOU SURE YOU WANT TO DO THIS, THIS WILL RESTART THE SERVICE. THIS ACTION WILL BE LOGGED")
    ) {
        this.serverConnection.server.restartServerEvent(this.currentServer);
    }
}

Mentula.prototype.StopServerEvent = function() {
    if (confirm(
        "ARE YOU SURE YOU WANT TO DO THIS, IF YOU CLICK OKAY THE SERVER OWNER WILL HAVE TO MANUALLY START THE SERVICE. THIS ACTION WILL BE LOGGED")
    ) {
        this.serverConnection.server.stopServerEvent(this.currentServer);
    }
}

Mentula.prototype.LoadVIPListEvent = function() {
    this.serverConnection.server.loadVIPListEvent(this.currentServer);
}

Mentula.prototype.AddVIPPlayerEvent = function(playerName) {
    this.serverConnection.server.addVIPPlayerEvent(this.currentServer, playerName);
}

Mentula.prototype.RemoveVIPPlayerEvent = function(playerName) {
    this.serverConnection.server.removeVIPPlayerEvent(this.currentServer, playerName);
}

Mentula.prototype.FreezeLobbyEvent = function(state) {
    this.serverConnection.server.freezeLobbyEvent(this.currentServer, state);
}

Mentula.prototype.ForceStartLobbyEvent = function() {
    this.serverConnection.server.forceStartLobbyEvent(this.currentServer);
}

Mentula.prototype.SetPrivacyEvent = function(privacy) {
    this.serverConnection.server.setPrivacyEvent(this.currentServer, privacy);
}

Mentula.prototype.SetForcedBipedEvent = function(biped) {
    this.serverConnection.server.setForcedBipedEvent(this.currentServer, biped);
}

Mentula.prototype.SetMaxPlayersEvent = function(playerCount) {
    this.serverConnection.server.setMaxPlayersEvent(this.currentServer, playerCount);
}

Mentula.prototype.SetXDelayTimer = function(xDelayTime) {
    this.serverConnection.server.setXDelayTimer(this.currentServer, xDelayTime);
}

Mentula.prototype.SetBRFixEvent = function(value) {
    this.serverConnection.server.setBRFixEvent(this.currentServer, value);
}

Mentula.prototype.SetDescriptionEvent = function(value) {
    this.serverConnection.server.setDescriptionEvent(this.currentServer, value);
}

Mentula.prototype.SetAFKTimerEvent = function(value) {
    this.serverConnection.server.setAFKTimerEvent(this.currentServer, value);
}
Mentula.prototype.SetPCRStateEvent = function(value) {
    this.serverConnection.server.setPCRStateEvent(this.currentServer, value);
}

Mentula.prototype.SetSyncProjEvent = function(value) {
    this.serverConnection.server.setSyncProjEvent(this.currentServer, value);
}

Mentula.prototype.AddNewServerMessageEvent = function(message, duration, all) {
    this.serverConnection.server.addNewServerMessageEvent(this.currentServer, message, duration, all);
}

Mentula.prototype.LoadServerMessagesEvent = function() {
    this.serverConnection.server.loadServerMessagesEvent(this.currentServer);
}

Mentula.prototype.DeleteServerMessageEvent = function(message) {
    this.serverConnection.server.deleteServerMessageEvent(this.currentServer, message);
}
/**************************
 *       CALL BACKS       *
 **************************/

Mentula.prototype.LoginCallback = function (result, Token) {
    if (result === "Success") {
        this.serverConnection.qs = { "Token": Token };
        this.serverConnection.server.getServerListEvent();
    } else {
        alert("Login Failed, Refresh page to try again.");
    }
}

Mentula.prototype.GetServerListCallback = function (result) {
    if (window.location.href.indexOf('Block') === -1) { //Used for design testing.
        $('.mdl-layout__tab-bar')[0].innerHTML = "";
        $('.mdl-layout__content')[0].innerHTML = "";
    }
    var servers = JSON.parse(result);
    this.serverCount = servers.length;
    var _this = this;
    servers.forEach(function (server) {
        _this.InitNewServerTab(server);
    });
    $('.mdl-layout__tab:first-of-type').addClass("is-active");
    $('.mdl-layout__tab-panel:first-of-type').addClass("is-active");
    this.tabChangedEvent($('.mdl-layout__tab:first-of-type')[0]);
    componentHandler.upgradeElement(document.body);
};

Mentula.prototype.GetServerStatusCallback = function (result) {
    var status = JSON.parse(result);
    var tabContent = this.currentTab.tabContent;
    if (this.serverCount !== parseInt(status["ServerCount"])) {
        this.serverConnection.server.getServerListEvent();
    } else {
        //Set background of current map
        tabContent.querySelector('.status-card .mdl-card__title').style.background =
            `url(${this.mapImageBaseURI}${status["CurrentMap"]}.png)`;
        //Update current game state and map name
        tabContent.querySelector('.status-card .mdl-card__title .mdl-card__title-text').innerText =
            `${this.extra.localizeStatus(status["GameState"])}: ${this.extra.localizeMapName(status["CurrentMap"])}`;
        //Display current variant
        tabContent.querySelector('[data-elm="currentvariant"]').innerText =
            `${status["CurrentName"]} on ${this.extra.localizeMapName(status["CurrentMap"])}`;
        //Display next variant
        tabContent.querySelector('[data-elm="nextvariant"]').innerText =
            `${status["NextName"]} on ${this.extra.localizeMapName(status["NextMap"])}`;

        tabContent.querySelector('[data-elm="currentplaylist"]').innerText =
            status["CurrentPlaylist"];

        if (tabContent.querySelector('.server-freeze-lobby input').checked !== (status["LobbyRunning"] !== "True")) {
            tabContent.querySelector('.server-freeze-lobby').MaterialSwitch
                .setState(status["LobbyRunning"] !== "True");
        }
        
        if (tabContent.controls.serverPrivacy.MaterialSelect.selectedItem() !== status["Privacy"] &&
            !tabContent.controls.serverPrivacy.isFocused())
            tabContent.controls.serverPrivacy.MaterialSelect.setSelectedItem(status["Privacy"], true);

        if (tabContent.controls.serverBiped.MaterialSelect.selectedItem() !== status["ForcedBiped"] &&
            !tabContent.controls.serverBiped.isFocused())
            tabContent.controls.serverBiped.MaterialSelect.setSelectedItem(status["ForcedBiped"], true);

        if (tabContent.controls.serverMaxPlayers.MaterialSelect.selectedItem() !== status["MaxPlayers"] &&
            !tabContent.controls.serverMaxPlayers.isFocused())
            tabContent.controls.serverMaxPlayers.MaterialSelect.setSelectedItem(status["MaxPlayers"], true);

        if (tabContent.controls.serverXDelayInput.value !== status["XDelayTimer"] &&
            !tabContent.controls.serverXDelay.isFocused())
            tabContent.controls.serverXDelay.MaterialTextfield.change(status["XDelayTimer"]);

        if (tabContent.controls.serverAFKTimerInput.value !== status["AFKTimer"] &&
            !tabContent.controls.serverAFKTimer.isFocused())
            tabContent.controls.serverAFKTimer.MaterialTextfield.change(status["AFKTimer"]);

        if (tabContent.controls.serverBattleRifleInput.value !== status["BRFix"] &&
            !tabContent.controls.serverBattleRifle.isFocused())
            tabContent.controls.serverBattleRifle.MaterialTextfield.change(status["BRFix"]);

        if (tabContent.controls.serverDescriptionInput.value !== status["Description"] &&
            !tabContent.controls.serverDescription.isFocused())
            tabContent.controls.serverDescription.MaterialTextfield.change(status["Description"]);

        if (tabContent.querySelector('.server-pcr input').checked !== (status["PCRState"] !== "True")) {
            tabContent.querySelector('.server-pcr').MaterialSwitch
                .setState(status["PCRState"] !== "True");
        }

        if (tabContent.querySelector('.server-sync-proj input').checked !== (status["SyncProj"] === "True")) {
            tabContent.querySelector('.server-sync-proj').MaterialSwitch
                .setState(status["SyncProj"] === "True");
        }

        tabContent.init = true;
        //Look at this hot garbage... need to do some cleanup and add a element container to tabContent..
        //if (tabContent.querySelector('.server-privacy').MaterialSelect.selectedItem() !== status["Privacy"])
        //    tabContent.querySelector('.server-privacy').MaterialSelect.setSelectedItem(status["Privacy"]);
        //if (tabContent.querySelector('.server-biped').MaterialSelect.selectedItem() !== status["ForcedBiped"])
        //    tabContent.querySelector('.server-biped').MaterialSelect.setSelectedItem(status["ForcedBiped"]);
        //if (tabContent.querySelector('.server-max-players').MaterialSelect.selectedItem() !== status["MaxPlayers"])
        //    tabContent.querySelector('.server-max-players').MaterialSelect.setSelectedItem(status["MaxPlayers"]);
        //if (tabContent.querySelector('.server-xdelay-timer input').value !== status["XDelayTimer"])
        //    tabContent.querySelector('.server-xdelay-timer').MaterialTextfield.change(status["XDelayTimer"]);
        //if (tabContent.querySelector('.server-br-fix input').value !== status["BRFix"])
        //    tabContent.querySelector('.server-br-fix').MaterialTextfield.change(status["BRFix"]);
        //if (tabContent.querySelector('.server-description input').value !== status["Description"])
        //    tabContent.querySelector('.server-description').MaterialTextfield.chnage(status["BRFix"]);
    }
}

Mentula.prototype.GetPlayerListCallback = function (result) {
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var playerTable = currentPanel.querySelector('.player-table tbody');
    playerTable.querySelectorAll('[data-name]').forEach(function(row) {
        row.setAttribute("data-keep", "0");
    });
    var players = JSON.parse(result);
    var _this = this;
    players.forEach(function (player) {
        if (playerTable.querySelector(`[data-name="${player["Name"]}`) === null) {
            var newRow = document.querySelector('#PlayerTableRowTemplate').content.cloneNode(true);
            newRow.querySelectorAll('.mdl-data-table__cell--non-numeric')[0].style.background =
                `url(${player["EmblemURL"]}`;
            newRow.querySelector('.player-row').setAttribute('data-keep', "1");
            newRow.querySelector('.player-row').setAttribute("data-name", player["Name"]);
            newRow.querySelectorAll('.mdl-data-table__cell--non-numeric')[1].innerText = player["Name"];
            newRow.querySelectorAll('.mdl-data-table__cell--non-numeric')[2].innerText = player["Team"];
            newRow.querySelectorAll('.material-icons')[0].addEventListener('click',
                function () {
                    _this.BanPlayerEvent(player["Name"]);
                });
            newRow.querySelectorAll('.material-icons')[1].addEventListener('click',
                function() {
                    _this.KickPlayerEvent(player["Name"]);
                });
            newRow.querySelectorAll('.material-icons')[2].addEventListener('click',
                function() {
                    _this.TimeoutPlayerEvent(player["Name"]);
                });
            playerTable.append(newRow);
        } else {
            playerTable.querySelector(`[data-name="${player["Name"]}`).setAttribute("data-keep", "1");
        }
    });
    playerTable.querySelectorAll('[data-keep="0"]').forEach(function (row) {
        row.outerHTML = "";
    });
};

Mentula.prototype.KickPlayerCallback = function (result) {
    Console.log("Player Kicked Successfully");
}

Mentula.prototype.SkipServerCallback = function (result) {
    Console.log("Server skipped successfully");
}

Mentula.prototype.LoadBanListCallback = function(result) {
    var bannedPlayers = JSON.parse(result);
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var banList = currentPanel.querySelector('.banlist');
    banList.innerHTML = "";
    var _this = this;
    bannedPlayers.forEach(function(bannedPlayer) {
        var newItem = document.querySelector('#ListRowTemplate').content.cloneNode(true);
        newItem.querySelector('.list-name').innerText = bannedPlayer;
        newItem.querySelector('i').addEventListener('click',
            function() {
                _this.UnBanPlayerEvent(bannedPlayer);
            });
        banList.append(newItem);
    });
}

Mentula.prototype.UnBanPlayerCallback = function(result) {
    var _this = this;
    window.setTimeout(function () {
        _this.LoadBanListEvent();
    }, 3000);
}

Mentula.prototype.BanPlayerCallback = function(result) {
    var _this = this;
    window.setTimeout(function () {
        _this.LoadBanListEvent();
    }, 3000);
}

Mentula.prototype.LoadPlayListsCallback = function(result) {
    var playlists = JSON.parse(result);
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var playList = currentPanel.querySelector('.playlist-container ul');
    playList.innerHTML = "";
    var _this = this;
    playlists.forEach(function(playlist) {
        var newItem = document.querySelector('#PlaylistRowTemplate').content.cloneNode(true);
        newItem.querySelector('.mdl-list__item-primary-content').innerText = playlist;
        newItem.querySelector('.mdl-list__item').addEventListener('click',
            function() {
                _this.ChangePlaylistEvent(playlist);
            });
        playList.append(newItem);
    });
}

Mentula.prototype.ChangePlayListCallback = function(result) {
}

Mentula.prototype.LoadServerLogCallback = function(result) {
    var Log = JSON.parse(result);
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var logElement = currentPanel.querySelector('.log');
    logElement.innerHTML = "";
    Log.forEach(function(logLine) {
        var newRow = document.createElement('p');
        newRow.innerText = logLine;
        newRow.className = "log-item";
        logElement.append(newRow);
    });
    logElement.scrollTop = logElement.scrollHeight;
}

Mentula.prototype.StopServerCallback = function(result) {

}

Mentula.prototype.RestartServerCallback = function(result) {

}

Mentula.prototype.LoadVIPListCallback = function(result) {
    var vipPlayers = JSON.parse(result);
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var vipList = currentPanel.querySelector('.viplist');
    vipList.innerHTML = "";
    var _this = this;
    vipPlayers.forEach(function (vipPlayer) {
        var newItem = document.querySelector('#ListRowTemplate').content.cloneNode(true);
        newItem.querySelector('.list-name').innerText = vipPlayer;
        newItem.querySelector('i').addEventListener('click',
            function () {
                _this.RemoveVIPPlayerEvent(vipPlayer);
            });
        vipList.append(newItem);
    });
}
Mentula.prototype.LoadServerMessagesCallback = function(result) {
    var messages = JSON.parse(result);
    var currentPanel = document.querySelector('.mdl-layout__tab-panel.is-active');
    var messageList = currentPanel.querySelector('.messages-container .mdl-list');
    messageList.innerHTML = "";
    var _this = this;
    messages.forEach(function(message) {
        var newItem = document.querySelector('#MessageListTemplate').content.cloneNode(true);
        newItem.querySelectorAll('.mdl-list__item-primary-content span')[0].innerHTML = message["Key"];
        newItem.querySelectorAll('.mdl-list__item-primary-content span')[1].innerHTML = message["Value"];
        newItem.querySelector('a').addEventListener('click',
            function() {
                _this.DeleteServerMessageEvent(message["Key"]);
            });
        messageList.append(newItem);
    });
}
Mentula.prototype.AddVIPPlayerCallback = function(result) {
    var _this = this;
    window.setTimeout(function () {
        _this.LoadVIPListEvent();
    }, 3000);
}

Mentula.prototype.RemoveVIPPlayerCallback = function(result) {
    var _this = this;
    window.setTimeout(function () {
        _this.LoadVIPListEvent();
    }, 3000);
}



HTMLElement.prototype.isFocused = function() {
    return this.classList.contains('is-focused');
}

document.addEventListener('DOMContentLoaded', function () {
    window["Mentula"] = new Mentula();
});