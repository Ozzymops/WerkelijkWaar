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
        public bool CheckLogin(string username, string password)
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

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@responseMessage"].Value.ToString().Contains("Success"))
                    {
                        l.WriteToLog("[CheckLogin]", "Log in found for user " + username, 1);
                        return true;
                    }
                    else
                    {
                        l.WriteToLog("[CheckLogin]", "Log in not found for user " + username, 1);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    l.WriteToLog("[CheckLogin]", "Something went wrong: " + ex, 1);
                    return false;
                }
            }
        }
        #endregion

        #region READ
        #endregion

        #region UPDATE
        #endregion

        #region DELETE
        #endregion
    }
}
