// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let REGULAR_SEASON = "Regular Season";
let PLAYOFFS = "Playoffs";

function EnableGoToDateButton() {
    $("#btn-go-to-date").removeClass("disabled");
}

function UpdateDisplay(endpoint) {
    $.ajax({
        url: endpoint,
        type: "POST",
        success: function (data) {
            $("#away-score").html(data.awayScore);
            $("#home-score").html(data.homeScore);
            $("#current-status").html(data.status);

            let finalizeButton = $("#btn-finalize-game");
            var condition = data.isLive && !data.isFinalized && data.period >= 3 && data.awayScore != data.homeScore;
            EnableOrDisableButton(finalizeButton, condition);

            let nextPeriodButton = $("#btn-next-period");
            condition = (data.period < 3 || data.awayScore == data.homeScore) &&
                ((data.type == REGULAR_SEASON && data.period < 5) || data.type == PLAYOFFS);
            EnableOrDisableButton(nextPeriodButton, condition);

            let awayGoalButton = $("#btn-away-goal");
            let homeGoalButton = $("#btn-home-goal");
            condition = data.period <= 3 || data.awayScore == data.homeScore;
            EnableOrDisableButton(awayGoalButton, condition);
            EnableOrDisableButton(homeGoalButton, condition);
        },
        error: function () {
            alert(`Could not update display. Endpoint: ${endpoint}`);
        }
    });
}

function EnableOrDisableButton(button, shouldEnable) {
    let disabled = "disabled";

    if (shouldEnable) {
        if (button.hasClass(disabled))
            button.removeClass(disabled);
    } else {
        if (!button.hasClass(disabled))
            button.addClass(disabled);
    }
}

$("#btn-away-goal").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-remove-away-goal").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-next-period").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-previous-period").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-home-goal").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-remove-home-goal").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    UpdateDisplay(url);
});

$("#btn-start-or-resume-game").click(function (event) {
    event.preventDefault();

    let url = $(this).data("url");
    $.ajax({
        url: url,
        type: "POST",
        success: function () {
            location.reload();
        },
        error: function () {
            alert(`Could not start or restart the game. Endpoint: ${url}`);
        }
    });
});