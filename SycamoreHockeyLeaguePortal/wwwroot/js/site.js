// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let localDomain = "http://localhost:7210/";
let liveDomain = "https://shl.azurewebsites.net/";

function UpdateField(field, newValue) {
    $(field).innerText = newValue;
}

function UpdateGameNotes(notes) {
    fetch(localDomain + `Schedule/Details?id=${gameId}`)
        .then(response => {
            return response.json();
        })
        .then(data => {

        })

    UpdateField("#game-notes", notes);
}

function UpdateAwayScore(score, gameId) {
    fetch(localDomain + `Schedule/Details?id=${gameId}`)
        .then(response => {
            return response.json();
        })
        .then(data => {
            let awayScore = data["AwayScore"];
            awayScore++;
        })

    UpdateField("#away-score", score);
}

function UpdateHomeScore(score) {
    UpdateField("#home-score", score);
}

function UpdateStatus(status) {
    UpdateField("#game-status", status);
}

$("#conference-tabs li").on("click", function () {
    $(".nav .nav-pills li").removeClass("active");
    $(this).addClass("active");
});