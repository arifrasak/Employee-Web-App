using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        public static string Sqlconnection;
        static AdminController()
        {
            Sqlconnection = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
        }
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Admin()
        {
            if (Session["username"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            string username = Session["username"] as string;
            ViewBag.Username = username;
            return View();

        }
        public ActionResult Fileupload()
        {
            if (Session["username"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            string username = Session["username"] as string;
            ViewBag.Username = username;
            return View();
        }
        [HttpPost]
        public JsonResult Fileupload(FormCollection form, HttpPostedFileBase DOCUMENT_1, HttpPostedFileBase DOCUMENT_2, HttpPostedFileBase DOCUMENT_3)
        {
            try
            {
                string Employeename = form["Employeename"];
                string Employeeemail = form["Employeeemail"];
                string Employeedesignation = form["Employeedesignation"];
                string Filename = form["fileName"];
                ///string savedFileName1 = DOCUMENT_1 != null && DOCUMENT_1.ContentLength > 0 ? DOCUMENT_1.FileName : null;
                //string savedFileName2 = DOCUMENT_2 != null && DOCUMENT_2.ContentLength > 0 ? DOCUMENT_2.FileName : null;
                // string savedFileName3 = DOCUMENT_3 != null && DOCUMENT_3.ContentLength > 0 ? DOCUMENT_3.FileName : null;

                string sanitizedEmployeeName = SanitizeFileName(Employeename);
                string employeeFolder = Path.Combine(Server.MapPath("~/Fileupload"), sanitizedEmployeeName);
                if (!Directory.Exists(employeeFolder))
                {
                    Directory.CreateDirectory(employeeFolder);
                }
                string savedFileName1 = SaveFile(DOCUMENT_1, employeeFolder);
                string savedFileName2 = SaveFile(DOCUMENT_2, employeeFolder);
                string savedFileName3 = SaveFile(DOCUMENT_3, employeeFolder);
                using (SqlConnection conn = new SqlConnection(Sqlconnection))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("Sp_InsertEmployee", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EMPLOYEE_NAME", Employeename);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_EMAIL", Employeeemail);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_DESIGNATION", Employeedesignation);
                        cmd.Parameters.AddWithValue("@DOCUMENT_1", savedFileName1);
                        cmd.Parameters.AddWithValue("@DOCUMENT_2", savedFileName2);
                        cmd.Parameters.AddWithValue("@DOCUMENT_3", savedFileName3);
                        cmd.Parameters.AddWithValue("@FILEPATH", Filename);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Files Uploaded and Data Inserted Successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "File upload or data insertion failed: " + ex.Message });
            }
        }
        private string SaveFile(HttpPostedFileBase file, string employeeFolder)
        {
            if (file == null || file.ContentLength <= 0)
                return null;
            byte[] fileBytes;
            using (var inputStream = file.InputStream)
            {
                using (var memoryStream = new MemoryStream())
                {
                    inputStream.CopyTo(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }
            }
            byte[] encryptedFileBytes = EncryptionHelper.EncryptFile(fileBytes);
            string fileName = Path.GetFileName(file.FileName);
            string savedFilePath = Path.Combine(employeeFolder, fileName);
            System.IO.File.WriteAllBytes(savedFilePath, encryptedFileBytes);
            return fileName;
        }
        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
        public static class EncryptionHelper
        {
            public static byte[] EncryptFile(byte[] fileBytes)
            {
                byte key = 0xAA;
                byte[] encryptedBytes = new byte[fileBytes.Length];

                for (int i = 0; i < fileBytes.Length; i++)
                {
                    encryptedBytes[i] = (byte)(fileBytes[i] ^ key);
                }

                return encryptedBytes;
            }

            public static byte[] DecryptFile(byte[] encryptedBytes)
            {
                return EncryptFile(encryptedBytes);
            }
        }

        public ActionResult DownloadFile(string fileName, string employeeName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(employeeName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "File name or employee name cannot be null or empty.");
            }

            string sanitizedEmployeeName = SanitizeFileName(employeeName);
            string employeeFolder = Path.Combine(Server.MapPath("~/Fileupload"), sanitizedEmployeeName);
            string filePath = Path.Combine(employeeFolder, fileName);
            System.Diagnostics.Debug.WriteLine($"File path: {filePath}");
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File not found.");
            }
            byte[] encryptedFileBytes;
            try
            {
                encryptedFileBytes = System.IO.File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error reading file: " + ex.Message);
            }

            byte[] decryptedFileBytes;
            try
            {
                decryptedFileBytes = EncryptionHelper.DecryptFile(encryptedFileBytes);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error decrypting file: " + ex.Message);
            }
            return File(decryptedFileBytes, "application/octet-stream", fileName);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Home");
        }
        //public ActionResult EditDcoumnet(int id)
        //{
        //    return View();
        //}
        [HttpPost]
        public ActionResult EditEmployee(int id)
        {
            TB_EMPLOYEE tbemp = new TB_EMPLOYEE();
            TB_EMPLOYEE_DETAILS tbempd = new TB_EMPLOYEE_DETAILS();

            using (SqlConnection conn = new SqlConnection(Sqlconnection))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("Sp_GetEmployeeById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EMPLOYEE_ID", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())

                        {
                            tbemp.EMPLOYEE_NAME = reader["EMPLOYEE_NAME"].ToString();
                            tbemp.EMPLOYEE_EMAIL = reader["EMPLOYEE_EMAIL"].ToString();
                            tbemp.EMPLOYEE_DESIGNATION = reader["EMPLOYEE_DESIGNATION"].ToString();
                            tbempd.FILENAME = reader["FILENAME"].ToString();
                            tbempd.DOCUMENT_1 = reader["DOCUMENT_1"].ToString();
                            tbempd.DOCUMENT_2 = reader["DOCUMENT_2"].ToString();
                            tbempd.DOCUMENT_3 = reader["DOCUMENT_3"].ToString();
                        };
                    }
                }
            }


            return View("EditEmployee");
        }
        [HttpPost]
        public ActionResult UpdateEmployee(int id, FormCollection form, HttpPostedFileBase DOCUMENT_1, HttpPostedFileBase DOCUMENT_2, HttpPostedFileBase DOCUMENT_3)
        {
            try
            {
                string Employeename = form["Employeename"];
                string Employeeemail = form["Employeeemail"];
                string Employeedesignation = form["Employeedesignation"];
                string Filename = form["fileName"];

                string sanitizedEmployeeName = SanitizeFileName(Employeename);
                string employeeFolder = Path.Combine(Server.MapPath("~/Fileupload"), sanitizedEmployeeName);

                if (!Directory.Exists(employeeFolder))
                {
                    Directory.CreateDirectory(employeeFolder);
                }

                // Save new files if provided
                string savedFileName1 = SaveFile(DOCUMENT_1, employeeFolder);
                string savedFileName2 = SaveFile(DOCUMENT_2, employeeFolder);
                string savedFileName3 = SaveFile(DOCUMENT_3, employeeFolder);

                using (SqlConnection conn = new SqlConnection(Sqlconnection))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("Sp_UpdateEmployee", conn)) // Assuming you have an update stored procedure
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EMPLOYEE_ID", id);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_NAME", Employeename);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_EMAIL", Employeeemail);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_DESIGNATION", Employeedesignation);

                        // Update documents only if new ones were provided
                        cmd.Parameters.AddWithValue("@DOCUMENT_1", savedFileName1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOCUMENT_2", savedFileName2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOCUMENT_3", savedFileName3 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FILEPATH", Filename);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Files Uploaded and Data Updated Successfully!";
                return RedirectToAction("Admin"); // Redirect to a relevant action/view
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                TempData["ErrorMessage"] = "File upload or data update failed: " + ex.Message;
                return RedirectToAction("Admin");
            }
        }

        public ActionResult DeleteEmployee(int id)
        {
            try
            {
                TB_EMPLOYEE tbemp = new TB_EMPLOYEE();
                TB_EMPLOYEE_DETAILS tbempd = new TB_EMPLOYEE_DETAILS();

                using (SqlConnection conn = new SqlConnection(Sqlconnection))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("Sp_DeleteEmployee", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EMPLOYEE_ID", id);
                        cmd.ExecuteNonQuery();

                    }
                }
                return View("Admin");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                ViewBag.ErrorMessage = "An error occurred while deleting the employee.";
                return View("Error");
            }
        }
    }
}

