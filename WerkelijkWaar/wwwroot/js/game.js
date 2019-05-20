// Start on load
$(document).ready(function () {
    //#region Web sockets
    var connection = new WebSocketManager.Connection("ws://localhost:50001/game");
    connection.enableLogging = false;

    // On connect
    connection.connectionMethods.onConnected = () => {
        // Add connection to global list
        connection.invoke("AddConnection", connection.connectionId);
    }

    // On disconnect
    connection.connectionMethods.onDisconnected = () => {
        // blank
    }
    //#endregion

    //#region Connection
    // Ping to server to inform you're alive
    connection.clientMethods["pingToServer"] = (socketId) => {
        if (socketId == connection.connectionId) {
            connection.invoke("AddPong", connection.connectionId);
        }
    }
    //#endregion

    //#region Communication
    // Set status message
    connection.clientMethods["setStateMessage"] = (socketId, message) => {
        if (socketId == connection.connectionId) {
            document.getElementById("statusMessage").innerHTML = message;
        }
    }
    //#endregion

    //#region Rooms
    // Host a room
    connection.clientMethods["hostRoom"] = (socketId, roomCode) => {
        if (socketId == connection.connectionId) {
            inRoom = true;
            $roomContent.val(roomCode);

            // Hide irrelevant elements
            document.getElementById("game-prep").style.display = "none";
            hideNavigation();

            // Show relevant elements
            document.getElementById("game-connected").style.display = "block";
            document.getElementById("gameCode").innerHTML = "Jouw spelcode is: " + roomCode;
        }
    }

    // Join a room
    connection.clientMethods["joinRoom"] = (socketId, roomCode) => {
        if (socketId == connection.connectionId) {
            inRoom = true;

            document.getElementById("statusMessage").innerHTML = "Kamer met code '" + roomCode + "' ingegaan als '" + $userContent.val().trim() + "'.";

            // Hide irrelevant elements
            document.getElementById("game-prep").style.display = "none";
            hideNavigation();

            // Show relevant elements
            document.getElementById("game-connected").style.display = "block";
        }
    }

    // Leave a room
    connection.clientMethods["leaveRoom"] = (socketId, kicked) => {
        if (socketId == connection.connectionId) {
            inRoom = false;

            if (kicked) {
                document.getElementById("statusMessage").innerHTML = "Je bent door de eigenaar van de kamer verwijderd.";
            }

            // Hide irrelevant elements
            document.getElementById("game-connected").style.display = "none";
            $roomContent.val('');

            // Show relevant elements
            document.getElementById("game-prep").style.display = "block";
            showNavigation();
        }
    }

    // Retrieve player list
    connection.clientMethods["retrievePlayerList"] = (roomCode, ownerId, playerList, withGroups) => {
        if ($roomContent.val() == roomCode) {
            var players = JSON.parse(playerList);

            $('#playerList').empty();

            for (var p in players) {
                var tempString = players[p];
                var tempArray = tempString.split(':|!');

                if (withGroups) {
                    $('#playerList').append('<li>G' + tempArray[2] + ". " + tempArray[0] + ", S" + tempArray[3] + '.</li>');
                }
                else {
                    if (ownerId == connection.connectionId) {
                        if (tempArray[1] == connection.connectionId) {
                            $('#playerList').append('<li>' + tempArray[0] + '</li>');
                        }
                        else {
                            var idString = "'" + tempString + "'";
                            $('#playerList').append('<li>' + tempArray[0] + '<input class="form-check" onClick="$.kickUser(' + idString + ')" type="button" value="Kick" />' + '</li>');
                        }
                    }
                    else {
                        $('#playerList').append('<li>' + tempArray[0] + '</li>');
                    }
                }
            }

            var playerCount = document.getElementById("playerList").getElementsByTagName("li").length;
            document.getElementById("playerCount").innerHTML = playerCount + " spelers zijn verbonden.";
        }
    }
    //#endregion

    //#region Game
    // Start game with current lobby
    connection.clientMethods["startGame"] = (roomCode, ownerId) => {
        if ($roomContent.val() == roomCode) {

            // Hide irrelevant elements
            document.getElementById("game-connected").style.display = "none";

            // Show relevant elements
            document.getElementById("statusMessage").innerHTML = "Started game.";
            document.getElementById("game-tutorial-1").style.display = "block";
        }
    }

    // Continue game from tutorial
    connection.clientMethods["continueGame"] = (roomCode) => {
        if ($roomContent.val() == roomCode) {
            // Hide irrelevant elements
            document.getElementById("game-tutorial-1").style.display = "none";
            document.getElementById("game-tutorial-2").style.display = "none";         

            // Show relevant elements
            document.getElementById("game-write").style.display = "block";
        }
    }

    // Start countdown timer
    connection.clientMethods["startCountdownTimer"] = (roomCode, time) => {
        if ($roomContent.val() == roomCode) {
            startTimer(time);
        }
    }
    //#endregion

    //#region Functions
    // Variables
    var $userContent = $('#usernameInput');
    var $roomContent = $('#roomInput');
    var inRoom = false;

    // Host a room
    $('#btn-openLobby').click(function () {
        // Refresh connection
        connection.invoke("AddConnection", connection.connectionId);

        // Clean and validate
        var user = $userContent.val().trim();

        if (user.length != 0) {
            connection.invoke("HostRoom", connection.connectionId, user);
        }
    });

    // Join a room
    $('#btn-connectLobby').click(function () {
        // Refresh connection
        connection.invoke("AddConnection", connection.connectionId);

        // Clean and validate
        var user = $userContent.val().trim();
        var room = $('#codeInput').val().trim();

        if (user.length != 0) {
            if (room.length != 0) {
                console.log("Connecting!");
                connection.invoke("JoinRoom", connection.connectionId, user, room);
            }
        }
    });

    // Leave a room
    $('#btn-leaveLobby').click(function () {
        // Clean and validate
        var room = $roomContent.val().trim();

        if (room.length != 0) {
            connection.invoke("LeaveRoom", connection.connectionId, room, false);

            // Clear values
            $roomContent.val('');
        }
    });

    // Start game with current lobby
    $('#btn-startGame').click(function () {
        // Clean and validate
        var room = $roomContent.val().trim();

        if (room.length != 0) {
            connection.invoke("StartGame", connection.connectionId, room);
        }
    });

    // Kick user from current lobby
    $.kickUser = function (user) {
        // Clean and validate
        var room = $roomContent.val().trim();
        var tempArray = user.split(":|!");

        if (room.length != 0) {
            connection.invoke("LeaveRoom", tempArray[1], room, true);
        }
    }

    // Continue tutorial
    $('#btn-nextTutorial').click(function () {
        nextTutorialPage();
    });

    // End tutorial
    $('#btn-endTutorial').click(function () {
        endTutorial();
    });

    // Start timer
    function startTimer(duration) {
        var maxSeconds = duration;
        var timer = duration, seconds;

        setInterval(function () {
            seconds = timer;
            document.getElementById("clock").textContent = seconds;
            document.getElementById("clockBar").style.width = (seconds / maxSeconds * 100) + "vw";

            if (--timer < 0) {
                timer = duration;
            }

        }, 1000);
    }

    function hideNavigation() {
        document.getElementById("topBar").style.display = "none";
        document.getElementById("sideBar").style.display = "none";
    }

    function showNavigation() {
        document.getElementById("topBar").style.display = "block";
        document.getElementById("sideBar").style.display = "block";
    }

    function nextTutorialPage() {
        document.getElementById("game-tutorial-1").style.display = "none";
        document.getElementById("game-tutorial-2").style.display = "block";
    }

    function endTutorial() {
        document.getElementById("game-tutorial-1").style.display = "none";
        document.getElementById("game-tutorial-2").style.display = "none";
        document.getElementById("game-write").style.display = "block";
    }
    //#endregion

    // Start WebSocket connection
    connection.start();
});