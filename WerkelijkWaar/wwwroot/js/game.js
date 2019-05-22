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
            $roomContent.val(roomCode);

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
    connection.clientMethods["startGame"] = (roomCode) => {
        if ($roomContent.val() == roomCode) {

            // Hide irrelevant elements
            document.getElementById("game-connected").style.display = "none";

            // Show relevant elements
            document.getElementById("statusMessage").innerHTML = "Started game.";
            document.getElementById("game-tutorial-1").style.display = "block";

            // Debug
            document.getElementById("game-tutorial-2").style.display = "none";
            document.getElementById("game-tutorial-3").style.display = "none";
            document.getElementById("game-write").style.display = "none";
            document.getElementById("game-waiting").style.display = "none";
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

            // Countdown timer for writing
            connection.invoke("StartGameTimer", roomCode, 60, connection.connectionId);
        }
    }

    connection.clientMethods["goToWritePhase"] = (roomCode) => {
        if ($roomContent.val() == roomCode) {
            startWriting();
        }
    }

    connection.clientMethods["goToReadPhase"] = (roomCode) => {
        if ($roomContent.val() == roomCode) {
            startReading();
        }
    }

    // Start countdown timer
    connection.clientMethods["startCountdownTimer"] = (roomCode, time) => {
        if ($roomContent.val() == roomCode) {
            startTimer(time);
        }
    }

    // Stop countdown timer
    connection.clientMethods["stopCountdownTimer"] = (roomCode) => {
        if ($roomContent.val() == roomCode) {
            stopTimer();
        }
    }

    // Retrieve root stories
    connection.clientMethods["retrieveRootStory"] = (roomCode, socketId, story) => {
        if ($roomContent.val() == roomCode) {
            if (socketId == connection.connectionId) {
                var tempStory = story;
                var tempArray = tempStory.split(":!|");

                currentStory = tempArray[0];
                document.getElementById("storyTitle").innerHTML = tempArray[1];
                document.getElementById("storyText").innerHTML = tempArray[2];
            }
        }
    }

    // Show written stories
    connection.clientMethods["showStories"] = (socketId, roomCode, stories) => {
        if (socketId == connection.connectionId) {
            if ($roomContent.val() == roomCode) {
                var stories = JSON.parse(stories);

                $('#stories').empty();

                var storyCount = 1;

                for (var s in stories) {
                    if (storyCount < 6) {
                        var tempString = stories[s];
                        var tempArray = tempString.split(':|!');

                        // 0 = id, 1 = title, 2 = story
                        //$('#stories').append('<li>' + tempArray[0] + " - " + + tempArray[1] + ": " + tempArray[2] + '</li>');
                        var contentString = tempArray[0] + ". " + tempArray[1] + ": " + tempArray[2];
                        console.Log(contentString);

                        var paragraphToEdit = "storySpot-" + storyCount;
                        var buttonToEdit = "#storyButton-" + storyCount;

                        document.getElementById(paragraphToEdit).innerHTML = contentString;
                        $(buttonToEdit).prop('value', tempArray[1]);
                        storyCount++;
                    }
                }
            }
        }
    }
    //#endregion

    //#region Functions
    // Variables
    var $userContent = $('#usernameInput');
    var $roomContent = $('#roomInput');
    var $roleContent = $('#roleId');
    var tutorialState = 0;
    var inRoom = false;
    var timer = 0;
    var soundState = 0;
    var currentTimer;
    var currentStory = 0;

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
                connection.invoke("JoinRoom", connection.connectionId, $('#idInput').val(), user, room);
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
    $('.btn-nextTutorial').click(function () {
        tutorialPage();
    });

    // Skip tutorial
    $('#btn-skipTutorial').click(function () {
        connection.invoke("SkipTutorial", $roomContent.val());
    });

    $('#btn-sendStory').click(function () {
        sendStory();
    });

    // Start timer
    function startTimer(duration) {
        document.getElementById("clockBar").style.display = "block";

        clearInterval(currentTimer);

        var maxSeconds = duration;
        // timer = duration, seconds;
        var seconds = maxSeconds;
        timer = maxSeconds;

        currentTimer = setInterval(function () {
            seconds = timer;
            document.getElementById("clock").textContent = seconds;
            document.getElementById("clockBar").style.width = (seconds / maxSeconds * 100) + "vw";

            if ($roleContent.val() == 1) {
                if ((seconds / maxSeconds * 100) > 25 && soundState == 0) {
                    document.getElementById("snd-timer-1").play();
                    soundState = 5;
                }
                else if ((seconds / maxSeconds * 100) > 10 && soundState == 0) {
                    document.getElementById("snd-timer-2").play();
                    soundState = 3;
                }
                else if (soundState == 0) {
                    document.getElementById("snd-timer-3").play();
                    soundState = 1;
                }     
            }

            soundState--;

            if (--timer < 0) {
                soundState = -1;
                timer = 0;
                clearInterval(currentTimer);
                connection.invoke("StopGameTimer", $roomContent.val(), connection.connectionId);

                if ($roleContent.val() == 1) {
                    document.getElementById("snd-timesup").play();
                }
            }

        }, 1000);
    }

    // Stop timer
    function stopTimer() {
        document.getElementById("clockBar").style.display = "none";

        timer = 0;
        clearInterval(currentTimer);
    }

    function hideNavigation() {
        document.getElementById("topBar").style.display = "none";
        document.getElementById("sideBar").style.display = "none";
    }

    function showNavigation() {
        document.getElementById("topBar").style.display = "block";
        document.getElementById("sideBar").style.display = "block";
    }

    function tutorialPage() {
        console.log("current tutorial state: " + tutorialState);

        tutorialState += 1;

        // pg 2
        if (tutorialState == 1) {
            document.getElementById("game-tutorial-1").style.display = "none";
            document.getElementById("game-tutorial-2").style.display = "block";
            document.getElementById("game-tutorial-3").style.display = "none";
            document.getElementById("game-waiting").style.display = "none";
        }

        // pg 3
        if (tutorialState == 2) {
            document.getElementById("game-tutorial-1").style.display = "none";
            document.getElementById("game-tutorial-2").style.display = "none";
            document.getElementById("game-tutorial-3").style.display = "block";
            document.getElementById("game-waiting").style.display = "none";
        }

        // end
        if (tutorialState == 3) {
            document.getElementById("game-tutorial-1").style.display = "none";
            document.getElementById("game-tutorial-2").style.display = "none";
            document.getElementById("game-tutorial-3").style.display = "none";
            document.getElementById("game-waiting").style.display = "block";

            connection.invoke("ReadyUpPlayer", connection.connectionId, $roomContent.val());
        }
    }

    function startReading() {
        document.getElementById("game-tutorial-1").style.display = "none";
        document.getElementById("game-tutorial-2").style.display = "none";
        document.getElementById("game-tutorial-3").style.display = "none";
        document.getElementById("game-waiting").style.display = "none";
        document.getElementById("game-write").style.display = "none";
        document.getElementById("game-read").style.display = "block";
    }

    function startWriting() {
        document.getElementById("game-tutorial-1").style.display = "none";
        document.getElementById("game-tutorial-2").style.display = "none";
        document.getElementById("game-tutorial-3").style.display = "none";
        document.getElementById("game-waiting").style.display = "none";
        document.getElementById("game-write").style.display = "block";
        document.getElementById("game-read").style.display = "none";
    }

    function sendStory() {
        var $storySource = $('#storySource').val();
        var $storyTitle = $('#writtenStoryTitle').val();
        var $storyText = $('#writtenStoryText').val();

        document.getElementById("write-busy").style.display = "none";
        document.getElementById("write-finished").style.display = "block";

        console.log($storySource);
        console.log($storyTitle);
        console.log($storyText);

        var story = $storySource + "_+_" + $storyTitle + "_+_" + $storyText;
        connection.invoke("UploadStory", $roomContent.val(), connection.connectionId, story);
    }
    //#endregion

    // Start WebSocket connection
    connection.start();
});