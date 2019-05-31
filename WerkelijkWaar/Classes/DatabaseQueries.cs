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
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Stopwatch sw = new Stopwatch();

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
            sw.Restart();
            l.WriteToLog("[RegisterUser]", "Trying to register user " + user.Username, 0);
            l.DebugToLog("[RegisterUser]", sw.ElapsedMilliseconds.ToString() + "ms. Registering " + user.Name, 0);

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

                l.DebugToLog("[RegisterUser]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                                "- Role Id: " + user.RoleId + ";\n" +
                                                "- Group Id: " + user.Group + ";\n" +
                                                "- Name: " + user.Name + ";\n" +
                                                "- Surname: " + user.Surname + ";\n" +
                                                "- Username: " + user.Username, 1);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        l.WriteToLog("[RegisterUser]", "User " + user.Username + " successfully registered.", 2);
                        l.DebugToLog("[RegisterUser]", sw.ElapsedMilliseconds.ToString() + "ms. User " + user.Username + " succesfully registered.", 2);

                        sw.Stop();
                        return true;
                    }
                    else
                    {
                        l.WriteToLog("[RegisterUser]", "Failed to register user " + user.Name + ".", 2);
                        l.DebugToLog("[RegisterUser]", sw.ElapsedMilliseconds.ToString() + "ms. Failed to register user " + user.Username + ": " + command.Parameters["@responseMessage"].Value.ToString(), 2);

                        sw.Stop();
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    l.DebugToLog("[RegisterUser]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RegisterUser]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[CreateScore]", "Trying to create score for user with Id " + score.OwnerId, 0);
            l.DebugToLog("[CreateScore]", sw.ElapsedMilliseconds.ToString() + "ms. Adding score of user " + score.OwnerId, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Score] SELECT @ownerId, @gameType, @followers, @cash, @votes, @answers, @date", connection);

                command.Parameters.Add(new SqlParameter("@ownerId", score.OwnerId));
                command.Parameters.Add(new SqlParameter("@gameType", score.GameType));
                command.Parameters.Add(new SqlParameter("@answers", score.Answers));
                command.Parameters.Add(new SqlParameter("@date", score.Date));

                l.DebugToLog("[CreateScore]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + score.OwnerId + ";\n" +
                                "- Gametype: " + score.GameType + ";\n" +
                                "- Answers: " + score.Answers + ";\n" +
                                "- Followers: " + score.FollowerAmount + ";\n" +
                                "- Cash: " + score.CashAmount + ";\n" +
                                "- Votes: " + score.AttainedVotes + ";\n" +
                                "- Date: " + score.Date, 1);

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
                        l.WriteToLog("[CreateScore]", "Score for " + score.OwnerId + " successfully added.", 2);
                        l.DebugToLog("[CreateScore]", sw.ElapsedMilliseconds.ToString() + "ms. Score for " + score.OwnerId + " succesfully added.", 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[CreateScore]", "Failed to add score for " + score.OwnerId + ".", 2);
                    l.DebugToLog("[CreateScore]", sw.ElapsedMilliseconds.ToString() + "ms. Failed to add score for " + score.OwnerId + ".", 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateScore]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[CreateScore]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return false;
                }
            }
        }

        public bool CreateStory(Story story)
        {
            sw.Restart();
            l.WriteToLog("[CreateStory]", "Trying to create story for user " + story.OwnerId, 0);
            l.DebugToLog("[CreateStory]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to create story for user " + story.OwnerId, 0);

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

                l.DebugToLog("[CreateStory]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Owner Id: " + story.OwnerId + ";\n" +
                                "- Root: " + story.IsRoot + ";\n" +
                                "- Title: " + story.Title + ";\n" +
                                "- Description: " + story.Description + ";\n" +
                                "- Status: " + 2 + ";\n" +
                                "- Source: " + story.Source + ";\n" +
                                "- Date: " + story.Date, 1);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[CreateStory]", "Story for " + story.OwnerId + " successfully added.", 2);
                        l.DebugToLog("[CreateStory", sw.ElapsedMilliseconds.ToString() + "ms. Story for " + story.OwnerId + " successfully added.", 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[CreateStory]", "Failed to add story for " + story.OwnerId + ".", 2);
                    l.DebugToLog("[CreateStory]", sw.ElapsedMilliseconds.ToString() + "ms. Failed to add story for " + story.OwnerId + ".", 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateStory]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[CreateStory]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return false;
                }
            }
        }

        public bool CreateGroup(Group group)
        {
            sw.Restart();
            l.WriteToLog("[CreateGroup]", "Trying to create group with name " + group.GroupName, 0);
            l.DebugToLog("[CreateGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to create group with name " + group.GroupName, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [Group] SELECT @SchoolId, @GroupId, @GroupName", connection);

                command.Parameters.Add(new SqlParameter("@SchoolId", group.SchoolId));
                command.Parameters.Add(new SqlParameter("@GroupId", group.GroupId));
                command.Parameters.Add(new SqlParameter("@GroupName", group.GroupName));

                l.DebugToLog("[CreateGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- School Id: " + group.SchoolId + ";\n" +
                                "- Group Id: " + group.GroupId + ";\n" +
                                "- Group name: " + group.GroupName, 1);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[CreateGroup]", "Group with name " + group.GroupName + " successfully added.", 2);
                        l.DebugToLog("[CreateGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Group " + group.GroupName + " successfully added.", 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[CreateGroup]", "Failed to add group with name " + group.GroupName + ".", 2);
                    l.DebugToLog("[CreateGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Failed to add group " + group.GroupName + ".", 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateGroup]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[CreateGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return false;
                }
            }
        }

        public bool CreateSchool(School school)
        {
            sw.Restart();
            l.WriteToLog("[CreateSchool]", "Trying to create school with name " + school.SchoolName, 0);
            l.DebugToLog("[CreateSchool]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to create school " + school.SchoolName, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("INSERT INTO [School] SELECT @SchoolName", connection);

                command.Parameters.Add(new SqlParameter("@SchoolName", school.SchoolName));

                l.DebugToLog("[CreateSchool]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- School name: " + school.SchoolName, 1);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[CreateSchool]", "School with name " + school.SchoolName + " successfully added.", 2);
                        l.DebugToLog("[CreateSchool]", sw.ElapsedMilliseconds.ToString() + "ms. School " + school.SchoolName + " successfully added.", 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[CreateSchool]", "Failed to add school with name " + school.SchoolName + ".", 2);
                    l.DebugToLog("[CreateSchool]", "Failed to add school " + school.SchoolName + ".", 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CreateSchool]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[CreateSchool]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Start();
            l.WriteToLog("[CheckLogin]", "Logging in " + username + "...", 0);
            l.DebugToLog("[CheckLogin]", sw.ElapsedMilliseconds.ToString() + "ms. Checking login for " + username, 0);

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
                        l.WriteToLog("[CheckLogin]", "Log in found for user " + username, 2);
                        l.DebugToLog("[CheckLogin]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + username, 2);

                        sw.Stop();
                        return (int)command.Parameters["@userId"].Value;
                    }
                    else
                    {
                        l.WriteToLog("[CheckLogin]", "Log in not found for user " + username, 2);
                        l.DebugToLog("[CheckLogin]", sw.ElapsedMilliseconds.ToString() + "ms. Did not find " + username, 2);

                        sw.Stop();
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CheckLogin]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[CheckLogin]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveUser]", "Attempting to retrieve user with id " + userId, 0);
            l.DebugToLog("[RetrieveUser]", sw.ElapsedMilliseconds.ToString() + "ms. Retrieving user " + userId, 0);

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
                                l.WriteToLog("[RetrieveUser]", "Found user with id " + userId, 2);
                                l.DebugToLog("[RetrieveUser]", sw.ElapsedMilliseconds.ToString() + "ms. Found user " + userId, 2);

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

                                sw.Stop();
                                return newUser;
                            }
                        }
                    }

                    l.WriteToLog("[RetrieveUser]", "Could not find user with id " + userId, 2);
                    l.DebugToLog("[RetrieveUser]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find " + userId, 2);
                    
                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveUser]", "Something went wrong. Check debug.txt" + ex, 2);
                    l.DebugToLog("[RetrieveUser]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveUserListByGroup]", "Attempting to retrieve users from group " + group, 0);
            l.DebugToLog("[RetrieveUserListByGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Retrieving users from group " + group, 0);

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

                                l.DebugToLog("[RetrieveUserListByGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newUser.Id + " - " + newUser.Username, 1);

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveUserListByGroup]", "Found " + rowCount + " users from group " + group, 2);
                            l.DebugToLog("[RetrieveUserListByGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + "users", 2);

                            sw.Stop();
                            return UserList;
                        }
                    }

                    l.WriteToLog("[RetrieveUserListByGroup]", "Could not find any users from group " + group, 2);
                    l.DebugToLog("[RetrieveUserListByGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any users", 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveUserListByGroup]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveUserListByGroup]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveAllUsers]", "Attempting to retrieve all users", 0);
            l.DebugToLog("[RetrieveAllUsers]", sw.ElapsedMilliseconds.ToString() + "ms. Retrieving all users", 0);

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

                                l.DebugToLog("[RetrieveAllUsers]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newUser.Id + " - " + newUser.Username, 1);

                                UserList.Add(newUser);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveAllUsers]", "Found " + rowCount + " users.", 2);
                            l.DebugToLog("[RetrieveAllUsers]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + "users", 2);

                            sw.Stop();
                            return UserList;
                        }
                    }

                    l.WriteToLog("[RetrieveAllUsers]", "Could not find any users.", 2);
                    l.DebugToLog("[RetrieveAllUsers]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any users", 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveAllUsers]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveAllUsers]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveScoresOfUser]", "Attempting to retrieve scores of user " + id, 0);
            l.DebugToLog("[RetrieveScoresOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Retrieving scores of user " + id, 0);

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

                                l.DebugToLog("[RetrieveScoresOfUser]", "Got: " + newScore.Id, 1);

                                ScoreList.Add(newScore);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveScoresOfUser]", "Found " + rowCount + " scores from user " + id, 2);
                            l.DebugToLog("[RetrieveScoresOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " scores from user " + id, 2);

                            sw.Stop();
                            return ScoreList;
                        }
                    }

                    l.WriteToLog("[RetrieveScoresOfUser]", "Could not find any scores of user " + id, 2);
                    l.DebugToLog("[RetrieveScoresOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any scores of user " + id, 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveScoresOfUser]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveScoresOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveStoriesOfUser]", "Attempting to retrieve stories of user " + id, 0);
            l.DebugToLog("[RetrieveStoriesOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve stories of user " + id, 0);

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

                                l.DebugToLog("[RetrieveStoriesOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title, 1);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveStoriesOfUser]", "Found " + rowCount + " stories from user " + id, 2);
                            l.DebugToLog("[RetrieveStoriesOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories from user " + id, 2);

                            sw.Stop();
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveStoriesOfUser]", "Could not find any stories of user " + id, 2);
                    l.DebugToLog("[RetrieveStoriesOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any stories of user " + id, 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStoriesOfUser]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveStoriesOfUser]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveAllStories]", "Attempting to retrieve all stories.", 0);
            l.DebugToLog("[RetrieveAllStories]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve all stories.", 0);

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

                                l.DebugToLog("[RetrieveAllStories]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title, 1);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveAllStories]", "Found " + rowCount + " stories.", 2);
                            l.DebugToLog("[RetrieveAllStories]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories.", 2);

                            sw.Stop();
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveAllStories]", "Could not find any stories", 2);
                    l.DebugToLog("[RetrieveAllStories]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any stories", 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveAllStories]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveAllStories]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveStory]", "Attempting to retrieve story with id " + storyId, 0);
            l.DebugToLog("[RetrieveStory]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve story with id " + storyId, 0);

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
                                l.DebugToLog("[RetrieveStory]", sw.ElapsedMilliseconds.ToString() + "ms. Found story with id " + storyId + " - " + (string)reader["Title"], 1);

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

                    l.WriteToLog("[RetrieveStory]", "Could not find story with id " + storyId, 2);
                    l.DebugToLog("[RetrieveStory]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStory]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveStory]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return null;
                }
            }
        }

        public Score RetrieveScore(int scoreId)
        {
            sw.Restart();
            l.WriteToLog("[RetrieveScore]", "Attempting to retrieve score with id " + scoreId, 0);
            l.DebugToLog("[RetrieveScore]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve score with id " + scoreId, 0);

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
                                l.WriteToLog("[RetrieveScore]", "Found score with id " + scoreId, 1);
                                l.DebugToLog("[RetrieveScore]", sw.ElapsedMilliseconds.ToString() + "ms. Found score with id " + scoreId, 1);

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

                                return newScore;
                            }
                        }
                    }

                    l.WriteToLog("[RetrieveScore]", "Could not find story with id " + scoreId, 2);
                    l.DebugToLog("[RetrieveScore]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + scoreId, 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveScore]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveScore]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[RetrieveStoryQueue]", "Attempting to retrieve stories with status 0 and 1.", 0);
            l.DebugToLog("[RetrieveStoryQueue]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve story queue", 0);

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

                                l.DebugToLog("[RetrieveStoryQueue]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newStory.Id + " - " + newStory.Title + ", status: " + newStory.Status, 1);

                                StoryList.Add(newStory);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveStoryQueue]", "Found " + rowCount + " stories.", 2);
                            l.DebugToLog("[RetrieveStoryQueue]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " stories.", 2);

                            sw.Stop();
                            return StoryList;
                        }
                    }

                    l.WriteToLog("[RetrieveStoryQueue]", "Could not find any stories with status 0 or 1", 2);
                    l.DebugToLog("[RetrieveStoryQueue]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any stories in queue", 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveStoryQueue]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveStoryQueue]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return null;
                }
            }
        }

        public List<School> RetrieveSchools()
        {
            sw.Restart();
            l.WriteToLog("[RetrieveSchools]", "Attempting to retrieve schools...", 0);
            l.DebugToLog("[RetrieveSchools]", sw.ElapsedMilliseconds.ToString() + "ms. Attempting to retrieve schools", 0);

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

                                l.DebugToLog("[RetrieveSchools]", sw.ElapsedMilliseconds.ToString() + "ms. Got: " + newSchool.Id + " - " + newSchool.SchoolName, 1);

                                SchoolList.Add(newSchool);
                                rowCount++;
                            }

                            l.WriteToLog("[RetrieveSchools]", "Found " + rowCount + " schools.", 2);
                            l.DebugToLog("[RetrieveSchools]", sw.ElapsedMilliseconds.ToString() + "ms. Found " + rowCount + " schools.", 2);

                            sw.Stop();
                            return SchoolList;
                        }
                    }

                    l.WriteToLog("[RetrieveSchools]", "Could not find any schools.", 2);
                    l.DebugToLog("[RetrieveSchools]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find any schools.", 2);

                    sw.Stop();
                    return null;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[RetrieveSchools]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[RetrieveSchools]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
        public bool EditUserNames(User user)
        {
            sw.Restart();
            l.WriteToLog("[EditUserNames]", "Trying to edit user " + user.Name, 0);
            l.DebugToLog("[EditUserNames]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to edit user " + user.Name, 0);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                command = new SqlCommand("UPDATE [User] SET [Name] = @name, [Surname] = @surname, [Username] = @username WHERE [Id] = @id", connection);
                command.Parameters.Add(new SqlParameter("@name", user.Name));
                command.Parameters.Add(new SqlParameter("@surname", user.Surname));
                command.Parameters.Add(new SqlParameter("@username", user.Username));
                command.Parameters.Add(new SqlParameter("@id", user.Id));

                l.DebugToLog("[EditUserNames]", sw.ElapsedMilliseconds.ToString() + "ms. Values are: " +
                                "- Name: " + user.Name + ";\n" +
                                "- Surname: " + user.Surname + ";\n" +
                                "- Username: " + user.Username, 1);

                try
                {
                    connection.Open();

                    int rows = command.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        l.WriteToLog("[EditUserNames]", "Edited user with id " + user.Id, 2);
                        l.DebugToLog("[EditUserNames]", sw.ElapsedMilliseconds.ToString() + "ms. Edited user with id " + user.Id, 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[EditUserNames]", "Could not find user with id " + user.Id, 2);
                    l.DebugToLog("[EditUserNames]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + user.Id, 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[EditUserNames]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[EditUserNames]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[EditUserAvatar]", "Trying to write to user " + id + "'s avatar.", 0);
            l.DebugToLog("[EditUserAvatar]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to write to user " + id + "'s avatar.", 0);

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
                        l.WriteToLog("[EditUserAvatar]", "Edited user with id " + id, 2);
                        l.DebugToLog("[EditUserAvatar]", sw.ElapsedMilliseconds.ToString() + "ms. Edited user with id " + id, 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[EditUserAvatar]", "Could not find user with id " + id, 2);
                    l.DebugToLog("[EditUserAvatar]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + id, 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[EditUserAvatar]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[EditUserAvatar]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[UpdateStoryStatus]", "Trying to edit story with id " + storyId, 0);
            l.DebugToLog("[UpdateStoryStatus]", sw.ElapsedMilliseconds.ToString() + "ms. Trying to edit story with id " + storyId, 0);

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
                        l.WriteToLog("[UpdateStoryStatus]", "Edited story with id " + storyId, 2);
                        l.DebugToLog("[UpdateStoryStatus]", sw.ElapsedMilliseconds.ToString() + "ms. Edited story with id " + storyId, 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[UpdateStoryStatus]", "Could not find story with id " + storyId, 2);
                    l.DebugToLog("[UpdateStoryStatus]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[UpdateStoryStatus]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[UpdateStoryStatus]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[DeleteUser]", "Attempting to delete user with id " + userId, 0);
            l.DebugToLog("[DeleteUser]", sw.ElapsedMilliseconds.ToString() + "ms. Deleting user " + userId, 0);

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
                        l.WriteToLog("[DeleteUser]", "Deleted user with id " + userId, 2);
                        l.DebugToLog("[DeleteUser]", sw.ElapsedMilliseconds.ToString() + "ms. Deleted user with id " + userId, 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[DeleteUser]", "Could not find user with id " + userId, 2);
                    l.DebugToLog("[DeleteUser]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find user with id " + userId, 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[DeleteUser]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[DeleteUser]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
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
            sw.Restart();
            l.WriteToLog("[DeleteStory]", "Attempting to delete story with id " + storyId, 0);
            l.DebugToLog("[DeleteStory]", sw.ElapsedMilliseconds.ToString() + "ms. Deleting story " + storyId, 0);

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
                        l.WriteToLog("[DeleteStory]", "Deleted user with id " + storyId, 2);
                        l.DebugToLog("[DeleteStory]", sw.ElapsedMilliseconds.ToString() + "ms. Deleted story with id " + storyId, 2);

                        sw.Stop();
                        return true;
                    }

                    l.WriteToLog("[DeleteStory]", "Could not find story with id " + storyId, 2);
                    l.DebugToLog("[DeleteStory]", sw.ElapsedMilliseconds.ToString() + "ms. Could not find story with id " + storyId, 2);

                    sw.Stop();
                    return false;
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[DeleteStory]", "Something went wrong. Check debug.txt", 2);
                    l.DebugToLog("[DeleteStory]", sw.ElapsedMilliseconds.ToString() + "ms. Exception:\n" + ex.ToString(), 2);

                    sw.Stop();
                    return false;
                }
            }
        }
        #endregion
    }
}
