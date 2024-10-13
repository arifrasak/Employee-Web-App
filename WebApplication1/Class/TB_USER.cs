using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;


public class TB_USER
{
    public static string Sqlconnection = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
    public int USERID { get; set; }
    public string USERNAME { get; set; }
    public string PASSWORD { get; set; }
    public int ROLEID { get; set; }
    public bool ISACTIVE { get; set; }

    public class LoginResult
    {
        public int ROLEID { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

    }

    public LoginResult CheckLogin(string username, string password)
    {
        //Using sql connection
        using (SqlConnection conn = new SqlConnection(Sqlconnection))
        {
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand("Sp_CheckLogin", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@PASSWORD", password);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);
            if (ds.Tables[0].Rows.Count > 0)
            {
                // Get the first row
                DataRow row = ds.Tables[0].Rows[0];
                int roleId = Convert.ToInt32(row["ROLEID"]);
                bool ISACTIVE = Convert.ToBoolean(row["ISACTIVE"]);
                if (ISACTIVE)
                {
                    return new LoginResult { IsSuccessful = true, ROLEID = roleId };
                }
                else
                {
                    return new LoginResult { IsSuccessful = false, ErrorMessage = "Your account is inactive. Please contact support." };

                }
            }
        }
        

        return new LoginResult { IsSuccessful = false, ErrorMessage = "Wrong or invalid username or password." };

    }
}