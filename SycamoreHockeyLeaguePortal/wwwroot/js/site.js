// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let localDomain = "http://localhost:7210/";
let liveDomain = "https://shl.azurewebsites.net/";

function EnableGoToDateButton() {
    $("#btn-go-to-date").removeClass("disabled");
}