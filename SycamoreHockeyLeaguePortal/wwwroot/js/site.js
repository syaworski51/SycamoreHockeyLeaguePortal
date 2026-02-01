// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const REGULAR_SEASON = "Regular Season";
const PLAYOFFS = "Playoffs";

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

            let isLive = data.liveStatus == "Live";

            const finalizeButton = $("#btn-finalize-game");
            var condition = isLive && data.period >= 3 && data.awayScore != data.homeScore;
            EnableOrDisableButton(finalizeButton, condition);

            const nextPeriodButton = $("#btn-next-period");
            condition = isLive && (data.period < 3 || data.awayScore == data.homeScore) &&
                ((data.type == REGULAR_SEASON && data.period < 5) || data.type == PLAYOFFS);
            EnableOrDisableButton(nextPeriodButton, condition);

            const previousPeriodButton = $("#btn-previous-period");
            EnableOrDisableButton(previousPeriodButton, isLive && data.period > 1);

            const awayGoalButton = $("#btn-away-goal");
            const homeGoalButton = $("#btn-home-goal");
            condition = (isLive && data.period <= 3) || data.awayScore == data.homeScore;
            EnableOrDisableButton(awayGoalButton, condition);
            EnableOrDisableButton(homeGoalButton, condition);

            const removeAwayGoalButton = $("#btn-remove-away-goal");
            const removeHomeGoalButton = $("#btn-remove-home-goal");
            EnableOrDisableButton(removeAwayGoalButton, isLive && data.awayScore > 0);
            EnableOrDisableButton(removeHomeGoalButton, isLive && data.homeScore > 0);
        },
        error: function () {
            alert(`Could not update display. Endpoint: ${endpoint}`);
        }
    });
}

function EnableOrDisableButton(button, shouldEnable) {
    const disabled = "disabled";

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

function AllowDrop(event) {
    event.preventDefault();
    $(event.target).addClass("drop-active");
}

function LeaveDropZone(event) {
    event.preventDefault();
    $(event.target).removeClass("drop-active");
}

function DragTeam(event) {
    event.dataTransfer.setData("text", event.target.id);
}

function DropTeam(event, divisionId) {
    event.preventDefault();

    console.log("Removing \"drop-active\" class..")
    $(event.target).removeClass("drop-active");

    console.log("Appending team to list...")
    var teamData = event.dataTransfer.getData("text");
    console.log(teamData);

    let teamList = document.getElementById(divisionId + "-team-list");
    teamList.appendChild(document.getElementById(teamData));

    let index = teamList.querySelectorAll("input").length;
    let teamInput = document.createElement("input");
    teamInput.type = "hidden";
    teamInput.name = `TeamAlignments["${divisionId.toUpperCase()}"][${index}]`;
    teamInput.value = teamData.toUpperCase();
    teamList.appendChild(teamInput);
    console.log("Added team to hidden list.");
}