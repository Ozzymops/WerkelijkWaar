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
        #endregion

        #region UPDATE
        #endregion

        #region DELETE
        #endregion
    }
}
