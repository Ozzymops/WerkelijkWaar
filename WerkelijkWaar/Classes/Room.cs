using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace WerkelijkWaar.Classes
{
    public class Room
    {
        // Static
        public string RoomCode { get; set; }
        public string RoomOwnerId { get; set; }
        public string RoomOwner { get; set; }
        public State RoomState { get; set; }
        public Configuration Config { get; set; }
        public int MaxIdleStrikes = 3;
        public int MaxProgressStrikes = 20;
        public int MaxPlayers = 30;
        public int MinPlayers = 3;
        // Dynamic
        public int CurrentStrikes;
        public enum State { Waiting, Writing, Reading, Finished, Dead };
        public List<Classes.User> Users { get; set; } = new List<Classes.User>();
        public List<List<Classes.User>> Groups { get; set; } = new List<List<Classes.User>>();
        public List<Classes.Story> RetrievedStories { get; set; } = new List<Classes.Story>();
        public List<Classes.Story> WrittenStories { get; set; } = new List<Classes.Story>();
        public int RemainingTime { get; set; }
        public int NumberOfReadyPlayers { get; set; }
        public int CurrentGroup = 0;

        // Configuration

        // Timers
        public Timer timer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // Tick every sixty seconds
        public Timer gameTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);


        /// <summary>
        /// Constructor: generate random code and set timer.
        /// </summary>
        public Room()
        {
            GenerateCode();

            // Idle timer
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(IdleTimer);
            timer.Start();

            // Game timer
            gameTimer.AutoReset = true;
            gameTimer.Elapsed += new ElapsedEventHandler(GameTimer);
        }

        /// <summary>
        /// Generate code - random six digit code consisting of upper- and lowercase letters and numbers.
        /// </summary>
        private void GenerateCode()
        {
            string code = "";

            // Six digit code
            for (int i = 0; i < 6; i++)
            {
                Random rng = new Random();
                if (rng.Next(0, 2) == 0)
                {
                    // Letter
                    if (rng.Next(0, 2) == 0)
                    {
                        // Upper - dec: 65 to 90
                        code += Char.ConvertFromUtf32(rng.Next(65, 91));
                    }
                    else
                    {
                        // Lower - dec: 97 to 122
                        code += Char.ConvertFromUtf32(rng.Next(97, 123));
                    }
                }
                else
                {
                    // Number
                    code += rng.Next(0, 10).ToString();
                }
            }

            RoomCode = code;
            RoomState = State.Waiting;
        }

        /// <summary>
        /// Idle timer - check if the room is still being actively used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void IdleTimer(object sender, ElapsedEventArgs e)
        {
            // Tick timer - reset in every function call from handler
            if (CurrentStrikes > 0)
            {
                CurrentStrikes -= 1;
            }

            // Die after strikes are up
            if (CurrentStrikes <= 0 && RoomState != State.Dead)
            {
                RoomState = State.Dead;
                timer.Stop();
            }
        }

        /// <summary>
        /// Reset the idle timer to the maximum allowed strikes.
        /// </summary>
        public void ResetTimer()
        {
            if (RoomState == State.Waiting)
            {
                CurrentStrikes = MaxIdleStrikes;
            }
            else if (RoomState == State.Writing)
            {
                CurrentStrikes = MaxProgressStrikes;
            }
            else if (RoomState == State.Reading)
            {
                CurrentStrikes = MaxProgressStrikes;
            }
        }

        public bool GamePreparation()
        {
            DatabaseQueries dq = new DatabaseQueries();
            int playerCount = Users.Count();

            // Enough players?
            if (playerCount >= (MinPlayers*2))
            {
                // Shuffle user list
                Random rng = new Random();
                List<User> shuffledUsers = Users;

                int playersToProcess = playerCount;
                while (playersToProcess > 1)
                {
                    playersToProcess--;
                    int userIndex = rng.Next(playersToProcess + 1);
                    User selectedUser = shuffledUsers[userIndex];
                    shuffledUsers[userIndex] = shuffledUsers[playersToProcess];
                    shuffledUsers[playersToProcess] = selectedUser;
                }

                // Assign players to groups
                int currentGroup = 1;
                int maxPlayersInGroup = MinPlayers;
                int unevenPlayers = 0;

                // Groups of six
                if (shuffledUsers.Count % MinPlayers == 0)
                {
                    unevenPlayers = 0;
                }
                // Groups of x
                else if (shuffledUsers.Count % MinPlayers >= 1)
                {
                    unevenPlayers = (shuffledUsers.Count % MinPlayers);
                }

                foreach (User user in shuffledUsers)
                {
                    // One extra player per group
                    if (unevenPlayers == 1)
                    {
                        maxPlayersInGroup += unevenPlayers;
                        unevenPlayers--;
                    }
                    // x extra players per group
                    else if (unevenPlayers >= 2)
                    {
                        maxPlayersInGroup += 1;
                        unevenPlayers -= 1;
                    }

                    user.GameGroup = currentGroup;
                    maxPlayersInGroup--;

                    if (maxPlayersInGroup <= 0)
                    {
                        currentGroup++;
                        maxPlayersInGroup = MinPlayers;
                    }
                }

                // Assign stories to groups
                List<Story> shuffledStories = dq.RetrieveAllStories();

                // Shuffle stories
                int storiesToProcess = shuffledStories.Count;
                while (storiesToProcess > 1)
                {
                    storiesToProcess--;
                    int storyIndex = rng.Next(storiesToProcess + 1);
                    Story selectedStory = shuffledStories[storyIndex];
                    shuffledStories[storyIndex] = shuffledStories[storiesToProcess];
                    shuffledStories[storiesToProcess] = selectedStory;
                }

                for (int group = 1; group < currentGroup; group++)
                {
                    RetrievedStories.Add(shuffledStories[group - 1]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void GameTimer(object sender, ElapsedEventArgs e)
        {
            RemainingTime -= 1;
        }
    }
}
