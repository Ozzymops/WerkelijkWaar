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
            document.getElementById("statusMessage").style.display = "block;";
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
                            $('#playerList').append('<li><p>' + tempArray[0] + '<input class="form-button-orange" onClick="$.kickUser(' + idString + ')" type="button" value="Kick" style="width: 50px;" /></p></li>');
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
    connection.clientMethods["startGame"] = (roomCode, gameGroup) => {
        if ($roomContent.val() == roomCode) {

            myGroup = gameGroup;
            console.log(myGroup + " " + gameGroup);

            // Hide irrelevant elements
            document.getElementById("game-connected").style.display = "none";

            // Show relevant elements
            document.getElementById("statusMessage").style.display = "none;";
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

    connection.clientMethods["showLeaderboards"] = (roomCode, rank) => {
        if ($roomContent.val() == roomCode) {
            var ranking = JSON.parse(rank);

            $('#leaderboard').empty();

            for (var r in ranking) {
                var tempString = ranking[r];
                var tempArray = tempString.split(':|!');

                $('#leaderboard').append('<li>'+ tempArray[0] + ". " + tempArray[1] + " - " + tempArray[2] + " volgers, " + tempArray[3] + ' euro</li>');
            }

            showLeaderboards();
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
                var rootStory = story;
                var rootStoryContent = rootStory.split(':!|');

                var rootStoryId = rootStoryContent[0];
                var rootStoryTitle = rootStoryContent[1];
                var rootStoryText = rootStoryContent[2];
                console.log("RootStory: " + rootStoryId + ". " + rootStoryTitle + ": " + rootStoryText);

                currentRootId = rootStoryId;

                $('#storyTitle').html("Titel: " + rootStoryTitle);
                $('#storyText').html(rootStoryText);
            }
        }
    }

    // Show written stories
    connection.clientMethods["showStories"] = (gameGroup, roomCode, stories) => {
        if ($roomContent.val() == roomCode) {
            if (gameGroup != myGroup) {
                $('#storyList').empty();

                var storyCount = 1;
                var buttonCount = 0;
                storyList = JSON.parse(stories);

                for (var story in storyList) {
                    if (storyCount <= 7) {
                        var selectedStory = storyList[story];
                        var selectedStoryContent = selectedStory.split(':!|');

                        var storyId = selectedStoryContent[0];
                        var storyTitle = selectedStoryContent[1];
                        var storyText = selectedStoryContent[2];
                        var storySpot = '#storySpot-' + storyCount;

                        console.log("WrittenStory: " + storyId + ". " + storyTitle + ": " + storyText + " FOR spot " + storySpot);

                        // $(storySpot).prop('value', storyTitle);
                        var newInput = document.createElement("input");
                        $(newInput).val(storyTitle);
                        $(newInput).prop('id', buttonCount);
                        $(newInput).prop('type', 'button');
                        $(newInput).addClass('btn-swapStory');
                        $('#storyList').append(newInput);
                        newInput.addEventListener("click", function () {
                            swapStory(this.id);
                        });
                        // $('#storyList').append('<input id="' + (storyCount-1) + '" class="btn-swapStory" type="button" value="' + storyTitle + '" />');

                        console.log($(storySpot).val());

                        if (storyCount == 1) {
                            $('#readStoryTitle').html("Titel: " + storyTitle);
                            $('#readStoryText').html(storyText);
                        }

                        if ($(storySpot).val() == undefined) {
                            $(storySpot).prop('display', 'none');
                        }

                        storyCount++;
                        buttonCount++;
                    }
                }
            }
            else {
                // show wait screen
            }
        }
    }

    connection.clientMethods["updatePowerups"] = (roomCode, socketId, powerup) => {
        if ($roomContent.val() == roomCode) {
            if (connection.connectionId == socketId) {

                console.log("update powerup " + powerup);

                if (powerup == 1) {
                    answerCount = 2;
                    document.getElementById("powerupFeedback").innerHTML = "Je kan nu twee antwoorden selecteren.";
                }
                else if (powerup == 2) {
                    document.getElementById("powerupFeedback").innerHTML = "Jouw antwoorden tellen nu 2x mee.";      
                }
                else if (powerup == 3) {
                    document.getElementById("powerupFeedback").innerHTML = "50% van de foute antwoorden zijn nu weggestreept.";               
                }
                else if (powerup == 4) {
                    document.getElementById("powerupFeedback").innerHTML = "Je kan nu het aantal stemmen zien.";
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
    var timer = 0;
    var soundState = 0;
    var currentTimer;
    var currentRootId = 0;
    var myGroup = 0;
    var selectedAnswer = "";
    var toSelect = 0;
    var answerCount = 1;

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
    $('.btn-skipTutorial').click(function () {
        connection.invoke("SkipTutorial", $roomContent.val());
    });

    $('#btn-sendStory').click(function () {
        sendStory();
    });

    $('#btn-shareStory').click(function () {
        sendAnswer(); 
    });

    $('#btn-activatePowerup').click(function () {
        if (document.getElementById("powerupBar").style.display == "none") {
            document.getElementById("powerupBar").style.display = "block";
        }
        else {
            document.getElementById("powerupBar").style.display = "none";
        }
    });

    $('#btn-activatePowerup-1').click(function () {
        console.log("powerup 1");
        activatePowerup(1);
    });

    $('#btn-activatePowerup-2').click(function () {
        console.log("powerup 2");
        activatePowerup(2);
    });

    $('#btn-activatePowerup-3').click(function () {
        console.log("powerup 3");
        activatePowerup(3);
    });

    $('#btn-activatePowerup-4').click(function () {
        console.log("powerup 4");
        activatePowerup(4);
    });

    $('#btn-nextRound').click(function () {
        connection.invoke("GoToReadPhase", $roomContent.val(), false);
    });

    // Start timer
    function startTimer(duration) {
        $('.clockBar').css('display', 'block');

        clearInterval(currentTimer);

        var maxSeconds = duration;
        // timer = duration, seconds;
        var seconds = maxSeconds;
        timer = maxSeconds;

        currentTimer = setInterval(function () {
            clockMinutes = parseInt(timer / 60, 10);
            clockSeconds = parseInt(timer % 60, 10);

            clockMinutes = clockMinutes < 10 ? "0" + clockMinutes : clockMinutes;
            clockSeconds = clockSeconds < 10 ? "0" + clockSeconds : clockSeconds;

            seconds = timer;
            $('.clockBar').html(clockMinutes + ':' + clockSeconds);
            $('.clockBar').css('width', (seconds / maxSeconds * 100) + "vw")
            // document.getElementById("clockBar").innerHTML = seconds;
            // document.getElementById("clockBar").style.width = (seconds / maxSeconds * 100) + "vw";

            if ($roleContent.val() == 1) {
                if ((seconds / maxSeconds * 100) > 50 && soundState == 0) {
                    document.getElementById("snd-timer-1").play();
                    soundState = 5;
                }
                else if ((seconds / maxSeconds * 100) > (120 / maxSeconds * 100) && soundState == 0) {
                    document.getElementById("snd-timer-1").play();
                    soundState = 3;
                }
                else if ((seconds / maxSeconds * 100) > (30 / maxSeconds * 100) && soundState == 0) {
                    document.getElementById("snd-timer-2").play();
                    soundState = 3;
                }
                else if (soundState == 0) {
                    document.getElementById("snd-timer-3").play();
                    soundState = 1;
                }

                if (seconds == 120) {
                    document.getElementById("snd-120seconds").play();
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

        }, 1000); // 1000
    }

    // Stop timer
    function stopTimer() {
        $('.clockBar').css('display', 'none');

        timer = 0;
        clearInterval(currentTimer);
    }

    function hideNavigation() {
        document.getElementById("html").style.backgroundColor = "#6ac2c4";
        document.getElementById("body").style.backgroundColor = "#6ac2c4";
        document.getElementById("topBar").style.display = "none";
        document.getElementById("sideBar").style.display = "none";
    }

    function showNavigation() {
        document.getElementById("html").style.backgroundColor = "#f5b91a";
        document.getElementById("body").style.backgroundColor = "#f5b91a";
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
        document.getElementById("game-leaderboard").style.display = "none";
    }

    function startWriting() {
        document.getElementById("game-tutorial-1").style.display = "none";
        document.getElementById("game-tutorial-2").style.display = "none";
        document.getElementById("game-tutorial-3").style.display = "none";
        document.getElementById("game-waiting").style.display = "none";
        document.getElementById("game-write").style.display = "block";
        document.getElementById("game-read").style.display = "none";
        document.getElementById("game-leaderboard").style.display = "none";

        $('#btn-sendStory').prop('disabled', false);
    }

    function sendStory() {
        $('#btn-sendStory').prop('disabled', true);

        var $storySource = $('#storySource').val();
        var $storyTitle = $('#writtenStoryTitle').val();
        var $storyText = $('#writtenStoryText').val();

        document.getElementById("write-busy").style.display = "none";
        document.getElementById("write-finished").style.display = "block";

        var story = $storySource + "_+_" + $storyTitle + "_+_" + $storyText;
        connection.invoke("UploadStory", $roomContent.val(), connection.connectionId, story);
    }

    function swapStory(storyNumber) {
        toSelect = storyNumber;

        var selectedStoryContent = storyList[storyNumber].split(':!|');

        var storyTitle = selectedStoryContent[1];
        var storyText = selectedStoryContent[2];

        $('#readStoryTitle').html(storyTitle);
        $('#readStoryText').html(storyText);
    }

    function sendAnswer() {
        answerCount--;

        selectedAnswer += toSelect;

        if (answerCount <= 0) {
            $('#btn-shareStory').prop('disabled', true);

            document.getElementById("read-busy").style.display = "none";
            document.getElementById("read-finished").style.display = "block";

            console.log("Answered with " + selectedAnswer);
            connection.invoke("UploadAnswer", $roomContent.val(), connection.connectionId, selectedAnswer);
        }
    }

    function activatePowerup(powerup) {
        console.log("powerup yeet " + powerup);

        document.getElementById("btn-activatePowerup").style.display = "block";
        document.getElementById("powerupBar").style.display = "none";

        connection.invoke("ActivatePowerup", $roomContent.val(), connection.connectionId, '' + powerup);
    }

    function showLeaderboards() {
        document.getElementById("game-write").style.display = "none";
        document.getElementById("game-read").style.display = "none";
        document.getElementById("game-leaderboard").style.display = "block";

        $('#btn-shareStory').prop('disabled', false);
        document.getElementById("read-busy").style.display = "block";
        document.getElementById("read-finished").style.display = "none";
        document.getElementById("btn-activatePowerup").style.display = "block";
        document.getElementById("powerupBar").style.display = "none";
    }

    //#endregion

    // Start WebSocket connection
    connection.start();
});