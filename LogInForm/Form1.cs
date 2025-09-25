using LiteDB;
using System;
using System.IO;
using System.Windows.Forms;

namespace LogInForm
{
    public partial class Form1 : Form
    {
        private const string DatabasePath = "C:\\Users\\771666\\OneDrive - wqeic.ac.uk\\users.db";
        // database could be stored on the web in theory for a more distributed system
        
        public Form1()
        {
            InitializeComponent();
            InitializeDatabase();            
            UpdateDatabaseInfo();
        }

        private void InitializeDatabase()
        {
            using (var db = new LiteDatabase(DatabasePath))
            {
                var users = db.GetCollection<User>("users");
                users.EnsureIndex(u => u.Username, true);

                if (users.Count() == 0)
                {
                    users.Insert(new User { Username = "admin", Password = "password" });
                }
            }
        }
        private void UpdateDatabaseInfo()
        {
            if (File.Exists(DatabasePath))
            {
                var fileInfo = new FileInfo(DatabasePath);
                lblStatus.Text = $"Database: {DatabasePath} ({fileInfo.Length} bytes)";
            }
            else
            {
                lblStatus.Text = "Database: Not found";
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (ValidateUser(username, password))
            {
                MessageBox.Show("Login successful! Data persists between sessions.");
            }
            else
            {
                MessageBox.Show("Invalid credentials.");
            }

            UpdateDatabaseInfo();
        }

        private bool ValidateUser(string username, string password)
        {
            using (var db = new LiteDatabase(DatabasePath))
            {
                var users = db.GetCollection<User>("users");
                return users.Exists(u => u.Username == username && u.Password == password);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (RegisterUser(username, password))
            {
                MessageBox.Show("Registration successful! This user will persist after app closes.");
            }
            else
            {
                MessageBox.Show("Registration failed. Username may already exist.");
            }

            UpdateDatabaseInfo();
        }

        private bool RegisterUser(string username, string password)
        {
            try
            {
                using (var db = new LiteDatabase(DatabasePath))
                {
                    var users = db.GetCollection<User>("users");

                    if (users.Exists(u => u.Username == username))
                        return false;

                    users.Insert(new User { Username = username, Password = password });
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void btnShowUsers_Click(object sender, EventArgs e)
        {
            using (var db = new LiteDatabase(DatabasePath))
            {
                var users = db.GetCollection<User>("users");
                var allUsers = users.FindAll();

                string userList = "Current users in database:\n";
                foreach (var user in allUsers)
                {
                    userList += $"- {user.Username}\n";
                }

                MessageBox.Show(userList);
            }
        }

    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
