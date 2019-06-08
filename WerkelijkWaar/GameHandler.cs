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
        private Classes.Logger logger = new Classes.Logger();
        private readonly GameManager _gameManager;
        private Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Do a ping or a pong?
        /// </summary>
        private bool PingOrPong = false;

        /// <summary>
        /// List of Pongs
        /// </summary>
        private List<string> Pongs = new List<string>();

        /// <summary>
        /// CONSTRUCTOR: set up WebSocket stuff including the GameManager
        /// </summary>
        /// <param name="webSocketConnectionManager"></param>
        /// <param name="gameManager"></param>
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
        /// <param name="socketId">Unique User socket ID</param>
        public void AddConnection(string socketId)
        {
            Classes.Connection newConnection = new Classes.Connection { SocketId = socketId, Pinged = true, Timeouts = 0 };

            // Check if Connection already exists in the list, to prevent spamming
            bool exists = false;

            foreach (Classes.Connection connection in _gameManager.Connections)
            {
                if (connection.SocketId == socketId)
                {
                    exists = true;
                    connection.Pinged = true;
                }
            }

            // If Connection does not exist, create a new Connection
            if (!exists)
            {
                _gameManager.Connections.Add(newConnection);
            }
        }

        /// <summary>
        /// Check if existing connections are still alive. Terminate dead connections
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        public async void PingPong(object sender, ElapsedEventArgs e)
        {
            List<Classes.Connection> connectionList = _gameManager.Connections;

            // Ping
            if (PingOrPong)
            {
                // 'Ping' each connection
                foreach (Classes.Connection connection in connectionList)
                {
                    await InvokeClientMethodToAllAsync("pingToServer", connection.SocketId);
                }
            }
            // Pong
            else
            {
                List<Task> taskList = new List<Task>();

                // Reset the timeouts of the current list
                foreach (Classes.Connection connection in connectionList)
                {
                    // Check if connection was pinged
                    if (connection.Pinged)
                    {
                        connection.Timeouts = 0;
                    }
                    else
                    {
                        connection.Timeouts += 1;
                    }

                    // If timeouts exceeds maximum amount of timeouts, remove connection from list and kick socket ID from everything applicable
                    if (connection.Timeouts >= 3)
                    {
                        // Remove connection from active rooms
                        foreach (Classes.Room room in _gameManager.Rooms)
                        {
                            foreach (Classes.User user in room.Users)
                            {
                                if (user.SocketId == connection.SocketId)
                                {
                                    // Create task to leave room
                                    Task leaveTask = new Task(() => {
                                        LeaveRoom(user.Id.ToString(), user.SocketId, room.RoomCode, true);
                                    });

                                    taskList.Add(leaveTask);
                                    leaveTask.Start();
                                }
                            }
                        }

                        // Create task to remove self
                        Task removalTask = new Task(() => {
                            _gameManager.Connections.Remove(connection);
                        });

                        taskList.Add(removalTask);
                        removalTask.Start();
                    }

                    // Reset pings
                    connection.Pinged = false;
                }

                // Execute tasks
                Task.WaitAll(taskList.ToArray());
            }

            PingOrPong = !PingOrPong; // switch to ping or pong for next tick
        }

        /// <summary>
        /// Add a pong to the list for PingPong.
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        public void AddPong(string socketId)
        {
            foreach (Classes.Connection connection in _gameManager.Connections)
            {
                if (connection.SocketId == socketId)
                {
                    connection.Pinged = true;
                }
            }
        }
        #endregion

        #region Rooms
        /// <summary>
        /// Publicly host a game room
        /// </summary>
        /// <param name="userId">Owner User ID</param>
        /// <param name="socketId">Owner unique User socket ID</param>
        /// <param name="username">Owner username</param>
        public async Task HostRoom(string userId, string socketId, string username)
        {
            logger.Log("[Game - HostRoom]", "User " + username + " (" + userId + ") is trying to host a room.", 0, 3, false);

            int correctedUserId = Convert.ToInt32(userId);

            // Role validation
            Classes.User host = dq.RetrieveUser(correctedUserId);

            if (host.RoleId == 1)
            {
                logger.Log("[Game - HostRoom]", "(" + correctedUserId + ") validated.", 1, 3, false);

                Classes.Room newRoom = new Classes.Room
                {
                    RoomOwnerId = socketId,
                    RoomOwner = username,
                    Teacher = host,
                    Config = dq.RetrieveConfig(correctedUserId)
                };

                logger.Log("[Game - HostRoom]", "(" + correctedUserId + ", " + newRoom.RoomCode + ") retrieved configuration with Id " + newRoom.Config.Id + ".", 1, 3, false);

                // Apply config
                newRoom.MaxPlayers = newRoom.Config.MaxPlayers;

                logger.Log("[Game - HostRoom]", "(" + correctedUserId + ", " + newRoom.RoomCode + ") applied configuration.", 1, 3, false);

                _gameManager.Rooms.Add(newRoom);

                logger.Log("[Game - HostRoom]", "(" + correctedUserId + ", " + newRoom.RoomCode + ") room is open and ready.", 2, 3, false);

                await InvokeClientMethodToAllAsync("hostRoom", socketId, newRoom.RoomCode);
                await RetrievePlayerList(newRoom.RoomCode);
            }
            else
            {
                logger.Log("[Game - HostRoom]", "(" + correctedUserId + ") tried to bypass security. Aborting...", 2, 3, false);
            }
        }

        /// <summary>
        /// Join a publicly hosted game room
        /// </summary>
        /// <param name="userId">Client User ID</param>
        /// <param name="socketId">Client unique User socket ID</param>
        /// <param name="username">Client username</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task JoinRoom(string userId, string socketId, string username, string roomCode)
        {
            logger.Log("[Game - JoinRoom]", "User " + username + " (" + userId + ") is trying to join room " + roomCode + ".", 0, 3, false);

            int correctedUserId = Convert.ToInt32(userId);

            // Role validation
            Classes.User client = dq.RetrieveUser(correctedUserId);

            if (client.RoleId == 0)
            {
                logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") validated.", 1, 3, false);

                foreach (Classes.Room room in _gameManager.Rooms)
                {
                    if (room.RoomCode == roomCode)
                    {
                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") found room " + roomCode + ".", 1, 3, false);

                        if (room.Users.Count == room.MaxPlayers)
                        {
                            logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") targeted room " + roomCode + " is already at maximum capacity (" + room.Users.Count + "/" + room.MaxPlayers + ")", 1, 3, false);

                            string message = "Kan niet meedoen met het spel - het gekozen spel is al vol (" + room.Users.Count + "/" + room.MaxPlayers + ").";
                            await InvokeClientMethodToAllAsync("setStateMessage", socketId, message);
                        }
                        else
                        {
                            if (room.RoomState == Classes.Room.State.Waiting)
                            {
                                logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") successfully joined room " + roomCode + ".", 2, 3, false);

                                room.ResetTimer();

                                room.Users.Add(new Classes.User { Id = correctedUserId, SocketId = socketId, Username = username,
                                                                  ReadyToPlay = false, WroteStory = false, ChoseStory = false });

                                await InvokeClientMethodToAllAsync("joinRoom", socketId, roomCode);
                                await RetrievePlayerList(room.RoomCode);
                                await PowerupVisuals(room.RoomCode);
                            }
                            else
                            {
                                string message = "";

                                switch (room.RoomState)
                                {
                                    case Classes.Room.State.Writing:
                                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") cannot join room " + roomCode + " - game has already begun.", 2, 3, false);

                                        message = "Kan niet meedoen met het spel - het gekozen spel is al begonnen.";
                                        break;

                                    case Classes.Room.State.Reading:
                                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") cannot join room " + roomCode + " - game has already begun.", 2, 3, false);

                                        message = "Kan niet meedoen met het spel - het gekozen spel is al begonnen.";
                                        break;

                                    case Classes.Room.State.Finished:
                                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") cannot join room " + roomCode + " - game has already finished.", 2, 3, false);

                                        message = "Kan niet meedoen met het spel - het gekozen spel is al afgelopen.";
                                        break;

                                    case Classes.Room.State.Dead:
                                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") cannot join room " + roomCode + " - room is dead.", 2, 3, false);

                                        message = "Kan niet meedoen met het spel - de kamer is 'dood' en wordt binnenkort opgeruimd.";
                                        break;

                                    default:
                                        logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") cannot join room " + roomCode + ".", 2, 3, false);

                                        message = "Kan niet meedoen met het spel.";
                                        break;
                                }

                                await InvokeClientMethodToAllAsync("setStateMessage", socketId, message);
                            }
                        }
                    }

                }
            }
            else
            {
                logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") tried to bypass security. Aborting...", 2, 3, false);
            }           
        }

        /// <summary>
        /// Leave a game room
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="kicked">Was the User kicked by the host?</param>
        public async Task LeaveRoom(string userId, string socketId, string roomCode, bool kicked)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    // If owner leaves...
                    if (socketId == room.RoomOwnerId)
                    {
                        logger.Log("[Game - LeaveRoom]", "(" + userId + ") is leaving room " + roomCode + ".", 0, 3, false);
                        logger.Log("[Game - LeaveRoom]", "(" + userId + ") was the owner of room " + roomCode + ". Closing room...", 2, 3, false);

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
                            logger.Log("[Game - LeaveRoom]", "(" + userId + ") left the room.", 2, 3, false);

                            room.Users.Remove(user);
                            room.ResetTimer();
                            await InvokeClientMethodToAllAsync("leaveRoom", socketId, kicked);
                            await RetrievePlayerList(room.RoomCode);
                        }
                    }

                    if (kicked)
                    {
                        if (userId == room.Teacher.Id.ToString())
                        {
                            foreach (Classes.User user in room.Users)
                            {
                                if (user.SocketId == socketId)
                                {
                                    logger.Log("[Game - LeaveRoom]", "(" + userId + ") was kicked from the room.", 2, 3, false);

                                    room.Users.Remove(user);
                                    room.ResetTimer();
                                    await InvokeClientMethodToAllAsync("leaveRoom", socketId, kicked);
                                    await RetrievePlayerList(room.RoomCode);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check room states and handle accordingly
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        public async void CheckRoomStates(object sender, ElapsedEventArgs e)
        {
            List<Task> taskList = new List<Task>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomState == Classes.Room.State.Dead)
                {
                    Task removalTask = new Task(() => {
                        foreach (Classes.User user in room.Users)
                        {
                            logger.Log("[Game - CheckRoomStates]", "Room " + room.RoomCode + " was idle for too long. Kicking users...", 0, 3, false);

                            InvokeClientMethodToAllAsync("leaveRoom", user.SocketId);

                            string message = "Room has died. Reason: idle for too long.";
                            InvokeClientMethodToAllAsync("setStateMessage", user.SocketId, message);
                        }

                        logger.Log("[Game - CheckRoomStates]", "Closed room " + room.RoomCode + ".", 2, 3, false);

                        _gameManager.Rooms.Remove(room);
                    });

                    taskList.Add(removalTask);
                    removalTask.Start();
                }
            }

            Task.WaitAll(taskList.ToArray());
        }
        #endregion

        #region Game
        /// <summary>
        /// Continue the game from the game lobby
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task StartGame(string socketId, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == socketId)
                {
                    logger.Log("[Game - StartGame]", "Starting game in room " + roomCode + "...", 0, 3, false);

                    room.RoomState = Classes.Room.State.Writing;
                    room.CurrentStrikes = room.MaxProgressStrikes;

                    if (room.GamePreparation())
                    {
                        logger.Log("[Game - StartGame]", "Preparations are complete. Game in room " + roomCode + " has started.", 2, 3, false);

                        await InvokeClientMethodToAllAsync("startGame", roomCode, room.CurrentGroup);
                        await RetrievePlayerList(room.RoomCode);
                    }
                }
            }
        }

        /// <summary>
        /// Check if all players are through the tutorial. If all players are through, start the writing phase
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
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
                        logger.Log("[Game - ReadyUpPlayer]", roomCode + " all players readied up.", 0, 3, false);
                        logger.Log("[Game - ReadyUpPlayer]", roomCode + " continuing game...", 2, 3, false);

                        await GoToWritePhase(room.RoomOwnerId, roomCode, false);
                    }
                }
            }
        }

        /// <summary>
        /// Skip ReadyUpPlayer and just start the writing phase
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task SkipTutorial(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - SkipTutorial]", roomCode + " tutorial will be skipped.", 0, 3, false);

                    foreach (Classes.User user in room.Users)
                    {
                        user.ReadyToPlay = true;
                    }

                    logger.Log("[Game - SkipTutorial]", roomCode + " continuing game...", 2, 3, false);

                    await GoToWritePhase(room.RoomOwnerId, roomCode, false);
                }
            }
        }

        /// <summary>
        /// Continue to the writing phase
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="ownerId">Room Owner ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="reset">Did the game loop around?</param>
        public async Task GoToWritePhase(string ownerId, string roomCode, bool reset)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.RoomOwnerId == ownerId)
                    {
                        logger.Log("[Game - GoToWritePhase]", roomCode + " trying to go to writing phase...", 0, 3, false);

                        room.RoomState = Classes.Room.State.Writing;

                        logger.Log("[Game - GoToWritePhase]", roomCode + " continuing to writing phase...", 2, 3, false);

                        await InvokeClientMethodToAllAsync("writePhase", roomCode);
                        await StartGameTimer(room.RoomOwnerId, roomCode, 120); // room.Config.MaxWritingTime
                        await RetrieveRootStory(roomCode);
                    }
                }
            }
        }

        /// <summary>
        /// Continue to the reading phase
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="start">Is this the first (starting) round?</param>
        public async Task GoToReadPhase(string userId, string roomCode, bool start)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (dq.RetrieveUser(Convert.ToInt32(userId)).RoleId == 1)
                    {
                        logger.Log("[Game - GoToReadPhase]", "(" + userId + ") validated.", 0, 3, false);

                        room.RoomState = Classes.Room.State.Reading;

                        room.CurrentGroup++;
                        logger.Log("[Game - GoToReadPhase]", roomCode + " has " + room.GroupCount + " groups.", 1, 3, false);
                        logger.Log("[Game - GoToReadPhase]", roomCode + " current group is " + room.CurrentGroup + ".", 1, 3, false);

                        int neededAnswers = 0;

                        foreach (Classes.User user in room.Users)
                        {
                            if (user.GameGroup == room.CurrentGroup)
                            {
                                neededAnswers++;
                            }
                        }

                        room.NeededAnswers = neededAnswers;

                        logger.Log("[Game - GoToReadPhase]", roomCode + " next list of answers needs " + room.NeededAnswers + " answers.", 1, 3, false);

                        if (!start)
                        {
                            if (room.CurrentGroup <= room.GroupCount)
                            {
                                logger.Log("[Game - GoToReadPhase]", roomCode + " next round.", 2, 3, false);

                                foreach (Classes.User user in room.Users)
                                {
                                    await InvokeClientMethodToAllAsync("readPhase", roomCode, user.SocketId, user.GameGroup, room.CurrentGroup);
                                }

                                // Host
                                await InvokeClientMethodToAllAsync("readPhase", roomCode, room.RoomOwnerId, 0, 0);

                                await PowerupVisuals(roomCode);
                                await StartGameTimer(room.RoomOwnerId, roomCode, room.Config.MaxReadingTime);
                                await RetrieveWrittenStories(roomCode, room.CurrentGroup);
                            }
                            else
                            {
                                logger.Log("[Game - GoToReadPhase]", roomCode + " game ended. Showing final leaderboard.", 2, 3, false);

                                await GiveMoney(roomCode, true);
                            }
                        }
                        else
                        {
                            logger.Log("[Game - GoToReadPhase]", roomCode + " first round.", 2, 3, false);

                            foreach (Classes.User user in room.Users)
                            {
                                await InvokeClientMethodToAllAsync("readPhase", roomCode, user.SocketId, user.GameGroup, room.CurrentGroup);
                            }

                            // Host
                            await InvokeClientMethodToAllAsync("readPhase", roomCode, room.RoomOwnerId, 0, 0);

                            await PowerupVisuals(roomCode);
                            await StartGameTimer(room.RoomOwnerId, roomCode, room.Config.MaxReadingTime);
                            await RetrieveWrittenStories(roomCode, room.CurrentGroup);
                        }
                    }
                    else
                    {
                        logger.Log("[Game - GoToReadPhase]", "(" + userId + ") tried to bypass security. Aborting...", 2, 3, false);
                    }                  
                }
            }
        }

        /// <summary>
        /// Start the ingame timer
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="time"></param>
        public async Task StartGameTimer(string userId, string roomCode, int time)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - StartGameTimer]", "(" + userId + ") wants to start a timer in " + roomCode + " for " + time + " seconds.", 0, 3, false);

                    if (room.RoomOwnerId == userId && dq.RetrieveUser(Convert.ToInt32(room.Teacher.Id)).RoleId == 1)
                    {
                        logger.Log("[Game - StartGameTimer]", "(" + userId + ") validated.", 1, 3, false);

                        room.RemainingTime = time;
                        room.gameTimer.Start();

                        logger.Log("[Game - StartGameTimer]", "(" + userId + ") timer started.", 2, 3, false);

                        await InvokeClientMethodToAllAsync("startTimer", roomCode, time);
                    }
                    else
                    {
                        logger.Log("[Game - StartGameTimer]", "(" + userId + ") tried to bypass security. Aborting...", 2, 3, false);
                    }                  
                }
            }
        }

        /// <summary>
        /// Stop the ingame timer
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task StopGameTimer(string userId, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - StopGameTimer]", "(" + userId + ") wants to stop a timer in " + roomCode + ".", 0, 3, false);

                    if (room.RoomOwnerId == userId)
                    {
                        logger.Log("[Game - StopGameTimer]", "(" + userId + ") validated.", 1, 3, false);

                        room.RemainingTime = 0;
                        room.gameTimer.Stop();

                        logger.Log("[Game - StopGameTimer]", "(" + userId + ") timer stopped.", 2, 3, false);

                        await InvokeClientMethodToAllAsync("stopTimer", roomCode);
                    }
                    else
                    {
                        logger.Log("[Game - StopGameTimer]", "(" + userId + ") tried to bypass security. Aborting...", 2, 3, false);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve stories with root = 1 (root stories)
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task RetrieveRootStory(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - RetrieveRootStory]", roomCode + " retrieving root stories...", 0, 3, false);

                    foreach (Classes.User user in room.Users)
                    {
                        string rootStory = room.RetrievedStories[user.GameGroup - 1].Id + ":!|" + room.RetrievedStories[user.GameGroup - 1].Title + ":!|" + room.RetrievedStories[user.GameGroup - 1].Description;

                        logger.Log("[Game - RetrieveRootStory]", roomCode + " (" + user.Id + " | " + user.SocketId + ") got story " + rootStory, 1, 3, false);

                        await InvokeClientMethodToAllAsync("retrieveRootStory", roomCode, user.SocketId, rootStory);
                    }

                    logger.Log("[Game - RetrieveRootStory]", "End", 2, 3, false);
                }
            }
        }

        /// <summary>
        /// Upload a story to the game room and database
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="story">Story string</param>
        public async Task UploadStory(string socketId, string roomCode, string story)
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
                            logger.Log("[Game - UploadStory]", roomCode + " (" + user.Id + " | " + socketId + ") uploaded a story.", 0, 3, false);

                            user.WroteStory = true;

                            string[] tempStory = story.Split("_+_");

                            Classes.Story newStory = new Classes.Story { SocketId = user.SocketId, GameGroup = user.GameGroup, IsRoot = false, Title = tempStory[1], Description = tempStory[2], OwnerId = user.Id, Source = Convert.ToInt32(tempStory[0]) };

                            if (!room.WrittenStories.Contains(newStory))
                            {
                                logger.Log("[Game - UploadStory]", roomCode + " (" + user.Id + " | " + socketId + ") story is '" + newStory.Title + "', '" + newStory.Description + "'", 2, 3, false);

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
                        logger.Log("[Game - UploadStory]", roomCode + " all users wrote their stories.", 0, 3, false);

                        await StopGameTimer(room.RoomOwnerId, room.RoomCode);

                        foreach (Classes.User user in room.Users)
                        {
                            user.WroteStory = false;
                        }

                        logger.Log("[Game - UploadStory]", roomCode + " continuing to read phase.", 2, 3, false);

                        await GoToReadPhase(room.Teacher.Id.ToString(), roomCode, true);
                    }
                }
            }
        }

        /// <summary>
        /// Upload a score to the game room and database
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="answer">Answer string</param>
        public async Task UploadAnswer(string socketId, string roomCode, string answer)
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
                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") uploaded a score: " + answer, 0, 3, false);

                            if (user.GameGroup != room.CurrentGroup)
                            {
                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") parsing score.", 1, 3, false);

                                // Power-up 1 check
                                if (user.PowerupOneActive)
                                {
                                    // Split answers
                                    int answerA = Convert.ToInt32(answer) / 10;  // first digit
                                    int answerB = Convert.ToInt32(answer) % 10;  // second digit

                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") has double answer power-up: A = " + answerA + ", B = " + answerB + " - from " + answer + ".", 1, 3, false);

                                    // Get 'best' answer
                                    if (answerA == room.CorrectAnswer)
                                    {
                                        answer = answerA.ToString();
                                    }
                                    else if (answerB == room.CorrectAnswer)
                                    {
                                        answer = answerB.ToString();
                                    }
                                    else
                                    {
                                        answer = answerA.ToString();
                                    }
                                }

                                // Apply vote to story
                                string storyFromAnswer = room.SentStories[Convert.ToInt32(answer)];
                                string[] storyFromAnswerArray = storyFromAnswer.Split(":!|");
                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") selected story is " + storyFromAnswer + ".", 1, 3, false);

                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    // User-made story
                                    if (storyFromAnswerArray[3] != 0.ToString())
                                    {
                                        logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") selected story is user-made: " + storyFromAnswerArray[3] + ".", 1, 3, false);

                                        if (score.SocketId == storyFromAnswerArray[3])
                                        {
                                            score.AttainedVotes++;
                                            score.LastResult = true;

                                            foreach (Classes.Story story in room.WrittenStories)
                                            {
                                                if (story.SocketId == storyFromAnswerArray[3])
                                                {
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
                                            if (story.OwnerId.ToString() == storyFromAnswerArray[0])
                                            {
                                                if (story.Title == storyFromAnswerArray[1])
                                                {
                                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") selected story is root: " + storyFromAnswerArray[3] + ".", 1, 3, false);

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
                                        score.Answers += answer;

                                        // Answer is correct
                                        if (Convert.ToInt32(answer) == room.CorrectAnswer)
                                        {
                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") answer was correct.", 1, 3, false);

                                            score.CorrectAnswers += '1';
                                            score.LastResult = true;

                                            int followerGain = room.Config.FollowerGain;

                                            if (user.PowerupOneActive)
                                            {
                                                followerGain /= 2;
                                            }
                                            else if (user.PowerupTwoActive)
                                            {
                                                followerGain *= 2;
                                            }

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") gained " + followerGain + " followers.", 1, 3, false);

                                            score.FollowerAmount += followerGain;
                                        }
                                        // Answer is false
                                        else
                                        {
                                            score.CorrectAnswers += '0';
                                            score.LastResult = false;

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") answer was false.", 1, 3, false);

                                            int followerLoss = room.Config.FollowerLoss;

                                            if (user.PowerupOneActive)
                                            {
                                                followerLoss /= 2;
                                            }
                                            else if (user.PowerupTwoActive)
                                            {
                                                followerLoss *= 2;
                                            }

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + socketId + ") lost " + followerLoss + " followers.", 1, 3, false);

                                            score.FollowerAmount -= followerLoss;
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

                        logger.Log("[Game - UploadAnswer]", roomCode + " User has been parsed. Everybody answered, continuing to leaderboard.", 2, 3, false);

                        await StopGameTimer(room.RoomOwnerId, room.RoomCode);
                        await GiveMoney(roomCode, false);
                    }
                    else
                    {
                        logger.Log("[Game - UploadAnswer]", roomCode + " User has been parsed.", 2, 3, false);
                    }
                }
            }
        }

        /// <summary>
        /// Give all players money based off of their followers and votes
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task GiveMoney(string roomCode, bool endGame)
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
                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + user.SocketId + ") acquired some cash.", 0, 3, false);

                                int followerChange = 0;
                                double cashGain = 1.00 + (room.Config.CashPerFollower * score.FollowerAmount);

                                // Per vote
                                bool doubleScore = false;

                                foreach (Classes.Story story in room.WrittenStories)
                                {
                                    if (user.SocketId == story.SocketId)
                                    {
                                        if (story.PowerupActive)
                                        {
                                            doubleScore = true;
                                        }
                                    }                               
                                }

                                if (user.GameGroup == room.CurrentGroup)
                                {
                                    if (score.RoundVotes > 0)
                                    {
                                        if (doubleScore)
                                        {
                                            cashGain += 10.00 + ((room.Config.CashPerVote * score.RoundVotes) * 2);
                                            followerChange = 10 + ((room.Config.FollowerPerVote * score.RoundVotes) * 2);
                                        }
                                        else
                                        {
                                            cashGain += 5.00 + (room.Config.CashPerVote * score.RoundVotes);
                                            followerChange = 5 + (room.Config.FollowerPerVote * score.RoundVotes);
                                        }

                                        score.AttainedVotes += score.RoundVotes;
                                        score.RoundVotes = 0;
                                    }
                                    else
                                    {
                                        if (doubleScore)
                                        {
                                            followerChange = -(room.Config.FollowerLoss * 2);
                                        }
                                        else
                                        {
                                            followerChange = -room.Config.FollowerLoss;
                                        }
                                    }
                                }

                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + user.SocketId + ") gained €" + cashGain + "-.", 1, 3, false);

                                if (followerChange > 0)
                                {
                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + user.SocketId + ") gained " + followerChange + " followers.", 1, 3, false);
                                }
                                else
                                {
                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + user.SocketId + ") lost " + followerChange + " followers.", 1, 3, false);
                                }

                                score.CashAmount += cashGain;
                                score.FollowerAmount += followerChange;

                                if (score.FollowerAmount <= 0)
                                {
                                    score.FollowerAmount = 0;
                                }

                                score.ActualScore = Convert.ToInt32(score.FollowerAmount) * Convert.ToInt32(score.CashAmount);

                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + user.SocketId + ") now has €" + score.CashAmount + "- and " + score.FollowerAmount + " followers.", 2, 3, false);

                                List<string> rankList = GenerateLeaderboard(roomCode);
                                await InvokeClientMethodToAllAsync("updateScore", roomCode, user.SocketId, score.LastResult, score.CashAmount, score.FollowerAmount, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame);
                            }
                        }
                    }

                    await PowerupVisuals(roomCode);
                }
            }
        }

        /// <summary>
        /// Generate the leaderboards based off of given scores
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task ShowLeaderboard(string roomCode, bool endGame)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = GenerateLeaderboard(roomCode);

                    await InvokeClientMethodToAllAsync("showLeaderboard", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame);
                }
            }
        }

        /// <summary>
        /// Generate the leaderboards based off of given scores
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        /// <returns>List of strings</returns>
        public List<string> GenerateLeaderboard(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = new List<string>();
                    List<Classes.Score> scoreList = room.SelectedAnswers;

                    // Sort scorelist
                    scoreList = scoreList.OrderByDescending(o => o.ActualScore).ToList();

                    // Sort by rank
                    foreach (Classes.Score rankedScore in scoreList)
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            if (user.SocketId == rankedScore.SocketId)
                            {
                                int rank = scoreList.IndexOf(rankedScore);
                                string rankString = (rank + 1) + ":|!" + user.SocketId + ":|!" + user.Username + ":|!" + rankedScore.FollowerAmount + ":|!" + rankedScore.CashAmount;
                                rankList.Add(rankString);
                            }
                        }
                    }

                    return rankList;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve user-made stories
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="gameGroup">Current game group</param>
        public async Task RetrieveWrittenStories(string roomCode, int gameGroup)
        {
            List<string> storiesToSend = new List<string>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - RetrieveWrittenStories]", roomCode + " retrieving written stories of group " + gameGroup + "...", 0, 3, false);

                    foreach (Classes.Story story in room.WrittenStories)
                    {
                        if (story.GameGroup == gameGroup)
                        {
                            string toSend = story.OwnerId.ToString() + ":!|" + story.Title + ":!|" + story.Description + ":!|" + story.SocketId;

                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + " got written story " + toSend, 1, 3, false);

                            storiesToSend.Add(toSend);
                        }
                    }

                    Classes.Story rootStory = room.RetrievedStories[gameGroup-1];
                    string root = rootStory.OwnerId.ToString() + ":!|" + rootStory.Title + ":!|" + rootStory.Description + ":!|0";

                    logger.Log("[Game - RetrieveWrittenStories]", roomCode + " got root story " + root, 1, 3, false);

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
                    int storyCount = 0;
                    foreach (string storyString in storiesToSend)
                    {
                        if (storyString == root)
                        {
                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + " correct answer is " + storyCount + ".", 2, 3, false);
                            room.CorrectAnswer = storyCount;
                        }

                        storyCount++;
                    }
                }

                room.SentStories = storiesToSend;
            }

            await InvokeClientMethodToAllAsync("retrieveWrittenStories", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(storiesToSend));
        }

        /// <summary>
        /// Activate a power-up in exchange for some cash
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="powerup">Selected power-up</param>
        public async Task ActivatePowerup(string socketId, string roomCode, string powerup)
        {
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
                                        logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") bought power-up " + powerup + ".", 0, 3, false);

                                        if (powerup == "1" && score.CashAmount >= (20.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Choose two answers for 50% value - €20,00-

                                            user.PowerupOneActive = true;

                                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got power-up " + powerup + " for €" + (20.00 * room.Config.PowerupsCostMult) + ",-.", 2, 3, false);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 1, score.CashAmount);
                                        }
                                        else if (powerup == "2" && score.CashAmount >= (25.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Answers count for 200% value, positively and negatively - €25,00-

                                            user.PowerupTwoActive = true;

                                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got power-up " + powerup + " for €" + (25.00 * room.Config.PowerupsCostMult) + ",-.", 2, 3, false);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 2, score.CashAmount);
                                        }
                                        else if (powerup == "3" && score.CashAmount >= (10.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Cross out 50% of the wrong answers - €10,00-

                                            user.PowerupThreeActive = true;

                                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got power-up " + powerup + " for €" + (10.00 * room.Config.PowerupsCostMult) + ",-.", 2, 3, false);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 3, score.CashAmount);
                                            await ReturnWrongAnswers(roomCode, socketId);
                                        }
                                        else if (powerup == "4" && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Show the amount of answers on each story - €15,00-

                                            user.PowerupFourActive = true;

                                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got power-up " + powerup + " for €" + (15.00 * room.Config.PowerupsCostMult) + ",-.", 2, 3, false);

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

                                                    logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got power-up " + powerup + " for €" + (15.00 * room.Config.PowerupsCostMult) + ",-.", 2, 3, false);
                                                }
                                            }

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 5, score.CashAmount);
                                        }
                                        else
                                        {
                                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + "(" + user.Id + " | " + user.SocketId + ") got no power-up (wrong number, broke).", 2, 3, false);

                                            await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 0, score.CashAmount);
                                        }
                                    }
                                }
                            }
                        }
                    }    
                    else
                    {
                        await InvokeClientMethodToAllAsync("updatePowerups", roomCode, socketId, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Power-up 3 - return wrong answers to strike through
        /// </summary>
        /// <param name="socketId">Unique User socket ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task ReturnWrongAnswers(string socketId, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
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
                                }
                            }

                            // Get rest
                            for (int i = newList.Count; i > 0; i--)
                            {
                                if (!newList[i].Contains(":!|1"))
                                {
                                    newList[i] += ":!|0";
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
        /// Retrieve the list of players currently connected to the game room
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task RetrievePlayerList(string roomCode)
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

                        tempString = user.Username + ":|!" + user.SocketId;

                        UsernameList.Add(tempString);
                    }
                }
            }

            await InvokeClientMethodToAllAsync("retrievePlayerList", ownerId, roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList));
        }

        /// <summary>
        /// Update the power-up button strings and colour based on their cost
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
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
