// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let localDomain = "https://localhost:7210/";
let liveDomain = "https://shl.azurewebsites.net/";

let localAPIDomain = "https://localhost:7008/api/"

function EnableGoToDateButton() {
    $("#btn-go-to-date").removeClass("disabled");
}

function UpdateDisplay(endpoint) {
    $.ajax({
        url: localAPIDomain + endpoint,
        type: "POST",
        success: function (data) {
            $("#away-score").html(data.awayScore);
            $("#home-score").html(data.homeScore);
            $("#current-status").html(data.status);

            let finalizeButton = $("#btn-finalize-game");
            let disabled = "disabled";

            if (data.isLive && !data.isFinalized && data.period >= 3 && data.awayScore != data.homeScore) {
                if (finalizeButton.hasClass(disabled))
                    finalizeButton.removeClass(disabled);
            }
            else {
                if (!finalizeButton.hasClass(disabled))
                    finalizeButton.addClass(disabled);
            }

        },
        error: function () {
            alert(`Could not update display. Endpoint: ${localAPIDomain + endpoint}`);
        }
    });
}

$("#btn-away-goal").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-remove-away-goal").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-next-period").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-previous-period").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-home-goal").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-remove-home-goal").click(function (event) {
    event.preventDefault();

    var url = $(this).data("url");
    UpdateDisplay(url);
});