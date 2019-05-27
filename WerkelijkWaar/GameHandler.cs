using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WebSocketManager;

namespace WerkelijkWaar
{
    public class GameHandler : WebSocketHandler
    {
        private Classes.Logger l = new Classes.Logger();
        private readonly GameManager _gameManager;
        private Classes.DatabaseQueries dq = new Classes.DatabaseQueries();
        private bool PingOrPong = false;
        private List<string> Pongs = new List<string>();

        public GameHandler(WebSocketConnectionManager webSocketConnectionManager, GameManager gameManager) : base(webSocketConnectionManager)
        {
            _gameManager = gameManager;

            // Room state timer
            Timer timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(CheckRoomStates);
            timer.Start();

            // Ping/pong timer
            Timer pingTimer = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);
            pingTimer.AutoReset = true;
            pingTimer.Elapsed += new ElapsedEventHandler(PingPong);
            pingTimer.Start();
        }

        #region Ping
        /// <summary>
        /// Add a connection to the connection list.
        /// </summary>
        /// <param name="socketId"></param>
        public void AddConnection(string socketId)
        {
            // Create a new connections object
            Classes.Connection newConnection = new Classes.Connection { SocketId = socketId, Pinged = true, Timeouts = 0 };

            // Check if ID already exists in the list, to prevent spamming.
            bool exists = false;

            foreach (Classes.Connection c in _gameManager.Connections)
            {
                if (c.SocketId == socketId)
                {
                    exists = true;
                    c.Pinged = true;
                }
            }

            // If ID does not exist, create new ID.
            if (!exists)
            {
                _gameManager.Connections.Add(newConnection);
            }
        }

        /// <summary>
        /// Check if existing connections are still alive and terminate connections that are not alive.
        /// </summary>
        /// <returns></returns>
        public async void PingPong(object sender, ElapsedEventArgs e)
        {
            // Get current list of connections
            List<Classes.Connection> connectionList = _gameManager.Connections;

            // Ping
            if (PingOrPong)
            {
                // 'Ping' each connection
                foreach (Classes.Connection c in connectionList)
                {
                    await InvokeClientMethodToAllAsync("pingToServer", c.SocketId);
                }
            }
            // Pong
            else
            {
                // Create tasklist for removals
                List<Task> taskList = new List<Task>();

                // Reset the timeouts of the current list
                foreach (Classes.Connection c in connectionList)
                {
                    // Check if connection was pinged
                    if (c.Pinged)
                    {
                        c.Timeouts = 0;
                    }
                    else
                    {
                        c.Timeouts += 1;
                    }

                    // If timeouts exceeds maximum amount of timeouts, remove connection from list and kick socket ID from everything applicable
                    if (c.Timeouts >= 3)
                    {
                        // Remove connection from active rooms
                        foreach (Classes.Room r in _gameManager.Rooms.ToList())
                        {
                            foreach (Classes.User u in r.Users.ToList())
                            {
                                if (u.SocketId == c.SocketId)
                                {
                                    // Create task to leave room
                                    var y = new Task(() => {
                                        LeaveRoom(u.SocketId, r.RoomCode, true);
                                    });

                                    taskList.Add(y);
                                    y.Start();
                                }
                            }
                        }

                        // Create task to remove self
                        var t = new Task(() => {
                            _gameManager.Connections.Remove(c);
                        });

                        taskList.Add(t);
                        t.Start();
                    }

                    // Reset pings
                    c.Pinged = false;
                }

                // Execute tasks
                Task.WaitAll(taskList.ToArray());
            }

            PingOrPong = !PingOrPong; // switch to ping or pong for next tick
        }

        /// <summary>
        /// Add a pong to the list for PingPong.
        /// </summary>
        /// <param name="socketId">Socket ID</param>
        /// <returns></returns>
        public void AddPong(string socketId)
        {
            foreach (Classes.Connection c in _gameManager.Connections)
            {
                if (c.SocketId == socketId)
                {
                    c.Pinged = true;
                }
            }
        }
        #endregion

        #region Rooms
        /// <summary>
        /// Open a room instance.
        /// </summary>
        /// <param name="socketId">Owner ID</param>
        /// <param name="username">Owner username</param>
        /// <returns></returns>
        public async Task HostRoom(string socketId, string username)
        {
            Classes.Room Room = new Classes.Room();

            Room.RoomOwnerId = socketId;
            Room.RoomOwner = username;
            // Room.Users.Add(new Classes.User { SocketId = socketId, Username = username });

            _gameManager.Rooms.Add(Room);

            await InvokeClientMethodToAllAsync("hostRoom", socketId, Room.RoomCode);
            await RetrievePlayerList(Room.RoomCode, false);
        }

        /// <summary>
        /// Join a room instance.
        /// </summary>
        /// <param name="socketId">Client ID</param>
        /// <param name="username">Client username</param>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task JoinRoom(string socketId, string userId, string username, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.Users.Count == room.MaxPlayers)
                    {
                        string message = "Kan niet meedoen met het spel - het gekozen spel is al vol (" + room.Users.Count + "/" + room.MaxPlayers + ").";
                        await InvokeClientMethodToAllAsync("setStateMessage", socketId, message);
                    }
                    else
                    {
                        if (room.RoomState == Classes.Room.State.Waiting)
                        {
                            room.ResetTimer();
                            room.Users.Add(new Classes.User { Id = Convert.ToInt32(userId), SocketId = socketId, Username = username, ReadyToPlay = false, WroteStory = false, ChoseStory = false });
                            await InvokeClientMethodToAllAsync("joinRoom", socketId, roomCode);
                            await RetrievePlayerList(room.RoomCode, false);
                        }
                        else
                        {
                            string message = "";

                            switch (room.RoomState)
                            {
                                case Classes.Room.State.Writing:
                                    message = "Kan niet meedoen met het spel - het gekozen spel is al begonnen.";
                                    break;

                                case Classes.Room.State.Reading:
                                    message = "Kan niet meedoen met het spel - het gekozen spel is al begonnen.";
                                    break;

                                case Classes.Room.State.Finished:
                                    message = "Kan niet meedoen met het spel - het gekozen spel is al afgelopen.";
                                    break;

                                case Classes.Room.State.Dead:
                                    message = "Kan niet meedoen met het spel - de kamer is 'dood' en wordt binnenkort opgeruimd.";
                                    break;

                                default:
                                    message = "Kan niet meedoen met het spel.";
                                    break;
                            }

                            await InvokeClientMethodToAllAsync("setStateMessage", socketId, message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Leave a room instance.
        /// </summary>
        /// <param name="socketId">Client ID</param>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task LeaveRoom(string socketId, string roomCode, bool kicked)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    // If owner leaves...
                    if (socketId == room.RoomOwnerId)
                    {
                        string message = "";

                        foreach (Classes.User user in room.Users)
                        {
                            await InvokeClientMethodToAllAsync("leaveRoom", user.SocketId, kicked);

                            message = "Kamer is gesloten omdat de eigenaar de kamer heeft verlaten.";
                            await InvokeClientMethodToAllAsync("setStateMessage", user.SocketId, message);
                        }

                        await InvokeClientMethodToAllAsync("leaveRoom", room.RoomOwnerId, kicked);
                        message = "Kamer is gesloten omdat de eigenaar de kamer heeft verlaten.";
                        await InvokeClientMethodToAllAsync("setStateMessage", room.RoomOwnerId, message);

                        _gameManager.Rooms.Remove(room);
                    }

                    // If regular user leaves
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            room.Users.Remove(user);
                            room.ResetTimer();
                            await InvokeClientMethodToAllAsync("leaveRoom", socketId, kicked);
                            await RetrievePlayerList(room.RoomCode, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check rooms for their states and handle accordingly.
        /// </summary>
        /// <returns></returns>
        public async void CheckRoomStates(object sender, ElapsedEventArgs e)
        {
            List<Task> taskList = new List<Task>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomState == Classes.Room.State.Dead)
                {
                    var t = new Task(() => {
                        foreach (Classes.User user in room.Users)
                        {
                            InvokeClientMethodToAllAsync("leaveRoom", user.SocketId);

                            string message = "Room has died. Reason: idle for too long.";
                            InvokeClientMethodToAllAsync("setStateMessage", user.SocketId, message);
                        }

                        _gameManager.Rooms.Remove(room);
                    });

                    taskList.Add(t);
                    t.Start();
                }
            }

            Task.WaitAll(taskList.ToArray());
        }
        #endregion

        #region Game
        /// <summary>
        /// Start a game with the current room.
        /// </summary>
        /// <param name="socketId">User ID</param>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task StartGame(string socketId, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == socketId)
                {
                    room.RoomState = Classes.Room.State.Writing;
                    room.CurrentStrikes = room.MaxProgressStrikes;

                    if (room.GamePreparation())
                    {
                        await InvokeClientMethodToAllAsync("startGame", roomCode, room.CurrentGroup);
                        await RetrievePlayerList(room.RoomCode, true);
                    }
                }
            }
        }

        /// <summary>
        /// Wacht tot alle spelers gereed zijn om te spelen.
        /// Start vervolgens het spel.
        /// </summary>
        /// <param name="socketId"></param>
        /// <param name="roomCode"></param>
        /// <returns></returns>
        public async Task ReadyUpPlayer(string socketId, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    room.NumberOfReadyPlayers = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            user.ReadyToPlay = true;
                        }

                        if (user.ReadyToPlay)
                        {
                            room.NumberOfReadyPlayers++;
                        }
                    }

                    if (room.NumberOfReadyPlayers == room.Users.Count)
                    {
                        await GoToWritePhase(roomCode, room.RoomOwnerId, false);
                    }
                }
            }
        }

        public async Task SkipTutorial(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        user.ReadyToPlay = true;
                    }

                    await GoToWritePhase(roomCode, room.RoomOwnerId, false);
                }
            }
        }

        /// <summary>
        /// Ga naar de schrijffase van het spel.
        /// </summary>
        /// <param name="roomCode"></param>
        /// <param name="ownerId"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public async Task GoToWritePhase(string roomCode, string ownerId, bool reset)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    room.RoomState = Classes.Room.State.Writing;

                    await InvokeClientMethodToAllAsync("goToWritePhase", roomCode);
                    await StartGameTimer(roomCode, 600, room.RoomOwnerId);
                    await RetrieveRootStory(roomCode);
                }
            }
        }

        /// <summary>
        /// Ga naar de leesfase van het spel.
        /// </summary>
        /// <param name="roomCode"></param>
        /// <param name="ownerId"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public async Task GoToReadPhase(string roomCode, bool reset)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    room.RoomState = Classes.Room.State.Reading;

                    room.CurrentGroup++;

                    await InvokeClientMethodToAllAsync("goToReadPhase", roomCode);
                    await StartGameTimer(roomCode, 900, room.RoomOwnerId);
                    await RetrieveWrittenStories(roomCode, room.CurrentGroup);
                }
            }
        }

        /// <summary>
        /// Start de game timer.
        /// </summary>
        /// <param name="roomCode"></param>
        /// <param name="time"></param>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task StartGameTimer(string roomCode, int time, string ownerId)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == ownerId)
                {
                    room.RemainingTime = time;
                    room.gameTimer.Start();
                    await InvokeClientMethodToAllAsync("startCountdownTimer", roomCode, time);
                }
            }
        }

        /// <summary>
        /// Stop de game timer.
        /// </summary>
        /// <param name="roomCode"></param>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task StopGameTimer(string roomCode, string ownerId)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == ownerId)
                {
                    room.RemainingTime = 0;
                    room.gameTimer.Stop();
                    await InvokeClientMethodToAllAsync("stopCountdownTimer", roomCode);
                }
            }
        }

        /// <summary>
        /// Haal alle root verhalen op om later te verdelen.
        /// </summary>
        /// <param name="roomCode"></param>
        public async Task RetrieveRootStory(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        string rootStory = room.RetrievedStories[user.GameGroup - 1].Id + ":!|" + room.RetrievedStories[user.GameGroup - 1].Title + ":!|" + room.RetrievedStories[user.GameGroup - 1].Description;
                        await InvokeClientMethodToAllAsync("retrieveRootStory", roomCode, user.SocketId, rootStory);
                    }
                }
            }
        }

        public async Task UploadStory(string roomCode, string socketId, string story)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    int usersThatWroteStories = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            user.WroteStory = true;

                            string[] tempStory = story.Split("_+_");

                            Classes.Story newStory = new Classes.Story { GameGroup = user.GameGroup, IsRoot = false, Title = tempStory[1], Description = tempStory[2], OwnerId = user.Id, Source = Convert.ToInt32(tempStory[0]) };

                            if (!room.WrittenStories.Contains(newStory))
                            {
                                newStory.Date = DateTime.Now;
                                room.WrittenStories.Add(newStory);
                                // dq.CreateStory(newStory);
                            }
                        }

                        if (user.WroteStory)
                        {
                            usersThatWroteStories++;
                        }
                    }

                    if (usersThatWroteStories == room.Users.Count)
                    {
                        await GoToReadPhase(roomCode, false);
                    }
                }
            }
        }

        public async Task UploadAnswer(string roomCode, string socketId, string answer)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    int usersThatSelectedAnswers = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            l.DebugToLog("[Game]", "Parsing score for user " + user.Username + " : " + socketId, 0);

                            user.ChoseStory = true;

                            bool scoreFound = false;
                            Classes.Score newScore = new Classes.Score();

                            if (room.SelectedAnswers != null && room.SelectedAnswers.Count != 0)
                            {
                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    // Search for score
                                    if (score.SocketId == user.SocketId)
                                    {
                                        scoreFound = true;

                                        // 2 answer power-up
                                        if (user.PowerupOneActive)
                                        {
                                            l.DebugToLog("[Game]", socketId + ": has used the 'two answer' power-up. Answers are " + answer, 1);

                                            string[] answers = answer.Split(':');

                                            if (Convert.ToInt32(answers[0]) == room.CorrectAnswer)
                                            {
                                                answer = answers[0];
                                            }
                                            else
                                            {
                                                answer = answers[1];
                                            }
                                        }

                                        // Apply answer
                                        score.Answers += answer;
                                        l.DebugToLog("[Game]", socketId + ": applied answer " + answer + ". Player now has answers " + score.Answers, 1);

                                        // Check correctness
                                        if (Convert.ToInt32(answer) == room.CorrectAnswer)
                                        {
                                            l.DebugToLog("[Game]", socketId + ": answer is correct btw", 1);

                                            score.CorrectAnswers += "1";

                                            if (user.PowerupTwoActive)
                                            {
                                                score.FollowerAmount += 10;
                                            }
                                            else
                                            {
                                                score.FollowerAmount += 5;
                                            }
                                        }
                                        else
                                        {
                                            l.DebugToLog("[Game]", socketId + ": answer is incorrect btw", 1);

                                            score.CorrectAnswers += "0";

                                            if (user.PowerupTwoActive)
                                            {
                                                newScore.FollowerAmount -= 10;
                                            }
                                            else
                                            {
                                                newScore.FollowerAmount -= 5;
                                            }

                                            if (score.FollowerAmount <= 0)
                                            {
                                                score.FollowerAmount = 0;
                                            }
                                        }

                                        l.DebugToLog("[Game]", socketId + ": followers are now " + score.FollowerAmount, 2);
                                    }
                                }                              
                            }

                            // Create score if not found
                            if (!scoreFound)
                            {
                                l.DebugToLog("[Game]", "Creating new score for " + socketId, 1);

                                newScore = new Classes.Score { SocketId = user.SocketId, OwnerId = user.Id, Date = DateTime.Now, Answers = "" };

                                // 2 answer power-up
                                if (user.PowerupOneActive)
                                {
                                    l.DebugToLog("[Game]", socketId + ": has used the 'two answer' power-up. Answers are " + answer, 1);

                                    string[] answers = answer.Split(':');

                                    if (Convert.ToInt32(answers[0]) == room.CorrectAnswer)
                                    {
                                        answer = answers[0];
                                    }
                                    else
                                    {
                                        answer = answers[1];
                                    }
                                }

                                // Apply answer
                                newScore.Answers += answer;
                                l.DebugToLog("[Game]", socketId + ": applied answer " + answer + ". Player now has answers " + newScore.Answers, 1);

                                // Check correctness
                                if (Convert.ToInt32(answer) == room.CorrectAnswer)
                                {
                                    l.DebugToLog("[Game]", socketId + ": answer is correct btw", 1);

                                    newScore.CorrectAnswers += "1";

                                    if (user.PowerupTwoActive)
                                    {
                                        newScore.FollowerAmount += 10;
                                    }
                                    else
                                    {
                                        newScore.FollowerAmount += 5;
                                    }
                                }
                                else
                                {
                                    l.DebugToLog("[Game]", socketId + ": answer is incorrect btw", 1);

                                    newScore.CorrectAnswers += "0";

                                    if (user.PowerupTwoActive)
                                    {
                                        newScore.FollowerAmount -= 10;
                                    }
                                    else
                                    {
                                        newScore.FollowerAmount -= 5;
                                    }

                                    if (newScore.FollowerAmount <= 0)
                                    {
                                        newScore.FollowerAmount = 0;
                                    }
                                }

                                l.DebugToLog("[Game]", socketId + ": followers are now " + newScore.FollowerAmount, 2);

                                room.SelectedAnswers.Add(newScore);
                            }
                        }

                        if (user.ChoseStory)
                        {
                            usersThatSelectedAnswers++;
                        }
                    }

                    if (usersThatSelectedAnswers == room.Users.Count)
                    {
                        // Loop
                        await GiveMoney(roomCode);
                    }
                }
            }
        }

        public async Task GiveMoney(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        foreach (Classes.Score score in room.SelectedAnswers)
                        {
                            if (user.SocketId == score.SocketId)
                            {
                                score.CashAmount += (1.00 * score.FollowerAmount);
                            }
                        }
                    }

                    await ShowLeaderboard(roomCode);
                }
            }
        }

        public async Task ShowLeaderboard(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = new List<string>();
                    List<Classes.Score> scoreList = new List<Classes.Score>();

                    foreach (Classes.Score score in room.SelectedAnswers)
                    {
                        scoreList.Add(score);
                    }

                    // Sort scorelist
                    scoreList = scoreList.OrderByDescending(o => o.CashAmount).ToList();

                    // Sort by rank
                    foreach (Classes.Score rankedScore in scoreList)
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            if (user.SocketId == rankedScore.SocketId)
                            {
                                int rank = scoreList.IndexOf(rankedScore);
                                string rankString = (rank + 1) + ":|!" + user.Username + ":|!" + rankedScore.FollowerAmount + ":|!" + rankedScore.CashAmount;
                                rankList.Add(rankString);
                            }
                        }
                    }

                    await InvokeClientMethodToAllAsync("showLeaderboards", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(rankList));
                }
            }
        }

        public async Task RetrieveWrittenStories(string roomCode, int gameGroup)
        {
            List<string> storiesToSend = new List<string>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.Story story in room.WrittenStories)
                    {
                        if (story.GameGroup == gameGroup)
                        {
                            string toSend = story.OwnerId.ToString() + ":!|" + story.Title + ":!|" + story.Description;
                            storiesToSend.Add(toSend);
                        }
                    }

                    Classes.Story rootStory = room.RetrievedStories[gameGroup-1];
                    string root = rootStory.OwnerId.ToString() + ":!|" + rootStory.Title + ":!|" + rootStory.Description;
                    storiesToSend.Add(root);

                    // Shuffle story list
                    Random rng = new Random();

                    int stringsLeft = storiesToSend.Count();

                    while (stringsLeft > 1)
                    {
                        stringsLeft--;
                        int index = rng.Next(stringsLeft + 1);
                        string selectedString = storiesToSend[index];
                        storiesToSend[index] = storiesToSend[stringsLeft];
                        storiesToSend[stringsLeft] = selectedString;
                    }

                    // Plant correct answer
                    l.WriteToLog("[Game]", "Planting correct answer...", 0);

                    int storyCount = 0;
                    foreach (string storyString in storiesToSend)
                    {
                        if (storyString == root)
                        {
                            l.WriteToLog("[Game]", "Correct answer is " + storyCount + " with story string '" + storyString + "'.", 1);
                            room.CorrectAnswer = storyCount;
                        }

                        storyCount++;
                    }
                }
            }

            await InvokeClientMethodToAllAsync("showStories", gameGroup, roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(storiesToSend));
        }

        public async Task ActivatePowerup(string roomCode, string socketId, string powerup)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            foreach (Classes.Score score in room.SelectedAnswers)
                            {
                                if (score.SocketId == socketId)
                                {
                                    if (Convert.ToInt32(powerup) == 1 && score.CashAmount >= (10.00 * room.Config.PowerupsCostMult))
                                    {
                                        score.CashAmount -= (10.00 * room.Config.PowerupsCostMult);
                                        user.PowerupOneActive = true;
                                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, true, false, false);
                                    }
                                    else if (Convert.ToInt32(powerup) == 2 && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                    {
                                        score.CashAmount -= (15.00 * room.Config.PowerupsCostMult);
                                        user.PowerupTwoActive = true;
                                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, false, true, false);
                                    }
                                    else if (Convert.ToInt32(powerup) == 3 && score.CashAmount >= (20.00 * room.Config.PowerupsCostMult))
                                    {
                                        score.CashAmount -= (20.00 * room.Config.PowerupsCostMult);
                                        user.PowerupThreeActive = true;
                                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, false, false, true);
                                    }
                                    else
                                    {
                                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, false, false, false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Visual
        /// <summary>
        /// Retrieve connected users inside of the current room.
        /// </summary>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task RetrievePlayerList(string roomCode, bool withGroup)
        {
            string ownerId = "";
            Classes.Room.State roomState = Classes.Room.State.Waiting;
            List<string> UsernameList = new List<string>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    ownerId = room.RoomOwnerId;

                    foreach (Classes.User user in room.Users)
                    {
                        string tempString = "";

                        // if (withGroup)
                        if (room.RoomState == Classes.Room.State.Writing || room.RoomState == Classes.Room.State.Reading)
                        {
                            roomState = room.RoomState;
                            tempString = user.Username + ":|!" + user.SocketId + ":|!" + user.GameGroup + ":|!" + room.RetrievedStories[user.GameGroup - 1].Title;
                        }
                        else
                        {
                            tempString = user.Username + ":|!" + user.SocketId;
                        }

                        UsernameList.Add(tempString);
                    }
                }
            }

            if (roomState == Classes.Room.State.Writing || roomState == Classes.Room.State.Reading)
            {
                await InvokeClientMethodToAllAsync("retrievePlayerList", roomCode, ownerId, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList), true);
            }
            else
            {
                await InvokeClientMethodToAllAsync("retrievePlayerList", roomCode, ownerId, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList), false);
            }
        }
        #endregion
    }
}
