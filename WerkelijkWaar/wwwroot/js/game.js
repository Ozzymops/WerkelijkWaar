// -----------------------------------------------------------
// |                   Werkelijk Waar?                       |
// -----------------------------------------------------------
// | Ontwikkeld door Justin Muris, Feel2B: https://feel2b.tv |
// |                        2019                             |
// -----------------------------------------------------------

$(document).ready(function () {
    // #region Variables
    var mySocketId = "";
    var userId = $('#userIdHolder').val().trim();
    var username = $('#usernameHolder').val().trim();
    var currentRoomCode = $('#roomCodeHolder').val().trim();
    var roleId = $('#roleIdHolder').val().trim();
    var storySource = $('#storySourceHolder').val().trim();
    var myGroup = 0;
    var inRoom = false;
    var tutorialPage = 1;
    var tickInterval = 0;
    var timer = 0;
    var currentTimer;
    // #endregion

    // #region WebSockets
    var connection = new WebSocketManager.Connection('ws://localhost:50001/game');
    connection.enableLogging = false;

    // -- On connect: add connection to global list. Also clean current data with .trim()
    connection.connectionMethods.onConnected = () => {
        console.log("Connection established: your socket ID = " + connection.connectionId);
        mySocketId = connection.connectionId;
        connection.invoke('AddConnection', connection.connectionId);
    }

    // -- Start
    connection.start();
    // #endregion

    // #region Connection
    // -- Response to PingToServer: ping to server to inform you're not idle
    connection.clientMethods['pingToServer'] = (socketId) => {
        if (mySocketId == socketId) {
            connection.invoke('AddPong', connection.connectionId);
        }
    }
    // #endregion

    // #region Communication
    // -- Response to SetStateMessage: change statusMessage <p> text
    connection.clientMethods['setStateMessage'] = (socketId, message) => {
        if (mySocketId == socketId) {
            $('#statusMessage').html = message;
        }
    }
    // #endregion

    // #region Rooms
    // -- Response to HostRoom
    connection.clientMethods['hostRoom'] = (socketId, roomCode) => {
        if (mySocketId == socketId) {
            console.log("Loading lobby...");

            HostRoom(roomCode);
        }
    }

    // -- Response to JoinRoom
    connection.clientMethods['joinRoom'] = (socketId, roomCode) => {
        if (mySocketId == socketId) {
            console.log("Loading lobby...");

            JoinRoom(roomCode);
        }
    }

    // -- Response to LeaveRoom
    connection.clientMethods['leaveRoom'] = (socketId, kicked) => {
        if (mySocketId == socketId) {
            console.log("Removed from lobby.");

            LeaveRoom(kicked);
        }
    }

    // -- Response to RetrievePlayerList
    connection.clientMethods['retrievePlayerList'] = (ownerSocketId, roomCode, playerList) => {
        if (currentRoomCode == roomCode) {
            RetrievePlayerList(ownerSocketId, playerList);
        }
    }
    // #endregion

    // #region Game
    // -- Response to StartGame
    connection.clientMethods['startGame'] = (roomCode, gameGroup) => {
        if (currentRoomCode == roomCode) {
            StartGame(gameGroup);
        }
    }

    // -- Response to WritePhase
    connection.clientMethods['writePhase'] = (roomCode) => {
        if (currentRoomCode == roomCode) {
            WritePhase();
        }
    }

    // -- Response to ReadPhase
    connection.clientMethods['readPhase'] = (roomCode, socketId, gameGroup, currentGroup) => {
        if (currentRoomCode == roomCode) {
            if (mySocketId == socketId) {
                ReadPhase(gameGroup, currentGroup);
            }
        }
    }

    connection.clientMethods['startTimer'] = (roomCode, time) => {
        if (currentRoomCode == roomCode) {
            StartTimer(time);
        }
    }

    connection.clientMethods['stopTimer'] = (roomCode) => {
        if (currentRoomCode == roomCode) {
            StopTimer();
        }
    }
    // #endregion

    // #region Client Methods
    function HideNavigationBars() {
        $('#html').css('background-color', '#6ac2c4');
        $('#body').css('background-color', '#6ac2c4');
        $('#topBar').css('display', 'none');
        $('#sideBar').css('display', 'none');
    }

    function ShowNavigationBars() {
        $('#html').css('background-color', '#f5b91a');
        $('#body').css('background-color', '#f5b91a');
        $('#topBar').css('display', 'block');
        $('#sideBar').css('display', 'block');
    }

    function HideAll() {
        $('#statusMessage').css('display', 'none');
        $('#game-prep').css('display', 'none');
        $('#game-connected').css('display', 'none');
        $('#game-tutorial-1').css('display', 'none');
        $('#game-tutorial-2').css('display', 'none');
        $('#game-tutorial-3').css('display', 'none');
        $('#game-write').css('display', 'none');
        $('#game-read').css('display', 'none');
        $('#game-waiting').css('display', 'none');
        $('#game-write').css('display', 'none');
        $('#game-read').css('display', 'none');
        $('#game-leaderboard').css('display', 'none');
    }

    function HostRoom(newRoomCode) {
        inRoom = true;
        currentRoomCode = newRoomCode;
        tutorialPage = 1;

        HideNavigationBars();
        HideAll();

        $('#game-connected').css('display', 'block');
        $('#gameCode').html("Jouw spelcode is: " + currentRoomCode);
    }

    function JoinRoom(newRoomCode) {
        inRoom = true;
        currentRoomCode = newRoomCode;
        tutorialPage = 1;

        HideNavigationBars();
        HideAll();

        $('#game-connected').css('display', 'block');
        $('#statusMessage').css('display', 'block');
        $('#statusMessage').html("Kamer met code " + currentRoomCode + " ingegaan als " + username);
    }

    function LeaveRoom(kicked) {
        if (kicked) {
            $('#statusMessage').html("Je bent uit de kamer verwijderd door de eigenaar.");
        }
        else {
            $('#statusMessage').html("Je bent de kamer verlaten.");
        }

        currentRoomCode = "";

        ShowNavigationBars();
        HideAll();

        $('#game-prep').css('display', 'block');
        $('#statusMessage').css('display', 'block');
        $('#personalScore').css('display', 'none');
    }

    function RetrievePlayerList(ownerSocketId, playerList) {
        $('#playerList').empty();

        var nameList = JSON.parse(playerList);

        for (var name in nameList) {
            var nameString = nameList[name].split(':|!');

            if (mySocketId == ownerSocketId) {
                $('#playerList').append('<li>' + nameString[0] + '<input class="form-button-orange" onClick="$.kickUser(' + "'" + nameString[1] + "'" + ')" type="button" value="Kick" style="width: 50px;" />' + '</li>');
            }
            else {
                if (mySocketId == nameString[1]) {
                    $('#playerList').append('<li style="color: red;">' + nameString[0] + '</li>');
                }
                else {
                    $('#playerList').append('<li>' + nameString[0] + '</li>');
                }
            }
        }

        var playerCount = $('#playerList li').length;
        $('#playerCount').html(playerCount + " spelers zijn verbonden.");
    }

    function StartGame(gameGroup) {
        myGroup = gameGroup;

        HideAll();

        $('#game-tutorial-1').css('display', 'block');
        $('#personalScore').css('display', 'block');
    }

    function NextTutorial() {
        HideAll();

        switch (tutorialPage) {
            case 0:
                $('#game-tutorial-1').css('display', 'block');
                break;

            case 1:
                $('#game-tutorial-2').css('display', 'block');
                break;

            case 2:
                $('#game-tutorial-3').css('display', 'block');
                break;

            case 3:
                $('#game-waiting').css('display', 'block');
                if (roleId != 1) {
                    connection.invoke('ReadyUpPlayer', mySocketId, currentRoomCode);
                }
                break;

            default:
                break;
        }

        tutorialPage++;
    }

    function SkipTutorial() {
        tutorialPage = 3;
        nextTutorial();

        if (roleId == 1) {
            connection.invoke('SkipTutorial', currentRoomCode);
        }
    }

    function WritePhase() {
        HideAll();

        $('#game-write').css('display', 'block');
    }

    function ReadPhase(gameGroup, currentGroup) {
        myGroup = gameGroup;

        HideAll();

        $('#game-read').css('display', 'block');

        if (myGroup == currentGroup) {
            $('#turnString').css('display', 'block');
            $('#articleString').html('Artikelen:');
            $('#btn-shareStory').css('display', 'none');
            $('#btn-activatePowerup-1').css("display", "none");
            $('#btn-activatePowerup-2').css("display", "none");
            $('#btn-activatePowerup-3').css("display", "none");
            $('#btn-activatePowerup-4').css("display", "none");
            $('#btn-activatePowerup-5').css("display", "block");
        }
        else {
            $('#turnString').css('display', 'none');
            $('#articleString').html('Welk artikel deel je?');
            $('#btn-shareStory').css('display', 'block');
            $('#btn-activatePowerup-1').css("display", "block");
            $('#btn-activatePowerup-2').css("display", "block");
            $('#btn-activatePowerup-3').css("display", "block");
            $('#btn-activatePowerup-4').css("display", "block");
            $('#btn-activatePowerup-5').css("display", "none");
        }
    }

    function StartTimer(time) {
        $('.timer').css('display', 'block');

        tickInterval = 0;
        clearInterval(currentTimer);

        var maxSeconds = time;
        var seconds = maxSeconds;
        timer = maxSeconds;

        currentTimer = setInterval(function () {
            timerMinutes = parseInt(timer / 60, 10);
            timerMinutes = timerMinutes < 10 ? "0" + timerMinutes : timerMinutes;

            timerSeconds = parseInt(timer % 60, 10);
            timerSeconds = timerSeconds < 10 ? "0" + timerSeconds : timerSeconds;

            seconds = timer;

            $('.timer').html(timerMinutes + ':' + clockSeconds);
            $('.timer').css('width', (seconds / maxSeconds * 100) + 'vw');

            if (roleId == 1) {
                if ((seconds / maxSeconds * 100) > 50 && tickInterval == 0) {                               // halfway through
                    $('#snd-timer-1').play();
                    tickInterval = 5;
                }
                else if ((seconds / maxSeconds * 100) > (120 / maxSeconds * 100) && tickInterval == 0) {    // final two minutes
                    $('#snd-timer-1').play();
                    tickInterval = 2;
                }
                else if ((seconds / maxSeconds * 100) > (60 / maxSeconds * 100) && tickInterval == 0) {     // final minute
                    $('#snd-timer-2').play();
                    tickInterval = 2;
                }
                else if ((seconds / maxSeconds * 100) > (10 / maxSeconds * 100) && tickInterval == 0) {     // final ten seconds
                    $('#snd-timer-3').play();
                    tickInterval = 1;
                }
            }

            tickInterval--;

            if (--timer < 0) {
                tickInterval = 0;
                timer = 0;
                clearInterval(currentTimer);
                connection.invoke("StopGameTimer", userId, currentRoomCode);

                if (roleId == 1) {
                    $('#snd-timer-end').play();
                }
            }

        }, 1000);
    }

    function StopTimer() {
        $('.timer').css('display', 'none');

        timer = 0;
        clearInterval(currentTimer);
    }
    // #endregion

    // #region Buttons/Input
    $('#btn-hostLobby').click(function () {
        connection.invoke('AddConnection', connection.connectionId);

        if (username.length != 0) {
            connection.invoke('HostRoom', userId, mySocketId, username);
        }
    });

    $('#btn-joinLobby').click(function () {
        connection.invoke('AddConnection', mySocketId);

        currentRoomCode = $('#roomCodeInput').val().trim();

        if (username.length != 0 && currentRoomCode.length != 0) {
            connection.invoke('JoinRoom', userId, mySocketId, username, currentRoomCode);
        }
    });

    $('#btn-leaveLobby').click(function () {
        connection.invoke('LeaveRoom', userId, mySocketId, currentRoomCode, false);
    });

    $('#btn-startGame').click(function () {
        connection.invoke('StartGame', mySocketId, currentRoomCode);
    });

    $('.btn-continueTutorial').click(function () {
        NextTutorial();
    });

    $('.btn-skipTutorial').click(function () {
        SkipTutorial();
    });

    $.kickUser = function (socketId) {
        if (currentRoomCode.length != 0) {
            connection.invoke("LeaveRoom", userId, socketId, currentRoomCode, true);
        }
    }
    // #endregion
});