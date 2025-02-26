// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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

            let disabled = "disabled";
            let finalizeButton = $("#btn-finalize-game");
            if (data.isLive && !data.isFinalized && data.period >= 3 && data.awayScore != data.homeScore) {
                if (finalizeButton.hasClass(disabled))
                    finalizeButton.removeClass(disabled);
            } else {
                if (!finalizeButton.hasClass(disabled))
                    finalizeButton.addClass(disabled);
            }

            let nextPeriodButton = $("#btn-next-period");
            if (data.period < 3 || data.awayScore == data.homeScore) {
                if (nextPeriodButton.hasClass(disabled))
                    nextPeriodButton.removeClass(disabled)
            } else {
                if (!nextPeriodButton.hasClass(disabled))
                    nextPeriodButton.addClass(disabled);
            }
        },
        error: function () {
            alert(`Could not update display. Endpoint: ${endpoint}`);
        }
    });
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