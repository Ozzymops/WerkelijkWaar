// Swaps GameInfo screen divs on button press.
var currentScreen = 0;

$('#swapButton').click(function () {
    // Swap variable
    if (currentScreen == 0) {
        currentScreen = 1;
    }
    else {
        currentScreen = 0;
    }

    // Swap elements
    if (currentScreen == 0) {
        // info-1
        document.getElementById("info-1").style.display = "block";
        document.getElementById("info-2").style.display = "none";
        $("#swapButton").prop('value', 'Meer over het spel');
    }
    else {
        // info-2
        document.getElementById("info-1").style.display = "none";
        document.getElementById("info-2").style.display = "block";
        $("#swapButton").prop('value', 'Hoe verloopt het spel?');
    }
});