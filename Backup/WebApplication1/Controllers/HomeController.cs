using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public static string Sqlconnection;
        static HomeController()
        {
            Sqlconnection = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
        }
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(FormCollection form)
        {
            //Using sql connection
            using (SqlConnection conn = new SqlConnection(Sqlconnection))
            {
                TB_USER users = new TB_USER();
                string username = form["Username"].ToString();
                string password= form["Password"].ToString();

                users.USERNAME = username;
                users.PASSWORD = password;
                users.ISACTIVE = false;
                users.ROLEID = 0;
                conn.Open();
                DataSet ds = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_CheckLogin", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@USERNAME", username);
                cmd.Parameters.AddWithValue("@PASSWORD", password);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                if(ds.Tables[0].Rows.Count>0)
                {
                    // Get the first row
                    DataRow row = ds.Tables[0].Rows[0];
                    int ROLEID= Convert.ToInt32(row["ROLEID"]);
                    bool ISACTIVE= Convert.ToBoolean(row["ISACTIVE"]);
                    if(ISACTIVE)
                    {
                        // Login successful
                        Session["username"]= username;
                        Session["RoleId"] = ROLEID;
                        if(ROLEID==1)
                        {
                         return RedirectToAction("Admin", "Home");
                        }
                       else if (ROLEID == 2)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        // User is not active
                        ViewBag.error = "Your account is inactive. Please contact support";
                    }
                }
                else
                { // Invalid username or password
                    ViewBag.error = "Wrong Or Invalid Username Or Password";
                }
            }
            return View();
        }
        public ActionResult Admin()
        {
            return View();
        }
    }
}