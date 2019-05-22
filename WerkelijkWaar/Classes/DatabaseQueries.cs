using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class DatabaseQueries
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();

        // private string connectionString = ConfigurationManager.AppSettings["connectionString"];
        private string connectionString = ConfigurationManager.AppSettings["connectionString"] + "User Id='" + ConfigurationManager.AppSettings["dbUsername"] + "';" +
            "Password='" + ConfigurationManager.AppSettings["dbPassword"] + "';";

        // CRUD
        #region CREATE
        /// <summary>
        /// Maak een gebruiker aan in het systeem.
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>true or false</returns>
        public bool RegisterUser(User user)
        {
            l.WriteToLog("[RegisterUser]", "Trying to register user " + user.Name, 0);

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

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        l.WriteToLog("[RegisterUser]", "User " + user.Name + " successfully added.", 1);
                        return true;
                    }
                    else
                    {
                        l.WriteToLog("[RegisterUser]", "Failed to add user " + user.Name + ".", 1);
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RegisterUser]", ex.ToString(), 1);
                    return false;
                }
            }
        }

        /// <summary>
        /// Maak een score aan in de database.
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>true or false</returns>
        public bool CreateScore(Score score)
        {
            l.WriteToLog("[CreateScore]", "Trying to create score for user " + score.OwnerId, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Score] SELECT @ownerId, @gameType, @followers, @cash, @votes, @answers, @date", connection);

                command.Parameters.Add(new SqlParameter("@ownerId", score.OwnerId));
                command.Parameters.Add(new SqlParameter("@gameType", score.GameType));
                command.Parameters.Add(new SqlParameter("@answers", score.Answers));
                command.Parameters.Add(new SqlParameter("@date", score.Date));

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
                        l.WriteToLog("[CreateScore]", "Score for " + score.OwnerId + " successfully added.", 1);
                        return true;
                    }

                    l.WriteToLog("[CreateScore]", "Failed to add score for " + score.OwnerId + ".", 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateScore]", ex.ToString(), 1);
                    return false;
                }
            }
        }

        public bool CreateStory(Story story)
        {
            l.WriteToLog("[CreateStory]", "Trying to create story for user " + story.OwnerId, 0);

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

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[CreateStory]", "Story for " + story.OwnerId + " successfully added.", 1);
                        return true;
                    }

                    l.WriteToLog("[CreateStory]", "Failed to add story for " + story.OwnerId + ".", 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateStory]", ex.ToString(), 1);
                    return false;
                }
            }
        }
        #endregion

        #region READ
        /// <summary>
        /// Check of de gegeven combinatie bestaat in de database.
        /// </summary>
        /// <param name="username">Gebruikersnaam</param>
        /// <param name="password">Wachtwoord</param>
        /// <returns>User ID</returns>
        public int CheckLogin(string username, string password)
        {
            l.WriteToLog("[CheckLogin]", "Checking log in for user " + username, 0);

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
                        return (int)command.Parameters["@userId"].Value;
                    }
                    else
                    {
                        l.WriteToLog("[CheckLogin]", "Log in not found for user " + username, 1);
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CheckLogin]", "Something went wrong: " + ex, 1);
                    return 0;
                }
            }
        }

        /// <summary>
        /// Haal een gebruiker op uit de database inclusief alle persoonsgegevens.
        /// </summary>
        /// <param name="userId">Id</param>
        /// <returns>User</returns>
        public User RetrieveUser(int userId)
        {
            l.WriteToLog("[RetrieveUser]", "Attempting to retrieve user with id " + userId, 0);

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
                                l.WriteToLog("[RetrieveUser]", "Found user with id " + userId, 1);

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

                                return newUser;
                            }
                        }
                    }

                    l.WriteToLog("[RetrieveUser]", "Could not find user with id " + userId, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveUser]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal een lijst van gebruikers op die uit dezelfde groep komen.
        /// </summary>
        /// <param name="group">Groep</param>
        /// <returns>Lijst van Users</returns>
        public List<User> RetrieveUserListByGroup(int group)
        {
            l.WriteToLog("[RetrieveUserListByGroup]", "Attempting to retrieve users from group " + group, 0);

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

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveUserListByGroup]", "Found " + rowCount + " users from group " + group, 1);
                            return UserList;
                        }
                    }

                    l.WriteToLog("[RetrieveUserListByGroup]", "Could not find any users from group " + group, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveUserListByGroup]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal alle gebruikers op uit het systeem.
        /// </summary>
        /// <returns>Lijst van Users</returns>
        public List<User> RetrieveAllUsers()
        {
            l.WriteToLog("[RetrieveAllUsers]", "Attempting to retrieve all users", 0);

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

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveAllUsers]", "Found " + rowCount + " users.", 1);
                            return UserList;
                        }
                    }

                    l.WriteToLog("[RetrieveAllUsers]", "Could not find any users.", 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveAllUsers]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal een lijst van scores op die bij een bepaalde gebruiker horen.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Lijst van Scores</returns>
        public List<Score> RetrieveScoresOfUser(int id)
        {
            l.WriteToLog("[RetrieveScoresOfUser]", "Attempting to retrieve scores of user " + id, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Score] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", id));

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
                                    Answers = (string)reader["Answers"],
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

                                ScoreList.Add(newScore);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveScoresOfUser]", "Found " + rowCount + " scores from user " + id, 1);
                            return ScoreList;
                        }
                    }

                    l.WriteToLog("[RetrieveScoresOfUser]", "Could not find any scores of user " + id, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveScoresOfUser]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal een lijst van verhalen op die bij een bepaalde gebruiker horen.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Lijst van Stories</returns>
        public List<Story> RetrieveStoriesOfUser(int id)
        {
            l.WriteToLog("[RetrieveStoriesOfUser]", "Attempting to retrieve stories of user " + id, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story] WHERE [OwnerId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", id));

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

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveStoriesOfUser]", "Found " + rowCount + " stories from user " + id, 1);
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveStoriesOfUser]", "Could not find any stories of user " + id, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStoriesOfUser]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal alle verhalen op uit het systeem.
        /// </summary>
        /// <returns>Lijst van Verhalen</returns>
        public List<Story> RetrieveAllStories()
        {
            l.WriteToLog("[RetrieveAllStories]", "Attempting to retrieve all stories.", 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT * FROM [Story]", connection);

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

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveAllStories]", "Found " + rowCount + " stories.", 1);
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveAllStories]", "Could not find any stories", 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveAllStories]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal een verhaal op uit de database.
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <returns>Story</returns>
        public Story RetrieveStory(int storyId)
        {
            l.WriteToLog("[RetrieveStory]", "Attempting to retrieve story with id " + storyId, 0);

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
                                l.WriteToLog("[RetrieveStory]", "Found story with id " + storyId, 1);

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

                    l.WriteToLog("[RetrieveStory]", "Could not find story with id " + storyId, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStory]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }

        /// <summary>
        /// Haal alle verhalen op met status 0 (ongelezen) of 1 (gelezen, maar nog niet goedgekeurd).
        /// </summary>
        /// <returns>Lijst van Stories</returns>
        public List<Story> RetrieveStoryQueue()
        {
            l.WriteToLog("[RetrieveStoryQueue]", "Attempting to retrieve stories with status 0 and 1.", 0);

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

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveStoryQueue]", "Found " + rowCount + " stories.", 1);
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveStoryQueue]", "Could not find any stories with status 0 or 1", 1);
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStoryQueue]", "Something went wrong: " + ex, 1);
                    return null;
                }
            }
        }
        #endregion

        #region UPDATE
        /// <summary>
        /// Wijzig de gegevens van een gebruiker.
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>true or false</returns>
        public bool EditUser(User user)
        {
            l.WriteToLog("[EditUser]", "Trying to edit user " + user.Name, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [Name] = @name, [Surname] = @surname, [Username] = @username WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@name", user.Name));
                command.Parameters.Add(new SqlParameter("@surname", user.Surname));
                command.Parameters.Add(new SqlParameter("@username", user.Username));
                command.Parameters.Add(new SqlParameter("@id", user.Id));

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[EditUser]", "Edited user with id " + user.Id, 1);
                        return true;
                    }

                    l.WriteToLog("[EditUser]", "Could not find user with id " + user.Id, 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[EditUser]", ex.ToString(), 1);
                    return false;
                }
            }
        }

        /// <summary>
        /// Wijzig de avatar van een gebruiker.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="path">Bestandsnaam</param>
        /// <returns>true or false</returns>
        public bool EditUserAvatar(int id, string path)
        {
            l.WriteToLog("[EditUserAvatar]", "Trying to write to user " + id + "'s avatar.", 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [ImageSource] = @path WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@path", path));
                command.Parameters.Add(new SqlParameter("@id", id));

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[EditUserAvatar]", "Edited user with id " + id, 1);
                        return true;
                    }

                    l.WriteToLog("[EditUserAvatar]", "Could not find user with id " + id, 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[EditUserAvatar]", ex.ToString(), 1);
                    return false;
                }
            }
        }

        /// <summary>
        /// Wijzig de status van een verhaal.
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <param name="status">Status</param>
        /// <returns>true or false</returns>
        public bool UpdateStoryStatus(int storyId, int status)
        {
            l.WriteToLog("[UpdateStoryStatus]", "Trying to edit story with id " + storyId, 0);

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
                        l.WriteToLog("[UpdateStoryStatus]", "Edited story with id " + storyId, 1);
                        return true;
                    }

                    l.WriteToLog("[UpdateStoryStatus]", "Could not find story with id " + storyId, 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[UpdateStoryStatus]", ex.ToString(), 1);
                    return false;
                }
            }
        }
        #endregion

        #region DELETE
        /// <summary>
        /// Verwijder een gebruiker. (inclusief scores/verhalen)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>true or false</returns>
        public bool DeleteUser(int userId)
        {
            l.WriteToLog("[DeleteUser]", "Attempting to delete user with id " + userId, 0);

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
                        l.WriteToLog("[DeleteUser]", "Deleted user with id " + userId, 1);
                        return true;
                    }

                    l.WriteToLog("[DeleteUser]", "Could not find user with id " + userId, 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[DeleteUser]", "Something went wrong: " + ex, 1);
                    return false;
                }
            }
        }

        /// <summary>
        /// Verwijder een verhaal.
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <returns>true or false</returns>
        public bool DeleteStory(int storyId)
        {
            l.WriteToLog("[DeleteStory]", "Attempting to delete user with id " + storyId, 0);

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
                        l.WriteToLog("[DeleteStory]", "Deleted user with id " + storyId, 1);
                        return true;
                    }

                    l.WriteToLog("[DeleteStory]", "Could not find user with id " + storyId, 1);
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[DeleteStory]", "Something went wrong: " + ex, 1);
                    return false;
                }
            }
        }
        #endregion
    }
}
