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

        private string connectionString = ConfigurationManager.AppSettings["connectionString"];

        // CRUD
        #region CREATE
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
                        //command = new SqlCommand("SELECT [Id] FROM [User] WHERE [Username] = @username", connection);
                        //command.Parameters.Add(new SqlParameter("@username", username));

                        //connection.Close();
                        //connection.Open();

                        //using (SqlDataReader reader = command.ExecuteReader())
                        //{
                        //    if (reader.HasRows)
                        //    {
                        //        while (reader.Read())
                        //        {
                        //            l.WriteToLog("[CheckLogin]", "Log in found for user with Id " + (int)reader["Id"], 1);
                        //            return (int)reader["Id"];
                        //        }
                        //    }
                        //    l.WriteToLog("[CheckLogin]", "Log in not found for user " + username, 1);
                        //    return 0;
                        //}

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

                                return new User
                                {
                                    Id = (int)reader["Id"],
                                    Group = (int)reader["uGroup"],
                                    Name = (string)reader["Name"],
                                    Surname = (string)reader["Surname"],
                                    Username = (string)reader["Username"],
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    LoginAttempts = Convert.ToInt32(reader["Attempts"])
                                };
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
                SqlCommand command = new SqlCommand("SELECT * FROM [User] WHERE [uGroup] = @group AND [RoleId] = 0", connection);
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
                                UserList.Add(new User
                                {
                                    Id = (int)reader["Id"],
                                    Group = (int)reader["uGroup"],
                                    Name = (string)reader["Name"],
                                    Surname = (string)reader["Surname"],
                                    Username = (string)reader["Username"],
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    LoginAttempts = Convert.ToInt32(reader["Attempts"])
                                });

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
                                    Title = (string)reader["Title"]
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
        #endregion
    }
}
