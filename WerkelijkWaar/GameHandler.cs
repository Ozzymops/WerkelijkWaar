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
        public async Task HostRoom(string userId, string socketId, string username)
        {
            l.DebugToLog("[Game]", "Creating new room.", 0);

            Classes.Room Room = new Classes.Room();

            Room.RoomOwnerId = socketId;
            Room.RoomOwner = username;
            Room.Teacher = dq.RetrieveUser(Convert.ToInt32(userId));
            Room.Config = dq.RetrieveConfig(Convert.ToInt32(userId));
            l.DebugToLog("[Game]", "Teacher: " + Room.Teacher.Username, 1);
            l.DebugToLog("[Game]", "Config: " + Room.Config.Id, 2);
            // Room.Users.Add(new Classes.User { SocketId = socketId, Username = username });

            // Apply config to relevant parts
            Room.MaxPlayers = Room.Config.MaxPlayers;

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
                            await PowerupVisuals(room.RoomCode);
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
                    await StartGameTimer(roomCode, room.Config.MaxWritingTime, room.RoomOwnerId);
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
        public async Task GoToReadPhase(string roomCode, bool start)
        {
            l.WriteToLog("[Game]", "Putting '" + roomCode + "' into read phase.", 0);
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    room.RoomState = Classes.Room.State.Reading;

                    room.CurrentGroup++;
                    l.WriteToLog("[Game]", "Room '" + roomCode + "' has " + room.GroupCount + " groups.", 1);
                    l.WriteToLog("[Game]", "Current group is '" + room.CurrentGroup + "' in room '" + roomCode + "'.", 1);

                    int neededAnswers = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.GameGroup == room.CurrentGroup)
                        {
                            neededAnswers++;
                        }
                    }

                    room.NeededAnswers = neededAnswers;

                    if (!start)
                    {
                        if (room.CurrentGroup <= room.GroupCount)
                        {
                            foreach (Classes.User user in room.Users)
                            {
                                l.WriteToLog("[Game]", "goToReadPhase: " + roomCode + " - " + user.SocketId + ": " + user.GameGroup + "|" + room.CurrentGroup + ".", 1);
                                await InvokeClientMethodToAllAsync("goToReadPhase", roomCode, user.SocketId, user.GameGroup, room.CurrentGroup);
                            }

                            // Host
                            l.WriteToLog("[Game]", "goToReadPhase (host): " + roomCode + " - " + room.RoomOwnerId, 1);
                            await InvokeClientMethodToAllAsync("goToReadPhase", roomCode, room.RoomOwnerId, 0, 0);

                            await PowerupVisuals(roomCode);
                            await StartGameTimer(roomCode, room.Config.MaxReadingTime, room.RoomOwnerId);
                            await RetrieveWrittenStories(roomCode, room.CurrentGroup);

                            l.WriteToLog("[Game]", "Game " + room.CurrentGroup + " of room '" + roomCode + "' started.", 2);
                        }
                        else
                        {
                            await ShowLeaderboard(roomCode, true);
                        }
                    }
                    else
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            l.WriteToLog("[Game]", "goToReadPhase: " + roomCode + " - " + user.SocketId + ": " + user.GameGroup + "|" + room.CurrentGroup + ".", 1);
                            await InvokeClientMethodToAllAsync("goToReadPhase", roomCode, user.SocketId, user.GameGroup, room.CurrentGroup);
                        }

                        // Host
                        l.WriteToLog("[Game]", "goToReadPhase (host): " + roomCode + " - " + room.RoomOwnerId, 1);
                        await InvokeClientMethodToAllAsync("goToReadPhase", roomCode, room.RoomOwnerId, 0, 0);

                        await PowerupVisuals(roomCode);
                        await StartGameTimer(roomCode, room.Config.MaxReadingTime, room.RoomOwnerId);
                        await RetrieveWrittenStories(roomCode, room.CurrentGroup);

                        l.WriteToLog("[Game]", "First game of room '" + roomCode + "' started.", 2);
                    }
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

                            Classes.Story newStory = new Classes.Story { SocketId = user.SocketId, GameGroup = user.GameGroup, IsRoot = false, Title = tempStory[1], Description = tempStory[2], OwnerId = user.Id, Source = Convert.ToInt32(tempStory[0]) };

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
                        foreach (Classes.User user in room.Users)
                        {
                            user.WroteStory = false;
                        }

                        await GoToReadPhase(roomCode, true);
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
                            if (user.GameGroup == room.CurrentGroup)
                            {
                                l.WriteToLog("[Game]", "Parsing score for user " + socketId, 0);

                                // Power-up 1 check
                                if (user.PowerupOneActive)
                                {
                                    // Split answers
                                    int answerA = Convert.ToInt32(answer) / 10;  // first digit
                                    int answerB = Convert.ToInt32(answer) % 10;  // second digit

                                    l.WriteToLog("[Game]", "Double power-up: A = " + answerA + ", B = " + answerB + " - from " + answer, 1);

                                    // Get 'best' answer
                                    if (answerA == room.CorrectAnswer)
                                    {
                                        answer = answerA.ToString();
                                    }
                                    else if (answerB == room.CorrectAnswer)
                                    {
                                        answer = answerB.ToString();
                                    }
                                }

                                // Apply vote to story
                                string storyFromAnswer = room.SentStories[Convert.ToInt32(answer)];
                                l.WriteToLog("[Game]", "Selected story is " + storyFromAnswer, 1);

                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    // User-made story
                                    if (Convert.ToInt32(storyFromAnswer.Split(":!|")[3]) != 0)
                                    {
                                        l.WriteToLog("[Game]", "Selected story is user-made: " + storyFromAnswer.Split(":!|")[3], 1);

                                        if (score.SocketId == storyFromAnswer.Split(":!|")[3])
                                        {
                                            l.WriteToLog("[Game]", "Added vote to score.", 1);

                                            score.AttainedVotes++;

                                            foreach (Classes.Story story in room.WrittenStories)
                                            {
                                                if (story.SocketId == storyFromAnswer.Split(":!|")[3])
                                                {
                                                    l.WriteToLog("[Game]", "Added vote to story.", 1);

                                                    story.Votes++;
                                                }
                                            }
                                        }
                                    }
                                    // Root story
                                    else
                                    {
                                        foreach (Classes.Story story in room.RetrievedStories)
                                        {
                                            if (story.OwnerId == Convert.ToInt32(storyFromAnswer.Split(":!|")[0]))
                                            {
                                                if (story.Title == storyFromAnswer.Split(":!|")[1])
                                                {
                                                    l.WriteToLog("[Game]", "Selected story is root: " + storyFromAnswer.Split(":!|")[3], 1);

                                                    l.WriteToLog("[Game]", "Added vote to story.", 1);

                                                    story.Votes++;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Apply variables to score
                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    if (score.SocketId == socketId)
                                    {
                                        l.WriteToLog("[Game]", "Added answer to score.", 1);

                                        score.Answers += answer;

                                        // Answer is correct
                                        if (Convert.ToInt32(answer) == room.CorrectAnswer)
                                        {
                                            l.WriteToLog("[Game]", "Answer was correct btw.", 1);

                                            score.CorrectAnswers += '1';

                                            if (user.PowerupOneActive)
                                            {
                                                l.WriteToLog("[Game]", "Bad player gained " + (room.Config.FollowerGain / 2) + " followers.", 2);

                                                score.FollowerAmount += (room.Config.FollowerGain / 2);
                                            }
                                            else if (user.PowerupTwoActive)
                                            {
                                                l.WriteToLog("[Game]", "Absolute gamer gained " + (room.Config.FollowerGain * 2) + " followers.", 2);

                                                score.FollowerAmount += (room.Config.FollowerGain * 2);
                                            }
                                            else
                                            {
                                                l.WriteToLog("[Game]", "Player gained " + room.Config.FollowerGain + " followers.", 2);

                                                score.FollowerAmount += room.Config.FollowerGain;
                                            }
                                        }
                                        // Answer is false
                                        else
                                        {
                                            score.CorrectAnswers += '0';

                                            if (user.PowerupOneActive)
                                            {
                                                l.WriteToLog("[Game]", "Bad player lost " + (room.Config.FollowerGain / 2) + " followers.", 2);

                                                score.FollowerAmount -= (room.Config.FollowerGain / 2);
                                            }
                                            else if (user.PowerupTwoActive)
                                            {
                                                l.WriteToLog("[Game]", "Absolute gamer lost " + (room.Config.FollowerGain * 2) + " followers.", 2);

                                                score.FollowerAmount -= (room.Config.FollowerGain * 2);
                                            }
                                            else
                                            {
                                                l.WriteToLog("[Game]", "Player lost " + room.Config.FollowerGain + " followers.", 2);

                                                score.FollowerAmount -= room.Config.FollowerGain;
                                            }
                                        }

                                        if (score.FollowerAmount <= 0)
                                        {
                                            score.FollowerAmount = 0;
                                        }
                                    }
                                }

                                user.ChoseStory = true;
                            }

                        }

                        if (user.ChoseStory)
                        {
                            usersThatSelectedAnswers++;
                        }
                    }

                    if (usersThatSelectedAnswers == room.NeededAnswers)
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            user.ChoseStory = false;
                        }

                        room.NeededAnswers = 0;
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
                                l.WriteToLog("[GiveMoney]", "Player " + user.Username + " | " + user.SocketId + " is getting sum money", 0);

                                score.CashAmount += 1.00 + (room.Config.CashPerFollower * score.FollowerAmount);

                                // Per vote
                                bool doubleScore = false;

                                foreach (Classes.Story story in room.WrittenStories)
                                {
                                    if (user.SocketId == story.SocketId)
                                    {
                                        if (story.PowerupActive)
                                        {
                                            l.WriteToLog("[GiveMoney]", "Player " + user.Username + " | " + user.SocketId + " has the double score power-up", 1);

                                            doubleScore = true;
                                        }
                                    }                               
                                }

                                if (user.GameGroup == room.CurrentGroup)
                                {
                                    if (score.RoundVotes > 0)
                                    {
                                        l.WriteToLog("[GiveMoney]", "Player " + user.Username + " | " + user.SocketId + " has > 0 votes", 1);

                                        if (doubleScore)
                                        {
                                            score.CashAmount += 10.00;
                                            score.CashAmount += (room.Config.CashPerVote * score.RoundVotes) * 2;
                                            score.FollowerAmount += 10 + ((room.Config.FollowerPerVote * score.RoundVotes) * 2);
                                        }
                                        else
                                        {
                                            score.CashAmount += 5.00;
                                            score.CashAmount += (room.Config.CashPerVote * score.RoundVotes);
                                            score.FollowerAmount += 5 + (room.Config.FollowerPerVote * score.RoundVotes);
                                        }

                                        score.AttainedVotes += score.RoundVotes;
                                        score.RoundVotes = 0;
                                    }
                                    else
                                    {
                                        l.WriteToLog("[GiveMoney]", "Player " + user.Username + " | " + user.SocketId + " has < 0 votes", 1);

                                        if (doubleScore)
                                        {
                                            score.FollowerAmount -= room.Config.FollowerLoss * 2;
                                        }
                                        else
                                        {
                                            score.FollowerAmount -= room.Config.FollowerLoss;
                                        }
                                    }
                                }
                                
                                if (score.FollowerAmount <= 0)
                                {
                                    score.FollowerAmount = 0;
                                }

                                l.WriteToLog("[GiveMoney]", "Player " + user.Username + " | " + user.SocketId + " now has " + score.FollowerAmount + " followers and €" + score.CashAmount + "- to his name.", 2);

                                await InvokeClientMethodToAllAsync("updateScore", roomCode, user.SocketId, score.CashAmount, score.FollowerAmount);
                            }
                        }
                    }

                    await PowerupVisuals(roomCode);
                    await ShowLeaderboard(roomCode, false);
                }
            }
        }

        public async Task ShowLeaderboard(string roomCode, bool endGame)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = GenerateLeaderboard(roomCode);

                    await InvokeClientMethodToAllAsync("showLeaderboards", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame);
                }
            }
        }

        public List<string> GenerateLeaderboard(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = new List<string>();
                    List<Classes.Score> scoreList = room.SelectedAnswers;

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

                    return rankList;
                }
            }

            return null;
        }

        public async Task RetrieveWrittenStories(string roomCode, int gameGroup)
        {
            List<string> storiesToSend = new List<string>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    l.WriteToLog("[Game]", "Retrieving written stories", 0);

                    foreach (Classes.Story story in room.WrittenStories)
                    {
                        if (story.GameGroup == gameGroup)
                        {
                            l.WriteToLog("[Game]", "Story " + story.Title + " socketId: " + story.SocketId, 1);

                            string toSend = story.OwnerId.ToString() + ":!|" + story.Title + ":!|" + story.Description + ":!|" + story.SocketId;
                            storiesToSend.Add(toSend);
                        }
                    }

                    Classes.Story rootStory = room.RetrievedStories[gameGroup-1];
                    string root = rootStory.OwnerId.ToString() + ":!|" + rootStory.Title + ":!|" + rootStory.Description + ":!|0";
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
                    l.WriteToLog("[Game]", "Planting correct answer...", 1);

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

                room.SentStories = storiesToSend;
            }

            await InvokeClientMethodToAllAsync("showStories", gameGroup, roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(storiesToSend));
        }

        public async Task ActivatePowerup(string roomCode, string socketId, string powerup)
        {
            l.WriteToLog("[Game]", "Activate power-up by " + socketId, 0);

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.Config.PowerupsAllowed)
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            if (user.SocketId == socketId)
                            {
                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    if (score.SocketId == socketId)
                                    {
                                        l.WriteToLog("[Game]", "User " + user.Username + " bought a power-up.", 1);

                                        //  && score.CashAmount >= (0.00 * room.Config.PowerupsCostMult)
                                        // score.CashAmount -= (0.00 * room.Config.PowerupsCostMult);

                                        if (powerup == "1" && score.CashAmount >= (20.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Choose two answers for 50% value - €20,00-

                                            user.PowerupOneActive = true;

                                            l.WriteToLog("[Game]", "Chosen power-up is 'choose two answers for 50% value'.", 2);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 1, score.CashAmount);
                                        }
                                        else if (powerup == "2" && score.CashAmount >= (25.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Answers count for 200% value, positively and negatively - €25,00-

                                            user.PowerupTwoActive = true;

                                            l.WriteToLog("[Game]", "Chosen power-up is 'answers count for 200% value'.", 2);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 2, score.CashAmount);
                                        }
                                        else if (powerup == "3" && score.CashAmount >= (10.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Cross out 50% of the wrong answers - €10,00-

                                            user.PowerupThreeActive = true;

                                            l.WriteToLog("[Game]", "Chosen power-up is 'cross out 50% of the wrong answers'.", 2);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 3, score.CashAmount);
                                            await ReturnWrongAnswers(roomCode, socketId);
                                        }
                                        else if (powerup == "4" && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Show the amount of answers on each story - €15,00-

                                            user.PowerupFourActive = true;

                                            l.WriteToLog("[Game]", "Chosen power-up is 'show the amount of answers on each story'.", 2);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 4, score.CashAmount);
                                        }
                                        else if (powerup == "5" && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Your story counts for 200% value when chosen - €15,00-

                                            foreach (Classes.Story story in room.WrittenStories)
                                            {
                                                if (story.SocketId == user.SocketId)
                                                {
                                                    story.PowerupActive = true;

                                                    l.WriteToLog("[Game]", "Chosen power-up is 'written story counts for 200% value when chosen'. Applied to '" + story.Title + "'", 2);
                                                }
                                            }

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 5, score.CashAmount);
                                        }
                                        else
                                        {
                                            l.WriteToLog("[Game]", "Sike, no power-up after all. Gottem.", 2);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 0, score.CashAmount);
                                        }
                                    }
                                }
                            }
                        }
                    }    
                    else
                    { 
                        l.WriteToLog("[Game]", "Sike, no power-up after all. Gottem.", 2);

                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 0);
                    }
                }
            }
        }

        public async Task ReturnWrongAnswers(string roomCode, string socketId)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            l.WriteToLog("[Game]", "Power-up 3 for user " + socketId + ". Correct answer is " + room.CorrectAnswer, 0);

                            Random rng = new Random();
                            List<string> newList = room.SentStories;

                            int maxStrikes = newList.Count / 2;

                            // Set flags
                            while (maxStrikes > 0)
                            {
                                int chosenStory = rng.Next(newList.Count);

                                if (chosenStory != room.CorrectAnswer)
                                {
                                    maxStrikes--;
                                    newList[chosenStory] += ":!|1";

                                    l.WriteToLog("[Game]", "Story " + chosenStory + " got flagged as wrong answer.", 1);
                                }
                            }

                            // Get rest
                            for (int i = newList.Count; i > 0; i--)
                            {
                                if (!newList[i].Contains(":!|1"))
                                {
                                    newList[i] += ":!|0";

                                    l.WriteToLog("[Game]", "Story " + i + " got flagged as possibly correct answer.", 1);
                                }
                            }

                            await InvokeClientMethodToAllAsync("returnWrongAnswers", roomCode, socketId, Newtonsoft.Json.JsonConvert.SerializeObject(newList));
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

        public async Task PowerupVisuals(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.Config.PowerupsAllowed)
                    {
                        await InvokeClientMethodToAllAsync("powerupVisuals", roomCode, room.Config.PowerupsAllowed, 20.00 * room.Config.PowerupsCostMult, 25.00 * room.Config.PowerupsCostMult, 10.00 * room.Config.PowerupsCostMult, 15.00 * room.Config.PowerupsCostMult, 15.00 * room.Config.PowerupsCostMult);
                    }
                    else
                    {
                        await InvokeClientMethodToAllAsync("powerupVisuals", roomCode, false, 0, 0, 0, 0, 0);
                    }
                }
            }
        }
        #endregion
    }
}
