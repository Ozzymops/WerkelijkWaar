using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace WerkelijkWaar.Classes
{
    public class Room
    {
        private Classes.Logger logger = new Classes.Logger();
        private Stopwatch stopWatch = new Stopwatch();
        public DatabaseQueries dq = new DatabaseQueries();

        #region Variables
        /// <summary>
        /// Idle timer. Ticks every 60 seconds
        /// </summary>
        public Timer timer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);

        /// <summary>
        /// Game timer. Ticks every second
        /// </summary>
        public Timer gameTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);

        /// <summary>
        /// Teacher (owner) of the room
        /// </summary>
        public User Teacher { get; set; }

        /// <summary>
        /// Configuration used for the room
        /// </summary>
        public Configuration Config { get; set; }

        /// <summary>
        /// Available game states
        /// </summary>
        public enum State { Waiting, Writing, Reading, Finished, Dead };

        /// <summary>
        /// Current game state
        /// </summary>
        public State RoomState { get; set; }

        /// <summary>
        /// List of connected users
        /// </summary>
        public List<Classes.User> Users { get; set; } = new List<Classes.User>();

        /// <summary>
        /// List of retrieved root stories
        /// </summary>
        public List<Classes.Story> RetrievedStories { get; set; } = new List<Classes.Story>();

        /// <summary>
        /// List of uploaded written stories from users
        /// </summary>
        public List<Classes.Story> WrittenStories { get; set; } = new List<Classes.Story>();

        /// <summary>
        /// List of uploaded answers from users
        /// </summary>
        public List<Classes.Score> SelectedAnswers { get; set; } = new List<Classes.Score>();

        /// <summary>
        /// List of stories sent to users (JavaScript JSON)
        /// </summary>
        public List<string> SentStories { get; set; } = new List<string>();

        /// <summary>
        /// Generated room code. Used to connect to the room
        /// </summary>
        public string RoomCode { get; set; }

        /// <summary>
        /// RoomOwner ID
        /// </summary>
        public string RoomOwnerId { get; set; }

        /// <summary>
        /// RoomOwner name
        /// </summary>
        public string RoomOwner { get; set; }

        /// <summary>
        /// Current idle strikes
        /// </summary>
        public int CurrentStrikes;

        /// <summary>
        /// Maximum idle strikes. 3 by default
        /// </summary>
        public int MaxIdleStrikes = 3;

        /// <summary>
        /// Maximum progress strikes (ingame idle strikes). 20 by default
        /// </summary>
        public int MaxProgressStrikes = 20;

        /// <summary>
        /// Maximum amount of allowed players. Acquired from Configuration
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Minimum amount of players needed to start the game. 3 by default
        /// </summary>
        public int MinPlayers = 1;

        /// <summary>
        /// Remaining amount of seconds for current game phase
        /// </summary>
        public int RemainingTime { get; set; }

        /// <summary>
        /// Number of players readied up (that read the tutorial)
        /// </summary>
        public int NumberOfReadyPlayers { get; set; }

        /// <summary>
        /// Current group used for the reading phase
        /// </summary>
        public int CurrentGroup = 0;

        /// <summary>
        /// Total amount of groups
        /// </summary>
        public int GroupCount = 0;

        /// <summary>
        /// Planted correct answer
        /// </summary>
        public int CorrectAnswer = 0;

        /// <summary>
        /// Is it the final round?
        /// </summary>
        public bool FinalRound { get; set; } = false;

        /// <summary>
        /// Needed amount of answers to progress
        /// </summary>
        public int NeededAnswers { get; set; }
        #endregion

        /// <summary>
        /// CONSTRUCTOR: set the room up
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
        /// Generate a random six digit code consisting out of numbers and upper-/lowercase characters. Applied immediately to RoomCode
        /// </summary>
        private void GenerateCode()
        {
            logger.Log("[Room - GenerateCode]", "Generating random code...", 1, 1, false);

            string code = "";

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

            logger.Log("[Room - GenerateCode]", "Generated code is " + RoomCode + ". Room state is now Waiting.", 2, 1, false);
        }

        /// <summary>
        /// Idle timeout ticks
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        public void IdleTimer(object sender, ElapsedEventArgs e)
        {
            if (CurrentStrikes > 0)
            {
                CurrentStrikes -= 1;
            }

            // Kill room after strikes are up
            if (CurrentStrikes <= 0 && RoomState != State.Dead)
            {
                logger.Log("[Room - IdleTimer]", "IdleStrikes are 0.", 0, 1, false);

                RoomState = State.Dead;
                timer.Stop();

                logger.Log("[Room - IdleTimer]", "Room " + RoomCode + " has died and is being removed.", 2, 1, false);
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

        /// <summary>
        /// Prepare the room for gameplay
        /// </summary>
        /// <returns>boolean</returns>
        public bool GamePreparation()
        {
            logger.Log("[Room - GamePreparation]", "Room is being prepared for gameplay.", 0, 1, false);

            try
            {
                int playerCount = Users.Count();

                logger.Log("[Room - GamePreparation]", "Room contains " + playerCount + " players.", 1, 1, false);

                // Enough players?
                if (playerCount >= (MinPlayers * 2))
                {
                    logger.Log("[Room - GamePreparation]", "Room has enough players (" + playerCount + "/" + MinPlayers + ")", 1, 1, false);

                    // Shuffle user list
                    Random rng = new Random();
                    List<User> shuffledUsers = Users;

                    logger.Log("[Room - GamePreparation]", "Shuffling user list...", 1, 1, false);

                    int playersToProcess = playerCount;
                    while (playersToProcess > 1)
                    {
                        playersToProcess--;
                        int userIndex = rng.Next(playersToProcess + 1);
                        User selectedUser = shuffledUsers[userIndex];
                        shuffledUsers[userIndex] = shuffledUsers[playersToProcess];
                        shuffledUsers[playersToProcess] = selectedUser;
                    }

                    logger.Log("[Room - GamePreparation]", "Assigning players to groups...", 1, 1, false);

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

                        GroupCount = (currentGroup - 1);
                    }

                    // Assign stories to groups
                    List<Story> shuffledStories = dq.RetrieveAllStories();

                    logger.Log("[Room - GamePreparation]", "Assigning stories to groups...", 1, 1, false);

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

                    logger.Log("[Room - GamePreparation]", "Creating score objects for the players...", 1, 1, false);

                    // Create empty Score list
                    foreach (User user in Users)
                    {
                        Score tempScore = new Score { OwnerId = user.Id, SocketId = user.SocketId, Answers = "", AttainedVotes = 0, CashAmount = 0.00, CorrectAnswers = "", GameType = 0, FollowerAmount = 0, Date = DateTime.Now };
                        SelectedAnswers.Add(tempScore);
                    }

                    logger.Log("[Room - GamePreparation]", "Room is prepared.", 2, 1, false);

                    return true;
                }
                else
                {
                    logger.Log("[Room - GamePreparation]", "Room does not have enough players (" + playerCount + "/" + MinPlayers + ")", 2, 1, false);

                    return false;
                }
            }
            catch (Exception exception)
            {
                logger.Log("[Room - GamePreparation]", "Something went wrong:\n" + exception, 2, 1, false);
            }

            return false;
        }

        /// <summary>
        /// Game timer for actions such as the writing/reading countdown
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        public void GameTimer(object sender, ElapsedEventArgs e)
        {
            RemainingTime -= 1;
        }
    }
}
