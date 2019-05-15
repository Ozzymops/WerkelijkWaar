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

            // Make everyone see the current connections
            await RetrieveConnections();

            // Ping
            if (PingOrPong)
            {
                // 'Ping' each connection
                foreach (Classes.Connection c in connectionList)
                {
                    await InvokeClientMethodToAllAsync("takePingAndSendPong", c.SocketId);
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

        #region Chat
        /// <summary>
        /// Send a message to everyone else in the room.
        /// </summary>
        /// <param name="socketId">Sender ID</param>
        /// <param name="username">Sender username</param>
        /// <param name="message">Message</param>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task SendMessage(string socketId, string username, string message, string roomCode)
        {
            dynamic dynamicMessage = new ExpandoObject();

            dynamicMessage.UserId = socketId;
            dynamicMessage.Username = username;
            dynamicMessage.Message = message;
            dynamicMessage.RoomCode = roomCode;

            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (room.RoomCode == roomCode)
                {
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            room.Messages.Add(dynamicMessage);
                            room.ResetTimer();
                            await InvokeClientMethodToAllAsync("pingMessage", username, message, roomCode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to everyone else in the room without a sender.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="roomCode">Room</param>
        /// <returns></returns>
        public async Task ServerMessage(string message, string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (roomCode == room.RoomCode)
                {
                    room.ResetTimer();
                }
            }
            await InvokeClientMethodToAllAsync("serverMessage", message, roomCode);
        }
        #endregion

        #region Rooms
        /// <summary>
        /// Open a room instance.
        /// </summary>
        /// <param name="socketId">Owner ID</param>
        /// <param name="username">Owner username</param>
        /// <returns></returns>
        public async Task CreateRoom(string socketId, string username)
        {
            Classes.Room Room = new Classes.Room();

            Room.RoomOwnerId = socketId;
            Room.RoomOwner = username;
            // Room.Users.Add(new Classes.User { SocketId = socketId, Username = username });

            _gameManager.Rooms.Add(Room);

            await InvokeClientMethodToAllAsync("returnRoomCode", socketId, Room.RoomCode);
            await InvokeClientMethodToAllAsync("retrieveRoomCount", _gameManager.Rooms.Count);
            await RetrieveUserList(Room.RoomCode, false);
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
                            room.Users.Add(new Classes.User { SocketId = socketId, Username = username });
                            await InvokeClientMethodToAllAsync("joinRoom", socketId, roomCode);
                            await RetrieveUserList(room.RoomCode, false);
                        }
                        else
                        {
                            string message = "";

                            switch (room.RoomState)
                            {
                                case Classes.Room.State.InProgress:
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
                        await InvokeClientMethodToAllAsync("retrieveRoomCount", _gameManager.Rooms.Count);
                    }

                    // If regular user leaves
                    foreach (Classes.User user in room.Users)
                    {
                        if (user.SocketId == socketId)
                        {
                            room.Users.Remove(user);
                            room.ResetTimer();
                            await InvokeClientMethodToAllAsync("leaveRoom", socketId, kicked);
                            await RetrieveUserList(room.RoomCode, false);
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
            await InvokeClientMethodToAllAsync("retrieveRoomCount", _gameManager.Rooms.Count);
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
                if (room.RoomCode == roomCode)
                {
                    room.RoomState = Classes.Room.State.InProgress;
                    room.CurrentStrikes = room.MaxProgressStrikes;
                    room.GamePreparation();

                    await InvokeClientMethodToAllAsync("startGame", roomCode, socketId);
                    await RetrieveUserList(room.RoomCode, true);
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
        public async Task RetrieveUserList(string roomCode, bool withGroup)
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

            await InvokeClientMethodToAllAsync("retrieveUserList", roomCode, ownerId, Newtonsoft.Json.JsonConvert.SerializeObject(UsernameList), withGroup);
        }
        #endregion

        #region DEBUG/TESTING
        public async Task CheckRoomState(string roomCode)
        {
            foreach (Classes.Room room in _gameManager.Rooms)
            {
                if (roomCode == room.RoomCode)
                {
                    string state = room.RoomState.ToString() + ": " + room.CurrentStrikes.ToString() + " minutes left before killing the room.";
                    await InvokeClientMethodToAllAsync("checkRoomState", room.RoomCode, state);
                }
            }
        }

        public async Task RetrieveConnections()
        {
            List<string> connectionList = new List<string>();

            foreach (Classes.Connection c in _gameManager.Connections)
            {
                string tempString = "";
                tempString = c.SocketId + ":|!" + c.Timeouts;
                connectionList.Add(tempString);
            }

            await InvokeClientMethodToAllAsync("retrieveConnections", Newtonsoft.Json.JsonConvert.SerializeObject(connectionList));
        }
        #endregion
    }
}
