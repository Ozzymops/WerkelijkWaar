// -----------------------------------------------------------
// |                   Werkelijk Waar?                       |
// -----------------------------------------------------------
// | Ontwikkeld door Justin Muris, Feel2B: https://feel2b.tv |
// |                        2019                             |
// -----------------------------------------------------------

$(document).ready(function () {
    // #region Variables
    var myId;
    var userId = $('#userIdHolder').val().trim();
    var username = $('#usernameHolder').val().trim();
    var currentRoomCode = $('#roomCodeHolder').val().trim();
    var roleId = $('#roleIdHolder').val().trim();
    var storySource = 0;
    var tutorialPage = 1;
    var tickInterval = 0;
    var timer = 0;
    var currentTimer;
    var powerupDrawer = false;

    var myGroup = 0;
    var myCash = 0.00;
    var myFollowers = 0;
    var selectedStory = 0;
    var maxAnswers = 1;
    var selectedAnswer = '';
    var currentStoryList;
    var currentGroup = '';
    // #endregion

    // #region SignalR
    var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
    connection.serverTimeoutInMilliseconds = 120000; // 120 second?

    // -- On connect: add connection to global list.
    connection.start().then(function () {
        
    })
    // #endregion

    // #region Connection
    // -- Response to RetrieveConnectionId: set your connection ID
    connection.on('retrieveConnectionId', function (connectionId) {
        myId = connectionId;

        $('#connectionIdHolder').html("CID: " + myId);
    })

    // -- Response to PingToServer: ping to server to inform you're not idle
    connection.on('pingToServer', function (connectionId) {
        if (myId == connectionId) {
            connection.invoke('AddPong', myId);
        }
    })
    // #endregion

    // #region Communication
    // -- Response to SetStateMessage: change statusMessage <p> text
    connection.on('setStateMessage', function (connectionId, message) {
        if (myId == connectionId) {
            $('#statusMessage').html = message;
        }
    })
    // #endregion

    // #region Rooms
    // -- Response to HostRoom
    connection.on('hostRoom', function (roomCode) {
        HostRoom(roomCode);
    })

    // -- Response to JoinRoom
    connection.on('joinRoom', function (roomCode) {
        JoinRoom(roomCode);
    })

    // -- Response to RetrievePlayerList
    connection.on('retrievePlayerList', function (ownerConnectionId, roomCode, playerList) {
        if (currentRoomCode == roomCode) {
            RetrievePlayerList(ownerConnectionId, playerList);
        }
    })

    // -- Response to LeaveRoom
    connection.on('leaveRoom', function (connectionId, kicked) {
        console.log('Targeted connection ID: ' + connectionId);
        console.log('My connection ID: ' + myId);

        if (myId == connectionId) {
            console.log("Removed from lobby.");

            LeaveRoom(kicked);
        }
    })
    // #endregion

    // #region Visuals
    // -- Response to RetrieveConfigurationDataForTutorial
    connection.on('retrieveConfigurationDataForTutorial', function (roomCode, followersPerAnswer, followersPerVote, followersLost, moneyPerFollower) {
        if (currentRoomCode == roomCode) {
            RetrieveConfigurationDataForTutorial(followersPerAnswer, followersPerVote, followersLost, moneyPerFollower);
        }
    })
    // #endregion

    // #region Game
    // -- Response to StartGame
    connection.on('startGame', function (roomCode, gameGroup) {
        if (currentRoomCode == roomCode) {
            StartGame(gameGroup);
        }
    })

    // -- Response to WritePhase
    connection.on('writePhase', function (roomCode) {
        if (currentRoomCode == roomCode) {
            WritePhase();
        }
    })

    // -- Response to RetrieveRootStory
    connection.on('retrieveRootStory', function (roomCode, socketId, rootStory) {
        if (currentRoomCode == roomCode) {
            if (myId == socketId) {
                RetrieveRootStory(rootStory);
            }
        }
    })

    // -- Response to RetrieveWrittenStories
    connection.on('retrieveWrittenStories', function (roomCode, stories) {
        if (currentRoomCode == roomCode) {
            RetrieveWrittenStories(stories);
        }
    })

    // -- Response to ReadPhase
    connection.on('readPhase', function (roomCode, socketId, gameGroup, thisGroup) {
        if (currentRoomCode == roomCode) {
            if (myId == socketId) {
                ReadPhase(gameGroup, thisGroup);
            }
        }
    })

    // -- Response to StartTimer
    connection.on('startTimer', function (roomCode, time) {
        if (currentRoomCode == roomCode) {
            StartTimer(time);
        }
    })

    // -- Response to StopTimer
    connection.on('stopTimer', function (roomCode) {
        if (currentRoomCode == roomCode) {
            StopTimer();
        }
    })

    // -- Response to UpdateScore
    connection.on('updateScore', function (roomCode, socketId, result, cash, followers, followerChange, cashGain, ranking, end, allowed) {
        if (currentRoomCode == roomCode) {
            if (myId == socketId || roleId == 1) {
                UpdateScore(socketId, result, cash, followers, followerChange, cashGain, ranking, end, allowed);
            }
        }
    })

    // -- Response to PowerupVisuals
    connection.on('powerupVisuals', function (roomCode, powerupsAllowed, powerupsCosts) {
        if (currentRoomCode == roomCode) {
            UpdatePowerups(powerupsAllowed, powerupsCosts);
        }
    })

    // -- Response to ActivatePowerup
    connection.on('activatePowerup', function (socketId, roomCode, powerup, cash) {
        if (currentRoomCode == roomCode) {
            if (myId == socketId) {
                ActivatePowerup(powerup, cash);
            }
        }
    })

    // -- Response to CrossOutWrongAnswers
    connection.on('crossOutWrongAnswers', function (socketId, roomCode, answers) {
        if (currentRoomCode == roomCode) {
            if (myId == socketId) {
                CrossOutWrongAnswers(answers);
            }
        }
    })
    // #endregion

    // #region JavaScript functions
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
        $('#game-end').css('display', 'none');

        // reset power-ups
        $('#btn-activatePowerup-1').css("display", "block");
        $('#btn-activatePowerup-2').css("display", "block");
        $('#btn-activatePowerup-3').css("display", "block");
        $('#btn-activatePowerup-4').css("display", "block");
        $('#btn-activatePowerup-5').css("display", "block");
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

        // Reset stats
        myFollowers = 0;
        myCash = 0;

        $('#game-prep').css('display', 'block');
        $('#cornerMascot').css('display', 'block');
        $('#statusMessage').css('display', 'block');
    }

    function RetrieveConfigurationDataForTutorial(followersGainedForRightAnswer, followersGainedPerVote, followersLostForWrongAnswer, moneyGainedPerFollower) {
        $('#followersGainedForRightAnswerString').html('+' + followersGainedForRightAnswer);
        $('#followersGainedPerVoteString').html('+' + followersGainedPerVote + ' per stem.');
        $('#followersLostForWrongAnswerString').html('-' + followersLostForWrongAnswer);
        $('#moneyGainedPerFollowerString').html('= €' + moneyGainedPerFollower + '-');
    }

    function RetrievePlayerList(ownerConnectionId, playerList) {
        console.log("Owner connection ID = " + ownerConnectionId);
        console.log("My connection ID = " + myId);

        $('#playerList').empty();

        var nameList = JSON.parse(playerList);

        for (var name in nameList) {
            var nameString = nameList[name].split(':|!');

            if (myId == ownerConnectionId) {
                console.log("Connection ID matched!");
                $('#playerList').append('<li>' + nameString[0] + '<input class="form-button-orange" onClick="$.kickUser(' + "'" + nameString[1] + "'" + ')" type="button" value="Kick" style="width: 50px; height: 30px;" />' + '</li>');
            }
            else {
                console.log("Connection ID did not match.");

                if (myId == nameString[1]) {
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
                    connection.invoke('ReadyUpPlayer', myId, currentRoomCode);
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

        // Reset writing fields
        $('#writtenStoryTitle').val('');
        $('#writtenStoryText').val('');

        // Reset power-up drawer
        powerupDrawer = false;

        $('#game-write').css('display', 'block');
        $('#write-busy').css('display', 'block');
    }

    function ReadPhase(gameGroup, thisGroup) {
        HideAll();

        myGroup = gameGroup;
        currentGroup = thisGroup;

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
        $('.game.timer-clock').css('display', 'block');
        $('.game.timer-clock').css('color', 'black');
        $('.game.timer-bar').css('display', 'block');
        $('.game.timer-bar').css('color', '#f7c747');
        $('#waitingForStories').css('color', 'black');
        $('#waitingForAnswers').css('color', 'black');

        tickInterval = 0;
        clearInterval(currentTimer);

        var maxSeconds = time;
        var seconds = maxSeconds;
        timer = maxSeconds;

        if (roleId == 1) {
            document.getElementById('snd-start').play();
        }

        currentTimer = setInterval(function () {
            timerMinutes = parseInt(timer / 60, 10);
            timerMinutes = timerMinutes < 10 ? "0" + timerMinutes : timerMinutes;

            timerSeconds = parseInt(timer % 60, 10);
            timerSeconds = timerSeconds < 10 ? "0" + timerSeconds : timerSeconds;

            seconds = timer;

            $('.game.timer-clock').html(timerMinutes + ':' + timerSeconds);
            $('.game.timer-bar').css('width', (seconds / maxSeconds * 100) + 'vw');

            if (roleId == 1) {
                if (tickInterval == 0) {
                    if ((seconds / maxSeconds * 100) > 75) {                // > 75%
                        document.getElementById('snd-timer-1').play();
                        tickInterval = 5;
                    }
                    else if ((seconds / maxSeconds * 100) > 50) {           // > 50%
                        document.getElementById('snd-timer-2').play();
                        tickInterval = 3;
                    }
                    else if (seconds > 30) {                                // > 30 seconds
                        document.getElementById('snd-timer-3').play();
                        tickInterval = 1;
                    }
                    else if (seconds <= 30) {                               // final 30 seconds
                        document.getElementById('snd-timer-4').play();
                        tickInterval = 1;
                    }
                }
            }

            if (seconds <= 30) {
                if (seconds % 2 == 0) {
                    $('.game.timer-clock').css('color', 'red');
                    $('.game.timer-bar').css('background-color', '#f7c747');

                    if (roleId == 1) {
                        $('#waitingForStories').css('color', 'red');
                        $('#waitingForAnswers').css('color', 'red');
                    }
                }
                else {
                    $('.game.timer-clock').css('color', 'black');
                    $('.game.timer-bar').css('background-color', 'red');

                    if (roleId == 1) {
                        $('#waitingForStories').css('color', 'black');
                        $('#waitingForAnswers').css('color', 'black');
                    }
                }
            }

            tickInterval--;

            if (--timer < 0) {
                tickInterval = 0;
                timer = 0;
                clearInterval(currentTimer);

                if (roleId == 1) {
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
        selectedStory = 0;

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
        connection.invoke('UploadStory', myId, currentRoomCode, story);
    }

    function UploadAnswer() {
        maxAnswers--;

        selectedAnswer += selectedStory;

        if (maxAnswers == 0) {
            HideAll();

            $('#game-read').css('display', 'block');
            $('#read-finished').css('display', 'block');

            connection.invoke('UploadAnswer', myId, currentRoomCode, selectedAnswer);

            maxAnswers = 1;
            selectedAnswer = '';
        }
    }

    function UpdateScore(connectionId, result, cash, followers, followerChange, cashGain, ranking, end, allowed) {
        HideAll();

        StopTimer();

        $('#game-results').css('display', 'block');

        if (roleId == 1) {
            // Big leaderboard
            $('#leaderboard-left').empty();
            $('#leaderboard-right').empty();

            document.getElementById('snd-end').play();

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
            myFollowers = followers;
            myCash = cash;

            $('#followerCount').html(myFollowers);
            $('#moneyCount').html("€" + myCash + "-");

            // Correct
            if (result) {
                $('#html').css('background-color', 'green');
                $('#body').css('background-color', 'green');
                $('#resultString').html('Geweldig!\nJe krijgt deze ronde ' + followerChange + ' volgers en €' + cashGain + '- erbij.\nJe hebt nu ' + myFollowers + ' volgers en €' + myCash + ',-');
            }
            // Incorrect
            else {
                $('#html').css('background-color', 'red');
                $('#body').css('background-color', 'red');
                $('#resultString').html('Jammer...\nJe verliest deze ronde ' + followerChange + ' volgers en krijgt €' + cashGain + ' erbij.\nJe hebt nu ' + myFollowers + ' volgers en €' + myCash + ',-');
            }
        }

        if (allowed || roleId == 0) {
            setTimeout(function () { ShowLeaderboard(connectionId, ranking, end); }, 5000);
        }
    }

    function ShowLeaderboard(connectionId, ranking, end) {
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

                if (myId == rankContent[1]) {
                    $('#rankString').html('Jij bent nummer ' + rankContent[0]);
                }
            }
        }

        if (end) {
            // Show confetti
            $('#leaderboard-final').css('display', 'block');
            // end game button
            setTimeout(EndGame, 10000);
        }
        else {
            setTimeout(function () { ResetColours(connectionId); }, 5000);
        }
    }

    function ResetColours(connectionId) {
        $('#html').css('background-color', '#7b6ea4');
        $('#body').css('background-color', '#7b6ea4');

        if (roleId == 1) {
            connection.invoke('GoToReadPhase', userId, currentRoomCode, false);
        }
    }

    function ShowPowerups() {
        powerupDrawer = !powerupDrawer;

        console.log(powerupDrawer);

        if (powerupDrawer) {
            $('#movingDrawer').css('margin-left', '1400px');
        }
        else {
            $('#movingDrawer').css('margin-left', '2000px');
        }
    }

    function UpdatePowerups(powerupsAllowed, powerupsCosts) {
        if (powerupsAllowed) {
            var costList = JSON.parse(powerupsCosts);

            $('#movingDrawer').css('display', 'block');
            $('#btn-openPowerupDrawer').css('visibility', 'visible');

            $('#btn-activatePowerup-1').val('Twee antwoorden kiezen (€' + costList[0] + '-)');
            $('#btn-activatePowerup-2').val('Dubbele punten (€' + costList[1] + '-)');
            $('#btn-activatePowerup-3').val('50% wegstrepen (€' + costList[2] + '-)');
            $('#btn-activatePowerup-4').val('Spieken (€' + costList[3] + '-)');
            $('#btn-activatePowerup-5').val('Dubbele punten voor jouw verhaal (€' + costList[4] + '-)');

            if (myCash < costList[0]) {
                $('#btn-activatePowerup-1').css('background-color', 'red');
            }
            else {
                $('#btn-activatePowerup-1').css('background-color', 'green');
            }

            if (myCash < costList[1]) {
                $('#btn-activatePowerup-2').css('background-color', 'red');
            }
            else {
                $('#btn-activatePowerup-2').css('background-color', 'green');
            }

            if (myCash < costList[2]) {
                $('#btn-activatePowerup-3').css('background-color', 'red');
            }
            else {
                $('#btn-activatePowerup-3').css('background-color', 'green');
            }

            if (myCash < costList[3]) {
                $('#btn-activatePowerup-4').css('background-color', 'red');
            }
            else {
                $('#btn-activatePowerup-4').css('background-color', 'green');
            }

            if (myCash < costList[4]) {
                $('#btn-activatePowerup-5').css('background-color', 'red');
            }
            else {
                $('#btn-activatePowerup-5').css('background-color', 'green');
            }
        }
        else {
            $('#movingDrawer').css('display', 'none');
            $('#btn-openPowerupDrawer').css('visibility', 'hidden');
        }
    }

    function ActivatePowerup(powerup, cash) {
        myCash = cash;

        powerupDrawer = false;
        $('#btn-openPowerupDrawer').css('visibility', 'hidden');
    }

    function CrossOutWrongAnswers(answers) {
        var stories = document.getElementById('storyList').getElementsByTagName('input');
        var answerList = JSON.parse(answers);

        console.log("answer list: " + answerList);
        console.log("answer 1: " + answerList[0] + ". answer 2: " + answerList[1]);

        for (var answer in answerList) {
            console.log(answerList[answer]);
            $(stories[answerList[answer]]).css('text-decoration', 'line-through');
        }
    }

    function EndGame() {
        HideAll();

        $('#game-end').css('display', 'block');
        $('#cornerMascot').css('display', 'none');
    }
    // #endregion

    // #region Buttons/Input
    $('#btn-hostLobby').click(function () {
        if (username.length != 0) {
            connection.invoke('HostRoom', userId, username);
        }
    });

    $('#btn-joinLobby').click(function () {
        currentRoomCode = $('#roomCodeInput').val().trim();

        if (username.length != 0 && currentRoomCode.length != 0) {
            connection.invoke('JoinRoom', userId, username, currentRoomCode);
        }
    });

    $('#btn-leaveLobby').click(function () {
        connection.invoke('LeaveRoom', userId, myId, currentRoomCode, false);
    });

    $('#btn-leaveGameOnEnd').click(function () {
        connection.invoke('LeaveRoom', userId, myId, currentRoomCode, false);
    });

    $('#btn-startGame').click(function () {
        connection.invoke('StartGame', myId, currentRoomCode);
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

    $('#btn-openPowerupDrawer').click(function () {
        ShowPowerups();
    });

    $('#btn-activatePowerup-1').click(function () {
        if (currentRoomCode.length != 0) {
            connection.invoke("ActivatePowerup", myId, currentRoomCode, 1)
        }
    });

    $('#btn-activatePowerup-2').click(function () {
        if (currentRoomCode.length != 0) {
            connection.invoke("ActivatePowerup", myId, currentRoomCode, 2)
        }
    });

    $('#btn-activatePowerup-3').click(function () {
        console.log('3');
        connection.invoke('ActivatePowerup', myId, currentRoomCode, '3');
    });

    $('#btn-activatePowerup-4').click(function () {
        if (currentRoomCode.length != 0) {
            connection.invoke("ActivatePowerup", myId, currentRoomCode, 4)
        }
    });

    $('#btn-activatePowerup-5').click(function () {
        if (currentRoomCode.length != 0) {
            connection.invoke("ActivatePowerup", myId, currentRoomCode, 5)
        }
    });

    $.kickUser = function (connectionId) {
        if (currentRoomCode.length != 0) {
            connection.invoke("LeaveRoom", userId, connectionId, currentRoomCode, true);
        }
    }
    // #endregion
});