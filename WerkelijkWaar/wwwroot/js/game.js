$(document).ready(function () {
    //#region WebSocketManager
    //#region connectionMethods
    // Edit port based on solution settings!
    var connection = new WebSocketManager.Connection("ws://localhost:50001/game");
    connection.enableLogging = false;

    // ~ When client connects with WebSocket
    connection.connectionMethods.onConnected = () => {
        connection.invoke("AddConnection", connection.connectionId);
        getRoomCount();
    }

    // ~ When client disconnects with WebSocket
    connection.connectionMethods.onDisconnected = () => {
        // blank
    }
    //#endregion

    //#region Ping
    // ~ Take ping and send pong
    connection.clientMethods["takePingAndSendPong"] = (socketId) => {
        if (socketId == connection.connectionId) {
            connection.invoke("AddPong", connection.connectionId);
        }
    }
    //#endregion

    //#region Debugging
    // ~ Set status message on specific client (#statusMessage)
    connection.clientMethods["setStateMessage"] = (socketId, message) => {
        if (socketId == connection.connectionId) {
            document.getElementById("statusMessage").innerHTML = message;
        }
    }

    // ~ Get room count and print
    connection.clientMethods["retrieveRoomCount"] = (roomCount) => {
        document.getElementById("rooms").innerHTML = "Kamers online: " + roomCount;
    }

    // ~ Check and print current room state
    connection.clientMethods["checkRoomState"] = (roomCode, state) => {
        if ($roomContent.val() == roomCode) {
            document.getElementById("roomState").innerHTML = state;
        }
    }

    // ~ Check connections and print socket ID + timeouts
    connection.clientMethods["retrieveConnections"] = (connectionList) => {
        var connections = JSON.parse(connectionList);

        $('#ppList').empty();

        for (var x in connections) {
            var tempString = connections[x];
            var tempArray = tempString.split(':|!');

            if (tempArray[0] == connection.connectionId) {
                $('#ppList').append('<li>' + tempArray[0] + " (jij!) - " + tempArray[1] + " timeouts." + '</li>');
            }
            else {
                $('#ppList').append('<li>' + tempArray[0] + " - " + tempArray[1] + " timeouts." + '</li>');
            }
        }
    }
    //#endregion

    //#region Rooms
    // ~ Open up a room and become the owner of the room.
    connection.clientMethods["returnRoomCode"] = (socketId, roomCode) => {
        if (socketId == connection.connectionId) {
            inRoom = true;

            $roomContent.val(roomCode);

            document.getElementById("statusMessage").innerHTML = "Kamer aangemaakt met code '" + roomCode + "' als '" + $userContent.val().trim() + "'. Jij bent de eigenaar van de kamer.";
            document.getElementById("roomState").style.display = "block";

            // Set host buttons
            // document.getElementById("startButton").style.display = "block";

            // Hide navigation
            hideNavigation();
        }
    }

    // ~ Join a open room and become a player.
    connection.clientMethods["joinRoom"] = (socketId, roomCode) => {
        if (socketId == connection.connectionId) {
            inRoom = true;

            document.getElementById("statusMessage").innerHTML = "Kamer met code '" + roomCode + "' ingegaan als '" + $userContent.val().trim() + "'.";
            document.getElementById("roomState").style.display = "block";

            // Set host buttons
            // document.getElementById("startButton").style.display = "none";

            // Hide navigation
            hideNavigation();
        }
    }

    // ~ Leave a room and return to main application.
    connection.clientMethods["leaveRoom"] = (socketId, kicked) => {
        if (socketId == connection.connectionId) {
            inRoom = false;

            if (kicked) {
                document.getElementById("statusMessage").innerHTML = "Je bent door de eigenaar van de kamer verwijderd.";
            }
            else {
                document.getElementById("statusMessage").innerHTML = "Je hebt de kamer verlaten.";
            }

            // Reset screen
            $roomContent.val('');
            document.getElementById("roomState").style.display = "none";

            // Show navigation
            showNavigation();
        }
    }

    // ~ Get list of users inside of room
    connection.clientMethods["retrieveUserList"] = (roomCode, ownerId, userList, withGroups) => {
        if ($roomContent.val() == roomCode) {

            var users = JSON.parse(userList);

            $('#users').empty();

            for (var x in users) {
                var tempString = users[x];
                var tempArray = tempString.split(':|!');

                if (withGroups) {
                    $('#users').append('<li>G' + tempArray[2] + ". " + tempArray[0] + ", S" + tempArray[3] + '.</li>');
                }
                else {
                    if (ownerId == connection.connectionId) {
                        if (tempArray[1] == connection.connectionId) {
                            $('#users').append('<li>' + tempArray[0] + '</li>');
                        }
                        else {
                            var idString = "'" + tempString + "'";
                            $('#users').append('<li>' + tempArray[0] + '<input class="form-check" onClick="$.kickUser(' + idString + ')" type="button" value="Kick" />' + '</li>');
                        }
                    }
                    else {
                        $('#users').append('<li>' + tempArray[0] + '</li>');
                    }
                }
            }
        }
    }
    //#endregion

    //#region Game
    // Start game with current room
    connection.clientMethods["startGame"] = (roomCode, ownerId) => {
        if ($roomContent.val() == roomCode) {
            document.getElementById("statusMessage").innerHTML = "Started game with room '" + $roomContent.val() + "'.";
            document.getElementById("game").style.display = "block";

            // If owner
            if (ownerId == connection.connectionId) {
                document.getElementById("game-owner").style.display = "block";
            }
            // If client
            else {
                document.getElementById("game-client").style.display = "block";
            }
        }
    }
    //#endregion
    //#endregion

    // ------------------------------------------------------------------------- //
    // Functions
    var $userContent = $('#usernameInput');
    var $roomContent = $('#roomInput');
    var inRoom = false;

    // - Create room
    $('#btn-openLobby').click(function () {
        // Refresh connection
        connection.invoke("AddConnection", connection.connectionId);

        var user = $userContent.val().trim();
        console.log("Opened lobby!");

        if (user.length != 0) {
            connection.invoke("CreateRoom", connection.connectionId, user);
        }
    });

    // - Join room
    $('#joinButton').click(function () {
        // Refresh connection
        connection.invoke("AddConnection", connection.connectionId);

        var user = $userContent.val().trim();
        var room = $roomContent.val().trim();

        if (user.length != 0) {
            if (room.length != 0) {
                connection.invoke("JoinRoom", connection.connectionId, user, room);
            }
        }
    });

    // - Leave room
    $('#leaveButton').click(function () {
        var room = $roomContent.val().trim();

        if (room.length != 0) {
            connection.invoke("LeaveRoom", connection.connectionId, room, false);

            $userContent.val('');
            $roomContent.val('');
        }
    });

    // - Start game with current room
    $('#startButton').click(function () {
        var room = $roomContent.val().trim();

        if (room.length != 0) {
            connection.invoke("StartGame", connection.connectionId, room);
        }
    });

    // - Kick user from lobby
    $.kickUser = function (user) {
        var room = $roomContent.val().trim();
        var tempArray = user.split(":|!");

        if (room.length != 0) {
            connection.invoke("LeaveRoom", tempArray[1], room, true);
        }
    }

    // - Get stuff every five seconds
    function repeatStuff() {
        connection.invoke("RetrieveRoomCount");

        setTimeout(repeatStuff, 5000); // repeat every five seconds
    }

    // - Get amount of online rooms
    function getRoomCount() {
        connection.invoke("RetrieveRoomCount");
    }

    // - Get current room state
    function getRoomState() {
        if (inRoom) {
            var room = $roomContent.val().trim();
            connection.invoke("CheckRoomState", room);
        }
    }

    function hideNavigation() {
        document.getElementById("topBar").style.display = "none";
        document.getElementById("sideBar").style.display = "none";
    }

    function showNavigation() {
        document.getElementById("topBar").style.display = "block";
        document.getElementById("sideBar").style.display = "block";
    }

    // - Open websocket connection
    connection.start();
});