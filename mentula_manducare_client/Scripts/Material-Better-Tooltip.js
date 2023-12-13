window.addEventListener("load",
    function () {
        document.mdlToolTipUpgraderCount = 0;
        document.upgradeToolTips = function (element) {
            var upgradeableElements = null;
            if (element)
                upgradeableElements = element.querySelectorAll(":not(.autotooltip)[data-mdl-tooltip]");
            else
                upgradeableElements = document.querySelectorAll(":not(.autotooltip)[data-mdl-tooltip]");
            upgradeableElements.forEach(function (Element) {
                var _id = Element.id || "mdl-auto-tooltip-" + document.mdlToolTipUpgraderCount;
                Element.classList.add("autotooltip");
                if (Element.id != _id)
                    Element.id = _id;
                var direction = Element.getAttribute("data-mdl-tooltip-direction") || "bottom";
                document.mdlToolTipUpgraderCount++;
                var newTooltip = document.createElement("div");
                newTooltip.classList.add("mdl-tooltip");
                newTooltip.classList.add("mdl-tooltip--" + direction);
                newTooltip.innerHTML = Element.getAttribute("data-mdl-tooltip");
                newTooltip.setAttribute("data-mdl-for", _id);
                Element.parentNode.appendChild(newTooltip);
                componentHandler.upgradeElement(newTooltip);
            });
        }
        document.upgradeToolTips();
        document.toolobserver = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                for (var i = 0; i < mutation.addedNodes.length; i++) {
                    if (typeof mutation.addedNodes[i].querySelector === "function")
                        document.upgradeToolTips(mutation.addedNodes[i]);
                }
            });
        });
        document.toolobserver.observe(document, { childList: true, subtree: true });


        window.setInterval(function () {
            CleanupToolTips();
        }, 1800000);
    });

function CleanupToolTips() {
    var tools = document.querySelectorAll('.mdl-tooltip');
    tools.forEach(function (tool) {
        if (document.querySelector('#' + tool.dataset["mdlFor"]) === null)
            tool.outerHTML = "";
    });
}