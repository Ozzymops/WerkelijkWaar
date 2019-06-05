using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class DatabaseQueries
    {
        // Standard classes
        Classes.Logger logger = new Classes.Logger();
        Stopwatch stopWatch = new Stopwatch();

        // Construct connection string
        private string connectionString = ConfigurationManager.AppSettings["connectionString"] + "User Id='" + ConfigurationManager.AppSettings["dbUsername"] + "';" +
            "password='" + ConfigurationManager.AppSettings["dbpassword"] + "';";

        // CRUD - CREATE, READ, UPDATE, DELETE
        #region CREATE
        /// <summary>
        /// Register a user to the database.
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>boolean</returns>
        public bool RegisterUser(User user)
        {
            stopWatch.Restart();
            logger.Log("[RegisterUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to register user " + user.Username, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("AddUser", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@pRoleId", user.RoleId));
                command.Parameters.Add(new SqlParameter("@pGroup", user.Group));
                command.Parameters.Add(new SqlParameter("@pName", user.Name));
                command.Parameters.Add(new SqlParameter("@pSurname", user.Surname));
                command.Parameters.Add(new SqlParameter("@pUsername", user.Username));
                command.Parameters.Add(new SqlParameter("@pPassword", user.Password));

                var output = new SqlParameter("@responseMessage", System.Data.SqlDbType.NVarChar);
                output.Direction = System.Data.ParameterDirection.Output;
                output.Size = 255;
                command.Parameters.Add(output);

                logger.Log("[RegisterUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                                "- Role Id: " + user.RoleId + ";\n" +
                                                "- Group Id: " + user.Group + ";\n" +
                                                "- Name: " + user.Name + ";\n" +
                                                "- Surname: " + user.Surname + ";\n" +
                                                "- Username: " + user.Username, 1, 1, false);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        logger.Log("[RegisterUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. User " + user.Username + " succesfully registered.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }
                    else
                    {
                        logger.Log("[RegisterUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Failed to register user " + user.Username + ": " + command.Parameters["@responseMessage"].Value.ToString(), 2, 1, false);

                        stopWatch.Stop();
                        return false;
                    }

                }
                catch (Exception exception)
                {
                    logger.Log("[RegisterUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Save a score object in the database
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>boolean</returns>
        public bool CreateScore(Score score)
        {
            stopWatch.Restart();
            logger.Log("[CreateScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Adding score of user " + score.OwnerId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Score] SELECT @ownerId, @gameType, @followers, @cash, @votes, @answers, @date", connection);

                command.Parameters.Add(new SqlParameter("@ownerId", score.OwnerId));
                command.Parameters.Add(new SqlParameter("@gameType", score.GameType));
                command.Parameters.Add(new SqlParameter("@answers", score.Answers));
                command.Parameters.Add(new SqlParameter("@date", score.Date));

                logger.Log("[CreateScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + score.OwnerId + ";\n" +
                                "- Gametype: " + score.GameType + ";\n" +
                                "- Answers: " + score.Answers + ";\n" +
                                "- Followers: " + score.FollowerAmount + ";\n" +
                                "- Cash: " + score.CashAmount + ";\n" +
                                "- Votes: " + score.AttainedVotes + ";\n" +
                                "- Date: " + score.Date, 1, 1, false);

                // Check for nullables
                if (score.FollowerAmount == -1)
                {
                    command.Parameters.Add(new SqlParameter("@followers", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(new SqlParameter("@followers", score.FollowerAmount));
                }

                if (score.CashAmount == -1)
                {
                    command.Parameters.Add(new SqlParameter("@cash", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(new SqlParameter("@cash", Convert.ToDecimal(score.CashAmount)));
                }

                if (score.AttainedVotes == -1)
                {
                    command.Parameters.Add(new SqlParameter("@votes", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(new SqlParameter("@votes", score.AttainedVotes));
                }

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[CreateScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Score for " + score.OwnerId + " succesfully added.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[CreateScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Failed to add score for " + score.OwnerId + ".", 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[CreateScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Save a story object in the database
        /// </summary>
        /// <param name="story">Story</param>
        /// <returns>boolean</returns>
        public bool CreateStory(Story story)
        {
            stopWatch.Restart();
            logger.Log("[CreateStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to create story for user " + story.OwnerId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Story] SELECT @ownerId, @isRoot, @title, @description, @date, @status, @source", connection);

                command.Parameters.Add(new SqlParameter("@ownerId", story.OwnerId));
                command.Parameters.Add(new SqlParameter("@isRoot", false));
                command.Parameters.Add(new SqlParameter("@title", story.Title));
                command.Parameters.Add(new SqlParameter("@description", story.Description));
                command.Parameters.Add(new SqlParameter("@date", story.Date));
                command.Parameters.Add(new SqlParameter("@status", 2));
                command.Parameters.Add(new SqlParameter("@source", story.Source));

                logger.Log("[CreateStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + story.OwnerId + ";\n" +
                                "- Root: " + story.IsRoot + ";\n" +
                                "- Title: " + story.Title + ";\n" +
                                "- Description: " + story.Description + ";\n" +
                                "- Status: " + 2 + ";\n" +
                                "- Source: " + story.Source + ";\n" +
                                "- Date: " + story.Date, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[CreateStory", stopWatch.ElapsedMilliseconds.ToString() + "ms. Story for " + story.OwnerId + " successfully added.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[CreateStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Failed to add story for " + story.OwnerId + ".", 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[CreateStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Save a group object in the database
        /// </summary>
        /// <param name="group">Group</param>
        /// <returns>boolean</returns>
        public bool CreateGroup(Group group)
        {
            stopWatch.Restart();
            logger.Log("[CreateGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to create group with name " + group.GroupName, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Group] SELECT @SchoolId, @GroupId, @GroupName", connection);

                command.Parameters.Add(new SqlParameter("@SchoolId", group.SchoolId));
                command.Parameters.Add(new SqlParameter("@GroupId", group.GroupId));
                command.Parameters.Add(new SqlParameter("@GroupName", group.GroupName));

                logger.Log("[CreateGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- School Id: " + group.SchoolId + ";\n" +
                                "- Group Id: " + group.GroupId + ";\n" +
                                "- Group name: " + group.GroupName, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[CreateGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Group " + group.GroupName + " successfully added.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[CreateGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Failed to add group " + group.GroupName + ".", 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[CreateGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Save a school object in the database
        /// </summary>
        /// <param name="school">School</param>
        /// <returns>boolean</returns>
        public bool CreateSchool(School school)
        {
            stopWatch.Restart();
            logger.Log("[CreateSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to create school " + school.SchoolName, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [School] SELECT @SchoolName", connection);

                command.Parameters.Add(new SqlParameter("@SchoolName", school.SchoolName));

                logger.Log("[CreateSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- School name: " + school.SchoolName, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[CreateSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. School " + school.SchoolName + " successfully added.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[CreateSchool]", "Failed to add school " + school.SchoolName + ".", 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[CreateSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Save a configuration object in the database
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>boolean</returns>
        public bool CreateConfig(Configuration config)
        {
            stopWatch.Restart();
            logger.Log("[CreateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Adding score of user " + config.OwnerId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Configuration] SELECT @ownerId, @maxWritingTime, @maxReadingTime, @followerGain, @followerLoss, " +
                                                    "@followerPerVote, @cashPerFollower, @cashPerVote, @maxPlayers, @powerupsAllowed, @powerupsCostMult", connection);

                command.Parameters.Add(new SqlParameter("@ownerId", config.OwnerId));
                command.Parameters.Add(new SqlParameter("@maxWritingTime", config.MaxWritingTime));
                command.Parameters.Add(new SqlParameter("@maxReadingTime", config.MaxReadingTime));
                command.Parameters.Add(new SqlParameter("@followerGain", config.FollowerGain));
                command.Parameters.Add(new SqlParameter("@followerLoss", config.FollowerLoss));
                command.Parameters.Add(new SqlParameter("@followerPerVote", config.FollowerPerVote));
                command.Parameters.Add(new SqlParameter("@cashPerFollower", config.CashPerFollower));
                command.Parameters.Add(new SqlParameter("@cashPerVote", config.CashPerVote));
                command.Parameters.Add(new SqlParameter("@maxPlayers", config.MaxPlayers));
                command.Parameters.Add(new SqlParameter("@powerupsAllowed", config.PowerupsAllowed));
                command.Parameters.Add(new SqlParameter("@powerupsCostMult", config.PowerupsCostMult));

                logger.Log("[CreateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + config.OwnerId + ";\n" +
                                "- Max writing time: " + config.MaxWritingTime + ";\n" +
                                "- Max reading time: " + config.MaxReadingTime + ";\n" +
                                "- Follower gain: " + config.FollowerGain + ";\n" +
                                "- Follower loss: " + config.FollowerLoss + ";\n" +
                                "- Followers per vote: " + config.FollowerPerVote + ";\n" +
                                "- Cash per follower: " + config.CashPerFollower + ";\n" +
                                "- Cash per vote: " + config.CashPerVote + ";\n" +
                                "- Max players: " + config.MaxPlayers + ";\n" +
                                "- Powerups allowed? " + config.PowerupsAllowed + ";\n" +
                                "- Powerup cost multiplier: " + config.PowerupsCostMult, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[CreateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Config for " + config.OwnerId + " succesfully added.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[CreateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Failed to add config for " + config.OwnerId + ".", 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[CreateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }
        #endregion

        #region READ
        /// <summary>
        /// Check if user exists in database
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>User ID</returns>
        public int CheckLogin(string username, string password)
        {
            stopWatch.Start();
            logger.Log("[CheckLogin]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Checking login for " + username, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("LogIn", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@pUsername", username));
                command.Parameters.Add(new SqlParameter("@pPassword", password));

                var output = new SqlParameter("@responseMessage", System.Data.SqlDbType.NVarChar);
                output.Direction = System.Data.ParameterDirection.Output;
                output.Size = 255;
                command.Parameters.Add(output);

                var id = new SqlParameter("@userId", System.Data.SqlDbType.Int);
                id.Direction = System.Data.ParameterDirection.Output;
                command.Parameters.Add(id);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        logger.Log("[CheckLogin]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + username, 2, 1, false);

                        stopWatch.Stop();
                        return (int)command.Parameters["@userId"].Value;
                    }
                    else
                    {
                        logger.Log("[CheckLogin]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Did not find " + username, 2, 1, false);

                        stopWatch.Stop();
                        return 0;
                    }
                }
                catch (Exception exception)
                {
                    logger.Log("[CheckLogin]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return 0;
                }
            }
        }

        /// <summary>
        /// Retrieve a user from the database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User</returns>
        public User RetrieveUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Retrieving user " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [User] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                logger.Log("[RetrieveUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found user " + userId, 2, 1, false);

                                User newUser = new User
                                {
                                    Id = (int)reader["Id"],
                                    Group = (int)reader["GroupId"],
                                    Name = (string)reader["Name"],
                                    Surname = (string)reader["Surname"],
                                    Username = (string)reader["Username"],
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    LoginAttempts = Convert.ToInt32(reader["Attempts"])
                                };

                                if (reader["ImageSource"] != System.DBNull.Value)
                                {
                                    newUser.ImageSource = (string)reader["ImageSource"];
                                }

                                stopWatch.Stop();
                                return newUser;
                            }
                        }
                    }

                    logger.Log("[RetrieveUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find " + userId, 2, 1, false);
                    
                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a list of users that belong to a certain Group ID from the database
        /// </summary>
        /// <param name="group">Group ID</param>
        /// <returns>List of Users</returns>
        public List<User> RetrieveUserListByGroup(int group)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveUserListByGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Retrieving users from group " + group, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [User] WHERE [GroupId] = @group AND [RoleId] = 0", connection);
                command.Parameters.Add(new SqlParameter("@group", group));

                List<User> UserList = new List<User>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                User newUser = new User
                                {
                                    Id = (int)reader["Id"],
                                    Group = (int)reader["GroupId"],
                                    Name = (string)reader["Name"],
                                    Surname = (string)reader["Surname"],
                                    Username = (string)reader["Username"],
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    LoginAttempts = Convert.ToInt32(reader["Attempts"])
                                };

                                if (reader["ImageSource"] != System.DBNull.Value)
                                {
                                    newUser.ImageSource = (string)reader["ImageSource"];
                                }

                                logger.Log("[RetrieveUserListByGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newUser.Id + " - " + newUser.Username, 1, 1, false);

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            logger.Log("[RetrieveUserListByGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + "users", 2, 1, false);

                            stopWatch.Stop();
                            return UserList;
                        }
                    }

                    logger.Log("[RetrieveUserListByGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any users", 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveUserListByGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a list of all users from the database
        /// </summary>
        /// <returns>List of Users</returns>
        public List<User> RetrieveAllUsers()
        {
            stopWatch.Restart();
            logger.Log("[RetrieveAllUsers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Retrieving all users", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [User]", connection);

                List<User> UserList = new List<User>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                User newUser = new User
                                {
                                    Id = (int)reader["Id"],
                                    Group = (int)reader["GroupId"],
                                    Name = (string)reader["Name"],
                                    Surname = (string)reader["Surname"],
                                    Username = (string)reader["Username"],
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    LoginAttempts = Convert.ToInt32(reader["Attempts"])
                                };

                                if (reader["ImageSource"] != System.DBNull.Value)
                                {
                                    newUser.ImageSource = (string)reader["ImageSource"];
                                }

                                logger.Log("[RetrieveAllUsers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newUser.Id + " - " + newUser.Username, 1, 1, false);

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            logger.Log("[RetrieveAllUsers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + "users", 2, 1, false);

                            stopWatch.Stop();
                            return UserList;
                        }
                    }

                    logger.Log("[RetrieveAllUsers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any users", 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveAllUsers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a list of scores that belong to a certain User ID from the database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of Scores</returns>
        public List<Score> RetrieveScoresOfUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Retrieving scores of user " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Score] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                List<Score> ScoreList = new List<Score>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                Score newScore = new Score
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    Date = (DateTime)reader["Date"],
                                    GameType = (int)reader["GameType"]
                                };

                                if (reader["AttainedVotes"] != System.DBNull.Value)
                                {
                                    newScore.AttainedVotes = (int)reader["AttainedVotes"];
                                }

                                if (reader["CashAmount"] != System.DBNull.Value)
                                {
                                    newScore.CashAmount = Convert.ToDouble(reader["CashAmount"]);
                                }

                                if (reader["FollowerAmount"] != System.DBNull.Value)
                                {
                                    newScore.FollowerAmount = (int)reader["FollowerAmount"];
                                }

                                if (reader["Answers"] != System.DBNull.Value)
                                {
                                    newScore.Answers = (string)reader["Answers"];
                                }
                                else
                                {
                                    newScore.Answers = "";
                                }

                                logger.Log("[RetrieveScoresOfUser]", "Got: " + newScore.Id, 1, 1, false);

                                ScoreList.Add(newScore);
                                rowCount++;
                            }

                            logger.Log("[RetrieveScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " scores from user " + userId, 2, 1, false);

                            stopWatch.Stop();
                            return ScoreList;
                        }
                    }

                    logger.Log("[RetrieveScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any scores of user " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a list of stories that belong to a certain User ID from the database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of Stories</returns>
        public List<Story> RetrieveStoriesOfUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve stories of user " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                List<Story> StoryList = new List<Story>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                Story newStory = new Story
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    Date = (DateTime)reader["Date"],
                                    Description = (string)reader["Description"],
                                    IsRoot = Convert.ToBoolean(reader["IsRoot"]),
                                    Title = (string)reader["Title"],
                                    Status = (int)reader["Status"],
                                    Source = (int)reader["Source"]
                                };

                                logger.Log("[RetrieveStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title, 1, 1, false);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            logger.Log("[RetrieveStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories from user " + userId, 2, 1, false);

                            stopWatch.Stop();
                            return StoryList;
                        }
                    }

                    logger.Log("[RetrieveStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any stories of user " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve all stories from the database
        /// </summary>
        /// <returns>List of Stories</returns>
        public List<Story> RetrieveAllStories()
        {
            stopWatch.Restart();
            logger.Log("[RetrieveAllStories]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve all stories.", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story] WHERE [Status] = 2", connection);

                List<Story> StoryList = new List<Story>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                Story newStory = new Story
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    Date = (DateTime)reader["Date"],
                                    Description = (string)reader["Description"],
                                    IsRoot = Convert.ToBoolean(reader["IsRoot"]),
                                    Title = (string)reader["Title"],
                                    Status = (int)reader["Status"],
                                    Source = (int)reader["Source"]
                                };

                                logger.Log("[RetrieveAllStories]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title, 1, 1, false);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            logger.Log("[RetrieveAllStories]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories.", 2, 1, false);

                            stopWatch.Stop();
                            return StoryList;
                        }
                    }

                    logger.Log("[RetrieveAllStories]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any stories", 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveAllStories]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a story from the database
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <returns>Story</returns>
        public Story RetrieveStory(int storyId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve story with id " + storyId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", storyId));

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                logger.Log("[RetrieveStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found story with id " + storyId + " - " + (string)reader["Title"], 1, 1, false);

                                return new Story
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    Title = (string)reader["Title"],
                                    Description = (string)reader["Description"],
                                    Date = (DateTime)reader["Date"],
                                    IsRoot = Convert.ToBoolean(reader["IsRoot"]),
                                    Status = (int)reader["Status"],
                                    Source = (int)reader["Source"]
                                };
                            }
                        }
                    }

                    logger.Log("[RetrieveStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a score from the database
        /// </summary>
        /// <param name="scoreId">Score ID</param>
        /// <returns>Score</returns>
        public Score RetrieveScore(int scoreId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve score with id " + scoreId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Score] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", scoreId));

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                logger.Log("[RetrieveScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found score with id " + scoreId, 1, 1, false);

                                Score newScore = new Score
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    GameType = (int)reader["GameType"],
                                    Date = (DateTime)reader["Date"]
                                };

                                if (reader["AttainedVotes"] != System.DBNull.Value)
                                {
                                    newScore.AttainedVotes = (int)reader["AttainedVotes"];
                                }

                                if (reader["CashAmount"] != System.DBNull.Value)
                                {
                                    newScore.CashAmount = Convert.ToDouble(reader["CashAmount"]);
                                }

                                if (reader["FollowerAmount"] != System.DBNull.Value)
                                {
                                    newScore.FollowerAmount = (int)reader["FollowerAmount"];
                                }

                                if (reader["Answers"] != System.DBNull.Value)
                                {
                                    newScore.Answers = (string)reader["Answers"];
                                }
                                else
                                {
                                    newScore.Answers = "";
                                }

                                return newScore;
                            }
                        }
                    }

                    logger.Log("[RetrieveScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + scoreId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveScore]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve all stories with a status of 0 or 1 from the database
        /// </summary>
        /// <returns>List of Stories</returns>
        public List<Story> RetrieveStoryQueue()
        {
            stopWatch.Restart();
            logger.Log("[RetrieveStoryQueue]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve story queue", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story] WHERE [Status] = 0 OR [Status] = 1", connection);

                List<Story> StoryList = new List<Story>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                Story newStory = new Story
                                {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    Date = (DateTime)reader["Date"],
                                    Description = (string)reader["Description"],
                                    IsRoot = Convert.ToBoolean(reader["IsRoot"]),
                                    Title = (string)reader["Title"],
                                    Status = (int)reader["Status"],
                                    Source = (int)reader["Source"]
                                };

                                logger.Log("[RetrieveStoryQueue]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title + ", status: " + newStory.Status, 1, 1, false);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            logger.Log("[RetrieveStoryQueue]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories.", 2, 1, false);

                            stopWatch.Stop();
                            return StoryList;
                        }
                    }

                    logger.Log("[RetrieveStoryQueue]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any stories in queue", 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveStoryQueue]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve all schools from the database
        /// </summary>
        /// <returns>List of Schools</returns>
        public List<School> RetrieveSchools()
        {
            stopWatch.Restart();
            logger.Log("[RetrieveSchools]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve schools", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [School]", connection);

                List<School> SchoolList = new List<School>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                School newSchool = new School
                                {
                                    Id = (int)reader["Id"],
                                    SchoolName = (string)reader["Name"]
                                };

                                logger.Log("[RetrieveSchools]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newSchool.Id + " - " + newSchool.SchoolName, 1, 1, false);

                                SchoolList.Add(newSchool);
                                rowCount++;
                            }

                            logger.Log("[RetrieveSchools]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " schools.", 2, 1, false);

                            stopWatch.Stop();
                            return SchoolList;
                        }
                    }

                    logger.Log("[RetrieveSchools]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any schools.", 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveSchools]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a school from the database
        /// </summary>
        /// <param name="schoolId">School ID</param>
        /// <returns>School</returns>
        public School RetrieveSchool(int schoolId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve school " + schoolId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [School] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", schoolId));

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                School newSchool = new School
                                {
                                    Id = (int)reader["Id"],
                                    SchoolName = (string)reader["Name"]
                                };

                                logger.Log("[RetrieveSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newSchool.Id + " - " + newSchool.SchoolName, 1, 1, false);

                                stopWatch.Stop();
                                return newSchool;
                            }
                        }
                    }

                    logger.Log("[RetrieveSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any schools with id " + schoolId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve all groups that belong to a certain School ID from the database
        /// </summary>
        /// <param name="schoolId">School ID</param>
        /// <returns>List of Groups</returns>
        public List<Group> RetrieveGroupsOfSchool(int schoolId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveGroupsOfSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve groups of school " + schoolId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Group] WHERE [SchoolId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", schoolId));

                List<Group> GroupList = new List<Group>();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int rowCount = 0;

                            while (reader.Read())
                            {
                                Group newGroup = new Group
                                {
                                    Id = (int)reader["Id"],
                                    SchoolId = (int)reader["SchoolId"],
                                    GroupId = (int)reader["GroupId"],
                                    GroupName = (string)reader["GroupName"]
                                };

                                logger.Log("[RetrieveGroupsOfSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newGroup.Id + " - " + newGroup.GroupName, 1, 1, false);

                                GroupList.Add(newGroup);
                                rowCount++;
                            }

                            logger.Log("[RetrieveGroupsOfSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " groups.", 2, 1, false);

                            stopWatch.Stop();
                            return GroupList;
                        }
                    }

                    logger.Log("[RetrieveGroupsOfSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any groups of school " + schoolId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveGroupsOfSchool]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a group from the database
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <returns>Group</returns>
        public Group RetrieveGroup(int groupId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve group " + groupId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Group] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", groupId));

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Group newGroup = new Group
                                {
                                    Id = (int)reader["Id"],
                                    SchoolId = (int)reader["SchoolId"],
                                    GroupId = (int)reader["GroupId"],
                                    GroupName = (string)reader["GroupName"]
                                };

                                logger.Log("[RetrieveGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newGroup.Id + " - " + newGroup.GroupName, 1, 1, false);

                                stopWatch.Stop();
                                return newGroup;
                            }
                        }
                    }

                    logger.Log("[RetrieveGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find any groups with id " + groupId, 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveGroup]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve a configuration that belongs to a certain User ID from the database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Configuration</returns>
        public Configuration RetrieveConfig(int userId)
        {
            stopWatch.Restart();
            logger.Log("[RetrieveConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve config", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Configuration] WHERE [OwnerId] = @ownerId", connection);
                command.Parameters.Add(new SqlParameter("@ownerId", userId));

                Configuration newConfig = new Configuration();

                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                newConfig = new Configuration {
                                    Id = (int)reader["Id"],
                                    OwnerId = (int)reader["OwnerId"],
                                    MaxWritingTime = (int)reader["MaxWritingTime"],
                                    MaxReadingTime = (int)reader["MaxReadingTime"],
                                    FollowerGain = (int)reader["FollowerGain"],
                                    FollowerLoss = (int)reader["FollowerLoss"],
                                    FollowerPerVote = (int)reader["FollowerPerVote"],
                                    CashPerFollower = Convert.ToDouble(reader["CashPerFollower"]),
                                    CashPerVote = Convert.ToDouble(reader["CashPerVote"]),
                                    MaxPlayers = (int)reader["MaxPlayers"],
                                    PowerupsAllowed = Convert.ToBoolean(reader["PowerupsAllowed"]),
                                    PowerupsCostMult = Convert.ToDouble(reader["PowerupsCostMult"])
                                };

                                logger.Log("[RetrieveConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Got: " + newConfig.Id + " - " + newConfig.OwnerId, 1, 1, false);
                            }

                            logger.Log("[RetrieveConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Found config.", 2, 1, false);

                            stopWatch.Stop();
                            return newConfig;
                        }
                    }

                    logger.Log("[RetrieveConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find a config.", 2, 1, false);

                    // Default values
                    newConfig = new Configuration
                    {
                        Id = 0,
                        OwnerId = userId,
                        MaxWritingTime = 600,
                        MaxReadingTime = 900,
                        FollowerGain = 5,
                        FollowerLoss = 5,
                        FollowerPerVote = 1,
                        CashPerFollower = 1.00,
                        CashPerVote = 0.50,
                        MaxPlayers = 30,
                        PowerupsAllowed = true,
                        PowerupsCostMult = 1.00
                    };

                    stopWatch.Stop();
                    return newConfig;
                }
                catch (Exception exception)
                {
                    logger.Log("[RetrieveConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return newConfig;
                }
            }
        }
        #endregion

        #region UPDATE
        /// <summary>
        /// Change the name, surname and/or username of a user
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>boolean</returns>
        public bool EditUserNames(User user)
        {
            stopWatch.Restart();
            logger.Log("[EditUserNames]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to edit user " + user.Name, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [Name] = @name, [Surname] = @surname, [Username] = @username WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@name", user.Name));
                command.Parameters.Add(new SqlParameter("@surname", user.Surname));
                command.Parameters.Add(new SqlParameter("@username", user.Username));
                command.Parameters.Add(new SqlParameter("@id", user.Id));

                logger.Log("[EditUserNames]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Name: " + user.Name + ";\n" +
                                "- Surname: " + user.Surname + ";\n" +
                                "- Username: " + user.Username, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[EditUserNames]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Edited user with id " + user.Id, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[EditUserNames]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + user.Id, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[EditUserNames]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Change the password of a user
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>boolean</returns>
        public bool EditPassword(User user)
        {
            stopWatch.Start();
            logger.Log("[EditPassword]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Changing password of " + user.Username, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("Editpassword", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@pId", user.Id));
                command.Parameters.Add(new SqlParameter("@pPassword", user.Password));

                var output = new SqlParameter("@responseMessage", System.Data.SqlDbType.NVarChar);
                output.Direction = System.Data.ParameterDirection.Output;
                output.Size = 255;
                command.Parameters.Add(output);;

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        logger.Log("[EditPassword]", stopWatch.ElapsedMilliseconds.ToString() + "password changed.", 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }
                    else
                    {
                        logger.Log("[EditPassword]", stopWatch.ElapsedMilliseconds.ToString() + "password not changed:\n" + command.Parameters["@responseMessage"].Value.ToString(), 2, 1, false);

                        stopWatch.Stop();
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    logger.Log("[EditPassword]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Change the avatar of a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileName">Filename</param>
        /// <returns>boolean</returns>
        public bool EditUserAvatar(int userId, string fileName)
        {
            stopWatch.Restart();
            logger.Log("[EditUserAvatar]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to write to user " + userId + "'s avatar.", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [ImageSource] = @fileName WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@fileName", fileName));
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[EditUserAvatar]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Edited user with id " + userId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[EditUserAvatar]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[EditUserAvatar]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Change the Role ID and/or Group ID of a user
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>boolean</returns>
        public bool EditUserNumbers(Classes.User user)
        {
            stopWatch.Restart();
            logger.Log("[EditUserNumbers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to edit user " + user.Id + " numbers", 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [RoleId] = @role, [GroupId] = @group WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@role", user.RoleId));
                command.Parameters.Add(new SqlParameter("@group", user.Group));
                command.Parameters.Add(new SqlParameter("@id", user.Id));

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[EditUserNumbers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Edited user with id " + user.Id, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[EditUserNumbers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + user.Id, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[EditUserNumbers]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Change the status of a story
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <param name="status">Status</param>
        /// <returns>boolean</returns>
        public bool UpdateStoryStatus(int storyId, int status)
        {
            stopWatch.Restart();
            logger.Log("[UpdateStoryStatus]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to edit story with id " + storyId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [Story] SET [Status] = @status WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", storyId));
                command.Parameters.Add(new SqlParameter("@status", status));

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[UpdateStoryStatus]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Edited story with id " + storyId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[UpdateStoryStatus]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[UpdateStoryStatus]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Change the configuration belonging to a user
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>boolean</returns>
        public bool UpdateConfig(Configuration config)
        {
            stopWatch.Restart();
            logger.Log("[UpdateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Trying to edit config with id " + config.Id, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [Configuration] SET [MaxWritingTime] = @maxWritingTime, [MaxReadingTime] = @maxReadingTime, " +
                                         "[FollowerGain] = @followerGain, [FollowerLoss] = @followerLoss, [FollowerPerVote] = @followerPerVote, " +
                                         "[CashPerFollower] = @cashPerFollower, [CashPerVote] = @cashPerVote, [MaxPlayers] = @maxPlayers, " +
                                         "[PowerupsAllowed] = @powerupsAllowed, [PowerupsCostMult] = @powerupsCostMult WHERE [Id] = @configId", connection);

                command.Parameters.Add(new SqlParameter("@configId", config.Id));
                command.Parameters.Add(new SqlParameter("@maxWritingTime", config.MaxWritingTime));
                command.Parameters.Add(new SqlParameter("@maxReadingTime", config.MaxReadingTime));
                command.Parameters.Add(new SqlParameter("@followerGain", config.FollowerGain));
                command.Parameters.Add(new SqlParameter("@followerLoss", config.FollowerLoss));
                command.Parameters.Add(new SqlParameter("@followerPerVote", config.FollowerPerVote));
                command.Parameters.Add(new SqlParameter("@cashPerFollower", config.CashPerFollower));
                command.Parameters.Add(new SqlParameter("@cashPerVote", config.CashPerVote));
                command.Parameters.Add(new SqlParameter("@maxPlayers", config.MaxPlayers));
                command.Parameters.Add(new SqlParameter("@powerupsAllowed", config.PowerupsAllowed));
                command.Parameters.Add(new SqlParameter("@powerupsCostMult", config.PowerupsCostMult));

                logger.Log("[UpdateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + config.OwnerId + ";\n" +
                                "- Max writing time: " + config.MaxWritingTime + ";\n" +
                                "- Max reading time: " + config.MaxReadingTime + ";\n" +
                                "- Follower gain: " + config.FollowerGain + ";\n" +
                                "- Follower loss: " + config.FollowerLoss + ";\n" +
                                "- Followers per vote: " + config.FollowerPerVote + ";\n" +
                                "- Cash per follower: " + config.CashPerFollower + ";\n" +
                                "- Cash per vote: " + config.CashPerVote + ";\n" +
                                "- Max players: " + config.MaxPlayers + ";\n" +
                                "- Powerups allowed? " + config.PowerupsAllowed + ";\n" +
                                "- Powerup cost multiplier: " + config.PowerupsCostMult, 1, 1, false);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[UpdateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Edited config with id " + config.Id, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[UpdateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find config with id " + config.Id, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[UpdateConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }
        #endregion

        #region DELETE
        /// <summary>
        /// Delete a user from the database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>boolean</returns>
        public bool DeleteUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[DeleteUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleting user " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM [User] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();
                    int rows = command.ExecuteNonQuery();
                    
                    if (rows > 0)
                    {
                        logger.Log("[DeleteUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleted user with id " + userId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[DeleteUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[DeleteUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete a story from the database
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <returns>boolean</returns>
        public bool DeleteStory(int storyId)
        {
            stopWatch.Restart();
            logger.Log("[DeleteStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleting story " + storyId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM [Story] WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", storyId));

                try
                {
                    connection.Open();
                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[DeleteStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleted story with id " + storyId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[DeleteStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[DeleteStory]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete all stories that belong to a certain User ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>boolean</returns>
        public bool DeleteStoriesOfUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[DeleteStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleting story with ownerId " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM [Story] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();
                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[DeleteStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleted story with ownerId " + userId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[DeleteStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find story with ownerId " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[DeleteStoriesOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete all scores that belong to a certain User ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>boolean</returns>
        public bool DeleteScoresOfUser(int userId)
        {
            stopWatch.Restart();
            logger.Log("[DeleteScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleting score with ownerId " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM [Score] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();
                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[DeleteScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleted score with ownerId " + userId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[DeleteScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find score with ownerId " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[DeleteScoresOfUser]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete a configuration that belongs to a certain User ID
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool DeleteConfig(int userId)
        {
            stopWatch.Restart();
            logger.Log("[DeleteConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleting config of " + userId, 0, 1, false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM [Configuration] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", userId));

                try
                {
                    connection.Open();
                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        logger.Log("[DeleteConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Deleted story with id " + userId, 2, 1, false);

                        stopWatch.Stop();
                        return true;
                    }

                    logger.Log("[DeleteConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Could not find config with ownerId " + userId, 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
                catch (Exception exception)
                {
                    logger.Log("[DeleteConfig]", stopWatch.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + exception.ToString(), 2, 1, false);

                    stopWatch.Stop();
                    return false;
                }
            }
        }
        #endregion
    }
}