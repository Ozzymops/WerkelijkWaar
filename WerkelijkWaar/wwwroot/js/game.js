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
    var storySource = 0;
    var tutorialPage = 1;
    var tickInterval = 0;
    var timer = 0;
    var currentTimer;
    var inRoom = false;

    var myGroup = 0;
    var myCash = 0.00;
    var myFollowers = 0;
    var selectedStory = 0;
    var maxAnswers = 1;
    var selectedAnswer = '';
    var currentStoryList;
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

    // -- Response to RetrieveRootStory
    connection.clientMethods['retrieveRootStory'] = (roomCode, socketId, rootStory) => {
        if (currentRoomCode == roomCode) {
            if (mySocketId == socketId) {
                RetrieveRootStory(rootStory);
            }
        }
    }

    // -- Response to RetrieveWrittenStories
    connection.clientMethods['retrieveWrittenStories'] = (roomCode, stories) => {
        if (currentRoomCode == roomCode) {
            RetrieveWrittenStories(stories);
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

    // -- Response to StartTimer
    connection.clientMethods['startTimer'] = (roomCode, time) => {
        if (currentRoomCode == roomCode) {
            StartTimer(time);
        }
    }

    // -- Response to StopTimer
    connection.clientMethods['stopTimer'] = (roomCode) => {
        if (currentRoomCode == roomCode) {
            StopTimer();
        }
    }

    // -- Response to UpdateScore
    connection.clientMethods['updateScore'] = (roomCode, socketId, result, cash, followers, ranking, end) => {
        if (currentRoomCode == roomCode) {
            if (mySocketId == socketId || roleId == 1) {
                UpdateScore(result, cash, followers, ranking, end);
            }
        }
    }
    // #endregion

    // #region Client Methods
    function HideNavigationBars() {
        $('#html').css('background-color', '#7b6ea4');
        $('#body').css('background-color', '#7b6ea4');
        $('#topBar').css('display', 'none');
        $('#sideBar').css('display', 'none');
        $('#persistentHeader').css('display', 'block');
    }

    function ShowNavigationBars() {
        $('#html').css('background-color', '#f5b91a');
        $('#body').css('background-color', '#f5b91a');
        $('#topBar').css('display', 'block');
        $('#sideBar').css('display', 'block');
        $('#persistentHeader').css('display', 'none');
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
        $('#write-busy').css('display', 'none');
        $('#write-finished').css('display', 'none');
        $('#game-read').css('display', 'none');
        $('#read-busy').css('display', 'none');
        $('#read-finished').css('display', 'none');
        $('#game-results').css('display', 'none');
        $('#game-leaderboard').css('display', 'none');
        $('#leaderboard-final').css('display', 'none');
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
        NextTutorial();

        if (roleId == 1) {
            connection.invoke('SkipTutorial', currentRoomCode);
        }
    }

    function WritePhase() {
        HideAll();

        $('#game-write').css('display', 'block');
        $('#write-busy').css('display', 'block');
    }

    function ReadPhase(gameGroup, currentGroup) {
        HideAll();

        myGroup = gameGroup;

        $('#game-read').css('display', 'block');
        $('#read-busy').css('display', 'block');

        if (myGroup == currentGroup) {
            $('#turnString').css('display', 'block');
            $('#articleString').html('Artikelen:');
            $('#btn-uploadAnswer').css('display', 'none');
            $('#btn-activatePowerup-1').css("display", "none");
            $('#btn-activatePowerup-2').css("display", "none");
            $('#btn-activatePowerup-3').css("display", "none");
            $('#btn-activatePowerup-4').css("display", "none");
            $('#btn-activatePowerup-5').css("display", "block");
        }
        else {
            $('#turnString').css('display', 'none');
            $('#articleString').html('Welk artikel deel je?');
            $('#btn-uploadAnswer').css('display', 'block');
            $('#btn-activatePowerup-1').css("display", "block");
            $('#btn-activatePowerup-2').css("display", "block");
            $('#btn-activatePowerup-3').css("display", "block");
            $('#btn-activatePowerup-4').css("display", "block");
            $('#btn-activatePowerup-5').css("display", "none");
        }
    }

    function StartTimer(time) {
        $('.timerClock').css('display', 'block');
        $('.timerClock').css('color', 'black');
        $('.timerBar').css('display', 'block');
        $('.timerBar').css('color', 'black');

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

            $('.timerClock').html(timerMinutes + ':' + timerSeconds);
            $('.timerBar').css('width', (seconds / maxSeconds * 100) + 'vw');

            if (roleId == 1) {
                if (tickInterval == 0) {
                    if ((seconds / maxSeconds * 100) > 75) {                // > 75%
                        document.getElementById('snd-timer-1').play();
                        tickInterval = 5;
                    }
                    else if ((seconds / maxSeconds * 100) > 50) {           // > 50%
                        document.getElementById('snd-timer-1').play();
                        tickInterval = 3;
                    }
                    else if ((seconds / maxSeconds * 100) > 25) {           // > 25%
                        document.getElementById('snd-timer-2').play();
                        tickInterval = 2;
                    }
                    else if (seconds > 10) {           // > 10%
                        document.getElementById('snd-timer-2').play();
                        tickInterval = 1;
                    }
                    else if (seconds <= 10) {                               // final ten seconds
                        document.getElementById('snd-timer-3').play();
                        tickInterval = 1;
                    }
                }
            }

            if (seconds <= 10) {                                            // final ten seconds
                document.getElementById('snd-timer-3').play();
                tickInterval = 1;

                if (seconds % 2 == 0) {
                    $('.timerClock').css('color', 'red');
                    $('.timerBar').css('background-color', '#f7c747');
                }
                else {
                    $('.timerClock').css('color', 'black');
                    $('.timerBar').css('background-color', '#f7c747');
                }
            }

            tickInterval--;

            if (--timer < 0) {
                tickInterval = 0;
                timer = 0;
                clearInterval(currentTimer);

                if (roleId == 1) {
                    document.getElementById('snd-timer-end').play();
                    connection.invoke("StopGameTimer", userId, currentRoomCode);
                }
            }

        }, 1000);
    }

    function StopTimer() {
        $('.timer').css('display', 'none');
        $('.timer').css('display', 'none');

        timer = 0;
        clearInterval(currentTimer);
    }

    function RetrieveRootStory(rootStory) {
        var storyContent = rootStory.split(':!|');

        storySource = storyContent[0];

        $('#storyTitle').html(storyContent[1]);
        $('#storyText').html(storyContent[2]);
    }

    function RetrieveWrittenStories(stories) {
        $('#storyList').empty();

        currentStoryList = JSON.parse(stories);

        var firstStory = true;
        var indexCount = 0;

        for (var story in currentStoryList) {
            var storyContent = currentStoryList[story].split(':!|');

            // Create button and append to list
            var button = document.createElement('input');
            $(button).addClass('btn-swapStory');
            $(button).prop('id', indexCount);
            $(button).prop('type', 'button');
            $(button).css('margin-bottom', '10px')
            $(button).val(storyContent[1]);

            $('#storyList').append(button);

            button.addEventListener('click', function () {
                SwapStory(this.id);
            });

            if (firstStory) {
                firstStory = false;

                selectedStory = 0;

                $('#readStoryTitle').html(storyContent[1]);
                $('#readStoryText').html(storyContent[2]);
            }

            indexCount++;
        }
    }

    function SwapStory(storyNumber) {
        selectedStory = storyNumber;

        var storyContent = currentStoryList[storyNumber].split(':!|');

        $('#readStoryTitle').html(storyContent[1]);
        $('#readStoryText').html(storyContent[2]);
    }

    function UploadStory() {
        HideAll();

        $('#game-write').css('display', 'block');
        $('#write-finished').css('display', 'block');

        var story = storySource + "_+_" + $('#writtenStoryTitle').val().trim() + "_+_" + $('#writtenStoryText').val().trim();
        connection.invoke('UploadStory', mySocketId, currentRoomCode, story)
    }

    function UploadAnswer() {
        maxAnswers--;

        selectedAnswer += selectedStory;

        if (maxAnswers == 0) {
            HideAll();

            $('#game-read').css('display', 'block');
            $('#read-finished').css('display', 'block');

            connection.invoke('UploadAnswer', mySocketId, currentRoomCode, selectedAnswer);

            maxAnswers = 1;
            selectedAnswer = '';
        }
    }

    function UpdateScore(result, cash, followers, ranking, end) {
        HideAll();

        StopTimer();

        $('#game-results').css('display', 'block');

        if (roleId == 1) {
            // Big leaderboard
            $('#leaderboard-left').empty();
            $('#leaderboard-right').empty();

            var rankList = JSON.parse(ranking);
            var processedRank = 0;

            for (var rank in rankList) {
                var rankContent = rankList[rank].split(':|!');

                processedRank++;

                if (processedRank <= 10) {
                    if (processedRank <= 5) {
                        $('#leaderboard-left').append('<li><p style="color: white;">' + rankContent[0] + '. ' + rankContent[2] + ' - ' + rankContent[3] + ' volgers, €' + rankContent[4] + '-</p></li>')
                    }
                    else {
                        $('#leaderboard-right').append('<li><p style="color: white;">' + rankContent[0] + '. ' + rankContent[2] + ' - ' + rankContent[3] + ' volgers, €' + rankContent[4] + '-</p></li>')
                    }
                }
            }
        }
        else {
            var followerDelta = Math.abs(myFollowers - followers);
            var cashDelta = Math.abs(myCash - cash);

            myFollowers = followers;
            myCash = cash;

            $('#followerCount').html(myFollowers);
            $('#moneyCount').html("€" + myCash + "-");

            // Correct
            if (result) {
                $('#html').css('background-color', 'green');
                $('#body').css('background-color', 'green');
                $('#resultString').html('Geweldig!\nJe krijgt deze ronde ' + followerDelta + ' volgers en €' + cashDelta + '- erbij.');
            }
            // Incorrect
            else {
                $('#html').css('background-color', 'red');
                $('#body').css('background-color', 'red');
                $('#resultString').html('Jammer...\nJe verliest deze ronde ' + followerDelta + ' volgers.');
            }
        }
       
        setTimeout(function () { ShowLeaderboard(ranking, end); }, 5000)
    }

    function ShowLeaderboard(ranking, end) {
        HideAll();

        $('#html').css('background-color', '#f5b91a');
        $('#body').css('background-color', '#f5b91a');

        var rankList = JSON.parse(ranking);

        $('#game-leaderboard').css('display', 'block');

        if (roleId == 1) {
            var rankList = JSON.parse(ranking);

            for (var rank in rankList) {
                var rankContent = rankList[rank].split(':|!');
            }
        }
        else {
            // Only you on screen
            $('#leaderboard-followers').html(myFollowers);
            $('#leaderboard-money').html('€' + myCash + '-');

            for (var rank in rankList) {
                var rankContent = rankList[rank].split(':|!');

                if (mySocketId == rankContent[1]) {
                    $('#rankString').html('Jij bent nummer ' + rankContent[0]);
                }
            }
        }

        if (end) {
            // Show confetti
            $('#leaderboard-final').css('display', 'block');
            // end game button
        }
        else {
            setTimeout(ResetColours, 5000);
        }       
    }

    function ResetColours() {
        $('#html').css('background-color', '#7b6ea4');
        $('#body').css('background-color', '#7b6ea4');

        if (roleId == 1) {
            connection.invoke('GoToReadPhase', userId, currentRoomCode, false);
        }
    }

    function UpdatePowerups(costs) {

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

    $('#btn-uploadStory').click(function () {
        UploadStory();
    });

    $('#btn-uploadAnswer').click(function () {
        UploadAnswer();
    });

    $.kickUser = function (socketId) {
        if (currentRoomCode.length != 0) {
            connection.invoke("LeaveRoom", userId, socketId, currentRoomCode, true);
        }
    }
    // #endregion
});