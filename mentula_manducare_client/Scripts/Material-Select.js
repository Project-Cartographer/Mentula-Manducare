var MaterialSelect = function MaterialSelect(element) {
    this.element_ = element;
    this.init();
}
window["MaterialSelect"] = MaterialSelect;
MaterialSelect.prototype.Constant_ = {};
MaterialSelect.prototype.cssClasses_ = {
    IS_INVALID: "is-invalid",
    IS_DISABLED: "is-disabled"
};
MaterialSelect.prototype.menuItemOnClick = function (item, noEvent = false) {
    var value = item.dataset["value"] || item.textContent;
    var text = item.textContent.trim();
    if (text !== this.input_.value || this.input_.dataset["value"] === undefined) {
        this.element_.MaterialTextfield.change(text);
        this.input_.dataset["value"] = value;
        this.element_.classList.add('mdl-select-hasValue');
        if (!noEvent) {
            if ("createEvent" in document) {
                var event = document.createEvent("HTMLEvents");
                event.initEvent("change", !1, !0);
                this.MaterialMenu.hide();
                this.element_.dispatchEvent(event);
                this.input_.dispatchEvent(event);
            }
        }
    }
};
MaterialSelect.prototype.filterMenuItems = function (e) {
    //persist menu showing while active
    if (e.keyCode !== 38 && e.keyCode !== 40) {
        if (!this.MaterialMenu.container_.classList.contains(this.MaterialMenu.CssClasses_.IS_VISIBLE))
            this.MaterialMenu.show();
    }

    var value = this.input_.value.toLowerCase();
    var items = this.MaterialMenu.element_.querySelectorAll("li");
    for (var i = 0; i < items.length; i++) {
        var item = items[i];
        if (value !== "") {
            var hide = true;
            if(item.dataset.hasOwnProperty("value"))
                if (item.dataset["value"].toLowerCase().indexOf(value) !== -1)
                    hide = false;
            if (item.textContent.trim().toLowerCase().indexOf(value) !== -1)
                hide = false;
            if (hide)
                item.style.display = "none";
            else
                item.style.display = "block";
        } else {
            item.style.display = "block";
        }
    }
    this.MaterialMenu.hide();
    this.MaterialMenu.show();

};
MaterialSelect.prototype.refreshMenu = function () {
    //this.MaterialMenu.
}
MaterialSelect.prototype.addMenuItems = function (items, hide) {
    var _this = this;
    for (var i = 0; i < items.length; i++) {
        _this.addMenuItem(items[i][0], items[i][1], hide);
    }
}
MaterialSelect.prototype.addMenuItem = function (value, text, hide) {
    var item = document.createElement('li');
    var hide_ = hide || false;
    item.dataset["value"] = value;
    item.innerText = text;
    item.className = "mdl-menu__item";
    this.MaterialMenu.element_.appendChild(item);
    var _this = this;
    item.addEventListener("click", function () {
        _this.menuItemOnClick(item);
    });
    if (!hide_) {
        this.MaterialMenu.hide();
        this.MaterialMenu.init();
        this.MaterialMenu.show();
    }
}
MaterialSelect.prototype.setSelectedItem = function (value, noEvent = false) {
    var _this = this;
    var a = _this.MaterialMenu.element_.querySelectorAll('li');
    for (var i = 0; i < a.length; i++) {
        if (a[i].dataset["value"] === value || value === a[i].textContent)
            _this.menuItemOnClick(a[i], noEvent);
    }
}
MaterialSelect.prototype.selectedItem = function () {
    return this.input_.value;
}
MaterialSelect.prototype.init = function () {
    var _this = this;

    window.setTimeout(function () {
        _this.required = _this.element_.hasAttribute("required");
        _this.dataType = _this.element_.getAttribute("data-type") || "simple";
        _this.dataSource = _this.element_.getAttribute("data-source") || "";
        _this.input_ = _this.element_.MaterialTextfield.input_;
        _this.menuHeight = _this.element_.getAttribute('data-height') || "";
        _this.menuWidth = _this.element_.getAttribute('data-width') || "";

        var arrowLabel = document.createElement("label");
        arrowLabel.setAttribute("for", _this.element_.MaterialTextfield.input_.id);
        arrowLabel.innerHTML = '<i class="mdl-icon-toggle__label material-icons">keyboard_arrow_down</i>';
        _this.element_.insertBefore(arrowLabel, _this.element_.MaterialTextfield.input_.nextSibling);
        var primitiveMenu = _this.element_.querySelector('ul');
        primitiveMenu.className = "mdl-menu mdl-menu--bottom-right mdl-js-menu";
        primitiveMenu.querySelectorAll('li').forEach(function (li) {
            li.className = "mdl-menu__item";
            li.addEventListener("click", function () {
                _this.menuItemOnClick(li);
            })
        });
        primitiveMenu.setAttribute("for", _this.element_.MaterialTextfield.input_.id);
        componentHandler.upgradeElement(primitiveMenu);
        if (_this.menuHeight !== "")
            primitiveMenu.style.height = _this.menuHeight + "px";
        if (_this.menuWidth !== "")
            if (_this.menuWidth === "auto")
                primitiveMenu.style.width = _this.element_.style.width + "px";
            else
                primitiveMenu.style.width = _this.menuWidth + "px";
        _this.MaterialMenu = primitiveMenu.MaterialMenu;

        if (_this.dataType === "simple") {
            if (_this.input_.value !== "") {
                var a = _this.MaterialMenu.element_.querySelectorAll('li');
                for (var i = 0; i < a.length; i++) {
                    if (a[i].dataset["value"] === _this.input_.value || _this.input_.value === a[i].textContent)
                        _this.menuItemOnClick(a[i]);
                }
            }
            _this.input_.addEventListener("keyup",
                function (e) {
                    _this.filterMenuItems(e);
                });
        } else if (_this.dataType === "ajax" && _this.dataSource !== "") {
            _this.NotSearch = new NotSearch("simple", _this.dataSource);
            if (_this.input_.value !== "") {
                _this.NotSearch.preformSearch(_this.input_.value,
                    function (items) {
                        _this.addMenuItems(items, true);

                        var a = _this.MaterialMenu.element_.querySelectorAll('li');
                        for (var i = 0; i < a.length; i++) {
                            if (a[i].dataset["value"] === _this.input_.value || _this.input_.value === a[i].textContent)
                                _this.menuItemOnClick(a[i]);
                        }
                        window.setTimeout(function () { _this.MaterialMenu.hide(); }, 260);

                    });

            }
            _this.input_.addEventListener("keyup",
                function (e) {
                    if (_this.input_.value !== "") {
                        _this.MaterialMenu.element_.innerHTML = "";
                        _this.NotSearch.preformSearch(_this.input_.value,
                            function (items) {
                                _this.addMenuItems(items);
                            });
                    }
                });
        }

    });
}
document.addEventListener("DOMContentLoaded",
    function () {
        componentHandler.register({
            constructor: MaterialSelect,
            classAsString: 'MaterialSelect',
            cssClass: 'mdl-js-select',
            widget: true
        });
    });