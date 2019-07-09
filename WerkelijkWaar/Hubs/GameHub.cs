using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR;

namespace WerkelijkWaar.Hubs
{
    public class GameHub : Hub
    {
        private Classes.Logger logger = new Classes.Logger();
        private Classes.DatabaseQueries dq = new Classes.DatabaseQueries();
        private readonly Classes.ConnectionManager _connectionManager;

        public GameHub(Classes.ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

            // Room state timer
            Timer timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(CheckRoomStates);
            timer.Start();
        }

        public override Task OnConnectedAsync()
        {
            _connectionManager.Connections.Add(Context.ConnectionId);
            Clients.Caller.SendAsync("retrieveConnectionId", Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _connectionManager.Connections.Remove(Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }

        #region Rooms
        /// <summary>
        /// Publicly host a game room
        /// </summary>
        /// <param name="userId">Owner User ID</param>
        /// <param name="username">Owner username</param>
        public async Task HostRoom(string userId, string username)
        {
            int correctedUserId = Convert.ToInt32(userId);

            // Role validation
            Classes.User host = dq.RetrieveUser(correctedUserId);

            if (host.RoleId == 1)
            {
                logger.Log("GameHub HostRoom", "(" + correctedUserId + ") hosting room...", 0, 3, false);

                Classes.Room newRoom = new Classes.Room
                {
                    RoomOwnerId = Context.ConnectionId,
                    RoomOwner = username,
                    Teacher = host,
                    Config = dq.RetrieveConfig(correctedUserId)
                };

                logger.Log("GameHub HostRoom", "(" + correctedUserId + ", " + newRoom.RoomCode + ") retrieved configuration with Id " + newRoom.Config.Id + ".", 1, 3, false);

                // Apply config
                newRoom.MaxPlayers = newRoom.Config.MaxPlayers;

                logger.Log("GameHub HostRoom", "(" + correctedUserId + ", " + newRoom.RoomCode + ") applied configuration.", 1, 3, false);

                _connectionManager.Rooms.Add(newRoom);

                logger.Log("GameHub HostRoom", "(" + correctedUserId + ", " + newRoom.RoomCode + ") room is open and ready.", 2, 3, false);

                await Clients.Caller.SendAsync("hostRoom", newRoom.RoomCode);
                await RetrievePlayerList(newRoom.RoomCode);
            }
            else
            {
                logger.Log("GameHub HostRoom", "(" + correctedUserId + ") tried to bypass security.", 0, 3, false);
                logger.Log("GameHub HostRoom", "Aborting...", 2, 3, false);
            }
        }

        /// <summary>
        /// Join a publicly hosted game room
        /// </summary>
        /// <param name="userId">Client User ID</param>
        /// <param name="connectionId">Client unique User connection ID</param>
        /// <param name="username">Client username</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task JoinRoom(string userId, string username, string roomCode)
        {
            int correctedUserId = Convert.ToInt32(userId);

            // Role validation
            Classes.User client = dq.RetrieveUser(correctedUserId);

            if (client.RoleId == 0)
            {
                foreach (Classes.Room room in _connectionManager.Rooms)
                {
                    if (room.RoomCode == roomCode)
                    {
                        logger.Log("GameHub JoinRoom", "(" + correctedUserId + ") found room " + roomCode + ".", 0, 3, false);

                        if (room.Users.Count == room.MaxPlayers)
                        {
                            logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") targeted room " + roomCode + " is already at maximum capacity (" + room.Users.Count + "/" + room.MaxPlayers + ")", 1, 3, false);

                            string message = "Kan niet meedoen met het spel - het gekozen spel is al vol (" + room.Users.Count + "/" + room.MaxPlayers + ").";
                            await Clients.Caller.SendAsync("setStateMessage", message);
                        }
                        else
                        {
                            if (room.RoomState == Classes.Room.State.Waiting)
                            {
                                logger.Log("[Game - JoinRoom]", "(" + correctedUserId + ") successfully joined room " + roomCode + ".", 2, 3, false);

                                room.ResetTimer();

                                room.Users.Add(new Classes.User
                                {
                                    Id = correctedUserId,
                                    ConnectionId = Context.ConnectionId,
                                    Username = username,
                                    ReadyToPlay = false,
                                    WroteStory = false,
                                    ChoseStory = false
                                });

                                await Clients.Caller.SendAsync("joinRoom", roomCode);
                                await RetrievePlayerList(room.RoomCode);
                                await Clients.All.SendAsync("retrieveConfigurationDataForTutorial", roomCode, room.Config.FollowerGain, room.Config.FollowerPerVote, room.Config.FollowerLoss, room.Config.CashPerFollower);
                                // await PowerupVisuals(room.RoomCode);
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

                                await Clients.Caller.SendAsync("setStateMessage", message);
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
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="kicked">Was the User kicked by the host?</param>
        public async Task LeaveRoom(string userId, string connectionId, string roomCode, bool kicked)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    // If owner leaves...
                    if (connectionId == room.RoomOwnerId)
                    {
                        logger.Log("[Game - LeaveRoom]", "(" + userId + ") is leaving room " + roomCode + ".", 0, 3, false);
                        logger.Log("[Game - LeaveRoom]", "(" + userId + ") was the owner of room " + roomCode + ". Closing room...", 2, 3, false);

                        string message = "";

                        foreach (Classes.User user in room.Users)
                        {
                            await Clients.All.SendAsync("leaveRoom", user.ConnectionId, kicked);

                            message = "Kamer is gesloten omdat de eigenaar de kamer heeft verlaten.";
                            await Clients.All.SendAsync("setStateMessage", user.ConnectionId, message);
                        }

                        await Clients.All.SendAsync("leaveRoom", room.RoomOwnerId, kicked);
                        message = "Kamer is gesloten omdat de eigenaar de kamer heeft verlaten.";
                        await Clients.All.SendAsync("setStateMessage", room.RoomOwnerId, message);

                        _connectionManager.Rooms.Remove(room);
                    }

                    // If regular user leaves
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.ConnectionId == connectionId)
                        {
                            logger.Log("[Game - LeaveRoom]", "(" + userId + ") left the room.", 2, 3, false);

                            room.Users.Remove(user);
                            room.ResetTimer();

                            if (kicked)
                            {
                                if (userId == room.Teacher.Id.ToString())
                                {
                                    logger.Log("[Game - LeaveRoom]", "(" + userId + ") was kicked from the room.", 2, 3, false);

                                    await Clients.All.SendAsync("leaveRoom", connectionId, kicked);
                                }
                            }
                            else
                            {
                                await Clients.Caller.SendAsync("leaveRoom", connectionId, kicked);
                            }

                            await RetrievePlayerList(room.RoomCode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the list of players currently connected to the game room
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task RetrievePlayerList(string roomCode)
        {
            string ownerId = "";
            Classes.Room.State roomState = Classes.Room.State.Waiting;
            List<string> UsernameList = new List<string>();

            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    ownerId = room.RoomOwnerId;

                    foreach (Classes.User user in room.Users)
                    {
                        string tempString = "";

                        tempString = user.Username + ":|!" + user.ConnectionId;

                        UsernameList.Add(tempString);
                    }
                }
            }

            await Clients.All.SendAsync("retrievePlayerList", ownerId, roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList));
        }

        /// <summary>
        /// Check room states and handle accordingly
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        public async void CheckRoomStates(object sender, ElapsedEventArgs e)
        {
            List<Task> taskList = new List<Task>();

            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomState == Classes.Room.State.Dead)
                {
                    Task removalTask = new Task(() => {
                        foreach (Classes.User user in room.Users)
                        {
                            logger.Log("[Game - CheckRoomStates]", "Room " + room.RoomCode + " was idle for too long. Kicking users...", 0, 3, false);

                            Clients.All.SendAsync("leaveRoom", user.ConnectionId);

                            string message = "Room has died. Reason: idle for too long.";
                            Clients.All.SendAsync("setStateMessage", user.ConnectionId, message);
                        }

                        logger.Log("[Game - CheckRoomStates]", "Closed room " + room.RoomCode + ".", 2, 3, false);

                        _connectionManager.Rooms.Remove(room);
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
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task StartGame(string connectionId, string roomCode)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == connectionId)
                {
                    logger.Log("[Game - StartGame]", "Starting game in room " + roomCode + "...", 0, 3, false);

                    room.RoomState = Classes.Room.State.Writing;
                    room.CurrentStrikes = room.MaxProgressStrikes;

                    if (room.GamePreparation())
                    {
                        logger.Log("[Game - StartGame]", "Preparations are complete. Game in room " + roomCode + " has started.", 2, 3, false);

                        await Clients.All.SendAsync("startGame", roomCode, room.CurrentGroup);
                        await RetrievePlayerList(room.RoomCode);
                    }
                }
            }
        }

        /// <summary>
        /// Check if all players are through the tutorial. If all players are through, start the writing phase
        /// </summary>
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task ReadyUpPlayer(string connectionId, string roomCode)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    room.NumberOfReadyPlayers = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.ConnectionId == connectionId)
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
            foreach (Classes.Room room in _connectionManager.Rooms)
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
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.RoomOwnerId == ownerId)
                    {
                        logger.Log("[Game - GoToWritePhase]", roomCode + " trying to go to writing phase...", 0, 3, false);

                        room.RoomState = Classes.Room.State.Writing;

                        logger.Log("[Game - GoToWritePhase]", roomCode + " continuing to writing phase...", 2, 3, false);

                        await Clients.All.SendAsync("writePhase", roomCode);
                        await StartGameTimer(room.RoomOwnerId, roomCode, room.Config.MaxWritingTime);
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
            foreach (Classes.Room room in _connectionManager.Rooms)
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
                            if (room.CurrentGroup < room.GroupCount)
                            {
                                logger.Log("[Game - GoToReadPhase]", roomCode + " next round.", 2, 3, false);

                                foreach (Classes.User user in room.Users)
                                {
                                    user.ChoseStory = false;

                                    await Clients.All.SendAsync("readPhase", roomCode, user.ConnectionId, user.GameGroup, room.CurrentGroup);
                                }

                                // Host
                                await Clients.All.SendAsync("readPhase", roomCode, room.RoomOwnerId, 0, 0);

                                await PowerupVisuals(roomCode);
                                await StartGameTimer(room.RoomOwnerId, roomCode, room.Config.MaxReadingTime);
                                await RetrieveWrittenStories(roomCode, room.CurrentGroup);
                            }
                            else
                            {
                                // final round
                                //logger.Log("[Game - GoToReadPhase]", roomCode + " game ended. Showing final leaderboard.", 2, 3, false);

                                //await GiveMoney(roomCode, true);

                                logger.Log("[Game - GoToReadPhase]", roomCode + " final round.", 2, 3, false);

                                room.FinalRound = true;

                                foreach (Classes.User user in room.Users)
                                {
                                    user.ChoseStory = false;

                                    await Clients.All.SendAsync("readPhase", roomCode, user.ConnectionId, user.GameGroup, room.CurrentGroup);
                                }

                                // Host
                                await Clients.All.SendAsync("readPhase", roomCode, room.RoomOwnerId, 0, 0);

                                await PowerupVisuals(roomCode);
                                await StartGameTimer(room.RoomOwnerId, roomCode, room.Config.MaxReadingTime);
                                await RetrieveWrittenStories(roomCode, room.CurrentGroup);
                            }
                        }
                        else
                        {
                            logger.Log("[Game - GoToReadPhase]", roomCode + " first round.", 2, 3, false);

                            foreach (Classes.User user in room.Users)
                            {
                                user.ChoseStory = false;

                                await Clients.All.SendAsync("readPhase", roomCode, user.ConnectionId, user.GameGroup, room.CurrentGroup);
                            }

                            // Host
                            await Clients.All.SendAsync("readPhase", roomCode, room.RoomOwnerId, 0, 0);

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
            foreach (Classes.Room room in _connectionManager.Rooms)
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

                        await Clients.All.SendAsync("startTimer", roomCode, time);
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
            foreach (Classes.Room room in _connectionManager.Rooms)
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

                        await Clients.All.SendAsync("stopTimer", roomCode);
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
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - RetrieveRootStory]", roomCode + " retrieving root stories...", 0, 3, false);

                    foreach (Classes.User user in room.Users)
                    {
                        string rootStory = room.RetrievedStories[user.GameGroup - 1].Id + ":!|" + room.RetrievedStories[user.GameGroup - 1].Title + ":!|" + room.RetrievedStories[user.GameGroup - 1].Description;

                        logger.Log("[Game - RetrieveRootStory]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") got story " + rootStory, 1, 3, false);

                        await Clients.All.SendAsync("retrieveRootStory", roomCode, user.ConnectionId, rootStory);
                    }

                    logger.Log("[Game - RetrieveRootStory]", "End", 2, 3, false);
                }
            }
        }

        /// <summary>
        /// Upload a story to the game room and database
        /// </summary>
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="story">Story string</param>
        public async Task UploadStory(string connectionId, string roomCode, string story)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    int usersThatWroteStories = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.ConnectionId == connectionId)
                        {
                            logger.Log("[Game - UploadStory]", roomCode + " (" + user.Id + " | " + connectionId + ") uploaded a story.", 0, 3, false);

                            user.WroteStory = true;

                            string[] tempStory = story.Split("_+_");

                            Classes.Story newStory = new Classes.Story { ConnectionId = user.ConnectionId, GameGroup = user.GameGroup, IsRoot = false, Title = tempStory[1], Description = tempStory[2], OwnerId = user.Id, Source = Convert.ToInt32(tempStory[0]) };

                            if (!room.WrittenStories.Contains(newStory))
                            {
                                logger.Log("[Game - UploadStory]", roomCode + " (" + user.Id + " | " + connectionId + ") story is '" + newStory.Title + "', '" + newStory.Description + "'", 2, 3, false);

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
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="answer">Answer string</param>
        public async Task UploadAnswer(string connectionId, string roomCode, string answer)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    int usersThatSelectedAnswers = 0;

                    foreach (Classes.User user in room.Users)
                    {
                        if (user.ConnectionId == connectionId)
                        {
                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") uploaded a score: " + answer, 0, 3, false);

                            if (user.GameGroup != room.CurrentGroup)
                            {
                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") parsing score.", 1, 3, false);

                                // Power-up 1 check
                                if (user.PowerupOneActive)
                                {
                                    // Split answers
                                    int answerA = Convert.ToInt32(answer) / 10;  // first digit
                                    int answerB = Convert.ToInt32(answer) % 10;  // second digit

                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") has double answer power-up: A = " + answerA + ", B = " + answerB + " - from " + answer + ".", 1, 3, false);

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
                                logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") selected story is " + storyFromAnswer + ".", 1, 3, false);

                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    // Reset deltas
                                    score.FollowerDelta = 0;
                                    score.CashDelta = 0;

                                    // User-made story
                                    if (storyFromAnswerArray[3] != 0.ToString())
                                    {
                                        logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") selected story is user-made: " + storyFromAnswerArray[3] + ".", 1, 3, false);

                                        if (score.ConnectionId == storyFromAnswerArray[3])
                                        {
                                            score.AttainedVotes++;
                                            score.RoundVotes++;
                                            score.LastResult = true;

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") score votes: " + score.AttainedVotes + ".", 1, 3, false);

                                            foreach (Classes.Story story in room.WrittenStories)
                                            {
                                                if (story.ConnectionId == storyFromAnswerArray[3])
                                                {
                                                    story.Votes++;
                                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") story votes: " + story.Votes + ".", 1, 3, false);
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
                                                    logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") selected story is root: " + storyFromAnswerArray[3] + ".", 1, 3, false);

                                                    story.Votes++;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Apply variables to score
                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    if (score.ConnectionId == connectionId)
                                    {
                                        score.Answers += answer;

                                        // Answer is correct
                                        if (Convert.ToInt32(answer) == room.CorrectAnswer)
                                        {
                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") answer was correct.", 1, 3, false);

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

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") gained " + followerGain + " followers.", 1, 3, false);

                                            score.FollowerDelta = followerGain;
                                            score.CashDelta = 5.00;
                                        }
                                        // Answer is false
                                        else
                                        {
                                            score.CorrectAnswers += '0';
                                            score.LastResult = false;

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") answer was false.", 1, 3, false);

                                            int followerLoss = room.Config.FollowerLoss;

                                            if (user.PowerupOneActive)
                                            {
                                                followerLoss /= 2;
                                            }
                                            else if (user.PowerupTwoActive)
                                            {
                                                followerLoss *= 2;
                                            }

                                            logger.Log("[Game - UploadAnswer]", roomCode + " (" + user.Id + " | " + connectionId + ") lost " + followerLoss + " followers.", 1, 3, false);

                                            score.FollowerDelta = -followerLoss;
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
                        room.NeededAnswers = 0;

                        logger.Log("[Game - UploadAnswer]", roomCode + " User has been parsed. Everybody answered, continuing to leaderboard.", 2, 3, false);

                        await StopGameTimer(room.RoomOwnerId, room.RoomCode);

                        if (room.FinalRound)
                        {
                            await GiveMoney(roomCode, true);
                        }
                        else
                        {
                            await GiveMoney(roomCode, false);
                        }
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
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    Classes.User lastUser = room.Users.Last();

                    foreach (Classes.User user in room.Users)
                    {
                        foreach (Classes.Score score in room.SelectedAnswers)
                        {
                            if (user.ConnectionId == score.ConnectionId)
                            {
                                logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") acquired some cash.", 0, 3, false);

                                int followerDelta = score.FollowerDelta;
                                double cashDelta = score.CashDelta;

                                // cash calc.
                                if (score.FollowerAmount > 0)
                                {
                                    cashDelta += (room.Config.CashPerFollower * score.FollowerAmount);
                                }


                                // follower calc.
                                bool doubleVoteScore = false;
                                foreach (Classes.Story story in room.WrittenStories)
                                {
                                    if (user.ConnectionId == story.ConnectionId)
                                    {
                                        if (story.PowerupActive)
                                        {
                                            doubleVoteScore = true;
                                        }
                                    }
                                }

                                // follower calc.: indiv. votes
                                if (user.GameGroup == room.CurrentGroup)
                                {
                                    if (score.RoundVotes > 0)
                                    {
                                        if (doubleVoteScore)
                                        {
                                            followerDelta += (room.Config.FollowerPerVote * score.RoundVotes) * 2;
                                            cashDelta += 10.00 + (room.Config.CashPerVote * score.RoundVotes) * 2;
                                        }
                                        else
                                        {
                                            followerDelta += (room.Config.FollowerPerVote * score.RoundVotes);
                                            cashDelta += 5.00 + (room.Config.CashPerVote * score.RoundVotes);
                                        }

                                        logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") gained " + score.RoundVotes + " votes.", 1, 3, false);
                                    }
                                    else
                                    {
                                        if (doubleVoteScore)
                                        {
                                            followerDelta -= room.Config.FollowerLoss * 2;
                                        }
                                        else
                                        {
                                            followerDelta -= room.Config.FollowerLoss;
                                        }
                                    }

                                    score.AttainedVotes += score.RoundVotes;
                                    score.RoundVotes = 0;
                                }

                                // apply followers
                                logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") follower delta: " + followerDelta, 1, 3, false);

                                score.FollowerAmount += followerDelta;

                                if (score.FollowerAmount <= 0)
                                {
                                    logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") followers below 0, gained some pity cash.", 1, 3, false);

                                    score.FollowerAmount = 0;
                                    cashDelta = 1.00;
                                }

                                // apply cash
                                logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") cash delta: " + cashDelta, 1, 3, false);

                                score.CashAmount += cashDelta;

                                // actual score calc.
                                score.ActualScore = Convert.ToInt32(score.FollowerAmount) * Convert.ToInt32(score.CashAmount);

                                logger.Log("[Game - GiveMoney]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") now has €" + score.CashAmount + "- and " + score.FollowerAmount + " followers.", 2, 3, false);

                                List<string> rankList = GenerateLeaderboard(roomCode);

                                await Clients.All.SendAsync("updateScore", roomCode, user.ConnectionId, score.LastResult, score.CashAmount, score.FollowerAmount, followerDelta, cashDelta, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame, false);

                                if (user.Equals(lastUser))
                                {
                                    await Clients.All.SendAsync("updateScore", roomCode, user.ConnectionId, score.LastResult, score.CashAmount, score.FollowerAmount, followerDelta, cashDelta, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame, true);
                                }
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
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    List<string> rankList = GenerateLeaderboard(roomCode);

                    await Clients.All.SendAsync("showLeaderboard", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(rankList), endGame);
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
            foreach (Classes.Room room in _connectionManager.Rooms)
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
                            if (user.ConnectionId == rankedScore.ConnectionId)
                            {
                                int rank = scoreList.IndexOf(rankedScore);
                                string rankString = (rank + 1) + ":|!" + user.ConnectionId + ":|!" + user.Username + ":|!" + rankedScore.FollowerAmount + ":|!" + rankedScore.CashAmount;
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

            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    logger.Log("[Game - RetrieveWrittenStories]", roomCode + " retrieving written stories of group " + gameGroup + "...", 0, 3, false);

                    foreach (Classes.Story story in room.WrittenStories)
                    {
                        if (story.GameGroup == gameGroup)
                        {
                            string toSend = story.OwnerId.ToString() + ":!|" + story.Title + ":!|" + story.Description + ":!|" + story.ConnectionId;

                            logger.Log("[Game - RetrieveWrittenStories]", roomCode + " got written story " + toSend, 1, 3, false);

                            storiesToSend.Add(toSend);
                        }
                    }

                    Classes.Story rootStory = room.RetrievedStories[gameGroup - 1];
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

            await Clients.All.SendAsync("retrieveWrittenStories", roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(storiesToSend));
        }

        /// <summary>
        /// Activate a power-up in exchange for some cash
        /// </summary>
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        /// <param name="powerup">Selected power-up</param>
        public async Task ActivatePowerup(string connectionId, string roomCode, string powerup)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    if (room.Config.PowerupsAllowed)
                    {
                        foreach (Classes.User user in room.Users)
                        {
                            if (user.ConnectionId == connectionId)
                            {
                                logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") activated a powerup.", 1, 3, false);

                                foreach (Classes.Score score in room.SelectedAnswers)
                                {
                                    if (score.ConnectionId == connectionId)
                                    {
                                        int powerupNum = Convert.ToInt32(powerup);

                                        logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") selected powerup is " + powerupNum + ".", 1, 3, false);

                                        if (powerupNum == 1 && score.CashAmount >= (20.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Select two answers for 50% value
                                            user.PowerupOneActive = true;

                                            score.CashAmount -= (20.00 * room.Config.PowerupsCostMult);

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") " + powerupNum + " activated for €" + 20.00 * room.Config.PowerupsCostMult + ".", 2, 3, false);
                                        }
                                        else if (powerupNum == 2 && score.CashAmount >= (25.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Answers count for 200% value
                                            user.PowerupTwoActive = true;

                                            score.CashAmount -= (25.00 * room.Config.PowerupsCostMult);

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ")" + powerupNum + " activated for €" + 25.00 * room.Config.PowerupsCostMult + ".", 2, 3, false);
                                        }
                                        else if (powerupNum == 3 && score.CashAmount >= (10.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Cross out 50% of wrong answers
                                            user.PowerupThreeActive = true;

                                            score.CashAmount -= (10.00 * room.Config.PowerupsCostMult);

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ")" + powerupNum + " activated for €" + 10.00 * room.Config.PowerupsCostMult + ".", 1, 3, false);

                                            await CrossOutWrongAnswers(connectionId, roomCode);
                                        }
                                        else if (powerupNum == 4 && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Show amount of votes
                                            user.PowerupFourActive = true;

                                            score.CashAmount -= (15.00 * room.Config.PowerupsCostMult);

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ")" + powerupNum + " activated for €" + 15.00 * room.Config.PowerupsCostMult + ".", 1, 3, false);

                                            await ReturnAnswerCount(connectionId, roomCode);
                                        }
                                        else if (powerupNum == 5 && score.CashAmount >= (15.00 * room.Config.PowerupsCostMult))
                                        {
                                            // Recieved votes count for 200% value
                                            foreach (Classes.Story story in room.WrittenStories)
                                            {
                                                if (story.ConnectionId == user.ConnectionId)
                                                {
                                                    story.PowerupActive = true;
                                                }
                                            }

                                            score.CashAmount -= (15.00 * room.Config.PowerupsCostMult);

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ")" + powerupNum + " activated for €" + 15.00 * room.Config.PowerupsCostMult + ".", 2, 3, false);
                                        }
                                        else
                                        {
                                            powerupNum = 0;

                                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") too poor or invalid powerup.", 2, 3, false);
                                        }

                                        await Clients.All.SendAsync("activatePowerup", connectionId, roomCode, powerupNum, score.CashAmount);
                                    }
                                }
                            }
                        }
                    }
                    await PowerupVisuals(roomCode);
                }
            }
        }

        /// <summary>
        /// Power-up 3 - return wrong answers to strike through
        /// </summary>
        /// <param name="connectionId">Unique User connection ID</param>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task CrossOutWrongAnswers(string connectionId, string roomCode)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.ConnectionId == connectionId)
                        {
                            // get answer list
                            List<string> sentStories = room.SentStories;
                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") stories:", 1, 3, false);

                            foreach (string story in sentStories)
                            {
                                logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") " + story, 1, 3, false);
                            }

                            // get amount
                            int crosses = sentStories.Count / 2;

                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") crosses: " + crosses, 1, 3, false);

                            // select random stories to strike through
                            int attempts = 10;
                            List<int> crossedStories = new List<int>();

                            while (crosses > 0)
                            {
                                Random rand = new Random();

                                int selected = rand.Next(0, sentStories.Count);

                                if (selected != room.CorrectAnswer && !crossedStories.Contains(selected))
                                {
                                    crossedStories.Add(selected);
                                    attempts = 10;
                                    crosses--;
                                }
                                else
                                {
                                    attempts--;
                                }

                                if (attempts <= 0)
                                {
                                    break;
                                }
                            }

                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") correct answer: " + room.CorrectAnswer + ". crossed stories:", 1, 3, false);

                            foreach (int storyNum in crossedStories)
                            {
                                logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") " + storyNum, 1, 3, false);
                            }

                            logger.Log("[Game - ActivatePowerup]", roomCode + " (" + user.Id + " | " + user.ConnectionId + ") done.", 2, 3, false);

                            await Clients.All.SendAsync("crossOutWrongAnswers", connectionId, roomCode, Newtonsoft.Json.JsonConvert.SerializeObject(crossedStories));
                        }
                    }
                }
            }
        }

        public async Task ReturnAnswerCount(string connectionId, string roomCode)
        {

        }
        #endregion

        #region Visual
        /// <summary>
        /// Update the power-up button strings and colour based on their cost
        /// </summary>
        /// <param name="roomCode">Targeted Room code</param>
        public async Task PowerupVisuals(string roomCode)
        {
            foreach (Classes.Room room in _connectionManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    double[] costArray = { 20.00 * room.Config.PowerupsCostMult, 25.00 * room.Config.PowerupsCostMult, 10.00 * room.Config.PowerupsCostMult, 15.00 * room.Config.PowerupsCostMult, 15.00 * room.Config.PowerupsCostMult };
                    await Clients.All.SendAsync("powerupVisuals", roomCode, room.Config.PowerupsAllowed, Newtonsoft.Json.JsonConvert.SerializeObject(costArray));
                }
            }
        }
        #endregion
    }
}
