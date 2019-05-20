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
        private readonly GameManager _gameManager;
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
        public async Task JoinRoom(string socketId, string username, string roomCode)
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
                            room.Users.Add(new Classes.User { SocketId = socketId, Username = username, WroteStory = false, ChoseStory = false });
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
                    // room.GamePreparation();

                    await InvokeClientMethodToAllAsync("startGame", roomCode);
                }
            }
        }

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
                        // continue to write phase
                        await InvokeClientMethodToAllAsync("goToWritePhase", roomCode);
                        await StartGameTimer(roomCode, 10, room.RoomOwnerId);
                    }
                }
            }
        }

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

        public async Task StopGameTimer(string roomCode, string ownerId)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode && room.RoomOwnerId == ownerId)
                {
                    room.RemainingTime = 0;
                    room.gameTimer.Stop();
                    await InvokeClientMethodToAllAsync("stopCountdownTimer", roomCode);

                    if (room.RoomState == Classes.Room.State.Writing)
                    {
                        await InvokeClientMethodToAllAsync("goToReadPhase", roomCode);
                        room.RoomState = Classes.Room.State.Reading;
                    }
                    else if (room.RoomState == Classes.Room.State.Reading)
                    {
                        await InvokeClientMethodToAllAsync("goToReadPhase", roomCode);
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
            List<string> UsernameList = new List<string>();

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    ownerId = room.RoomOwnerId;

                    foreach (Classes.User user in room.Users)
                    {
                        string tempString = "";

                        if (withGroup)
                        {
                            tempString = user.Username + ":|!" + user.SocketId + ":|!" + user.GameGroup + ":|!" + room.Stories[user.GameGroup - 1].Title;
                        }
                        else
                        {
                            tempString = user.Username + ":|!" + user.SocketId;
                        }

                        UsernameList.Add(tempString);
                    }
                }
            }

            await InvokeClientMethodToAllAsync("retrievePlayerList", roomCode, ownerId, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList), withGroup);
        }
        #endregion
    }
}
