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
                string password = form["Password"].ToString();
                var LoginResult = users.CheckLogin(username, password);
                users.USERNAME = username;
                users.PASSWORD = password;
                users.ISACTIVE = false;
                users.ROLEID=0;
                if (LoginResult.IsSuccessful)
                {
                    // Login successful
                    Session["username"] = username;
                    Session["RoleId"] = LoginResult.ROLEID;
                    if (LoginResult.ROLEID == 1)
                    {
                        return RedirectToAction("Admin", "Admin");
                    }
                    else if (LoginResult.ROLEID == 2)
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

                    ViewBag.error = "Check username and password";

                }

                return View();
            }

        }
    }
}